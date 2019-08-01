using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using Newtonsoft.Json.Linq;
using System.IO;

namespace DGTools.Database
{
    public class Database : StaticManager<Database>
    {
        #region Public Variables
        [Header("Database Infos")]
        [SerializeField] [Readonly] string currentVersion;

        [Header("Runtime Settings")]
        [Tooltip("If set to true, Database will automatically load on Start()")]
        [SerializeField] bool loadOnStart = false;
        [Tooltip("If set to true, Database will automatically update to the last schema")]
        [SerializeField] bool autoUpdate = true;
        #endregion

        #region Private Variables
        DatabaseSettings settings;
        Dictionary<Type, Table> tables;
        #endregion

        #region Events
        /// <summary>
        /// Is called when database is loaded
        /// </summary>
        [Header("Events")]
        [SerializeField] public UnityEvent OnDatabaseLoaded = new UnityEvent();
        #endregion

        #region Static Properties
        /// <summary>
        /// True if database is loaded
        /// </summary>
        public static bool isLoaded { get; private set; } = false;

        /// <summary>
        /// The full absolute path to the database folder
        /// </summary>
        public static string databaseFolderPath => Path.Combine(PathUtilities.absolutePath, active.settings.databaseFolderPath);

        /// <summary>
        /// The <see cref="databaseFolderPath"/> of the active <see cref="Database"/>
        /// </summary>
        public static DatabaseSettings Settings => active.settings;
        #endregion

        #region Static Methods
        /// <summary>
        /// Loads the database asynchronously and invokes <paramref name="onLoadDone"/> when it's done
        /// </summary>
        /// <param name="onLoadDone">The <see cref="UnityAction"/> to invoke when database is loaded</param>
        public static void Load(UnityAction onLoadDone = null)
        {
            if (isLoaded) throw new Exception("Database already loaded, call Reload() if you want to refresh");
            active.StartCoroutine(active.RunLoad(onLoadDone));
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets a <see cref="Table"/> for a given <see cref="Type"/> that implements <see cref="IDatabasable"/>
        /// </summary>
        /// <typeparam name="Titem">Type of an item that implements <see cref="IDatabasable"/></typeparam>
        /// <returns>A <see cref="Table"/> of type <typeparamref name="Titem"/></returns>
        public Table<Titem> GetTable<Titem>() where Titem : IDatabasable, new()
        {
            if (tables.ContainsKey(typeof(Titem)))
                return tables[typeof(Titem)] as Table<Titem>;

            throw new Exception(string.Format("No table of type {0} found in database", typeof(Titem).ToString()));
        }

        /// <summary>
        /// Gets a <see cref="Table"/> for a given <see cref="Type"/>
        /// </summary>
        /// <param name="tableType">The generic type of the desired table</param>
        /// <returns>A <see cref="Table"/> with <see cref="Table.itemType"/> == <paramref name="tableType"/></returns>
        public Table GetTable(Type tableType)
        {
            if (tables.ContainsKey(tableType))
                return tables[tableType];

            throw new Exception(string.Format("No table of type {0} found in database", tableType));
        }

        /// <summary>
        /// Save the database settings
        /// </summary>
        public void Save()
        {
            string json = Serialize().ToString();
            File.WriteAllText(Path.Combine(databaseFolderPath, settings.databaseFileName + ".json"), json);
        }
        #endregion

        #region Private Methods
        void LoadSettings()
        {
            settings = Resources.Load<DatabaseSettings>("Database/" + DatabaseSettings.FILE_NAME);
        }

        void LoadDatabaseFile()
        {
            string databaseFilePath = Path.Combine(databaseFolderPath, settings.databaseFileName + ".json");

            if (!File.Exists(databaseFilePath))
                throw new Exception(string.Format("Database file was not found, try to create it in <b>DGTools/Manage Database/Settings</b>"));

            string json = File.ReadAllText(databaseFilePath);

            if (!string.IsNullOrEmpty(json))
            {
                Unserialize(JObject.Parse(json));
            }
        }

        protected virtual JObject Serialize()
        {
            JObject datas = new JObject();

            datas.Add("currentVersion", currentVersion);

            return datas;
        }

        protected virtual void Unserialize(JObject datas)
        {
            currentVersion = (string)datas.SelectToken("currentVersion");
        }
        #endregion

        #region Coroutines
        IEnumerator RunLoad(UnityAction onLoadDone = null)
        {
            LoadSettings();
            LoadDatabaseFile();

            yield return RunSchema();
            yield return RunLoadTables();

            isLoaded = true;
            onLoadDone.Invoke();
            OnDatabaseLoaded.Invoke();
        }

        IEnumerator RunSchema()
        {
            float startTime = Time.realtimeSinceStartup;
            SchemaBuilder schemaBuilder = new SchemaBuilder(settings);

            if (schemaBuilder.availableVersions.Count == 0)
            {
                throw new Exception("No Schema found, you have to build a schema from <b>DGTools/Manage Database/Schema</b> window before using database");
            }

            tables = new Dictionary<Type, Table>();
            if (!autoUpdate && !string.IsNullOrEmpty(currentVersion))
            {
                schemaBuilder.LoadVersion(currentVersion);
            }

            currentVersion = schemaBuilder.activeSchema.version;

            foreach (TableSchema tableSchema in schemaBuilder.activeSchema.GetTableSchemas())
            {
                if (Time.realtimeSinceStartup - startTime > settings.coroutinesMaxExecutionTime)
                {
                    yield return new WaitForEndOfFrame();
                    startTime = Time.realtimeSinceStartup;
                }

                try
                {
                    Table table = Table.Build(tableSchema);
                    tables.Add(table.itemType, table);
                }
                catch (Exception e)
                {
                    Debug.LogError(string.Format("Failed to load {0} table : {1}", tableSchema.itemType, e));
                }
            }

            Save();
        }

        IEnumerator RunLoadTables()
        {
            foreach (Table table in tables.Values)
            {
                table.LoadTable();
                yield return null;
            }
        }
        #endregion

        #region Runtime Methods
        private void Start()
        {
            if (loadOnStart) Load();
        }
        #endregion
    }
}

