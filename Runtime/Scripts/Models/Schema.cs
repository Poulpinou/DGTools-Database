using System.Collections.Generic;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DGTools.Database
{
    public class Schema
    {
        #region Private Variables
        List<TableSchema> tableSchemas;
        #endregion

        #region Properties
        public string version { get; private set; }
        #endregion

        #region Constructors
        public Schema(string version, Schema refSchema = null)
        {
            this.version = version;

            if (refSchema != null)
            {
                tableSchemas = refSchema.tableSchemas;
            }
            else
            {
                tableSchemas = new List<TableSchema>();
            }
        }

        public Schema(JObject datas)
        {
            version = (string)datas.SelectToken("version");

            JArray tableSchemasDatas = (JArray)datas.SelectToken("tableSchemas");
            tableSchemas = new List<TableSchema>();
            foreach (JObject tableSchemasData in tableSchemasDatas)
            {
                tableSchemas.Add(new TableSchema(tableSchemasData));
            }
        }
        #endregion

        #region Public Methods
        public JObject Serialize()
        {
            JObject datas = new JObject();

            datas.Add("version", version);

            JArray tableSchemasDatas = new JArray();
            foreach (TableSchema schema in tableSchemas)
            {
                tableSchemasDatas.Add(schema.Serialize());
            }

            datas.Add("tableSchemas", tableSchemasDatas);

            return datas;
        }

        public void AddTableSchema(TableSchema schema)
        {
            if (tableSchemas.Where(s => s.itemType == schema.itemType).Count() > 0)
                throw new Exception(string.Format("Settings already contains a table of type " + schema.itemType));
            tableSchemas.Add(schema);
        }

        public TableSchema GetTableSchema(Type type)
        {
            return tableSchemas.Where(t => t.itemType == type).FirstOrDefault();
        }

        public List<TableSchema> GetTableSchemas()
        {
            return tableSchemas;
        }

        public void RemoveTableSchema(TableSchema schema)
        {
            if (tableSchemas.Contains(schema))
                tableSchemas.Remove(schema);
            else
                throw new Exception(string.Format("Schema does not contain a table of type " + schema.itemType));
        }

        public void RebuildTableSchema(Type tableType)
        {
            TableSchema toRemove = tableSchemas.Where(t => t.itemType == tableType).First();
            if (toRemove == null)
                throw new Exception(string.Format("No table of type {0} found in this schema", tableType.ToString()));

            tableSchemas.Remove(toRemove);
            tableSchemas.Add(new TableSchema(tableType));
        }
        #endregion
    }
}