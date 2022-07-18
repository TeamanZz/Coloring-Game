using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames.PictureColoring
{
	public class ScreenshotManager : SingletonComponent<ScreenshotManager>
	{
		#region Inspector Variables
		
		[SerializeField] private Camera			screenshotCamera	= null;
		[SerializeField] private Canvas			screenshotCanvas	= null;
		[SerializeField] private PictureCreator	pictureCreator		= null;
		
		#endregion // Inspector Variables

		#region Member Variables
		
		private RenderTexture renderTexture;
		private Rect readRect;
		private System.Action<Texture2D> callback;
		
		#endregion // Member Variables

		#region Unity Methods
		
		private void Start()
		{
			renderTexture = new RenderTexture(UnityEngine.Screen.width, UnityEngine.Screen.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
		}
		
		#endregion // Unity Methods

		#region Public Methods

		public void GetShareableTexture(LevelData levelData, System.Action<Texture2D> callback)
		{
			this.callback = callback;

			// Set the size and scale of the pictureCreator so it expands to fit the screen
			float containerWidth	= (pictureCreator.transform.parent as RectTransform).rect.width;
			float containerHeight	= (pictureCreator.transform.parent as RectTransform).rect.height;
			float contentWidth		= levelData.LevelFileData.imageWidth;
			float contentHeight		= levelData.LevelFileData.imageHeight;
			float scale				= Mathf.Min(containerWidth / contentWidth, containerHeight / contentHeight, 1f);

			pictureCreator.RectT.sizeDelta	= new Vector2(contentWidth, contentHeight);
			pictureCreator.RectT.localScale	= new Vector3(scale, scale, 1f);

			// Get the read position/size in screen space
			int readWidth	= Mathf.RoundToInt(contentWidth * scale * screenshotCanvas.scaleFactor);
			int readHeight	= Mathf.RoundToInt(contentHeight * scale * screenshotCanvas.scaleFactor);
			int readX		= Mathf.RoundToInt((UnityEngine.Screen.width - readWidth) / 2f);
			int readY		= Mathf.RoundToInt((UnityEngine.Screen.height - readHeight) / 2f);

			readRect = new Rect(readX, readY, readWidth, readHeight);

			// Setup the PictureCreator so display the image
			pictureCreator.Setup(levelData);

			StartCoroutine(TakeScreenshot());
		}
		
		#endregion // Public Methods

		#region Private Methods
		
		/// <summary>
		/// Renders the screenshotCamera and captures it's pixels
		/// </summary>
		private IEnumerator TakeScreenshot()
		{
			yield return new WaitForEndOfFrame();

			screenshotCamera.targetTexture = renderTexture;
			screenshotCamera.Render();

			RenderTexture curTexture = RenderTexture.active;

			RenderTexture.active = renderTexture;

			// Read the pixels of the now active render texture
			Texture2D texture = new Texture2D((int)readRect.width, (int)readRect.height, TextureFormat.RGB24, false);
			texture.ReadPixels(readRect, 0, 0);
			texture.Apply();

			// Clear everything
			RenderTexture.active = curTexture;
			screenshotCamera.targetTexture = null;
			pictureCreator.Clear();

			callback(texture);
		}
		
		#endregion // Private Methods
	}
}
