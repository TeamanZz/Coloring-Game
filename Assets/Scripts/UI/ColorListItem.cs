using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.PictureColoring
{
    public class ColorListItem : ClickableListItem
    {
        #region Inspector Variables

        [SerializeField] private Image colorImage = null;
        [SerializeField] private Text numberText = null;
        [SerializeField] private GameObject completedObj = null;
        [SerializeField] private GameObject selectedObj = null;

        [SerializeField] private Vector2 diselectedSize;
        [SerializeField] private Vector2 selectedSize;

        #endregion

        #region Public Methods

        public void Setup(Color color, int number)
        {
            colorImage.color = color;
            numberText.text = number.ToString();

            numberText.enabled = true;

            selectedObj.SetActive(false);
            completedObj.SetActive(false);
            colorImage.rectTransform.sizeDelta = diselectedSize;
        }

        public void SetSelected(bool isSelected)
        {
            Debug.Log("Selected");
            selectedObj.SetActive(isSelected);

            if (isSelected)
            {
                colorImage.rectTransform.sizeDelta = selectedSize;
                //if (GameManager.gameManager != null)
                //    GameManager.gameManager.CategoriesTitlePositioning();
            }
            else
                colorImage.rectTransform.sizeDelta = diselectedSize;
        }

        public void SetCompleted()
        {
            Debug.Log("Diselected");
            numberText.enabled = false;
            completedObj.SetActive(true);
        }

        #endregion
    }
}
