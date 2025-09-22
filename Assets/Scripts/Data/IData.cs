using Newtonsoft.Json.Linq;

namespace GMLM.Data {
    public interface IData {
        public void SetFromJson(JToken json);
        public string[] Keys { get; }
    }
}