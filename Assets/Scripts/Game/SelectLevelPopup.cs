using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.PictureColoring
{
	public class SelectLevelPopup : Popup
	{
		#region Inspector Variables

		[Space]

		[SerializeField] private RectTransform	pictureContainer	= null;
		[SerializeField] private PictureCreator	pictureCreator		= null;
		[SerializeField] private GameObject		loadingIndicator	= null;
		[SerializeField] private float			containerSize		= 0f;
		[Space]
		[SerializeField] private GameObject	continueButton		= null;
		[SerializeField] private GameObject	deleteButton		= null;
		[SerializeField] private GameObject	restartButton		= null;
		[SerializeField] private GameObject	unlockButton		= null;
		[SerializeField] private Text		unlockAmountText	= null;

		#endregion

		#region Member Variables

		private LevelData	levelData;
		private int			loadId;
		private bool		isLoading;

		#endregion

		#region Public Methods

		public override void OnShowing(object[] inData)
		{
			base.OnShowing(inData);

			levelData = inData[0] as LevelData;

			bool isLocked = (bool)inData[1];

			bool isCompleted	= !isLocked && levelData.LevelSaveData.isCompleted;
			bool isPlaying		= !isLocked && !isCompleted && GameManager.Instance.IsLevelPlaying(levelData.Id);

			continueButton.SetActive(isPlaying);
			deleteButton.SetActive(isPlaying || isCompleted);
			restartButton.SetActive(isPlaying || isCompleted);
			unlockButton.SetActive(isLocked);

			if (isLocked)
			{
				unlockAmountText.text = levelData.coinsToUnlock.ToString();
			}

			SetThumbnaiImage();
		}

		public override void OnHiding(bool cancelled)
		{
			base.OnHiding(cancelled);

			if (isLoading)
			{
				isLoading = false;

				LoadManager.Instance.Cancel(levelData.Id, loadId);
			}
		}

		#endregion

		#region Private Methods

		private void SetThumbnaiImage()
		{
			loadId = LoadManager.Instance.LoadLevel(levelData, OnLoadManagerFinished);

			if (loadId == 0)
			{
				// loadId of 0 means the LevelData is already loaded and ready to use
				SetupImages();

				loadingIndicator.SetActive(false);
			}
			else
			{
				// LevelLoadManager is loading the data needed to display the thumbnail
				isLoading = true;

				loadingIndicator.SetActive(true);
			}
		}

		private void OnLoadManagerFinished(LevelData levelData, bool success)
		{
			isLoading = false;

			loadingIndicator.SetActive(false);

			if (success)
			{
				SetupImages();
			}
		}

		private void SetupImages()
		{
			LevelFileData levelFileData = levelData.LevelFileData;

			float imageWidth	= levelFileData.imageWidth;
			float imageHeight	= levelFileData.imageHeight;
			float xScale		= imageWidth >= imageHeight ? 1f : imageWidth / imageHeight;
			float yScale		= imageWidth <= imageHeight ? 1f : imageHeight / imageWidth;

			pictureContainer.sizeDelta = new Vector2(containerSize * xScale, containerSize * yScale);

			float pictureScale = Mathf.Min(pictureContainer.rect.width / imageWidth, pictureContainer.rect.height / imageHeight, 1f);

			pictureCreator.RectT.sizeDelta	= new Vector2(imageWidth, imageHeight);
			pictureCreator.RectT.localScale	= new Vector3(pictureScale, pictureScale, 1f);

			pictureCreator.Setup(levelData);
		}

		#endregion
	}
}
