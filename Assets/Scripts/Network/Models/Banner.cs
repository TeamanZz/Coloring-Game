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
        public int category_id;
        public string lang;
        public int lifetime;
        public DateTime getDate;

        public BannerType Type
        {
            get
            {
                return (BannerType)category_id;
            }
        }
    }

    public enum BannerType
    {
        Top = 0,
        News = 1
    }
}
