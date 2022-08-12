using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.PictureColoring
{
    public class GameScreen : Screen
    {
        #region Inspector Variables

        public static GameScreen Instance;

        [Space]
        [SerializeField] private PictureArea pictureArea = null;
        [SerializeField] private ColorList colorList = null;
        [SerializeField] private GameObject levelLoadingIndicator = null;

        [Space]
        [SerializeField] private CanvasGroup gameplayUI = null;
        [SerializeField] private CanvasGroup levelCompleteUI = null;

        [Space]
        [SerializeField] private GameObject awardedHintTextContainer = null;
        [SerializeField] private GameObject awardedCoinsTextContainer = null;
        [SerializeField] private Text awardedCoinsAmountText = null;

        [Space]
        [SerializeField] private GameObject shareButtonsContainer = null;
        [SerializeField] private CanvasGroup notificationContainer = null;
        [SerializeField] private Text notificationText = null;

        private LevelData activeLevelData;
        private int loadId;
        [SerializeField] private RectTransform pictureContainer = null;
        [SerializeField] private PictureCreator pictureCreator = null;
        [SerializeField] private float containerSize = 0f;

        [Header("Others UI")]
        [Space]
        [SerializeField] private SizeReturner sizeReturner;
        public bool holdSelection;
        #endregion

        #region Public Methods
        public void Awake()
        {
            Instance = this;
        }

        public override void Initialize()
        {
            base.Initialize();

            GameEventManager.Instance.RegisterEventHandler(GameEventManager.LevelLoadingEvent, OnLevelLoading);
            GameEventManager.Instance.RegisterEventHandler(GameEventManager.LevelLoadFinishedEvent, OnLevelLoadFinished);

            pictureArea.Initialize();
            colorList.Initialize();

            pictureArea.OnPixelClicked = OnPixelClicked;
            pictureArea.OnPixelLongPress += OnPixelLongPress;

            colorList.OnColorSelected = OnColorSelected;

            gameplayUI.gameObject.SetActive(true);
            levelCompleteUI.gameObject.SetActive(true);

            shareButtonsContainer.SetActive(NativePlugin.Exists());
        }

        public void SetHoldSelectednOff(bool isOn)
        {
            Debug.Log($"Hold Selection {holdSelection}");
            holdSelection = isOn;
        }

        private void OnPixelLongPress(int x, int y)
        {
            if (!holdSelection)
                return;

            LevelData activeLevelData = GameManager.Instance.ActiveLevelData;

            if (activeLevelData != null)
            {
                var region = GameManager.Instance.GetRegionAt(x, y);

                if (region == null) return;

                var color = region.colorIndex;

                if (color != -1)
                    colorList.SelectColor(color);
            }
        }

        /// <summary>
        /// Invoked when the hint button is clicked
        /// </summary>
        public void OnHintButtonClicked()
        {
            LevelData activeLevelData = GameManager.Instance.ActiveLevelData;

            if (activeLevelData != null)
            {
                // Get a random uncolored region inside the selected color region
                int selectedColoredIndex = colorList.SelectedColorIndex;
                int regionIndex = activeLevelData.GetSmallestUncoloredRegion(selectedColoredIndex);

                // If -1 is returned then all regions have been colored
                if (CurrencyManager.Instance.TrySpend("hints", 1))
                {
                    if (regionIndex != -1)
                    {
                        // Region is not -1 and we successfully spend a currency to use the hint
                        Region region = activeLevelData.LevelFileData.regions[regionIndex];

                        pictureArea.ZoomInOnRegion(region);

                        SoundManager.Instance.Play("hint-used");

                        if (sizeReturner != null)
                            sizeReturner.SaveRectTransform();
                        else
                            Debug.Log($"Size Returner{sizeReturner}");
                    }
                }
                else
                    PopupManager.Instance.Show("store");

                //else
                //{
                //    if (sizeReturner != null)
                //    {
                //        sizeReturner.ClearRectTransform();
                //    }
                //    else
                //        Debug.Log($"Size Returner{sizeReturner}");
                //}
            }
        }

        /// <summary>
        /// Invoked when the Twitter button is clicked
        /// </summary>
        public void OnTwitterButtonClicked()
        {
            LoadShareTexture(ShareToTwitter);
        }

        /// <summary>
        /// Invoked when the Instagram button is clicked
        /// </summary>
        public void OnInstagramButtonClicked()
        {
            LoadShareTexture(ShareToInstagram);
        }

        /// <summary>
        /// Invoked when the Share Other button is clicked
        /// </summary>
        public void OnShareOtherButtonClicked()
        {
            LoadShareTexture(ShareToOther);
        }

        /// <summary>
        /// Invoked when the Save button is clicked
        /// </summary>
        public void OnSaveToDevice()
        {
            LoadShareTexture(SaveShareTextureToDevice);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Invoked by the GameEventManager.LevelLoadingEvent when a level is loading
        /// </summary>
        private void OnLevelLoading(string eventId, object[] data)
        {
            // Show the loading indicator
            levelLoadingIndicator.SetActive(true);

            // Clear and reset the UI
            pictureArea.Clear();
            colorList.Clear();

            ResetUI();
        }

        /// <summary>
        /// Invoked by the GameEventManager.LevelLoadFinishedEvent when the level has finished loading and has all required textures to play the game
        /// </summary>
        private void OnLevelLoadFinished(string eventId, object[] data)
        {
            // Hide the loading indicator
            levelLoadingIndicator.SetActive(false);

            // First argument is a boolean, if true the level loaded successfully
            bool success = (bool)data[0];

            if (success)
            {
                int firstSelectedColor = 0;

                // Setup the picture and the color list items
                pictureArea.Setup(firstSelectedColor);
                colorList.Setup(firstSelectedColor);

                ResetUI();
            }
        }

        /// <summary>
        /// Resets the UI
        /// </summary>
        private void ResetUI()
        {
            gameplayUI.interactable = true;
            gameplayUI.blocksRaycasts = true;
            gameplayUI.alpha = 1f;

            levelCompleteUI.interactable = false;
            levelCompleteUI.blocksRaycasts = false;
            levelCompleteUI.alpha = 0f;

            colorList.ScrollTo(colorList.SelectedColorIndex);
        }

        /// <summary>
        /// Invoked by ColorList when a new color has been selected
        /// </summary>
        private void OnColorSelected(int colorIndex)
        {
            pictureArea.SetSelectedRegion(colorIndex);
        }

        /// <summary>
        /// Invoked by PictureArea when the picture is clicked, x/y is relative to the bottom left corner of the picture
        /// </summary>
        private void OnPixelClicked(int x, int y)
        {
            LevelData activeLevelData = GameManager.Instance.ActiveLevelData;

            if (activeLevelData != null)
            {
                bool levelCompleted;
                bool hintAwarded;
                bool coinsAwarded;

                // Try and color the region which contains the pixel at x,y
                bool regionColored = GameManager.Instance.TryColorRegion(x, y, colorList.SelectedColorIndex, out levelCompleted, out hintAwarded, out coinsAwarded);

                if (regionColored)
                {
                    // Update the color list item if the color region is now complete
                    colorList.CheckCompleted(colorList.SelectedColorIndex);

                    // Notify the picture area that a new region was colored so it can remove the number text for that region
                    pictureArea.NotifyRegionColored();

                    // Check if the level has been completed
                    if (levelCompleted)
                    {
                        // Show the level completed UI
                        LevelCompleted(hintAwarded, coinsAwarded);

                        SoundManager.Instance.Play("level-completed");
                    }
                    else
                    {
                        // Only play the region colored sound if the level wasn't completed so it doesn't play over the completed level sound
                        SoundManager.Instance.Play("region-colored");
                    }
                }
            }
        }

        /// <summary>
        /// Displays the active level as compelted
        /// </summary>
        private void LevelCompleted(bool hintAwarded, bool coinsAwarded)
        {
            activeLevelData = GameManager.Instance.ActiveLevelData;

            if (activeLevelData != null)
            {
                // Tell PictureArea the level is completed
                pictureArea.NotifyLevelCompleted();
                SetThumbnaiImage();
                // Show the reward containers if hints/coins where rewarded
                awardedHintTextContainer.SetActive(hintAwarded);
                awardedCoinsTextContainer.SetActive(coinsAwarded);

                if (coinsAwarded)
                {
                    // Set the amount of coins that where rewarded
                    awardedCoinsAmountText.text = activeLevelData.coinsToAward.ToString();
                }

                // Fade out the gameplay UI and fade in the level completed UI
                UIAnimation anim;

                anim = UIAnimation.Alpha(gameplayUI, 0f, 0.5f);
                anim.style = UIAnimation.Style.EaseOut;
                anim.Play();

                anim = UIAnimation.Alpha(levelCompleteUI, 1f, 0.5f);
                anim.style = UIAnimation.Style.EaseOut;
                anim.Play();

                gameplayUI.interactable = false;
                gameplayUI.blocksRaycasts = false;

                levelCompleteUI.interactable = true;
                levelCompleteUI.blocksRaycasts = true;
            }
        }

        private void SetThumbnaiImage()
        {
            loadId = LoadManager.Instance.LoadLevel(activeLevelData, OnLoadManagerFinished);

            if (loadId == 0)
            {
                // loadId of 0 means the LevelData is already loaded and ready to use
                SetupImages();
            }

        }

        private void OnLoadManagerFinished(LevelData levelData, bool success)
        {
            if (success)
            {
                SetupImages();
            }
        }



        private void SetupImages()
        {
            LevelFileData levelFileData = activeLevelData.LevelFileData;

            float imageWidth = levelFileData.imageWidth;
            float imageHeight = levelFileData.imageHeight;
            float xScale = imageWidth >= imageHeight ? 1f : imageWidth / imageHeight;
            float yScale = imageWidth <= imageHeight ? 1f : imageHeight / imageWidth;

            pictureContainer.sizeDelta = new Vector2(containerSize * xScale, containerSize * yScale);

            float pictureScale = Mathf.Min(pictureContainer.rect.width / imageWidth, pictureContainer.rect.height / imageHeight, 1f);

            pictureCreator.RectT.sizeDelta = new Vector2(imageWidth, imageHeight);
            pictureCreator.RectT.localScale = new Vector3(pictureScale, pictureScale, 1f);

            pictureCreator.Setup(activeLevelData);
        }



        /// <summary>
        /// Loads the share texture and calls onTextureLoaded which it is loaded
        /// </summary>
        private void LoadShareTexture(System.Action<Texture2D> onTextureLoaded)
        {
            ScreenshotManager.Instance.GetShareableTexture(GameManager.Instance.ActiveLevelData, onTextureLoaded);
        }

        /// <summary>
        /// Shares the given texture to the twitter app if it's installed
        /// </summary>
        private void ShareToTwitter(Texture2D texture)
        {
            bool opened = ShareManager.Instance.ShareToTwitter(texture);

            if (!opened)
            {
                ShowNotification("Twitter is not installed");
            }

            Destroy(texture);
        }

        /// <summary>
        /// Shares the given texture to the instagram app if it's installed
        /// </summary>
        private void ShareToInstagram(Texture2D texture)
        {
            bool opened = ShareManager.Instance.ShareToInstagram(texture);

            if (!opened)
            {
                ShowNotification("Instagram is not installed");
            }

            Destroy(texture);
        }

        /// <summary>
        /// Shares the given texture letting the user pick what app to use
        /// </summary>
        private void ShareToOther(Texture2D texture)
        {
            ShareManager.Instance.ShareToOther(texture);

            Destroy(texture);
        }

        /// <summary>
        /// Saves the share texture to device
        /// </summary>
        private void SaveShareTextureToDevice(Texture2D texture)
        {
            ShareManager.Instance.SaveImageToPhotos(texture, OnSaveToPhotosResponse);

            Destroy(texture);
        }

        /// <summary>
        /// Shows the notification
        /// </summary>
        private void ShowNotification(string message)
        {
            // Set the text for the notification
            notificationText.text = message;

            // Fade in the notification
            UIAnimation.Alpha(notificationContainer, 1f, 0.35f).Play();

            // Wait a couple seconds then hide the notification
            StartCoroutine(WaitThenHideNotification());
        }

        /// <summary>
        /// Hides the notification.
        /// </summary>
        private void HideNotification()
        {
            UIAnimation.Alpha(notificationContainer, 0f, 0.35f).Play();
        }

        /// <summary>
        /// Waits 3 seconds then hides notification.
        /// </summary>
        private IEnumerator WaitThenHideNotification()
        {
            yield return new WaitForSeconds(3);

            HideNotification();
        }

        /// <summary>
        /// Invoked when the ShareManager has either save the image to photos or failed to due to permissions not being granted
        /// </summary>
        private void OnSaveToPhotosResponse(bool success)
        {
            if (success)
            {
                ShowNotification("Picture saved to device!");
            }
            else
            {
#if UNITY_IOS
				PopupManager.Instance.Show("permissions", new object[] { "Photos" });
#elif UNITY_ANDROID
                PopupManager.Instance.Show("permissions", new object[] { "Storage" });
#endif
            }
        }

        #endregion
    }
}
