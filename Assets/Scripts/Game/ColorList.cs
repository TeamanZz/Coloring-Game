using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.PictureColoring
{
    public class ColorList : MonoBehaviour
    {
        #region Inspector Variables

        [SerializeField] private ColorListItem colorListItemPrefab = null;
        [SerializeField] private RectTransform colorListContainer = null;

        #endregion

        #region Member Variables

        private ObjectPool colorListItemPool;
        private ScrollRect scrollRect;
        private List<ColorListItem> colorListItems;

        #endregion

        #region Properties

        public int SelectedColorIndex { get; set; }
        public System.Action<int> OnColorSelected { get; set; }

        #endregion

        #region Public Methods

        public void Initialize()
        {
            colorListItemPool = new ObjectPool(colorListItemPrefab.gameObject, 1, colorListContainer);
            colorListItems = new List<ColorListItem>();
            scrollRect = GetComponent<ScrollRect>();    
        }

        public void Setup(int selectedColorIndex)
        {
            Clear();

            LevelData activeLevelData = GameManager.Instance.ActiveLevelData;

            if (activeLevelData != null)
            {
                // Setup each color list item
                for (int i = 0; i < activeLevelData.LevelFileData.colors.Count; i++)
                {
                    Color color = activeLevelData.LevelFileData.colors[i];
                    ColorListItem colorListItem = colorListItemPool.GetObject<ColorListItem>();

                    colorListItems.Add(colorListItem);

                    colorListItem.Setup(color, i + 1);
                    colorListItem.SetSelected(i == selectedColorIndex);

                    CheckCompleted(i);

                    colorListItem.Index = i;
                    colorListItem.OnListItemClicked = OnColorListItemClicked;
                }
            }

            SelectedColorIndex = selectedColorIndex;
        }

        public void Clear()
        {
            // Clear the list
            colorListItemPool.ReturnAllObjectsToPool();
            colorListItems.Clear();
        }

        /// <summary>
        /// Checks if the color region is completed and if so sets the ColorListItem as completed
        /// </summary>
        public void CheckCompleted(int colorIndex)
        {
            LevelData activeLevelData = GameManager.Instance.ActiveLevelData;

            if (activeLevelData != null && colorIndex < colorListItems.Count && activeLevelData.IsColorComplete(colorIndex))
            {
                colorListItems[colorIndex].SetCompleted();
            }
        }

        public void SelectColor(int index)
        {
            if (index != SelectedColorIndex)
            {
                colorListItems[SelectedColorIndex].SetSelected(false);
                colorListItems[index].SetSelected(true);

                SelectedColorIndex = index;

                scrollRect.ScrollToCenter(colorListItems[SelectedColorIndex].RectT, RectTransform.Axis.Horizontal);

                OnColorSelected(index);
            }
        }

        #endregion

        #region Private Methods

        private void OnColorListItemClicked(int index, object data)
        {
            if (index != SelectedColorIndex)
            {
                // Set the current selected ColorListItem to un-selected and select the new one
                colorListItems[SelectedColorIndex].SetSelected(false);
                colorListItems[index].SetSelected(true);

                SelectedColorIndex = index;


                OnColorSelected(index);
            }
        }

        #endregion
    }
}
