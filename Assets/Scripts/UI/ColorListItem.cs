using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace BizzyBeeGames.PictureColoring
{
    public class ColorListItem : ClickableListItem
    {
        #region Inspector Variables

        public Image colorImage = null;
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

            transform.localScale = Vector3.one;
        }

        public void SetSelected(bool isSelected)
        {
            selectedObj.SetActive(isSelected);

            if (isSelected)
                colorImage.rectTransform.sizeDelta = selectedSize;
            else
                colorImage.rectTransform.sizeDelta = diselectedSize;
        }
        public void SetHideCompleted()
        {
            //Debug.Log("Hide Diselected");
            numberText.enabled = false;
            completedObj.SetActive(true);

            gameObject.SetActive(false);
            transform.localScale = Vector3.zero;
        }

        public void SetCompleted()
        {
            numberText.enabled = false;
            completedObj.SetActive(true);
            StartCoroutine(HideObject());
        }

        public IEnumerator HideObject()
        {
            transform.DOScale(0, 0.45f).SetEase(Ease.InBack);
            yield return new WaitForSeconds(0.6f);
            gameObject.SetActive(false);
        }

        #endregion
    }
}
