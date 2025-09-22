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
        [MenuItem("GMLM/Datas")]
        private static void OpenWindow() {
            var window = GetWindow<DataEditorWindow>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(1200, 700);
            window.Show();
        }

        // 개발 들어가기 전에 어떻게 할 지
        // 설계를 해보자
        
        
        // IDataTable 만들어서
        // 위에 Create 메뉴 만들기
        // 

        protected override OdinMenuTree BuildMenuTree() {
            
            var config = DataEditorConfig.Instance;

            var tree = new OdinMenuTree() {
                { "0. Settings", config },
            };

            /*

            var createNewStageData = new CreateMenu<StageDataTable, StageData>(config.StageDataTablePath);
            tree.Add("1. Stages", createNewStageData);
            tree.AddAllAssetsAtPath("1. Stages", config.StageDataTablePath, typeof(StageDataTable), true, true);

            var createNewFirstEventData = new CreateMenu<FirstEventDataTable, FirstEventData>(config.FirstEventDataTablePath);
            tree.Add("2. First Events", createNewFirstEventData);
            tree.AddAllAssetsAtPath("2. First Events", config.FirstEventDataTablePath, typeof(FirstEventDataTable), true, true);

            var createNewBreakEventData = new CreateMenu<BreakEventDataTable, BreakEventData>(config.BreakEventDataTablePath);
            tree.Add("3. Break Events", createNewBreakEventData);
            tree.AddAllAssetsAtPath("3. Break Events", config.BreakEventDataTablePath, typeof(BreakEventDataTable), true, true);

            var createNewBattleEventData = new CreateMenu<BattleEventDataTable, BattleEventData>(config.BattleEventDataTablePath);
            tree.Add("4. Battle Events", createNewBattleEventData);
            tree.AddAllAssetsAtPath("4. Battle Events", config.BattleEventDataTablePath, typeof(BattleEventDataTable), true, true);

            var createNewCommonEventData = new CreateMenu<CommonEventDataTable, CommonEventData>(config.CommonEventDataTablePath);
            tree.Add("5. Common Events", createNewCommonEventData);
            tree.AddAllAssetsAtPath("5. Common Events", config.CommonEventDataTablePath, typeof(CommonEventDataTable), true, true);

            var createNewAbilityData = new CreateMenu<AbilityDataTable, AbilityData>(config.AbilityDataTablePath);
            tree.Add("6. Abilities", createNewAbilityData);
            tree.AddAllAssetsAtPath("6. Abilities", config.AbilityDataTablePath, typeof(AbilityDataTable), true, true);

            */
            
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