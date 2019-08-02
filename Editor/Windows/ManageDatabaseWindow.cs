using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DGTools.Editor;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;

namespace DGTools.Database.Editor
{
    public class ManageDatabaseWindow : DGToolsWindow
    {
        #region Enums
        enum WindowType { Settings = 1, Schema = 2, Explore = 3 }
        #endregion

        #region Properties
        WindowType activeWindow { get; set; }

        DatabaseSettings settings { get; set; }

        TableSchema selectedTable
        {
            get { return _selectedTable; }
            set
            {
                _selectedTable = value;
                if (_selectedTable != null)
                    refTable = new TableSchema(value.itemType);
                else
                    refTable = null;
            }
        }
        #endregion

        #region Private Variables
        //Gobals
        string log = "";
        SchemaBuilder schemaBuilder;

        //Settings Window
        Vector2 scrollPosition = Vector2.zero;

        //Schema Window
        int versionIndex;
        bool autoSaveSchema = true;
        Vector2 leftscrollPosition = Vector2.zero;
        Vector2 rightscrollPosition = Vector2.zero;       
        TableSchema _selectedTable;
        TableSchema refTable;

        //Explore Window
        TableSchema exploredTable;
        Vector2 tableScroll = Vector2.zero;
        Vector2 fieldScroll = Vector2.zero;
        float gridCellsWidth = 150;
        float gridCellsHeight = 30;
        JArray tableDatas = null;
        #endregion

        #region Private Methods
        void SettingsWindow()
        {
            //Create Settings Window
            if (settings == null)
            {
                GUILayout.BeginVertical(skin.box);
                GUILayout.Label("No " + DatabaseSettings.FILE_NAME + " file found in " + DatabaseSettings.DIRECTORY_PATH, skin.FindStyle("Title"));
                if (GUILayout.Button("Create One", skin.button))
                {
                    DatabaseSettings.Create();
                    LoadSettings();
                }
                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.Label("Database Settings", skin.FindStyle("Title"));

                scrollPosition = GUILayout.BeginScrollView(scrollPosition, skin.box);
                UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(settings);
                editor.OnInspectorGUI();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset", skin.button))
                {
                    DatabaseSettings.Create();
                    LoadSettings();
                }

                if (GUILayout.Button("Create Database", skin.button))
                {
                    settings.CreateDatabaseForSettings();
                }
                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();
            }
        }

