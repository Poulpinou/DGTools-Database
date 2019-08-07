using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace DGTools.Database
{
    public abstract class Table
    {
        #region Private Methods
        protected TableSchema schema;
        #endregion

        #region Properties
        /// <summary>
        /// The <see cref="Type"/> of the item handled by this table
        /// </summary>
        public abstract Type itemType { get; }

        /// <summary>
        /// The path of the table's datas file
        /// </summary>
        public string path => Path.Combine(
            Database.databaseFolderPath,
            Database.Settings.tablesFolderName,
            itemType.ToString().Replace(".", "") + Database.Settings.tableFileSuffix + ".json"
        );
        #endregion

        #region Static Methods
        /// <summary>
        /// Builds a <see cref="Table"/> from a <see cref="Schema"/>
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        public static Table Build(TableSchema schema)
        {
            Type type = typeof(Table<>).MakeGenericType(schema.itemType);
            return GenericsUtilities.Call(type, "Build", null, new TableSchema[] { schema }) as Table;
        }
        #endregion

        #region Abstract Methods
        public abstract void SaveTable();
        public abstract void LoadTable();
        #endregion
    }

    public class Table<Titem> : Table where Titem : IDatabasable, new()
    {
        #region Properties
        /// <summary>
        /// <see cref="Table"/>'s datas in json
        /// </summary>
        public JArray datas { get; private set; }

        /// <summary>
        /// The last ID of the table
        /// </summary>
        public int currentID { get; private set; } = 0;

        /// <summary>
        /// The <see cref="Type"/> of the item handled by this table
        /// </summary>
        public override Type itemType => typeof(Titem);
        #endregion

        #region Static Methods
        public static Table<TTitem> Build<TTitem>(TableSchema schema) where TTitem : IDatabasable, new()
        {
            Table<TTitem> table = new Table<TTitem>();
            table.schema = schema;
            return table;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns true if ID exists in <see cref="Table"/>'s datas
        /// </summary>
        /// <param name="ID">The ID to test</param>
        public bool IDExists(int ID)
        {
            return datas.Count(d => (int)d["ID"] == ID) > 0;
        }

        public bool IsUnique(string key, string value) {
            return datas.Count(d => (string)d[key] == value) == 0;
        }

        /// <summary>
        /// Saves tables datas
        /// </summary>
        public override void SaveTable()
        {
            JObject tableDatas = new JObject();

            tableDatas.Add("currentID", currentID);
            tableDatas.Add("datas", datas);

            File.WriteAllText(path, tableDatas.ToString());
        }

        /// <summary>
        /// Loads tables datas
        /// </summary>
        public override void LoadTable()
        {
            if (!File.Exists(path))
            {
                currentID = 0;
                datas = new JArray();
                SaveTable();
            }

            string json = File.ReadAllText(path);
            JObject tableDatas = JObject.Parse(json);

            currentID = (int)tableDatas.SelectToken("currentID");
            datas = (JArray)tableDatas.SelectToken("datas");
        }

        /// <summary>
        /// Returns one item with item.ID == <paramref name="ID"/>
        /// </summary>
        /// <param name="ID">The ID of the desired item</param>
        /// <returns>The item loaded from database</returns>
        public Titem GetOneByID(int ID)
        {
            return GetOne(t => (int)t["ID"] == ID);
        }

        /// <summary>
        /// Returns the first item that match the filter from the database
        /// </summary>
        /// <param name="filter">
        /// The filter function for the request
        /// ex : <code>
        ///     new GetRequest<MyItem>(datas => (string)datas["myCustomName"] == "Shuckle");
        /// </code>
        /// </param>
        /// <returns>The item loaded from database</returns>
        public Titem GetOne(Func<JToken, bool> filter)
        {
            return LoadItem(ApplyFilter(filter).FirstOrDefault());
        }

        /// <summary>
        /// Returns a <see cref="List{Titem}"/> of items that match the filter from database
        /// </summary>
        /// <param name="filter">
        /// The filter function for the request
        /// ex : <code>
        ///     new GetRequest<MyItem>(datas => (string)datas["myCustomName"] == "Shuckle");
        /// </code>
        /// </param>
        /// <returns>The List of items loaded from database</returns>
        public List<Titem> GetMany(Func<JToken, bool> filter)
        {
            List<Titem> items = new List<Titem>();
            foreach (JToken itemDatas in ApplyFilter(filter))
            {
                items.Add(LoadItem(itemDatas));
            }
            return items;
        }

        /// <summary>
        /// Loads an item from its datas
        /// </summary>
        /// <param name="itemDatas">Item's datas as <see cref="JToken"/></param>
        /// <returns>The loaded item</returns>
        public Titem LoadItem(JToken itemDatas)
        {
            return Unserialize(itemDatas);
        }

        /// <summary>
        /// Saves the item in tables datas
        /// </summary>
        /// <param name="item">The item to save</param>
        /// <param name="authorizeCreation">If true, the item will be added to the database if it doesn't exists</param>
        public void SaveItem(Titem item, bool authorizeCreation = true)
        {
            if (item.ID == 0 && authorizeCreation)
            {
                CreateItem(item);
                return;
            }

            JObject itemDatas = Serialize(item);

            if (IDExists(item.ID))
            {
                IEnumerable<JToken> datasToRemove = datas.Where(t => (int)t["ID"] == item.ID);
                foreach (JToken toRemove in datasToRemove) {
                    datas.Remove(toRemove);
                }
            }

            datas.Add(itemDatas);
        }

        /// <summary>
        /// Creates and save an item in <see cref="Table"/>'s datas
        /// </summary>
        /// <param name="item"></param>
        public void CreateItem(Titem item)
        {
            item.ID = GenerateNewID();
            SaveItem(item);
        }
        #endregion

        #region Private Methods
        IEnumerable<JToken> ApplyFilter(Func<JToken, bool> filter)
        {
            return datas.Where(filter);
        }

        int GenerateNewID()
        {
            currentID++;
            return currentID;
        }

        string ValueToString(object value)
        {
            return value == null ? null : value.ToString();
        }

        protected virtual object DataToObject(JToken datas, Type objectType) {
            if (objectType == typeof(Vector3)) return new Vector3().FromString(datas.Value<string>());
            if (objectType == typeof(Vector2)) return new Vector2().FromString(datas.Value<string>());

            return datas.ToObject(objectType);
        }

        JObject Serialize(Titem item)
        {
            JObject itemDatas = new JObject();

            foreach (TableField field in schema.fields)
            {
                if (field.fieldType is IDatabasable)
                {
                    if (field.isProperty)
                        itemDatas.Add(field.fieldName + "_ID", ValueToString(field.fieldType.GetProperty("ID").GetValue(item)));
                    else
                        itemDatas.Add(field.fieldName + "_ID", ValueToString(field.fieldType.GetField("ID").GetValue(item)));
                }
                else
                {
                    if (field.isProperty)
                        itemDatas.Add(field.fieldName, ValueToString(item.GetType().GetProperty(field.fieldName).GetValue(item)));
                    else
                        itemDatas.Add(field.fieldName, ValueToString(item.GetType().GetField(field.fieldName).GetValue(item)));
                }
            }
            return itemDatas;
        }

        Titem Unserialize(JToken itemDatas)
        {
            Titem item = new Titem();
            foreach (JProperty itemData in itemDatas.Children())
            {
                TableField field = schema.GetFieldByName(itemData.Name);
                if (field != null)
                {
                    if (itemData.Value != null)
                    {
                        object value = null;
                        try
                        {
                            value =  DataToObject(itemData.Value, field.fieldType);
                        }
                        catch(Exception e)
                        {
                            Debug.Log(string.Format("{0} type is not supported by database : {1}", field.fieldType, e.Message));
                            continue;
                        }

                        if (field.fieldType is IDatabasable)
                        {
                            /* TODO Load Linked Items
                            IDatabasable linkedItem = default;
                            IEnumerator<IDatabasable> loading = 
                            while (loading.MoveNext())
                            {
                                linkedItem = loading.Current;
                                yield return default;
                            }

                            if (field.isProperty)
                            {
                                typeof(Titem).GetProperty(field.fieldName).SetValue(item, linkedItem);
                            }
                            else
                            {
                                typeof(Titem).GetField(field.fieldName).SetValue(item, linkedItem);
                            }*/
                        }
                        else
                        {
                            if (field.isProperty)
                                typeof(Titem).GetProperty(field.fieldName).SetValue(item, value);
                            else
                                typeof(Titem).GetField(field.fieldName).SetValue(item, value);
                        }
                    }
                    else
                    {
                        Debug.Log(string.Format("Failed to load {0}({1}) field from {2} in {3}", field.fieldName, field.fieldType, itemDatas, typeof(Titem)));
                        continue;
                    }
                }
            }

            return item;
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// Gets a <see cref="List{Titem}"/> of items that matches the filter asynchronously
        /// </summary>
        /// <param name="filter">
        /// The filter function for the request
        /// ex : <code>
        ///     new GetRequest<MyItem>(datas => (string)datas["myCustomName"] == "Shuckle");
        /// </code>
        /// </param>
        /// <returns>The List of items loaded from database</returns>
        public IEnumerator<List<Titem>> GetManyAsync(Func<JToken, bool> filter)
        {
            List<Titem> items = new List<Titem>();
            foreach (JToken itemDatas in ApplyFilter(filter))
            {
                items.Add(LoadItem(itemDatas));
                yield return null;
            }
            yield return items;
        }
        #endregion
    }
}
