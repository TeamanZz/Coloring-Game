using System;

namespace Assets.Scripts.Network.Models
{
    [System.Serializable]
    public class Banner
    {
        public int id;
        public string name;
        public string description;
        public string link;
        public int categoryId;
        public string lang;
        public DateTime expires;

        public BannerType Type
        {
            get
            {
                return (BannerType)categoryId;
            }
        }
    }

    public enum BannerType
    {
        Top = 0,
        News = 1
    }
}
