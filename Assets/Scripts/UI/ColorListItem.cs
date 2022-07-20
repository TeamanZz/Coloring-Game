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

        #endregion

        #region Public Methods

        public void Setup(Color color, int number)
        {
            colorImage.color = color;
            numberText.text = number.ToString();

            numberText.enabled = true;

            selectedObj.SetActive(false);
            completedObj.SetActive(false);
        }

        public void SetSelected(bool isSelected)
        {
            selectedObj.SetActive(isSelected);
        }

        public void SetCompleted()
        {
            numberText.enabled = false;
            completedObj.SetActive(true);
        }

        #endregion
    }
}
