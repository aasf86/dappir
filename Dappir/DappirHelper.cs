using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Data;
using Dapper;
using System.Runtime.Serialization;

namespace Dappir
{
    public interface IModel { }

    public static class DappirHelperExtensions
    {
        #region Sql Command from model

        const string SELECT_ONE_string = " SELECT * FROM {0} WHERE {1} = @{2} ";
        const string SELECT_ALL_string = " SELECT * FROM {0} ";
        const string INSERT_string = " INSERT INTO {0} ({1}) VALUES ({2}); SELECT SCOPE_IDENTITY() ";
        const string UPDATE_string = " UPDATE {0} SET {1} WHERE {2} = @{3} ";
        const string DELETE_ONE_string = " DELETE FROM {0} WHERE {1} = @{2} ";
        const string DELETE_ALL_string = " DELETE FROM {0} ";

        private static List<PropertyInfo> GetColumnWithOutKey(this Type type)
        {
            return type.GetProperties().Where(x => x.Name.ToUpper() != "ID" && !x.GetSetMethod().IsVirtual && !x.GetCustomAttributes(true).ToList().Exists(z => { return z is ColumnAttribute && (z as ColumnAttribute).IsPrimaryKey; })).ToList();
        }

        private static string GetNameTable(Type typeModel)
        {
            var attrTable = typeModel.GetCustomAttributes(true).SingleOrDefault(x => x is TableAttribute) as TableAttribute;

            if (attrTable != null)
            {
                return attrTable.Name;
            }
            return typeModel.Name;
        }

        private static string GetNamePrimaryKey(this IModel model)
        {
            var propertyPrimaryKey = model.GetType().GetProperties().Where(x => x.GetCustomAttributes(true).ToList().Exists(z => { return z is ColumnAttribute && (z as ColumnAttribute).IsPrimaryKey; })).FirstOrDefault();
            if (propertyPrimaryKey == null)
                throw new InvalidOperationException("Primary key dont defined => [Column(IsPrimaryKey = true)]");
            return propertyPrimaryKey.Name;
        }

        private static int GetValuePrimaryKey(this IModel model)
        {
            var propertyPrimaryKey = model.GetType().GetProperties().Where(x => x.GetCustomAttributes(true).ToList().Exists(z => { return z is ColumnAttribute && (z as ColumnAttribute).IsPrimaryKey; })).FirstOrDefault();
            if (propertyPrimaryKey == null)
                throw new InvalidOperationException("Primary key dont defined => [Column(IsPrimaryKey = true)]");
            return (int)propertyPrimaryKey.GetValue(model, null);
        }

        private static void SetValue(this IModel model, string propertyName, object value)
        {
            var property = model.GetType().GetProperties().Where(x => x.Name == propertyName).FirstOrDefault();
            if (property == null) return;
            property.SetValue(model, value, null);
        }

        public static string ToSqlForSelectOne<TModel>() where TModel : IModel
        {
            var type = typeof(TModel);
            var tabela = GetNameTable(type);
            var keyName = Activator.CreateInstance<TModel>().GetNamePrimaryKey();
            var sql = string.Format(SELECT_ONE_string, tabela, keyName, keyName);
            return sql;
        }

        public static string ToSqlForSelectAll<TModel>() where TModel : IModel
        {
            var type = typeof(TModel);
            var tabela = GetNameTable(type);
            var sql = string.Format(SELECT_ALL_string, tabela);
            return sql;
        }

        public static string ToSqlForInsert(this IModel model)
        {
            model.GetNamePrimaryKey();
            if (model == null) return null;
            var type = model.GetType();
            var attrTable = type.GetCustomAttributes(true).SingleOrDefault(x => x is TableAttribute) as TableAttribute;
            var properties = type.GetColumnWithOutKey();

            properties = properties.Where(x =>
            {
                var attrs = x.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), false).ToArray();
                return attrs.Length <= 0;
            }).ToList();

