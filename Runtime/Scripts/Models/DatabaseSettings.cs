using UnityEngine;
using UnityEditor;
using System.IO;

namespace DGTools.Database {
    public class DatabaseSettings : ScriptableObject
    {
        #region Constants
        public const string DIRECTORY_PATH = "Resources/Database";
        public const string FILE_NAME = "DatabaseSettings";
        #endregion

        #region Public Variables
        [Header("Path Settings")]
        [FolderPath] public string databaseFolderPath;
        public string databaseFileName = "database";
        public string tablesFolderName = "Tables";
        public string tableFileSuffix = "_table";
        public string imagesFolderName = "Images";
        public string schemasFolderName = "Schemas";
        public string schemaFilePrefix = "schema_v";

        [Header("Performences Settings")]
        [Range(0.001f, 1)] public float coroutinesMaxExecutionTime = 0.01f;
        #endregion

        #region Static Methods
        /// <summary>
        /// Creates setting at "Assets/<see cref="DIRECTORY_PATH"/>/<see cref="FILE_NAME"/>.asset"
        /// </summary>
        /// <returns><see cref="DatabaseSettings"/> that was created</returns>
        public static DatabaseSettings Create()
        {
            DatabaseSettings asset = ScriptableObject.CreateInstance<DatabaseSettings>();

            string directory = Path.Combine(Application.dataPath, DIRECTORY_PATH);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            AssetDatabase.CreateAsset(asset, Path.Combine("Assets", DIRECTORY_PATH, FILE_NAME + ".asset"));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return asset;
        }

        /// <summary>
        /// Creates all the files and foldres that the <see cref="Database"/> will need
        /// </summary>
        public void CreateDatabaseForSettings()
        {
            string path = Path.Combine(PathUtilities.absolutePath, databaseFolderPath);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            string tablePath = Path.Combine(path, tablesFolderName);
            if (!Directory.Exists(tablePath)) Directory.CreateDirectory(tablePath);

            string imagesPath = Path.Combine(path, imagesFolderName);
            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

            string schemasPath = Path.Combine(path, schemasFolderName);
            if (!Directory.Exists(schemasPath)) Directory.CreateDirectory(schemasPath);

            File.WriteAllText(Path.Combine(path, databaseFileName + ".json"), "");
        }
        #endregion
    }
}

