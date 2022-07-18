using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames.PictureColoring
{
	#region Main Class

	public class LevelFileData
	{
		public int			imageWidth;
		public int			imageHeight;
		public List<Color>	colors;
		public List<Region>	regions;
	}

	#endregion

	#region Supporting Classes

	public class Region
	{
		public int				id;
		public int				colorIndex;
		public RegionBounds		bounds;
		public int				numberX;
		public int				numberY;
		public int				numberSize;
		public List<Vector2>	points;
		public List<int>		triangles;
	}

	public class RegionBounds
	{
		public int minX;
		public int maxX;
		public int minY;
		public int maxY;

		public RegionBounds(int minX, int minY, int maxX, int maxY)
		{
			this.minX = minX;
			this.minY = minY;
			this.maxX = maxX;
			this.maxY = maxY;
		}

		public int Width	{ get { return maxX - minX + 1; } }
		public int Height	{ get { return maxY - minY + 1; } }
	}

	#endregion
}
