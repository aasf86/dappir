Dappir - more extensions for dapper
===========================================

`=>`This is a small tribute to our developer department DAPI; Why the name Dappir?: Dapper + Dapi = Dappir. [Departamento de Aprimoramento da Primeira Instância - DAPI](http://wikicti.tjmt.jus.br/index.php?title=Departamento_de_Aprimoramento_da_Primeira_Inst%C3%A2ncia_-_DAPI)

Features
--------
Dappir contains a number of helper methods for inserting, getting,
updating and deleting records and on cascade too \o/.

You remember of Contrib, ow yeah, its true, but is not, haha!!!

This is for SQL SERVER still, but we want them for all data bases. do you want help us, do fork now.

The full list of extension methods in Dappir on 'IDbTransaction' right now are:

```csharp

//we are using this litle interface for facibily, all methods have this interface like constraint
public interface IModel { }

//now we have this methods

//inserts
void Insert<TModel>(TModel entity);
void InsertAll<TModel>(IEnumerable<TModel> listEntity);
void InsertOnCascade<TModel>(TModel entity);

//selects
IEnumerable<TModel> SelectAll<TModel>();
IEnumerable<TModel> Select<TModel>(object filterDynamic);
TModel Select<TModel>(int key);
TModel SelectOnCascade<TModel>(int key);

//updates
void Update<TModel>(TModel entity);
void UpdateAll<TModel>(IEnumerable<TModel> listEntity);
void UpdateOnCascade<TModel>(TModel entity);

//deletes
void Delete<TModel>(int key);
void Delete<TModel>(TModel entity);
void DeleteAll<TModel>(IEnumerable<TModel> listEntity);
void DeleteOnCascade<TModel>(int key);

```

For these extensions to work, the entity in question _MUST_ have a
key property decorate with [Column(IsPrimaryKey = true)].

```csharp
public class Car
{
    [Column(IsPrimaryKey = true)]
    public int CarId { get; set; }
    public string Name { get; set; }
}
```

For your entity working with cascade, you must decorate yours property on `[Association]`.

```csharp
public class Car
{
    [Column(IsPrimaryKey = true)]
    public int CarId { get; set; }
    public string Name { get; set; }
    
    [Association]
    public Carmaker Maker { get; set; }

    [Association]
    public List<Dealership> Dealerships { get; set; }
}

public class Carmaker
{
    [Column(IsPrimaryKey = true)]
    public int CarmakerId { get; set; }
    public int CarId { get; set; }
    public string Name { get; set; }
}

public class Dealership
{
    [Column(IsPrimaryKey = true)]
    public int DealershipsId { get; set; }
    public int CarId { get; set; }
    public string Name { get; set; }
}

```

`CarId` Look this is the relationship between entities.

`Select` methods
-------

Get one specific entity based on id

```csharp
var car = transaction.Select<Car>(1);

var carmaker = transaction.Select<Carmaker>(1);

var listDealerships = transaction.Select<Dealership>(new { CarId = 1 });
```

or a list of all entities in the table.

```csharp
var listDealerships = transaction.SelectAll<DealershipCar>();
```

or still you can select on cascade, look that:

```csharp
var car = transaction.SelectOnCascade<Car>(1);

var carmaker = car.Maker;

var listDealerships = car.Dealerships;
```

`\o/ its amazing` Yeah, cascade is very nice.

`Insert` methods
-------

Insert one entity

```csharp
var car = new Car { Name = "520" };
car.Maker = new Carmaker { "Volvo" };
car.Dealerships = new List<Dealership> 
{ 
    new Dealership { Name = "Veronica Vehicles" }, 
    new Dealership { Name = "Heavy Loader Trucks" } 
};

transaction.Insert(car);

car.Maker.CarId = car.CarId;

transaction.Insert(car.Maker);

car.Dealerships.ForEach(x =>
{
    x.CarId = car.CarId;
    transaction.Insert(x);
})
```
or a list of entities.

```csharp
var car = new Car { Name = "520" };
car.Maker = new Carmaker { "Volvo" };
car.Dealerships = new List<Dealership> 
{ 
    new Dealership { Name = "Veronica Vehicles" }, 
    new Dealership { Name = "Heavy Loader Trucks" } 
};

transaction.Insert(car);

car.Maker.CarId = car.CarId;

transaction.Insert(car.Maker);

car.Dealerships.ForEach(x => x.CarId = car.CarId);

transaction.InsertAll(car.Dealerships);
```

or insert with cascade, more easy:

```csharp
var car = new Car { Name = "520" };
car.Maker = new Carmaker { "Volvo" };
car.Dealerships = new List<Dealership> 
{ 
    new Dealership { Name = "Veronica Vehicles" }, 
    new Dealership { Name = "Heavy Loader Trucks" } 
};

transaction.InsertOnCascade(car);
```


`Update` methods
-------
Update one specific entity

```csharp
transaction.Update(new Car() { CarId = 1, Name = "Saab" });
```

or update a list of entities.

```csharp
transaction.UpdateAll(cars);
```

and ofcourse, cascade.

```csharp
transaction.UpdateOnCascade(car);
```

`Delete` methods
-------
Delete an entity by the specified `[Column(IsPrimaryKey = true)]` property

```csharp
transaction.Delete(new Car() { CarId = 1 });
```

```csharp
transaction.Delete<Car>(1);
```

a list of entities

```csharp
transaction.DeleteAll(cars);
```

and our good and friend old cascade

`pay attention` to the external relations with the car class on use of cascade.

```csharp
transaction.DeleteOnCascade<Car>(1);
```

Special Attributes
----------
Dappir makes use of some optional attributes:

* `[Table("Tablename")]` - use another table name instead of the name of the class

    ```csharp
    [Table ("emps")]
    public class Employee
    {
        [Column(IsPrimaryKey = true)]
        public int EmployeeId { get; set; }
        public string Name { get; set; }
    }
    ```
Limitations and caveats
-------
