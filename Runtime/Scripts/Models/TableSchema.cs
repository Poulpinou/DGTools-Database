using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace DGTools.Database
{
    public class TableSchema
    {
        #region Public Variables
        [SerializeField] public Type itemType;
        [SerializeField] public List<TableField> fields;
        #endregion

        #region Constructors
        public TableSchema(Type type)
        {
            if (!typeof(IDatabasable).IsAssignableFrom(type))
                throw new Exception(string.Format("{0} doesn't implement IDatabasable, aborted.", type));

            if (type.GetConstructor(Type.EmptyTypes) == null)
                throw new Exception(string.Format("{0} should have an empty constructor", type));

            itemType = type;
            fields = new List<TableField>();

            foreach (FieldInfo field in type.GetFields())
            {
                DatabaseFieldAttribute attribute = field.GetCustomAttribute<DatabaseFieldAttribute>();
                if (attribute != null)
                    fields.Add(new TableField(field.FieldType, field.Name, false));
            }

            bool foundID = false;
            foreach (PropertyInfo property in type.GetProperties())
            {
                DatabaseFieldAttribute attribute = property.GetCustomAttribute<DatabaseFieldAttribute>();

                if (!foundID && property.Name == "ID" && property.PropertyType == typeof(int))
                {
                    fields.Add(new TableField(property.PropertyType, property.Name, true));
                    foundID = true;
                }
                else if (attribute != null)
                {
                    fields.Add(new TableField(property.PropertyType, property.Name, true));
                }
            }

            if (!foundID)
                throw new Exception(string.Format("{0} should implement <b>public int ID {get; set;}</b>", type));
        }

        public TableSchema(JObject datas)
        {
            itemType = Type.GetType((string)datas.SelectToken("itemType"));

            JArray fieldsdatas = (JArray)datas.SelectToken("fields");
            fields = new List<TableField>();
            foreach (JObject field in fieldsdatas)
            {
                fields.Add(new TableField(
                    TypeUtilities.GetTypeFromString((string)field.SelectToken("fieldType")),
                    (string)field.SelectToken("fieldName"),
                    (bool)field.SelectToken("isProperty")
                ));
            }
        }
        #endregion

        #region Public Methods
        public JObject Serialize()
        {
            JObject datas = new JObject();

            datas.Add("itemType", itemType.ToString());

            JArray fieldsdatas = new JArray();
            foreach (TableField field in fields)
            {
                JObject fieldData = new JObject();
                fieldData.Add("fieldType", field.fieldType.ToString());
                fieldData.Add("fieldName", field.fieldName);
                fieldData.Add("isProperty", field.isProperty);

                fieldsdatas.Add(fieldData);
            }

            datas.Add("fields", fieldsdatas);

            return datas;
        }

        public bool ContainsField(TableField field, bool onlyName = false)
        {
            if (fields == null || fields.Count == 0) return false;
            return fields.Where(f => f.fieldName == field.fieldName && (onlyName || (f.fieldType == field.fieldType && f.isProperty == field.isProperty))).Count() > 0;
        }

        public void AddField(TableField field, bool replace = false)
        {
            if (ContainsField(field, true))
            {
                if (replace)
                    RemoveField(field);
                else
                    throw new Exception(string.Format("Table of type {0} already contains a field named {1}", itemType, field.fieldName));
            }
            fields.Add(field);
        }

        public void RemoveField(TableField field)
        {
            if (!ContainsField(field, true))
                throw new Exception(string.Format("Table of type {0} doesn't contain a field named {1}", itemType, field.fieldName));
            TableField toRemove = fields.Where(f => f.fieldName == field.fieldName).First();
            fields.Remove(toRemove);
        }

        public TableField GetFieldByName(string name)
        {
            return fields.Where(f => f.fieldName == name).FirstOrDefault();
        }
        #endregion
    }
}