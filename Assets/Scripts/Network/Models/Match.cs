using Newtonsoft.Json;

namespace Assets.Scripts.Network.Models
{
    [System.Serializable]
    public struct Match
    {
        public Match(string geo, string lang)
        {
            Geo = geo;
            Lang = lang;
        }

        [JsonProperty("geo")]
        public string Geo { get; set; }

        [JsonProperty("lang")]
        public string Lang { get; set; }
    }
}