        void SchemaWindow()
        {
            GUILayout.BeginVertical(skin.box);
            GUILayout.Label("Drop here some scripts you want to add to the schema", skin.FindStyle("Title"));

            MonoScript script = null;
            script = EditorGUILayout.ObjectField(script, typeof(MonoScript), false) as MonoScript;

            if (script != null)
            {
                Type type = script.GetClass();
                try
                {
                    schemaBuilder.activeSchema.AddTableSchema(new TableSchema(type));
                    log = string.Format("Table {0} added to list!", type);

                    if (autoSaveSchema)
                        schemaBuilder.SaveActiveSchema();
                }
                catch (Exception e)
                {
                    log = e.Message;
                }
            }
            GUILayout.EndVertical();

            //Table Header
            GUILayout.BeginHorizontal();

            GUILayout.Label("Version", skin.FindStyle("Title"));
            versionIndex = schemaBuilder.availableVersions.IndexOf(schemaBuilder.activeSchema.version);
            int index = EditorGUILayout.Popup(versionIndex, schemaBuilder.availableVersions.ToArray());
            if (index != versionIndex)
            {
                schemaBuilder.LoadSchema(schemaBuilder.availableVersions[index]);
                if (selectedTable != null)
                {
                    try
                    {
                        selectedTable = schemaBuilder.activeSchema.GetTableSchema(selectedTable.itemType);
                    }
                    catch (Exception e)
                    {
                        log = e.Message;
                    }
                }
            }

            GUILayout.FlexibleSpace();
            autoSaveSchema = EditorGUILayout.Toggle("Auto-save Schema", autoSaveSchema);


            if (GUILayout.Button("Save Schema", skin.button))
            {

                try
                {
                    schemaBuilder.SaveActiveSchema();
                    log = string.Format("Schema v{0} successfully saved!", schemaBuilder.activeSchema.version);
                }
                catch (Exception e)
                {
                    log = e.Message;
                }
            }

            if (GUILayout.Button("New Schema", skin.button))
            {
                try
                {
                    schemaBuilder.CreateSchemaForCurrentVersion();
                    log = string.Format("Schema v{0} successfully created!", schemaBuilder.activeSchema.version);

                    if (autoSaveSchema)
                        schemaBuilder.SaveActiveSchema();
                }
                catch (Exception e)
                {
                    log = e.Message;
                }
            }

            GUILayout.EndHorizontal();

            try
            {
                //Grid Block
                GUILayout.BeginHorizontal(skin.box);

                //Left block
                GUILayout.BeginVertical(skin.box);
                GUILayout.Label("Tables", skin.FindStyle("Title"));

                leftscrollPosition = GUILayout.BeginScrollView(leftscrollPosition);
                if (schemaBuilder.activeSchema == null)
                {
                    GUILayout.Label("No Schema found", skin.FindStyle("Italic"));
                }
                else if (schemaBuilder.activeSchema.GetTableSchemas().Count == 0)
                {
                    GUILayout.Label("No Table found", skin.FindStyle("Italic"));
                }
                else {
                    foreach (TableSchema schema in schemaBuilder.activeSchema.GetTableSchemas())
                    {
                        if (schema.isValid)
                        {
                            if (GUILayout.Button(schema.itemType.ToString()))
                            {
                                selectedTable = schema;
                                rightscrollPosition = Vector2.zero;
                            }
                        }
                        else {
                            schemaBuilder.activeSchema.RemoveTableSchema(schema);
                        }
                        
                    }
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                //Right block
                GUILayout.BeginVertical(skin.box);
                GUILayout.Label("Fields", skin.FindStyle("Title"));

                rightscrollPosition = GUILayout.BeginScrollView(rightscrollPosition);

                if (selectedTable != null)
                {
                    GUILayout.BeginVertical();

                    List<TableField> fields = selectedTable.fields.Where(f => refTable.ContainsField(f)).ToList();
                    if (fields.Count > 0)
                    {
                        GUILayout.Label("Fields to use", skin.FindStyle("Title"), GUILayout.ExpandWidth(false));

                        foreach (TableField field in fields)
                        {
                            GUILayout.BeginHorizontal();
                            try
                            {
                                GUILayout.Label(field.isProperty ? "P:" : "F:", GUILayout.ExpandWidth(false));
                                GUILayout.Label(field.fieldName, skin.FindStyle("Italic"), GUILayout.ExpandWidth(false));
                                GUILayout.Label("(" + field.fieldType.ToString() + ")", GUILayout.ExpandWidth(false));
                            }
                            catch
                            {
                                GUILayout.Label("Field type not supported!");
                            }

                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Don't Use", GUILayout.ExpandWidth(false)))
                            {
                                selectedTable.RemoveField(field);
                                if (autoSaveSchema)
                                    schemaBuilder.SaveActiveSchema();
                            }
                            GUILayout.EndHorizontal();
                        }
                        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                    }

                    fields = refTable.fields.Where(f => !selectedTable.ContainsField(f)).ToList();
                    if (fields.Count > 0)
                    {
                        GUILayout.Label("Available Fields", skin.FindStyle("Title"), GUILayout.ExpandWidth(false));
                        foreach (TableField field in fields)
                        {
                            GUILayout.BeginHorizontal();
                            try
                            {
                                GUILayout.Label(field.isProperty ? "P:" : "F:", GUILayout.ExpandWidth(false));
                                GUILayout.Label(field.fieldName, skin.FindStyle("Italic"), GUILayout.ExpandWidth(false));
                                GUILayout.Label("(" + field.fieldType.ToString() + ")", GUILayout.ExpandWidth(false));
                            }
                            catch
                            {
                                GUILayout.Label("Field type not supported!");
                            }

                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Use", GUILayout.ExpandWidth(false)))
                            {
                                selectedTable.AddField(field);
                                if (autoSaveSchema)
                                    schemaBuilder.SaveActiveSchema();
                            }
                            GUILayout.EndHorizontal();
                        }
                        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                    }

                    fields = selectedTable.fields.Where(f => !refTable.ContainsField(f)).ToList();
                    if (fields.Count > 0)
                    {
                        GUILayout.Label("Deprecated Fields", skin.FindStyle("Title"), GUILayout.ExpandWidth(false));
                        foreach (TableField field in fields)
                        {
                            GUILayout.BeginHorizontal();
                            try
                            {
                                GUILayout.Label(field.isProperty ? "P:" : "F:", GUILayout.ExpandWidth(false));
                                GUILayout.Label(field.fieldName, skin.FindStyle("Italic"), GUILayout.ExpandWidth(false));
                                GUILayout.Label("(" + field.fieldType.ToString() + ")", GUILayout.ExpandWidth(false));
                            }
                            catch
                            {
                                GUILayout.Label("Field type not supported!");
                            }

                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Delete", GUILayout.ExpandWidth(false)))
                            {
                                selectedTable.RemoveField(field);
                                if (autoSaveSchema)
                                    schemaBuilder.SaveActiveSchema();
                            }
                            GUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.EndVertical();

                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Remove Table", skin.button))
                    {
                        schemaBuilder.activeSchema.RemoveTableSchema(selectedTable);
                        selectedTable = null;
                        if (autoSaveSchema)
                            schemaBuilder.SaveActiveSchema();
                    }

                    if (GUILayout.Button("Update all fields", skin.button))
                    {
                        Type tableType = selectedTable.itemType;
                        schemaBuilder.activeSchema.RebuildTableSchema(tableType);
                        selectedTable = schemaBuilder.activeSchema.GetTableSchemas().Where(t => t.itemType == tableType).First();
                        if (autoSaveSchema)
                            schemaBuilder.SaveActiveSchema();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();

            }
            catch (Exception e)
            {
                log = e.Message;
            }
        }

        void ExploreWindow() {
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Schema Version", skin.FindStyle("Title"), GUILayout.ExpandWidth(false));
            versionIndex = schemaBuilder.availableVersions.IndexOf(schemaBuilder.activeSchema.version);
            int index = EditorGUILayout.Popup(versionIndex, schemaBuilder.availableVersions.ToArray(), GUILayout.ExpandWidth(false));
            if (index != versionIndex)
            {
                schemaBuilder.LoadSchema(schemaBuilder.availableVersions[index]);
                if (exploredTable != null)
                {
                    try
                    {
                        exploredTable = schemaBuilder.activeSchema.GetTableSchema(selectedTable.itemType);
                    }
                    catch (Exception e)
                    {
                        log = e.Message;
                    }
                }
            }

            if (exploredTable != null)
            {
                GUILayout.Label(exploredTable.itemType.ToString(), skin.FindStyle("Title"));
                if (GUILayout.Button("Back"))
                {
                    exploredTable = null;
                    tableDatas = null;
                }
            }
            else {
                GUILayout.Label("Tables", skin.FindStyle("Title"));
            }
            GUILayout.EndHorizontal();

            tableScroll = GUILayout.BeginScrollView(tableScroll, skin.box);
            GUILayout.BeginVertical();
            if (exploredTable == null)
            {
                foreach (TableSchema schema in schemaBuilder.activeSchema.GetTableSchemas()) {
                    if (GUILayout.Button(schema.itemType.ToString())){
                        exploredTable = schema;
                        LoadDatas(exploredTable);
                    }
                }
            }
            else {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                GUILayout.Button("ID", skin.FindStyle("ArrayHeader"), GUILayout.Width(gridCellsWidth));

                foreach (TableField field in exploredTable.fields) {
                    if (field.fieldName != "ID")
                        GUILayout.Button(field.fieldName, skin.FindStyle("ArrayHeader"), GUILayout.Width(gridCellsWidth));
                }
                GUILayout.EndHorizontal();

                if (tableDatas != null)
                {
                    foreach (JObject tableData in tableDatas) {
                        GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                        GUILayout.Label((string)tableData.SelectToken("ID"), skin.FindStyle("ArrayLine"), GUILayout.Width(gridCellsWidth), GUILayout.Height(gridCellsHeight));

                        foreach (TableField field in exploredTable.fields)
                        {
                            if(field.fieldName != "ID")
                            {
                                try
                                {
                                    GUILayout.Label((string)tableData.SelectToken(field.fieldName), skin.FindStyle("ArrayLine"), GUILayout.Width(gridCellsWidth), GUILayout.Height(gridCellsHeight));
                                }
                                catch
                                {
                                    GUILayout.Label("error", skin.FindStyle("ArrayLine"), GUILayout.Width(gridCellsWidth), GUILayout.Height(gridCellsHeight));
                                }
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        void LoadSettings()
        {
            settings = Resources.Load<DatabaseSettings>("Database/" + DatabaseSettings.FILE_NAME);
        }

        void LoadScripts()
        {
            List<MonoScript> scripts = new List<MonoScript>();

        }

        void LoadDatas(TableSchema table) {
            string path = Path.Combine(
                PathUtilities.absolutePath,
                settings.databaseFolderPath,
                settings.tablesFolderName,
                table.itemType.ToString().Replace(".", "") + settings.tableFileSuffix + ".json"
            );
            try
            {
                JObject datas = JObject.Parse(File.ReadAllText(path));
                tableDatas = (JArray)datas.SelectToken("datas");
            }
            catch (Exception e) {
                log = string.Format("Failed to load table from {0} : {1}", path, e.Message);
            }
        }
        #endregion

        #region Editor Methods
        [MenuItem("DGTools/Manage Database")]
        public static void ShowWindow()
        {
            ManageDatabaseWindow window = GetWindow(typeof(ManageDatabaseWindow)) as ManageDatabaseWindow;
            window.LoadSettings();
            window.titleContent = new GUIContent("Manage Database");
            window.activeWindow = WindowType.Settings;
        }

        void OnGUI()
        {
            //Affiche les erreurs
            if (!string.IsNullOrEmpty(log))
                GUILayout.Label(log);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Settings")) activeWindow = WindowType.Settings;
            if (settings != null)
            {
                if (schemaBuilder == null)
                    schemaBuilder = new SchemaBuilder(settings);

                if (GUILayout.Button("Schema")) activeWindow = WindowType.Schema;
                if (GUILayout.Button("Explore")) activeWindow = WindowType.Explore;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(skin.box);
            switch (activeWindow)
            {
                case WindowType.Settings:
                    SettingsWindow(); break;
                case WindowType.Schema:
                    SchemaWindow(); break;
                case WindowType.Explore:
                    ExploreWindow(); break;
            }
            GUILayout.EndVertical();
        }
        #endregion
    }
}
