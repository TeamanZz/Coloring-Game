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

            SetupBannerList();
        }

        private void SetupBannerList()
        {
            for (int i = 0; i < 5; i++)
            {
                Instantiate(bannerItemPrefab, bannerListContainer.transform).Setup(new Assets.Scripts.Data.BannerData

                {
                    Title = "Test Title!",
                    Description = "Test news banner description.",
                    ImageUrl = "https://xiaomido.ru/wp-content/uploads/2020/09/bez_imeni.png"
                });
            }
        }

        #endregion
    }
}
