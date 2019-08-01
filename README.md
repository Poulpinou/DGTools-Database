# DGTools Database

A Database system for Unity

## Unity already have a database system... so... Why?

Unity's asset database is great when all you resources are created from editor, but it has its limits when your game creates resources in Runtime. 
This Database system works as SQL database (with tables and fields...etc) and can trade datas with a web server and can easily handle resources created at runtime!
Furthermore, it is really easy to use, more explanations in the How To Use? section.

## Installing

To install this package you'll need DGTools-Core, to install it you just have to add this to your Project/Packages/manifest.json :

```
{
	"dependencies": {
		"com.dgtools.core": "https://github.com/Poulpinou/DGTools-Core.git",
	}
}
```

Then open "DGTools/Package Importer" window in the Unity Editor and click on "Install" next to "com.dgtools.database".

## How to use?

### 1 - Create and edit your database settings

Open the <strong>DGTools/Manage Database</strong> window and press <strong>Create Settings</strong>.
This will create some database settings in your Resources/Database folder. You can now edit thos settings from the window. Choose your database folder path by clicking on Browse. This will be the path where you database datas will be stored.
Now, you just have to click <strong>Create database</strong> and it will create for you all the files and folders required by the database.

### 2 - Prepare your Items

To be stored in the Database, items should implement IDatabasable interface and have DatabaseFiled attributes on fields you want to save. Easy, isn't it? Let's see an example :

```
using DGTools.Database;

public class MyItem : IDatabasable
{
    public int ID { get; set; }                                 //Implement the ID like this

    [DatabaseField] public string willBeStored;                 //This string will be saved

    public string wontBeStored;                                 //This one won't

    [DatabaseField] private int justANumberToStore;             //This field won't be saved because it's private

    [DatabaseField] public Vector2 position { get; set; }       // You can save properties to if they have a get and a set
}
```

### 3 - Create your Schema

A schema?
Database will need a Schema to know which table and field should be used and how. Furthermore, it will help you with versioning : Each version of your application has its own Schema.
The database will update to fetch current version schema and make sure that your users have an up to date database.

To create you Schema, open the <strong>DGTools/Manage Database</strong> window and click on <strong>Schema</strong>. From this window, all you have to do is to drop the script of the object you want to add to the schema, configure the fields and click save (or enable auto-saving)

### 4 - Use the Database

To use the database, the most common way is to use requests. A lot of requests are available and you can create your own by extending Resquest or its subclasses.
Let's see an example : 

```
using DGTools.Database;

private void Start()
{
    Database.Load(OnDatabaseLoaded);                                                                        //Load the database and choose the action to perform when it's done
}

void OnDatabaseLoaded() {
    MyItem item = new MyItem() {
        willBeStored = "Test",
        position = new Vector3(0, 42, 3.1416)
    };

    item.Save();                                                                                             //Save the item to the Database

    GetOneRequest<MyItem> request = new GetOneRequest<MyItem>(d => (int)d["willBeStored"] == "Test");        //Create a GetOneRequest, this request needs a filter, we want a MyItem where item.willBeSotred == "Test"
    request.Execute();																						 //Execute the request
    item = request.result;																					 //Get the result from the request

    GetManyRequest<MyItem> requestAsync = new GetManyRequest<MyItem>(d => (int)d["willBeStored"] == "Test"); //GetManyRequest can be executed asynchronously
    getMany.ExecuteAsync(OnResult);																			 //Execute the request and give it the action to do 
}

void OnResult(IRequestAsyncable request) {                                                                   //This action will be called when requestAsync is done
    GetManyRequest<MyItem> requestAsync = (GetManyRequest<MyItem>)request;                                   //Cast the request (Todo: auto cast)

    foreach (MyItem item in requestAsync.results) {
        Debug.Log(item.position.y);
    }   
}

```

See [Documentation API](https://poulpinou.github.io/DGTools-Database/annotated.html) for more details.

## Authors

* **Donovan Persent ([Poulpinou](https://github.com/Poulpinou))**

## Licenses
See [Licence](https://github.com/Poulpinou/DGTools-Core/LICENCE.md)
