using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.PictureColoring
{
    public class PictureArea : MonoBehaviour
    {
        #region Inspector Variables

        [Header("Picture Settings")]
        [SerializeField] private PictureScrollArea pictureScrollArea = null;
        [SerializeField] private float edgePadding = 0;
        [SerializeField] private float maxScale = 1;
        [SerializeField] private Texture2D regionBackground = null;
        [SerializeField] private float borderSize = 1;
        [SerializeField] private Color borderColor = Color.white;

        [Header("Number Text Settings")]
        [SerializeField] private Font numberTextFont = null;
        [SerializeField] private Color numberTextColor = Color.white;
        [SerializeField] private float maxNumberSize = 100;
        [SerializeField] private float minNumberSize = 10;
        [SerializeField] private float digitSpacing = 0f;
        [SerializeField] private float numberPadding = 0f;

        [Header("Other UI")]
        [SerializeField] private GameObject borderPrefab;
        [SerializeField] private Sprite borderSprite;

        [Space]
        [Range(1, 0)] [SerializeField] private float numberStartAppearing = 0f;
        [Range(1, 0)] [SerializeField] private float numberEndAppearing = 0f;

        #endregion

        #region Member Variables

        private RectTransform pictureContainer;
        private PictureCreator pictureCreator;
        private ColorNumbersText colorNumbersText;

        private Camera canvasCamera;

        #endregion

        #region Properties

        public PixelClickedHandler OnPixelClicked { get; set; }
        public PixelClickedHandler OnPixelLongPress { get; set; }

        #endregion

        #region Delegates

        public delegate void PixelClickedHandler(int pixelX, int pixelY);

        #endregion

        #region Unity Methods

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (numberStartAppearing < numberEndAppearing)
            {
                numberEndAppearing = numberStartAppearing;
            }
        }
#endif

        #endregion

        #region Public Methods

        public void Initialize()
        {
            // Get the camera the Canvas is set to, if the canvas is set to Screen Space Overally this will be null
            canvasCamera = Utilities.GetCanvasCamera(transform);

            CreatePictureObjects();

            pictureScrollArea.OnClick += OnPictureAreaClicked;
            pictureScrollArea.OnLongPress += OnPictureLongPress;
            pictureScrollArea.OnZoom += OnPictureAreaZoomed;
        }

        private void OnPictureLongPress(Vector2 screenPosition)
        {
            Vector2 localPosition;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(pictureContainer, screenPosition, canvasCamera, out localPosition);

            localPosition += pictureContainer.rect.size / 2f;


            if (localPosition.x >= 0 && localPosition.y >= 0 && localPosition.x < pictureContainer.rect.width && localPosition.y < pictureContainer.rect.height)
            {
                OnPixelLongPress((int)localPosition.x, (int)localPosition.y);
            }
        }

        public void Setup(int selectedColorIndex)
        {
            Clear();

            LevelData activeLevelData = GameManager.Instance.ActiveLevelData;

            if (activeLevelData != null)
            {
                ResetPictureContainer();

                // Get the scale to use so the picture fits the container
                float contentWidth = activeLevelData.LevelFileData.imageWidth;
                float contentHeight = activeLevelData.LevelFileData.imageHeight;
                float scale = Mathf.Min(pictureContainer.rect.width / contentWidth, pictureContainer.rect.height / contentHeight, 1f);

                pictureContainer.sizeDelta = new Vector2(contentWidth, contentHeight);
                pictureContainer.localScale = new Vector3(scale, scale, 1f);

                pictureScrollArea.enabled = true;
                pictureScrollArea.CurrentZoom = 1f;
                pictureScrollArea.MaxZoom = maxScale + (scale < 1 ? 1f / scale : -scale);

                SetSelectedRegion(selectedColorIndex);

                colorNumbersText.LevelData = activeLevelData;

                UpdateColorNumbersText();

                colorNumbersText.enabled = true;

                pictureCreator.Setup(activeLevelData, regionBackground);
                pictureCreator.SetSelectedColor(0);

                pictureContainer.gameObject.SetActive(true);
            }
        }

        public void Clear()
        {
            pictureCreator.Clear();

            colorNumbersText.enabled = false;
            pictureScrollArea.DisableTouch = false;

            pictureScrollArea.ResetObj();

            pictureContainer.gameObject.SetActive(false);
        }

        public void SetSelectedRegion(int colorIndex)
        {
            pictureCreator.SetSelectedColor(colorIndex);
        }

        /// <summary>
        /// Zooms in on the given region
        /// </summary>
        public void ZoomInOnRegion(Region region)
        {
            float zoomToX = region.numberX;
            float zoomToY = region.numberY;
            float toScale = pictureScrollArea.MaxZoom;

            float x1 = (zoomToX - pictureContainer.rect.width / 2f) * (pictureContainer.localScale.x * pictureScrollArea.CurrentZoom);
            float y1 = (zoomToY - pictureContainer.rect.height / 2f) * (pictureContainer.localScale.y * pictureScrollArea.CurrentZoom);

            float psaX = pictureScrollArea.Content.anchoredPosition.x;
            float psaY = pictureScrollArea.Content.anchoredPosition.y;

            float x2 = psaX + x1;
            float y2 = psaY + y1;

            float toX = psaX - x2;
            float toY = psaY - y2;

            float posScale = toScale / pictureScrollArea.CurrentZoom;

            pictureScrollArea.ZoomTo(toX * posScale, toY * posScale, toScale);
        }

        public void NotifyRegionColored()
        {
            // If a region was colored we need to set colorNumbersText to dirty so it will remove the number for that region
            colorNumbersText.SetAllDirty();
            pictureCreator.SetAllDirty();
        }

        public void NotifyLevelCompleted()
        {
            pictureScrollArea.ZoomTo(0, 0, pictureScrollArea.MinZoom);

            pictureScrollArea.DisableTouch = true;
        }

        // public Texture2D TakeScreenshot()
        // {
        // 	int width	= Mathf.RoundToInt(pictureContainer.rect.width * pictureContainer.localScale.x);
        // 	int height	= Mathf.RoundToInt(pictureContainer.rect.height * pictureContainer.localScale.y);
        // 	int x		= -width / 2;
        // 	int y		= -height / 2;

        // 	Debug.Log("width: " + width);
        // 	Debug.Log("height: " + height);
        // 	Debug.Log("width: " + width);
        // 	Debug.Log("width: " + width);
        // }

        #endregion

        #region Private Methods

        private void OnPictureAreaClicked(Vector2 screenPosition)
        {
            // Convert the screenPosition to the local position inside pictureContainer
            Vector2 localPosition;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(pictureContainer, screenPosition, canvasCamera, out localPosition);

            // Adjust the position so it is relative to the bottom/left corner 
            localPosition += pictureContainer.rect.size / 2f;

            // Check if the position is within the bounds of the picture
            if (localPosition.x >= 0 && localPosition.y >= 0 && localPosition.x < pictureContainer.rect.width && localPosition.y < pictureContainer.rect.height)
            {
                OnPixelClicked((int)localPosition.x, (int)localPosition.y);
            }
        }

        private void OnPictureAreaZoomed(Vector2 vector2)
        {
            UpdateColorNumbersText();
        }

        /// <summary>
        /// Sets the min size fo numbers that can appear
        /// </summary>
        private void UpdateColorNumbersText()
        {
            float zoomTotal = pictureScrollArea.MaxZoom - pictureScrollArea.MinZoom;
            float begin = zoomTotal * (1f - numberStartAppearing);
            float end = zoomTotal * numberEndAppearing;
            float current = pictureScrollArea.CurrentZoom - pictureScrollArea.MinZoom;

            float t = Mathf.Clamp01((current - begin) / (zoomTotal - begin - end));
            float size = Mathf.Lerp(maxNumberSize, minNumberSize, t);

            colorNumbersText.MinSizeToShow = size;

            colorNumbersText.SetAllDirty();
        }

        /// <summary>
        /// Resets the pictureContainer size and position
        /// </summary>
        private void ResetPictureContainer()
        {
            pictureContainer.anchoredPosition = Vector2.zero;
            pictureContainer.localScale = Vector3.one;
            pictureContainer.sizeDelta = new Vector2(pictureScrollArea.Content.rect.width - edgePadding * 2f,
                                                              pictureScrollArea.Content.rect.height - edgePadding * 2f);
        }

        /// <summary>
        /// Creates the GameObjects and components needed to display a level
        /// </summary>
        private void CreatePictureObjects()
        {
            // Create the main container that will hold all UI elements
            pictureContainer = new GameObject("picture_container").AddComponent<RectTransform>();
            pictureContainer.SetParent(pictureScrollArea.Content, false);

            // Check if we need to add a border
            if (borderSize > 0)
            {
                // Need to add an Image component or the Outline wont show
                Image imageBorder = pictureContainer.gameObject.AddComponent<Image>();
                imageBorder.sprite = borderSprite;
                imageBorder.type = Image.Type.Sliced;

                pictureContainer.gameObject.AddComponent<Mask>();
                GameObject border = Instantiate(borderPrefab, pictureContainer);

                //// Add an outline component
                //Outline outline = pictureContainer.gameObject.AddComponent<Outline>();
                //outline.effectDistance = new Vector2(borderSize, borderSize);
                //outline.effectColor = borderColor;
            }

            // Create the PictureImage component used to display the actual image
            pictureCreator = CreateContainerObj("picture_creator", pictureContainer).AddComponent<PictureCreator>();

            // Create the ColorNumberText component which will handle displaying the numbers
            colorNumbersText = CreateContainerObj("color_numbers_text", pictureContainer).AddComponent<ColorNumbersText>();
            colorNumbersText.text = "0123456789";
            colorNumbersText.font = numberTextFont;
            colorNumbersText.fontSize = 300;
            colorNumbersText.color = numberTextColor;
            colorNumbersText.rectTransform.pivot = Vector2.zero;
            colorNumbersText.horizontalOverflow = HorizontalWrapMode.Overflow;
            colorNumbersText.verticalOverflow = VerticalWrapMode.Overflow;
            colorNumbersText.MaxNumberSize = maxNumberSize;
            colorNumbersText.MinNumberSize = minNumberSize;
            colorNumbersText.LetterSpacing = digitSpacing;
            colorNumbersText.Padding = numberPadding;
        }

        /// <summary>
        /// Creates a GameObject, sets it's parent, then sets the anchors to stretch to fill
        /// </summary>
        private GameObject CreateContainerObj(string containerName, Transform containersParent)
        {
            // Create numbers container
            GameObject containerObj = new GameObject(containerName);
            RectTransform containerRectT = containerObj.AddComponent<RectTransform>();

            containerRectT.SetParent(containersParent, false);

            containerRectT.anchoredPosition = Vector2.zero;
            containerRectT.anchorMin = Vector2.zero;
            containerRectT.anchorMax = Vector2.one;
            containerRectT.offsetMax = Vector2.zero;
            containerRectT.offsetMin = Vector2.zero;

            return containerObj;
        }

        #endregion
    }
}
