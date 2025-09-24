using NPOI.SS.Formula.Functions;
using NUnit.Framework.Interfaces;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using System.IO;
using UnityEngine;

namespace GMLM.Data.Editor {
    public class DataEditorWindow : OdinMenuEditorWindow {
        [MenuItem("GMLM/Open Data Editor")]
        private static void OpenWindow() {
            var window = GetWindow<DataEditorWindow>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(1200, 700);
            window.Show();
        }

        protected override OdinMenuTree BuildMenuTree() {
            
            var config = DataEditorConfig.Instance;

            var tree = new OdinMenuTree() {
                { "0. Settings", config },
            };

            var createNewPilotData = new CreateMenu<PilotDataTable, PilotData>(config.PilotDataTablePath);
            tree.Add("1. Pilots", createNewPilotData);
            tree.AddAllAssetsAtPath("1. Pilots", config.PilotDataTablePath, typeof(PilotDataTable), true, true);
            
            tree.SortMenuItemsByName(false);

            tree.Config.DrawSearchToolbar = true;
            

            return tree;
        }


        #region
        public class CreateMenu<Table, Data> where Table : DataTableBase<Table, Data> where Data : IData, new() {
            [InlineEditor(Expanded = true, ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
            public Table table;
            
            private string path;

            public CreateMenu(string path) {
                this.path = path;
                table = CreateInstance<Table>();
            }

            [Button("Add New Data", Style = ButtonStyle.Box, ButtonHeight = 50)]
            private void CreateNew() {
                string path = EditorUtility.SaveFilePanel("Create", this.path, "", "asset");
                path = Path.GetRelativePath(Path.Combine(Application.dataPath, "../"), path);

                AssetDatabase.CreateAsset(table, path);
                AssetDatabase.SaveAssets();

                table = CreateInstance<Table>();
            }
        }
        #endregion
    }
}