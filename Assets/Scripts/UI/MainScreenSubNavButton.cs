using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.PictureColoring
{
    public class MainScreenSubNavButton : MonoBehaviour
    {
        #region Inspector Variables

        [SerializeField] private GameObject activeIcon;
        [SerializeField] private GameObject buttonFade;
        [SerializeField] private Text buttonText = null;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = Color.white;

        #endregion

        #region Unity Methods

        public void SetSelected(bool isSelected)
        {
            Vector3 startScale = Vector3.zero;
            Vector3 endScale = Vector3.one;

            if (!isSelected)
            {
                startScale = Vector3.one;
                endScale = Vector3.zero;
            }

            activeIcon.SetActive(isSelected);
            buttonFade.SetActive(isSelected);
            activeIcon.transform.DOScale(endScale, .5f).From(startScale).SetEase(Ease.InOutCubic);
            buttonFade.transform.DOScale(endScale, .65f).From(startScale).SetEase(Ease.InOutCubic);

            buttonText.color = isSelected ? selectedColor : normalColor;
        }

        #endregion
    }
}
