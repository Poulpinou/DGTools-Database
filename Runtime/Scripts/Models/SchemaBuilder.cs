using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DGTools.Database
{
    public class SchemaBuilder
    {
        #region Public Variables
        public readonly string path;
        #endregion

        #region Private Variables
        string prefix;
        #endregion

        #region Properties
        public List<string> availableVersions { get; private set; }

        public string lastVersion => availableVersions.Max();

        public Schema activeSchema { get; private set; }
        #endregion

        #region Constructors
        public SchemaBuilder(DatabaseSettings settings)
        {
            path = Path.Combine(PathUtilities.absolutePath, settings.databaseFolderPath, settings.schemasFolderName);
            prefix = settings.schemaFilePrefix;

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException(path);

            ReloadVersions();

            if (availableVersions.Count > 0)
                LoadSchema(lastVersion);
            else
                CreateSchemaForCurrentVersion();
        }
        #endregion

        #region Public Methods
        public void ReloadVersions()
        {
            availableVersions = new List<string>();
            foreach (string file in Directory.GetFiles(path).Where(f => f.Contains(prefix) && f.Contains(".json") && !f.Contains(".meta")))
            {
                string version = file.Split(new string[] { prefix }, StringSplitOptions.None)[1];
                version = version.Replace(".json", "");
                availableVersions.Add(version);
            }

            if (activeSchema != null && !availableVersions.Contains(activeSchema.version))
                availableVersions.Add(activeSchema.version);
        }

        public void LoadSchema(string version)
        {
            if (activeSchema != null && activeSchema.version == version) return;

            activeSchema = new Schema(LoadVersion(version));
            ReloadVersions();
        }

        public void CreateSchemaForCurrentVersion(string fromVersion = null)
        {
            if (string.IsNullOrEmpty(fromVersion) && availableVersions.Count > 0) fromVersion = lastVersion;
            if (IsVersionExists(Application.version))
            {
                throw new Exception(string.Format("Impossible to create a schema for version {0}, it already exists", Application.version));
            }

            if (fromVersion != null)
            {
                LoadSchema(fromVersion);
                activeSchema = new Schema(Application.version, activeSchema);
            }
            else
            {
                activeSchema = new Schema(Application.version);
            }

            ReloadVersions();
        }

        public JObject LoadVersion(string version)
        {
            string path = GetPathForVersion(version);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            return JObject.Parse(File.ReadAllText(path));
        }

        public string GetPathForVersion(string version)
        {
            return Path.Combine(path, prefix + version + ".json");
        }

        public bool IsVersionExists(string version)
        {
            return availableVersions.Contains(version);
        }

        public void SaveActiveSchema()
        {
            SaveSchema(activeSchema);
        }

        public void SaveSchema(Schema schema)
        {
            string json = schema.Serialize().ToString();
            File.WriteAllText(GetPathForVersion(schema.version), json);
        }
        #endregion
    }
}