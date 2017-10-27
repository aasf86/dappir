using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Dappir;
using System.Data.Linq.Mapping;
using System.Collections.Generic;
using System.Linq;

namespace Dappir.Test
{
    #region poco

    public class Pedido : IModel
    {
        public Pedido()
        {
            Entrega = new Endereco();
            ItensPedido = new List<PedidoItem>();
        }

        [Column(IsPrimaryKey = true)]
        public int PedidoId { get; set; }
        public string Descricao { get; set; }

        [Association]
        public virtual Endereco Entrega { get; set; }

        [Association]
        public virtual List<PedidoItem> ItensPedido { get; set; }
    }

    public class PedidoItem : IModel
    {
        [Column(IsPrimaryKey = true)]
        public int PedidoItemId { get; set; }
        public int PedidoId { get; set; }
        public string Descricao { get; set; }
    }

    public class Endereco : IModel
    {
        [Column(IsPrimaryKey = true)]
        public int EnderecoId { get; set; }
        public int PedidoId { get; set; }
        public string Localizacao { get; set; }
    }

    #endregion

    [TestClass]
    public class DappirTestCrud
    {
        [TestMethod]
        public void Insert_on_cascade_entity()
        {
            var pedido = new Pedido();

            pedido.Descricao = "Pedido de pizza grande com refrigerante";

            pedido.Entrega.Localizacao = "minha casa";

            pedido.ItensPedido.Add(new PedidoItem { Descricao="coca cola" });
            pedido.ItensPedido.Add(new PedidoItem { Descricao = "pizza grande tamanho familia" });

            CommandDB((trans) => 
            {
                trans.InsertOnCascade(pedido);
            });

            Assert.IsTrue(pedido.PedidoId > 0 && pedido.Entrega.EnderecoId > 0 && pedido.ItensPedido.Sum(x=>x.PedidoItemId) > 0);
        }

        [TestMethod]
        public void Select_entity()
        {
            Pedido pedido = null;

            CommandDB((trans) => 
            {
                pedido = trans.Select<Pedido>(1);
            });

            Assert.IsNotNull(pedido);
        }

        [TestMethod]
        public void Update_entity()
        {
            Pedido pedido = null;
            Pedido pedidoEditadoNoBanco = null;
            var guid = Guid.NewGuid().ToString();

            CommandDB((trans) =>
            {
                pedido = trans.Select<Pedido>(1);
                pedido.Descricao += guid;

                trans.Update(pedido);

                pedidoEditadoNoBanco = trans.Select<Pedido>(1);

            });          

            Assert.IsTrue(pedidoEditadoNoBanco.Descricao.Contains(guid));
        }        

        public void CommandDB(Action<IDbTransaction> cmd)
        {
            using (var connection = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=D:\School\dappir\Dappir.Test\Dappir.Test.Database.mdf;Integrated Security=True;Connect Timeout=30"))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        cmd(transaction);
                        transaction.Commit();
                    }
                    catch (Exception exc)
                    {
                        transaction.Rollback();
                        transaction.Dispose();

                        connection.Close();
                        connection.Dispose();

                        throw exc;
                    }
                }
            }
        }
    }
}
