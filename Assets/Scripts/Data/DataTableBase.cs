using System;
using System.Collections;
using System.Collections.Generic;
// Unity
using UnityEngine;
// Etc
using Sirenix.OdinInspector;
// NPOI
using NPOI.SS.UserModel;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using Newtonsoft.Json.Linq;
using UnityEditor;
using System.Linq;

namespace GMLM.Data {
    [Serializable]
    public abstract class DataTableBase<Table, Data> : ScriptableObject where Table : DataTableBase<Table, Data> where Data : IData, new() {
#if UNITY_EDITOR
        [Sirenix.OdinInspector.FilePath(AbsolutePath = false, Extensions = "xls, xlsx")]
        [HorizontalGroup("group")]
        [BoxGroup("group/파일 선택")]
        [BoxGroup("group/파일 선택/파일 경로")]
        [OnValueChanged("LoadExcelFile")]
        [SerializeField]
        [HideLabel]
        private string filePath;
        [HorizontalGroup("group")]
        [BoxGroup("group/시트 선택")]
        [BoxGroup("group/시트 선택/시트 이름")]
        [ValueDropdown("sheets")]
        [OnValueChanged("GetSheet")]
        [SerializeField]
        [HideLabel]
        private string sheetName;

        public void SetDefaultExcelSettings(string filePath) {
            this.filePath = filePath;
            LoadExcelFile();
        }

        private IWorkbook workBook;
        private ISheet sheet;

        private List<string> sheets {
            get {
                if(workBook == null)
                    return null;
                var sheets = new List<string>();
                for(int i = 0; i < workBook.NumberOfSheets; i++) {
                    sheets.Add(workBook.GetSheetName(i));
                }
                return sheets;
            }
        }

        //[ShowIf("@sheet==null")]
        [Button("엑셀 파일 로드하기", Style = ButtonStyle.Box, ButtonHeight = 50)]
        private void LoadExcelFile() {
            if(filePath == null || filePath == "") return;

            try {
                var path = Path.Combine(Application.dataPath, filePath);
                Debug.Log(path);
                using(var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
                    if(filePath.EndsWith("xls")) {
                        this.workBook = new HSSFWorkbook(fs);
                    } else if(filePath.EndsWith("xlsx")) {
                        this.workBook = new XSSFWorkbook(fs);
                    } else {
                        throw new NotSupportedException();
                    }
                }
            }
            catch(Exception e) {
                Debug.LogError(e.Message);
            }

            GetSheet();
        }

        private void GetSheet() {
            if(workBook == null)
                return;
            if(sheetName == null || sheetName == "") {
                sheet = null;
                return;
            }

            sheet = workBook.GetSheet(sheetName);
        }

        private bool isAvailable {
            get {
                if(sheet == null) return false;

                var data = new Data();

                var keys = data.Keys;

                var cols = new List<string>();
                foreach(var col in sheet.GetRow(0)) {
                    var value = col.ToString();
                    cols.Add(value);
                }

                foreach(var key in keys) {
                    if(!cols.Contains(key))
                        return false;
                }

                return true;
            }
        }

        [ShowIf("isAvailable")]
        [Button("데이터 가져오기", Style = ButtonStyle.Box, ButtonHeight = 50)]

        public void SetDataFromExcel() {
            datas = new List<Data>();

            var keys = new Data().Keys;

            var cols = new Dictionary<string, int>();

            var firstRow = sheet.GetRow(0);
            for(int i = 0; i < firstRow.LastCellNum; i++) {
                var value = firstRow.GetCell(i).ToString();
                cols.Add(value, i);
            }

            for(int i = 1; i <= sheet.LastRowNum; i++) {
                var row = sheet.GetRow(i);
                var json = new JObject();

                foreach(var col in cols) {
                    var value = row.GetCell(col.Value).ToString();
                    json.Add(col.Key, value);
                }

                var data = new Data();
                data.SetFromJson(json);
                datas.Add(data);
            }
        }
#endif

        [LabelText("데이터")]
        [SerializeField]
        private List<Data> datas;

        public List<Data> Datas => datas;
    }
}