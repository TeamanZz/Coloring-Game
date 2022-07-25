using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.PictureColoring
{
    public class LevelListItem : RecyclableListItem<LevelData>
    {
        #region Inspector Variables

        [SerializeField] private PictureCreator pictureCreator = null;
        [SerializeField] private GameObject loadingIndicator = null;
        [SerializeField] private GameObject completedIndicator = null;
        [SerializeField] private GameObject playedIndicator = null;
        [SerializeField] private GameObject lockedIndicator = null;
        [SerializeField] private GameObject coinCostContainer = null;
        [SerializeField] private Text coinCostText = null;

        #endregion

        #region Member Variables

        private string levelId;
        private int loadId;
        private bool isLoading;

        #endregion

        #region Public Methods

        public override void Initialize(LevelData dataObject)
        {
        }

        public override void Removed()
        {
            if (isLoading)
            {
                LoadManager.Instance.Cancel(levelId, loadId);

                isLoading = false;

                pictureCreator.Clear();
            }
        }

        public override void Setup(LevelData levelData)
        {
            UpdateUI(levelData);

            levelId = levelData.Id;
            loadId = LoadManager.Instance.LoadLevel(levelData, OnLoadManagerFinished);

            if (loadId == 0)
            {
                // loadId of 0 means the LevelData is already loaded and ready to use
                SetImages(levelData);

                loadingIndicator.SetActive(false);
            }
            else
            {
                // Hide the images while the level is loading
                pictureCreator.Clear();

                // LevelLoadManager is loading the data needed to display the thumbnail
                isLoading = true;

                loadingIndicator.SetActive(true);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Invoked when the LevelLoadManager finishes loading everything needed to display the levels thumbnail
        /// </summary>
        private void OnLoadManagerFinished(LevelData levelData, bool success)
        {
            isLoading = false;

            loadingIndicator.SetActive(false);

            if (success)
            {
                SetImages(levelData);
            }
        }

        /// <summary>
        /// Sets the images
        /// </summary>
        private void SetImages(LevelData levelData)
        {
            LevelFileData levelFileData = levelData.LevelFileData;

            float containerWidth = (pictureCreator.transform.parent as RectTransform).rect.width;
            float containerHeight = (pictureCreator.transform.parent as RectTransform).rect.height;
            float contentWidth = levelFileData.imageWidth;
            float contentHeight = levelFileData.imageHeight;
            float scale = Mathf.Min(containerWidth / contentWidth, containerHeight / contentHeight, 1f);

            pictureCreator.RectT.sizeDelta = new Vector2(contentWidth, contentHeight);
            pictureCreator.RectT.localScale = new Vector3(scale, scale, 1f);

            pictureCreator.Setup(levelData);
        }

        /// <summary>
        /// Updates the UI of the list item
        /// </summary>
        private void UpdateUI(LevelData levelData)
        {
            bool isLocked = levelData.locked && !levelData.LevelSaveData.isUnlocked;
            bool isPlaying = GameManager.Instance.IsLevelPlaying(levelData.Id);
            bool isCompleted = levelData.LevelSaveData.isCompleted;

            completedIndicator.SetActive(isCompleted);
            playedIndicator.SetActive(!isCompleted && isPlaying);
            lockedIndicator.SetActive(isLocked);
            coinCostContainer.SetActive(isLocked);

            coinCostText.text = levelData.coinsToUnlock.ToString();
        }

        #endregion
    }
}
