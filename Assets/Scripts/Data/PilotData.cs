using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace GMLM.Data
{
    public class PilotData : IData
    {
        public string id { get; private set; }
        public string name { get; private set; }
        public string description { get; private set; }
        public string image { get; private set; }

        public void SetFromJson(JToken json)
        {
            id = json["id"].ToString();
            name = json["name"].ToString();
            description = json["description"].ToString();
            image = json["image"].ToString();
        }

        public string[] Keys => new string[] { "id", "name", "description", "image" };
    }
}