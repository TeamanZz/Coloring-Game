using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames.PictureColoring
{
	/// <summary>
	/// Unitys UI meshes can only handle up to 65000 vertices, if one of the levels has more that that it will require more than one PictureImage component to display
	/// the levels image. This class is responsible for splitting up the regions so that multiple PictureImages are used if the number of vertices exceeds 65000.
	/// </summary>
	public class PictureCreator : MonoBehaviour
	{
		#region Member Variables

		private const int MaxVertsPerPictureImage = 60000;
		
		private bool				isInitialized;
		private List<PictureImage>	pictureImages;
		
		#endregion // Member Variables

		#region Properties
		
		public RectTransform RectT { get { return transform as RectTransform; } }
		
		#endregion // Properties

		#region Public Methods
		
		public void Setup(LevelData levelData, Texture2D selectedRegionImage = null)
		{
			if (!isInitialized)
			{
				Initialize();
			}

			LevelFileData		levelFileData	= levelData.LevelFileData;
			int					vertCount		= 0;
			List<List<Region>>	splitRegions	= new List<List<Region>>();

			splitRegions.Add(new List<Region>());

			// Split the regions 
			for (int i = 0; i < levelFileData.regions.Count; i++)
			{
				Region region = levelFileData.regions[i];

				if (vertCount + region.points.Count > MaxVertsPerPictureImage)
				{
					splitRegions.Add(new List<Region>());
					vertCount = 0;
				}

				splitRegions[splitRegions.Count - 1].Add(region);

				vertCount += region.points.Count;
			}

			int index = 0;
			int regionIndexOffset = 0;

			// Setup and create and PictureImages needed
			for (index = 0; index < splitRegions.Count; index++)
			{
				if (index == pictureImages.Count)
				{
					pictureImages.Add(CreatePictureImage());
				}

				List<Region> regions = splitRegions[index];

				pictureImages[index].texture = null;
				pictureImages[index].Setup(levelData, regions, regionIndexOffset, index == 0);

				regionIndexOffset += regions.Count;
			}

			// If there is a selectedRegionImage then we need to create a PictureImage just for displayed the regions that are selected
			if (selectedRegionImage != null)
			{
				if (index == pictureImages.Count)
				{
					pictureImages.Add(CreatePictureImage());
				}

				pictureImages[index].texture = selectedRegionImage;
				pictureImages[index].Setup(levelData, levelFileData.regions, 0, false, true);

				index++;
			}

			// Clear and disable any PictureImages that are no longer needed (ie. pool them)
			while (index < pictureImages.Count)
			{
				pictureImages[index].Clear();
				pictureImages[index].enabled = false;

				index++;
			}
		}

		public void SetSelectedColor(int colorIndex)
		{
			if (isInitialized)
			{
				for (int i = 0; i < pictureImages.Count; i++)
				{
					pictureImages[i].SetSelectedColor(colorIndex);
				}
			}
		}

		public void Clear()
		{
			if (isInitialized)
			{
				for (int i = 0; i < pictureImages.Count; i++)
				{
					pictureImages[i].Clear();
				}
			}
		}

		public void SetAllDirty()
		{
			if (isInitialized)
			{
				for (int i = 0; i < pictureImages.Count; i++)
				{
					pictureImages[i].SetAllDirty();
				}
			}
		}
		
		#endregion // Public Methods

		#region Private Methods
		
		private void Initialize()
		{
			pictureImages = new List<PictureImage>();

			// Add the first PictureImage
			pictureImages.Add(CreatePictureImage());

			isInitialized = true;
		}

		private PictureImage CreatePictureImage()
		{
			// Create the GameObject
			GameObject		containerObj	= new GameObject("picture_image");
			RectTransform	containerRectT	= containerObj.AddComponent<RectTransform>();

			containerRectT.SetParent(transform, false);

			// Expand to fill
			containerRectT.anchoredPosition	= Vector2.zero;
			containerRectT.anchorMin		= Vector2.zero;
			containerRectT.anchorMax		= Vector2.one;
			containerRectT.offsetMax		= Vector2.zero;
			containerRectT.offsetMin		= Vector2.zero;

			return containerObj.AddComponent<PictureImage>();
		}
		
		#endregion // Private Methods
	}
}
