using Assets.Scripts.Network.Models;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.PictureColoring
{
    public class NewsScreen : Screen
    {
        #region Inspector Variables

        [SerializeField] private BannerListItem bannerItemPrefab = null;
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
            for (int i = 0; i < bannerListContainer.transform.childCount; i++)
            {
                Destroy(bannerListContainer.transform.GetChild(i).gameObject);
            }

            var banners = PanelApiManager.Instance.Banners.Where(b => b.Type == BannerType.News).ToArray();

            for (int i = 0; i < banners.Length; i++)
            {
                Instantiate(bannerItemPrefab, bannerListContainer.transform).Setup(banners[i]);
            }
        }

        #endregion
    }
}
