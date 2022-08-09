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
        [SerializeField] private List<ColorListItem> colorListItems;
        [SerializeField] private List<ColorListItem> copyColorList;

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
                    CustomBucketToggle.Instance.SetToggleBackgroundColor(colorListItems[selectedColorIndex].colorImage.color);

                    if (CheckHideCompleted(i) == false)
                        copyColorList.Add(colorListItem);

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
            copyColorList.Clear();
        }

        public int ConvertIndexGlobalToCopy(int globalIndex)
        {
            int copy = 0;
            for (int i = 0; i < copyColorList.Count; i++)
            {
                if (copyColorList[i] == colorListItems[globalIndex])
                {
                    copy = i;
                    Debug.Log($"Global Index {copy}");
                }
            }
            return copy;
        }

        public int ConvertIndexCopyToGlobal(int copyIndex)
        {
            int global = 0;
            for (int i = 0; i < colorListItems.Count; i++)
            {
                if (colorListItems[i] == copyColorList[copyIndex])
                {
                    global = i;
                    Debug.Log($"Copy Index {global}");
                }
            }
            return global;
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
                int copyIndex = ConvertIndexGlobalToCopy(colorIndex);
                Debug.Log($"Copy Index {copyIndex}");

                int nextIndex = 0;
                if (copyColorList.Count > 1)
                {
                    if (copyIndex == copyColorList.Count - 1)
                        nextIndex = copyIndex - 1;
                    else
                        nextIndex = copyIndex + 1;
                }

                Debug.Log($"Next Index {nextIndex}");
                SelectColor(ConvertIndexCopyToGlobal(nextIndex));

                copyColorList.RemoveAt(copyIndex);
            }
        }


        public bool CheckHideCompleted(int colorIndex)
        {
            LevelData activeLevelData = GameManager.Instance.ActiveLevelData;

            if (activeLevelData != null && colorIndex < colorListItems.Count && activeLevelData.IsColorComplete(colorIndex))
            {
                colorListItems[colorIndex].SetHideCompleted();
                return true;
            }

            return false;
        }

        public void SelectColor(int index)
        {
            if (index != SelectedColorIndex)
            {
                colorListItems[SelectedColorIndex].SetSelected(false);
                colorListItems[index].SetSelected(true);
                CustomBucketToggle.Instance.SetToggleBackgroundColor(colorListItems[index].colorImage.color);

                SelectedColorIndex = index;
                ScrollTo(index);

                OnColorSelected(index);
            }
        }

        public void ScrollTo(int index)
        {
            scrollRect.ScrollToCenter(colorListItems[index].RectT, RectTransform.Axis.Horizontal);
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
                CustomBucketToggle.Instance.SetToggleBackgroundColor(colorListItems[index].colorImage.color);
                Debug.Log(colorListItems[index].colorImage.color);

                SelectedColorIndex = index;


                OnColorSelected(index);
            }
        }

        #endregion
    }
}
