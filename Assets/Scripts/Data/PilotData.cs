using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using GMLM.Game;

namespace GMLM.Data
{
    public class PilotData : IData
    {
        public string id { get; private set; }
        public string name { get; private set; }
        public string description { get; private set; }
        public string image { get; private set; }
        public CombatStyle combatStyle { get; private set; }

        public void SetFromJson(JToken json)
        {
            id = json["id"].ToString();
            name = json["name"].ToString();
            description = json["description"].ToString();
            image = json["image"].ToString();
            
            // combatStyle 파싱 (기본값: Ranged)
            string styleStr = json["combatStyle"]?.ToString() ?? "Ranged";
            if (System.Enum.TryParse<CombatStyle>(styleStr, true, out CombatStyle style))
            {
                combatStyle = style;
            }
            else
            {
                combatStyle = CombatStyle.Ranged; // 파싱 실패 시 기본값
            }
        }

        public string[] Keys => new string[] { "id", "name", "description", "image", "combatStyle" };
    }
}