            var tabela = GetNameTable(type);
            var campos = string.Join(", ", properties.Select(x => x.Name).ToArray());
            var parametros = string.Join(", ", properties.Select(x => "@" + x.Name).ToArray());
            var sql = string.Format(INSERT_string, tabela, campos, parametros);
            return sql;
        }

        public static string ToSqlForUpdate(this IModel model)
        {
            model.GetNamePrimaryKey();
            if (model == null) return null;
            var type = model.GetType();
            var properties = type.GetColumnWithOutKey();
            var tabela = GetNameTable(type);
            var campos = string.Join(", ", properties.Select(x => x.Name + " = @" + x.Name).ToArray());
            var keyName = model.GetNamePrimaryKey();
            var sql = string.Format(UPDATE_string, tabela, campos, keyName, keyName);
            return sql;
        }

        public static string ToSqlForDelete(this IModel model)
        {
            model.GetNamePrimaryKey();
            if (model == null) return null;
            var type = model.GetType();
            var tabela = GetNameTable(type);
            var keyName = model.GetNamePrimaryKey();
            var sql = string.Format(DELETE_ONE_string, tabela, keyName, keyName);
            return sql;
        }

        public static string ToSqlForDeleteAll<TModel>() where TModel : IModel
        {
            var type = typeof(TModel);
            var tabela = GetNameTable(type);
            var sql = string.Format(DELETE_ALL_string, tabela);
            return sql;
        }

        public static void SetValuePrimaryKey(this IModel model, int value)
        {
            var propertyPrimaryKey = model.GetType().GetProperties().Where(x => x.GetCustomAttributes(true).ToList().Exists(z => { return z is ColumnAttribute && (z as ColumnAttribute).IsPrimaryKey; })).FirstOrDefault();
            if (propertyPrimaryKey == null) return;
            propertyPrimaryKey.SetValue(model, value, null);
        }

        #region Crud Dapper

        public static void Insert<TModel>(this IDbTransaction transaction, TModel entity) where TModel : IModel
        {
            var sql = entity.ToSqlForInsert();
            entity.SetValuePrimaryKey(transaction.Connection.Query<int>(sql, entity, transaction: transaction).Single());
        }

        public static void InsertAll<TModel>(this IDbTransaction transaction, IEnumerable<TModel> listEntity) where TModel : IModel
        {
            foreach (var item in listEntity)
            {
                transaction.Insert(item);
            }
        }

        public static IEnumerable<TModel> Select<TModel>(this IDbTransaction transaction, object filterDynamic) where TModel : IModel
        {
            var sql = ToSqlForSelectAll<TModel>();
            var sqlFilter = "";
            filterDynamic
                .GetType()
                .GetProperties()
                .ToList()
                .ForEach(x =>
                {
                    sqlFilter += x.Name + " = @" + x.Name;
                });

            if (!string.IsNullOrEmpty(sqlFilter))
                sqlFilter = " where " + sqlFilter;

            return transaction.Connection.Query<TModel>(sql + sqlFilter, filterDynamic, transaction: transaction);
        }

        public static TModel Select<TModel>(this IDbTransaction transaction, int key) where TModel : IModel
        {
            var sql = ToSqlForSelectOne<TModel>();
            var entity = Activator.CreateInstance<TModel>();
            entity.SetValuePrimaryKey(key);
            return transaction.Connection.Query<TModel>(sql, entity, transaction: transaction).SingleOrDefault();
        }

        public static void Update<TModel>(this IDbTransaction transaction, TModel entity) where TModel : IModel
        {
            var sql = entity.ToSqlForUpdate();
            transaction.Connection.Execute(sql, entity, transaction: transaction);
        }

        public static void UpdateAll<TModel>(this IDbTransaction transaction, IEnumerable<TModel> listEntity) where TModel : IModel
        {
            foreach (var item in listEntity)
            {
                transaction.Update(item);
            }
        }

        public static void Delete<TModel>(this IDbTransaction transaction, int key) where TModel : IModel
        {
            var entity = Activator.CreateInstance<TModel>();
            var sql = entity.ToSqlForDelete();
            entity.SetValuePrimaryKey(key);
            transaction.Delete(entity);
        }

        public static void Delete<TModel>(this IDbTransaction transaction, TModel entity) where TModel : IModel
        {
            var sql = entity.ToSqlForDelete();
            transaction.Connection.Execute(sql, entity, transaction: transaction);
        }

        public static void DeleteAll<TModel>(this IDbTransaction transaction, IEnumerable<TModel> listEntity) where TModel : IModel
        {
            foreach (var item in listEntity)
            {
                transaction.Delete(item);
            }
        }

        public static IEnumerable<TModel> GetAll<TModel>(this IDbTransaction transaction) where TModel : IModel
        {
            var sql = ToSqlForSelectAll<TModel>();
            return transaction.Connection.Query<TModel>(sql, null, transaction: transaction);
        }

        public static void InsertOnCascade<TModel>(this IDbTransaction transaction, TModel entity) where TModel : IModel
        {
            transaction.Insert(entity);

            var listProperties = entity.GetType().GetProperties().ToList().Where(x => x.GetSetMethod().IsVirtual && x.GetCustomAttributes(true).ToList().Where(z => z is AssociationAttribute).ToList().Count > 0).ToList();

            foreach (var itemProperty in listProperties)
            {
                var itemValueProperty = itemProperty.GetValue(entity, null);

                if (itemValueProperty is IModel)
                {
                    var itemModel = itemValueProperty as IModel;
                    var primaryKeyParent = entity.GetNamePrimaryKey();
                    itemModel.SetValue(primaryKeyParent, entity.GetValuePrimaryKey());
                    transaction.InsertOnCascade(itemModel);
                    continue;
                }

                if (itemValueProperty is IEnumerable<IModel>)
                {
                    var itensModel = itemValueProperty as IEnumerable<IModel>;

                    foreach (var item in itensModel)
                    {
                        var itemModel = item as IModel;
                        var primaryKeyParent = entity.GetNamePrimaryKey();
                        itemModel.SetValue(primaryKeyParent, entity.GetValuePrimaryKey());
                        transaction.InsertOnCascade(itemModel);
                    }
                }
            }
        }

        public static void UpdateOnCascade<TModel>(this IDbTransaction transaction, TModel entity) where TModel : IModel
        {
            transaction.Update(entity);

            var listProperties = entity.GetType().GetProperties().ToList().Where(x => x.GetSetMethod().IsVirtual && x.GetCustomAttributes(true).ToList().Where(z => z is AssociationAttribute).ToList().Count > 0).ToList();

            foreach (var itemProperty in listProperties)
            {
                var itemValueProperty = itemProperty.GetValue(entity, null);

                if (itemValueProperty is IModel)
                {
                    var itemModel = itemValueProperty as IModel;
                    var primaryKeyParent = entity.GetNamePrimaryKey();
                    itemModel.SetValue(primaryKeyParent, entity.GetValuePrimaryKey());

                    if (itemModel.GetValuePrimaryKey() == 0)
                        transaction.InsertOnCascade(itemModel);
                    else
                        transaction.UpdateOnCascade(itemModel);
                    continue;
                }

                if (itemValueProperty is IEnumerable<IModel>)
                {
                    var itensModel = itemValueProperty as IEnumerable<IModel>;
                    var idsDontRemove = "0,";
                    IModel modeItemList = null;

                    foreach (var item in itensModel)
                    {
                        var itemModel = item as IModel;
                        var primaryKeyParent = entity.GetNamePrimaryKey();
                        itemModel.SetValue(primaryKeyParent, entity.GetValuePrimaryKey());
                        if (itemModel.GetValuePrimaryKey() == 0)
                            transaction.InsertOnCascade(itemModel);
                        else
                            transaction.UpdateOnCascade(itemModel);

                        idsDontRemove += item.GetValuePrimaryKey() + ",";
                        modeItemList = item;
                    }

                    var sql = string.Format(DELETE_ALL_string, GetNameTable(modeItemList.GetType()))
                        + " where "
                        + entity.GetNamePrimaryKey()
                        + " = "
                        + entity.GetValuePrimaryKey()
                        + " and  "
                        + modeItemList.GetNamePrimaryKey()
                        + " not in (" + idsDontRemove.TrimEnd(',') + ")";

                    transaction.Connection.Execute(sql, null, transaction: transaction);
                }
            }
        }

        public static TModel SelectOnCascade<TModel>(this IDbTransaction transaction, int key) where TModel : IModel
        {
            var entity = transaction.Select<TModel>(key);
            return transaction.SelectOnCascade(entity);
        }

        private static TModel SelectOnCascade<TModel>(this IDbTransaction transaction, TModel entity) where TModel : IModel
        {
            var listProperties = entity.GetType().GetProperties().ToList().Where(x => x.GetSetMethod().IsVirtual && x.GetCustomAttributes(true).ToList().Where(z => z is AssociationAttribute).ToList().Count > 0).ToList();

            foreach (var itemProperty in listProperties)
            {
                var itemValueProperty = itemProperty.GetValue(entity, null) ?? Activator.CreateInstance(itemProperty.PropertyType);

                if (itemValueProperty is IModel)
                {
                    var itemModel = itemValueProperty as IModel;
                    var primaryKeyParent = entity.GetNamePrimaryKey();
                    var sql = string.Format(SELECT_ONE_string, GetNameTable(itemModel.GetType()), primaryKeyParent, primaryKeyParent);
                    itemModel = transaction.Connection.Query(itemModel.GetType(), sql, entity, transaction: transaction, buffered: true, commandTimeout: null, commandType: null).SingleOrDefault() as IModel;
                    if (itemModel != null) entity.SetValue(itemProperty.Name, transaction.SelectOnCascade(itemModel));
                    continue;
                }

                if (itemValueProperty is IEnumerable<IModel>)
                {
                    var itensModel = itemValueProperty as IEnumerable<IModel>;
                    var type = itensModel.GetType().GetGenericArguments()[0];
                    var itemModel = Activator.CreateInstance(type) as IModel;
                    var primaryKeyParent = entity.GetNamePrimaryKey();
                    var sql = string.Format(SELECT_ONE_string, GetNameTable(itemModel.GetType()), primaryKeyParent, primaryKeyParent);
                    var list = transaction.Connection.Query(itemModel.GetType(), sql, entity, transaction: transaction, buffered: true, commandTimeout: null, commandType: null);

                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            var itemValueDb = item as IModel;
                            transaction.SelectOnCascade(itemValueDb);
                            itemValueProperty.GetType().InvokeMember("Add", BindingFlags.InvokeMethod, null, itemValueProperty, new object[] { itemValueDb });
                        }
                    }
                }
            }

            return entity;
        }

        public static void DeleteOnCascade<TModel>(this IDbTransaction transaction, int key) where TModel : IModel
        {
            var entity = transaction.SelectOnCascade<TModel>(key);
            transaction.DeleteOnCascade(entity);
            transaction.Delete(entity);
        }

        private static void DeleteOnCascade<TModel>(this IDbTransaction transaction, TModel entity) where TModel : IModel
        {
            var listProperties = entity.GetType().GetProperties().ToList().Where(x => x.GetSetMethod().IsVirtual && x.GetCustomAttributes(true).ToList().Where(z => z is AssociationAttribute).ToList().Count > 0).ToList();

            if (listProperties.Count == 0)
            {
                transaction.Delete(entity);
            }
            else
            {
                foreach (var itemProperty in listProperties)
                {
                    var itemValueProperty = itemProperty.GetValue(entity, null) ?? Activator.CreateInstance(itemProperty.PropertyType);

                    if (itemValueProperty is IModel)
                    {
                        var itemModel = itemValueProperty as IModel;
                        transaction.DeleteOnCascade(itemModel);
                        continue;
                    }

                    if (itemValueProperty is IEnumerable<IModel>)
                    {
                        var itensModel = itemValueProperty as IEnumerable<IModel>;

                        foreach (var item in itensModel)
                        {
                            transaction.DeleteOnCascade(item);
                        }
                    }
                }
            }
        }

        #endregion

        #endregion
    }
}