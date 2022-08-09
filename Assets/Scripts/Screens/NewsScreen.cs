using Assets.Scripts.Network.Models;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.PictureColoring
{
    public class NewsScreen : Screen
    {
        #region Inspector Variables

        [SerializeField] private NewsListItem bannerItemPrefab = null;
        [SerializeField] private GridLayoutGroup bannerListContainer = null;
        #endregion

        #region Public Methods

        public override void Initialize()
        {
            base.Initialize();
        }

        private void OnEnable()
        {
            PanelApiManager.Instance.OnBannersRefreshed += SetupBannerList;
        }


        private void SetupBannerList()
        {
           var banners = PanelApiManager.Instance.Banners.Where(b => b.Type == BannerType.News && (DateTime.UtcNow - b.getDate).Days <= b.lifetime).ToArray();

            for (int i = 0; i < banners.Length; i++)
            {
                Instantiate(bannerItemPrefab, bannerListContainer.transform).Setup(banners[i]);
            }
        }

        #endregion
    }
}
