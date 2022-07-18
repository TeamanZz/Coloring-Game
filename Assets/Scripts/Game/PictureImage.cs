using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.PictureColoring
{
	public class PictureImage : RawImage
	{
		#region Member Variables
		
		private LevelData		levelData;
		private List<Region>	regions;
		private int				regionIndexOffset;
		private bool			createBackground;
		private bool			displaySelectedRegions;
		private int				selectedColorIndex;

		#endregion // Member Variables

		#region Unity Methods
		
		#endregion // Unity Methods

		#region Public Methods

		public void Setup(LevelData levelData, List<Region> regions, int regionIndexOffset, bool createBackground = false, bool displaySelectedRegions = false)
		{
			this.levelData 				= levelData;
			this.regions				= regions;
			this.regionIndexOffset		= regionIndexOffset;
			this.createBackground		= createBackground;
			this.displaySelectedRegions	= displaySelectedRegions;
			this.selectedColorIndex		= -1;

			enabled = true;

			SetAllDirty();
		}

		public void SetSelectedColor(int colorIndex)
		{
			selectedColorIndex = colorIndex;

			SetAllDirty();
		}

		public void Clear()
		{
			levelData			= null;
			enabled				= false;
			selectedColorIndex	= -1;

			SetAllDirty();
		}
		
		#endregion // Public Methods

		#region Protected Methods

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();

			if (levelData != null && Application.isPlaying)
			{
				LevelFileData levelFileData = levelData.LevelFileData;
				LevelSaveData levelSaveData = levelData.LevelSaveData;

				float	xPivotOffset	= rectTransform.pivot.x * rectTransform.rect.width;
				float	yPivotOffset	= rectTransform.pivot.y * rectTransform.rect.height;
				Vector2	pivotOffset		= new Vector2(xPivotOffset, yPivotOffset);

				int triangleIndexOffset = 0;

				if (createBackground)
				{
					AddBlackBackground(vh, pivotOffset);

					triangleIndexOffset += 4;
				}

				for (int i = 0; i < regions.Count; i++)
				{
					Region	region				= regions[i];
					bool	isRegionSelected	= region.colorIndex > -1 && selectedColorIndex == region.colorIndex;
					bool	isRegionColored		= levelSaveData.coloredRegions.Contains(i + regionIndexOffset);

					if ((displaySelectedRegions && isRegionSelected && !isRegionColored) || (!displaySelectedRegions && (!isRegionSelected || isRegionColored)))
					{
						Color regionColor = isRegionColored ? levelFileData.colors[region.colorIndex] : Color.white;

						for (int j = 0; j < region.points.Count; j++)
						{
							Vector2 point = region.points[j];

							point.x += region.bounds.minX;
							point.y += region.bounds.minY;

							AddVert(vh, point, regionColor, pivotOffset, !isRegionColored && isRegionSelected);
						}

						for (int j = 0; j < region.triangles.Count; j += 3)
						{
							int index1 = region.triangles[j] + triangleIndexOffset;
							int index2 = region.triangles[j+1] + triangleIndexOffset;
							int index3 = region.triangles[j+2] + triangleIndexOffset;
							
							vh.AddTriangle(index1, index2, index3);
						}

						triangleIndexOffset += region.points.Count;
					}
				}
			}
		}

		private void AddBlackBackground(VertexHelper vh, Vector2 pivotOffset)
		{
			float width		= rectTransform.rect.width;
			float height	= rectTransform.rect.height;

			AddVert(vh, new Vector2(0, 0), Color.black, pivotOffset);
			AddVert(vh, new Vector2(0, height), Color.black, pivotOffset);
			AddVert(vh, new Vector2(width, height), Color.black, pivotOffset);
			AddVert(vh, new Vector2(width, 0), Color.black, pivotOffset);

			vh.AddTriangle(0, 1, 2);
			vh.AddTriangle(0, 2, 3);
		}

		private void AddVert(VertexHelper vh, Vector2 point, Color color, Vector2 pivotOffset, bool setUVs = false)
		{
			vh.AddVert(point - pivotOffset, color, (setUVs && texture != null) ? GetUV(point) : Vector2.zero);
		}

		private Vector2 GetUV(Vector2 point)
		{
			float uvX = point.x / texture.width;
			float uvY = point.y / texture.height;

			return new Vector2(uvX, uvY);
		}

		#endregion // Protected Methods
	}
}
