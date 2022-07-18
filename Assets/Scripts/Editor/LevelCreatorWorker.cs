using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace BizzyBeeGames.PictureColoring
{
	public class LevelCreatorWorker : Worker
	{
		#region Classes

		public class Settings
		{
			public Color[]		lineTexturePixels;
			public Color[]		colorTexturePixels;
			public Vector2		imageSize;
			public float		lineThreshold;
			public bool			ignoreWhiteRegions;
			public float		regionSizeThreshold;
			public float		colorMergeThreshold;
			public string		outPath;
		}

		private class LineImage
		{
			public int					width;
			public int					height;
			public List<List<Pixel>>	pixels;

			public Pixel GetPixel(int x, int y)
			{
				return (x < 0 || y < 0 || x >= width || y >= height) ? null : pixels[x][y];
			}

			public int GetLineAlpha(int x, int y)
			{
				return (x < 0 || y < 0 || x >= width || y >= height) ? 0 : pixels[x][y].alpha;
			}

			public void SetAlpha(int x, int y, int alpha)
			{
				if (!(x < 0 || y < 0 || x >= width || y >= height)) pixels[x][y].alpha = alpha;
			}
		}

		public class Pixel
		{
			public int		x;
			public int		y;
			public int		alpha;
			public bool		marked;
		}

		public class Region
		{
			public int					colorIndex;
			public int					width;
			public int					height;
			public int					minX;
			public int					minY;
			public List<List<Pixel>>	pixels;
			public int[]				bounds;
		}
		
		private class Cell
		{
			public int			x;
			public int			y;
			public bool			containsPixel;
			public CellLocation	location;
			public ulong		marker;
			public int			insideAreaIndex;
		}

		public class AlgoProgress
		{
			public enum Step
			{
				LoadingTextures,
				GatheringRegions,
				GetPoints,
				Triangulation
			}

			public int	totalRegions;
			public int	curRegion;
			public Step	regionStep;
			public int	totalPoints;
			public int	numPointsLeft;

			public string	batchModeFilename;
			public int		curBatchFile;
		}
		
		#endregion // Classes

		#region Enums
		
		public enum CellLocation
		{
			Unknown,
			Outside,
			Inside
		}

		private enum Corner
		{
			NON, OTL, OTR, OBL, OBR, ITL, ITR, IBL, IBR
		}

		#endregion // Enums

		#region Member Variables
		
		public Settings			settings;
		private bool			batchMode;
		private int				curBatchIndex;
		private List<string>	coloredFiles;
		private List<string>	lineFiles;

		private readonly object	batchOperationsLock = new object();
		private bool			batchNeedImagePixels;
		private string			batchLineFilePath;
		private string			batchColoredFilePath;
		private Color[]			batchLineTexturePixels;
		private Color[]			batchColorTexturePixels;
		private Vector2			batchImageSize;
		private string			bacthLoadTextureError;

		private readonly object	algoProgressLock = new object();
		private ulong			currentMarker;
		private AlgoProgress 	algoProgress;

		#endregion // Member Variables

		#region Properties
		
		public bool BatchNeedImagePixels
		{
			get { lock (batchOperationsLock) return batchNeedImagePixels; }
			set { lock (batchOperationsLock) batchNeedImagePixels = value; }
		}
		
		public string BatchLineFilePath
		{
			get { lock (batchOperationsLock) return batchLineFilePath; }
			set { lock (batchOperationsLock) batchLineFilePath = value; }
		}
		
		public string BatchColoredFilePath
		{
			get { lock (batchOperationsLock) return batchColoredFilePath; }
			set { lock (batchOperationsLock) batchColoredFilePath = value; }
		}
		
		public Color[] BatchLineTexturePixels
		{
			get { lock (batchOperationsLock) return batchLineTexturePixels; }
			set { lock (batchOperationsLock) batchLineTexturePixels = value; }
		}
		
		public Color[] BatchColorTexturePixels
		{
			get { lock (batchOperationsLock) return batchColorTexturePixels; }
			set { lock (batchOperationsLock) batchColorTexturePixels = value; }
		}
		
		public Vector2 BatchImageSize
		{
			get { lock (batchOperationsLock) return batchImageSize; }
			set { lock (batchOperationsLock) batchImageSize = value; }
		}
		
		public string BatchLoadTextureError
		{
			get { lock (batchOperationsLock) return bacthLoadTextureError; }
			set { lock (batchOperationsLock) bacthLoadTextureError = value; }
		}
		
		public int ProgressTotalRegions
		{
			get { lock (algoProgressLock) return algoProgress.totalRegions; }
			set { lock (algoProgressLock) algoProgress.totalRegions = value; }
		}

		public int ProgressCurrentRegion
		{
			get { lock (algoProgressLock) return algoProgress.curRegion; }
			set { lock (algoProgressLock) algoProgress.curRegion = value; }
		}

		public AlgoProgress.Step ProgressStep
		{
			get { lock (algoProgressLock) return algoProgress.regionStep; }
			set { lock (algoProgressLock) algoProgress.regionStep = value; }
		}

		public int ProgressTotalPoints
		{
			get { lock (algoProgressLock) return algoProgress.totalPoints; }
			set { lock (algoProgressLock) algoProgress.totalPoints = value; }
		}

		public int ProgressPointsLeft
		{
			get { lock (algoProgressLock) return algoProgress.numPointsLeft; }
			set { lock (algoProgressLock) algoProgress.numPointsLeft = value; }
		}

		public string ProgressBatchFilename
		{
			get { lock (algoProgressLock) return algoProgress.batchModeFilename; }
			set { lock (algoProgressLock) algoProgress.batchModeFilename = value; }
		}

		public int ProgressCurBatchFile
		{
			get { lock (algoProgressLock) return algoProgress.curBatchFile; }
			set { lock (algoProgressLock) algoProgress.curBatchFile = value; }
		}

		#endregion // Properties

		public LevelCreatorWorker(Settings settings)
		{
			this.settings		= settings;
			this.algoProgress	= new AlgoProgress();
		}

		public LevelCreatorWorker(Settings settings, List<string> coloredFiles, List<string> lineFiles)
		{
			this.settings		= settings;
			this.batchMode		= true;
			this.coloredFiles	= coloredFiles;
			this.lineFiles		= lineFiles;
			this.algoProgress	= new AlgoProgress();
		}

		protected override void Begin()
		{

		}

		protected override void DoWork()
		{
			if (batchMode)
			{
				// We need to load the png into a Texture2D and get the pixels since thats the only way we can get the images pixels in a readable format.
				// However that can only be done on the main thread but we don't want to load all pngs at once before the algo starts because if there are
				// alot of images it could freeze Unity for a while not to mention the amount of memory it could take. So we set some properties which
				// signal the LevelCreatorWindow on the main thread to load the ong and set the corresponding pixels.
				BatchLineFilePath		= lineFiles[curBatchIndex];
				BatchColoredFilePath	= coloredFiles[curBatchIndex];
				BatchNeedImagePixels	= true;

				ProgressStep			= AlgoProgress.Step.LoadingTextures;
				ProgressBatchFilename	= System.IO.Path.GetFileNameWithoutExtension(BatchColoredFilePath);
				ProgressCurBatchFile	= curBatchIndex;

				// Wait for the main thread to set BatchNeedImagePixels back to false, that is when we know it has set the pixels and image size
				while (BatchNeedImagePixels)
				{
					System.Threading.Thread.Sleep(100);
				}

				string loadError = BatchLoadTextureError;

				if (!string.IsNullOrEmpty(loadError))
				{
					Debug.LogError(loadError);
					curBatchIndex++;
					return;
				}

				// Set the values in settings
				settings.lineTexturePixels	= BatchLineTexturePixels;
				settings.colorTexturePixels	= BatchColorTexturePixels;
				settings.imageSize			= BatchImageSize;
			}

			List<Region>	regions;
			List<Color>		colors;

			ProgressStep = AlgoProgress.Step.GatheringRegions;

			ProcessTextures(out regions, out colors);

			ProgressTotalRegions = regions.Count;

			List<List<Vector2>>	allRegionPoints;
			List<List<int>>		allRegionTriangles;

			ProcessRegions(regions, out allRegionPoints, out allRegionTriangles);

			WriteLevelFiles(regions, allRegionPoints, allRegionTriangles, colors);

			if (!batchMode || curBatchIndex == coloredFiles.Count)
			{
				Stop();
				return;
			}

			curBatchIndex++;
		}
		
		/// <summary>
		/// Processes the line and color textures, splitting the images pixels into regions seperated by line pixels and getting the colors of all the regions
		/// </summary>
		private void ProcessTextures(out List<Region> regions, out List<Color> colors)
		{
			regions		= new List<Region>();
			colors		= new List<Color>();

			LineImage lineImage	= CreateLineImage();

			for (int x = 0; x < lineImage.width; x++)
			{
				for (int y = 0; y < lineImage.height; y++)
				{
					Pixel pixel = lineImage.GetPixel(x, y);

					// Check if the pixel has been marked
					if (!pixel.marked && !IsLinePixel(pixel.alpha))
					{
						List<Pixel> pixelsInRegion = new List<Pixel>();

						// Get the regio of pixels
						int numTransparent = GetPixelsInRegion(pixel, lineImage, pixelsInRegion);

						// Check if the region is big enough to be consider part of the final picture
						if (numTransparent < settings.regionSizeThreshold)
						{
							// Region is to small
							continue;
						}

						Region	region		= CreateRegion(pixelsInRegion);
						Color	regionColor	= GetRegionColor(region);

						bool noColorRegion = (regionColor.a < 1 || (settings.ignoreWhiteRegions && regionColor == Color.white));

						region.colorIndex = noColorRegion ? -1 : GetColorIndex(colors, regionColor);

						regions.Add(region);
					}
				}
			}
		}

		private LineImage CreateLineImage()
		{
			LineImage lineImage = new LineImage();

			lineImage.width		= (int)settings.imageSize.x;
			lineImage.height	= (int)settings.imageSize.y;
			lineImage.pixels	= new List<List<Pixel>>();

			for (int x = 0; x < lineImage.width; x++)
			{
				lineImage.pixels.Add(new List<Pixel>());

				for (int y = 0; y < lineImage.height; y++)
				{
					Color color = GetPixel(x, y, settings.lineTexturePixels, lineImage.width);
					Pixel pixel = new Pixel();

					pixel.x		= x;
					pixel.y		= y;

					if (color.a < 1)
					{
						pixel.alpha = Mathf.RoundToInt(color.a * 255f);
					}
					else
					{
						pixel.alpha	= 255 - Mathf.RoundToInt(color.grayscale * 255f);
					}

					lineImage.pixels[x].Add(pixel);
				}
			}

			return lineImage;
		}

		private Color GetPixel(int x, int y, Color[] pixels, int width)
		{
			return pixels[x + y * width];
		}

		private int GetPixelsInRegion(Pixel startPixel, LineImage workingImage, List<Pixel> pixelsInRegion)
		{
			int numTransparent = 0;

			Stack<Pixel> pixelsToCheck = new Stack<Pixel>();

			pixelsToCheck.Push(startPixel);

			while (pixelsToCheck.Count > 0)
			{
				Pixel pixel = pixelsToCheck.Pop();

				if (pixel != null && !pixel.marked)
				{
					pixel.marked = true;

					if (!IsLinePixel(pixel.alpha))
					{
						pixelsInRegion.Add(pixel);

						if (pixel.alpha == 0)
						{
							numTransparent++;
						}

						pixelsToCheck.Push(workingImage.GetPixel(pixel.x + 1, pixel.y));
						pixelsToCheck.Push(workingImage.GetPixel(pixel.x - 1, pixel.y));
						pixelsToCheck.Push(workingImage.GetPixel(pixel.x, pixel.y + 1));
						pixelsToCheck.Push(workingImage.GetPixel(pixel.x, pixel.y - 1));
					}
				}
			}

			return numTransparent;
		}

		private bool IsLinePixel(int alpha)
		{
			return alpha >= settings.lineThreshold;
		}

		private Region CreateRegion(List<Pixel> pixelsInRegion)
		{
			Region region = new Region();

			region.pixels = new List<List<Pixel>>();

			int minX = int.MaxValue;
			int maxX = int.MinValue;
			int minY = int.MaxValue;
			int maxY = int.MinValue;

			// Get the min/max x and y for the pixels in the region
			for (int i = 0; i < pixelsInRegion.Count; i++)
			{
				Pixel pixel = pixelsInRegion[i];

				minX = Mathf.Min(minX, pixel.x);
				maxX = Mathf.Max(maxX, pixel.x);
				minY = Mathf.Min(minY, pixel.y);
				maxY = Mathf.Max(maxY, pixel.y);
			}

			int regionWidth		= maxX - minX + 1;
			int regionHeight	= maxY - minY + 1;

			// Create the new region pixels matrix
			for (int x = 0; x < regionWidth; x++)
			{
				region.pixels.Add(new List<Pixel>());

				for (int y = 0; y < regionHeight; y++)
				{
					region.pixels[x].Add(null);
				}
			}

			// Add all the pixels to the matrix in their proper location
			for (int i = 0; i < pixelsInRegion.Count; i++)
			{
				Pixel pixel = pixelsInRegion[i];

				int regionX = pixel.x - minX;
				int regionY = pixel.y - minY;

				region.pixels[regionX][regionY] = pixel;
			}

			region.width	= regionWidth;
			region.height	= regionHeight;
			region.minX		= minX;
			region.minY		= minY;
			region.bounds	= FindNumberLocation(region.pixels, regionWidth, regionHeight);

			return region;
		}

		private int[] FindNumberLocation(List<List<Pixel>> regionPixels, int regionWidth, int regionHeight)
		{
			int[] histogram = new int[regionWidth];

			int[] maxAreaBounds = new int[4];

			for (int row = 0; row < regionHeight; row++)
			{
				// Update heights in histogram
				for (int col = 0; col < regionWidth; col++)
				{
					Pixel	pixel = regionPixels[col][row];
					bool	isOne = pixel != null && !IsLinePixel(pixel.alpha);

					histogram[col] = isOne ? histogram[col] + 1 : 0;
				}

				// Calculate new largest rectangle using histogram
				Stack<int>	stack	= new Stack<int>(); 
				int			i		= 0; 

				while (i < regionWidth) 
				{ 
					if (stack.Count == 0 || histogram[stack.Peek()] <= histogram[i]) 
					{ 
						stack.Push(i++); 
					} 
					else
					{ 
						int topIndex = stack.Pop();

						int[] areaBounds =
						{
							(stack.Count == 0) ? 0 : stack.Peek() + 1,
							row - histogram[topIndex] + 1,
							i - 1,
							row
						};

						maxAreaBounds = MaxBounds(maxAreaBounds, areaBounds);
					} 
				} 

				while (stack.Count > 0) 
				{ 
					int topIndex = stack.Pop();

					int[] areaBounds =
					{
						(stack.Count == 0) ? 0 : stack.Peek() + 1,
						row - histogram[topIndex] + 1,
						i - 1,
						row
					};

					maxAreaBounds = MaxBounds(maxAreaBounds, areaBounds);
				}
			}

			return maxAreaBounds;
		}

		private int[] MaxBounds(int[] maxBounds, int[] newBounds)
		{
			return Mathf.Min(newBounds[2] - newBounds[0], newBounds[3] - newBounds[1]) > Mathf.Min(maxBounds[2] - maxBounds[0], maxBounds[3] - maxBounds[1])
				        ? newBounds
					    : maxBounds;
		}

		private Color GetRegionColor(Region region)
		{
			int numAreaWidth	= (region.bounds[2] - region.bounds[0]);
			int numAreaHeight	= (region.bounds[3] - region.bounds[1]);

			// Get the color of the region
			int middleX = region.minX + region.bounds[0] + Mathf.FloorToInt(numAreaWidth / 2f);
			int middleY = region.minY + region.bounds[1] + Mathf.FloorToInt(numAreaHeight / 2f);

			return GetPixel(middleX, middleY, settings.colorTexturePixels, (int)settings.imageSize.x);
		}

		private int GetColorIndex(List<Color> colors, Color newColor)
		{
			if (colors.Count == 0)
			{
				colors.Add(newColor);

				return 0;
			}

			float colorDiff;

			int colorIndex = GetClosestColor(newColor, colors, out colorDiff);

			if (colorDiff <= settings.colorMergeThreshold)
			{
				return colorIndex;
			}

			colors.Add(newColor);

			return colors.Count - 1;
		}
        
        /// <summary>
        /// Gets the PaletteItem that is closest to the given PaletteItem
        /// </summary>
		public int GetClosestColor(Color toColor, List<Color> colors, out float diff)
        {
			int		closestColorIndex	= 0;
            float	minDiff				= float.MaxValue;

            for (int i = 0; i < colors.Count; i++)
            {
				Color color = colors[i];

                if (toColor == color)
                {
                    diff = 0f;

                    return i;
                }

				float colorDiff = ColorUtils.GetColorDiff(toColor, color);

                if (colorDiff < minDiff)
                {
					closestColorIndex	= i;
                    minDiff				= colorDiff;
                }
            }

            diff = minDiff;

			return closestColorIndex;
        }

		private void ProcessRegions(List<Region> regions, out List<List<Vector2>> allRegionPoints, out List<List<int>> allRegionTriangles)
		{
			allRegionPoints		= new List<List<Vector2>>();
			allRegionTriangles	= new List<List<int>>();

			for (int r = 0; r < regions.Count; r++)
			{
				if (Stopping) return;

				ProgressCurrentRegion	= r;
				ProgressStep			= AlgoProgress.Step.GetPoints;

				Region				region	= regions[r];
				List<List<Cell>>	cells	= ConverRegionPixelsToCells(region.pixels);

				SetOutsideCellLocations(cells);

				int numInsideAreas;

				SetInsideCellLocations(cells, out numInsideAreas);

				List<List<Vector2>>	allPoints				= new List<List<Vector2>>();
				List<Vector2>		leftMostPoints			= new List<Vector2>();
				List<int>			leftMostPointIndices	= new List<int>();

				for (int i = -1; i < numInsideAreas; i++)
				{
					if (Stopping) return;

					Vector2	start;
					Vector2	leftMostPoint;
					int		leftMostPointIndex;

					List<List<Corner>>	corners	= GetCorners(cells, i, out start);
					List<Vector2>		points	= GetPoints(corners, start, i >= 0);

					RemoveClosePoints(points);
					SmoothPoints(points);

					leftMostPointIndex	= GetLeftMostPointIndex(points);
					leftMostPoint		= points[leftMostPointIndex];

					if (i <= 0)
					{
						allPoints.Add(points);
						leftMostPoints.Add(leftMostPoint);
						leftMostPointIndices.Add(leftMostPointIndex);
					}
					else
					{
						int insertIndex;

						for (insertIndex = 1; insertIndex < leftMostPoints.Count; insertIndex++)
						{
							if (leftMostPoint.x <= leftMostPoints[insertIndex].x)
							{
								break;
							}
						}

						allPoints.Insert(insertIndex, points);
						leftMostPoints.Insert(insertIndex, leftMostPoint);
						leftMostPointIndices.Insert(insertIndex, leftMostPointIndex);
					}
				}

				List<Vector2> regionPoints = BridgeHoles(allPoints, leftMostPoints, leftMostPointIndices);

				ProgressStep		= AlgoProgress.Step.Triangulation;
				ProgressTotalPoints	= regionPoints.Count;
				ProgressPointsLeft	= regionPoints.Count;

				List<Vector2>	trianglePoints		= GetTrianglePoints(regionPoints);
				List<int>		triangleIndices		= GetTriangleIndices(trianglePoints, out regionPoints);

				allRegionPoints.Add(regionPoints);
				allRegionTriangles.Add(triangleIndices);
			}
		}

		private List<List<Cell>> ConverRegionPixelsToCells(List<List<Pixel>> pixels)
		{
			List<List<Cell>> cells = new List<List<Cell>>();

			for (int x = 0; x < pixels.Count; x++)
			{
				cells.Add(new List<Cell>());

				for (int y = 0; y < pixels[x].Count; y++)
				{
					Cell cell = new Cell();

					cell.x					= x;
					cell.y					= y;
					cell.containsPixel		= (pixels[x][y] != null);
					cell.insideAreaIndex	= -1;

					cells[x].Add(cell);
				}
			}

			return cells;
		}

		private void SetOutsideCellLocations(List<List<Cell>> cells)
		{
			currentMarker++;

			for (int x = 0; x < cells.Count; x++)
			{
				for (int y = 0; y < cells[x].Count; y++)
				{
					if (x == 0 || y == 0 || x == cells.Count - 1 || y == cells[x].Count - 1)
					{
						MarkCells(cells[x][y], cells, CellLocation.Outside, -1);
					}
				}
			}
		}

		private void SetInsideCellLocations(List<List<Cell>> cells, out int numInsideAreas)
		{
			currentMarker++;

			int insideAreaIndex = 0;

			for (int x = 0; x < cells.Count; x++)
			{ 
				for (int y = 0; y < cells[x].Count; y++)
				{
					Cell cell = cells[x][y];

					// Outside cells have already been set, if a cell doesn't contian a pixel and it's location is Unknown then it is an inside cell
					if (!cell.containsPixel && cell.location == CellLocation.Unknown)
					{
						if (MarkCells(cell, cells, CellLocation.Inside, insideAreaIndex))
						{
							insideAreaIndex++;
						}
					}
				}
			}

			numInsideAreas = insideAreaIndex;
		}

		private bool MarkCells(Cell startCell, List<List<Cell>> cells, CellLocation cellLocation, int insideAreaIndex)
		{
			List<Cell> cellsToMark = GetCells(startCell, cells);

			bool regionMarked = cellsToMark.Count > 4;

			for (int i = 0; i < cellsToMark.Count; i++)
			{
				Cell cell = cellsToMark[i];

				if (regionMarked)
				{
					cell.location			= cellLocation;
					cell.insideAreaIndex	= insideAreaIndex;
				}
				else
				{
					cell.containsPixel = true;
				}
			}

			return regionMarked;
		}

		private List<Cell> GetCells(Cell startCell, List<List<Cell>> cells)
		{
			Stack<Cell>	cellsToCheck	= new Stack<Cell>();
			List<Cell>	returnCells		= new List<Cell>();

			cellsToCheck.Push(startCell);

			while (cellsToCheck.Count > 0)
			{
				Cell cell = cellsToCheck.Pop();

				if (cell.containsPixel || cell.marker == currentMarker)
				{
					continue;
				}

				returnCells.Add(cell);

				cell.marker = currentMarker;

				PushCellIfValid(cell.x - 1, cell.y, cellsToCheck, cells);
				PushCellIfValid(cell.x + 1, cell.y, cellsToCheck, cells);
				PushCellIfValid(cell.x, cell.y - 1, cellsToCheck, cells);
				PushCellIfValid(cell.x, cell.y + 1, cellsToCheck, cells);
			}

			return returnCells;
		}

		private void PushCellIfValid(int cellX, int cellY, Stack<Cell> stack, List<List<Cell>> cells)
		{
			if (cellX < 0 || cellY < 0 || cellX >= cells.Count || cellY >= cells[cellX].Count)
			{
				return;
			}

			stack.Push(cells[cellX][cellY]);
		}

		private List<List<Corner>> GetCorners(List<List<Cell>> cells, int insideAreaIndex, out Vector2 start)
		{
			bool foundStart = false;
			start = Vector2.zero;

			List<List<Corner>> corners = new List<List<Corner>>();

			int width	= cells.Count;
			int height	= cells[0].Count;

			// Initialize list
			for (int x = 0; x < width + 1; x++)
			{
				corners.Add(new List<Corner>());

				for (int y = 0; y < height + 1; y++)
				{
					corners[x].Add(Corner.NON);
				}
			}

			int xStart = (insideAreaIndex == -1) ? 0 : 1;
			int yStart = (insideAreaIndex == -1) ? 0 : 1;
			
			int xEnd = (insideAreaIndex == -1) ? width + 1 : width;
			int yEnd = (insideAreaIndex == -1) ? height + 1 : height;

			for (int x = xStart; x < xEnd; x++)
			{
				for (int y = yStart; y < yEnd; y++)
				{
					Cell aCell = (x < width	&& y < height)	? cells[x][y]			: null;
					Cell bCell = (x < width	&& y > 0)		? cells[x][y - 1]		: null;
					Cell cCell = (x > 0		&& y > 0)		? cells[x - 1][y - 1]	: null;
					Cell dCell = (x > 0		&& y < height)	? cells[x - 1][y]		: null;

					int a, b, c, d;

					if (insideAreaIndex == -1)
					{
						a = (aCell != null && (aCell.containsPixel || aCell.location == CellLocation.Inside)) ? 1 : 0;
						b = (bCell != null && (bCell.containsPixel || bCell.location == CellLocation.Inside)) ? 1 : 0;
						c = (cCell != null && (cCell.containsPixel || cCell.location == CellLocation.Inside)) ? 1 : 0;
						d = (dCell != null && (dCell.containsPixel || dCell.location == CellLocation.Inside)) ? 1 : 0;
					}
					else
					{
						a = (aCell != null && (aCell.containsPixel || aCell.insideAreaIndex != insideAreaIndex)) ? 1 : 0;
						b = (bCell != null && (bCell.containsPixel || bCell.insideAreaIndex != insideAreaIndex)) ? 1 : 0;
						c = (cCell != null && (cCell.containsPixel || cCell.insideAreaIndex != insideAreaIndex)) ? 1 : 0;
						d = (dCell != null && (dCell.containsPixel || dCell.insideAreaIndex != insideAreaIndex)) ? 1 : 0;
					}

					int t = a + b + c + d;
					
					Corner corner = Corner.NON;

					if (t == 2 && ((b == 1 && d == 1) || (a == 1 && c == 1)))
					{
						Debug.LogError("Error 1: This shouldn't happen");
						return null;
					}
					else if (t == 1)
					{
						if (a == 1) corner = Corner.OBL;
						else if (b == 1) corner = Corner.OTL;
						else if (c == 1) corner = Corner.OTR;
						else if (d == 1) corner = Corner.OBR;
					}
					else if (t == 3)
					{
						if (a == 0) corner = Corner.IBL;
						else if (b == 0) corner = Corner.ITL;
						else if (c == 0) corner = Corner.ITR;
						else if (d == 0) corner = Corner.IBR;
					}

					if (!foundStart && corner != Corner.NON)
					{
						start		= new Vector2(x, y);
						foundStart	= true;
					}

					corners[x][y] = corner;
				}
			}

			return corners;
		}

		private List<Vector2> GetPoints(List<List<Corner>> corners, Vector2 start, bool isInside)
		{
			bool isStart = true;

			int xDir = 1;
			int yDir = 0;

			int xPos = (int)start.x;
			int yPos = (int)start.y;

			List<Corner>	lookingFor	= new List<Corner>() { isInside ? Corner.IBL : Corner.OBL };
			List<Vector2>	verts		= new List<Vector2>();

			while (true)
			{
				if (xPos < 0 || xPos >= corners.Count || yPos < 0 || yPos >= corners[xPos].Count)
				{
					Debug.LogError("Error 2: This shouldn't happen");
					return null;
				}

				// Back at the start, we are finished
				if (!isStart && xPos == start.x && yPos == start.y)
				{
					break;
				}

				Corner corner = corners[xPos][yPos];

				if (lookingFor.Contains(corner))
				{
					Vector2 point = new Vector2(xPos, yPos);

					verts.Add(point);

					if (isStart)
					{
						isStart		= false;
						xDir		= 0;
						yDir		= 1;

						if (isInside)
						{
							lookingFor	= new List<Corner>() { Corner.ITL, Corner.OTR };
						}
						else
						{
							lookingFor	= new List<Corner>() { Corner.OTL, Corner.ITR };
						}
					}
					else
					{
						switch (corner)
						{
							case Corner.OBL:
							{
								if (yDir == -1)
								{
									xDir		= 1;
									yDir		= 0;
									lookingFor	= new List<Corner>() { Corner.OBR, Corner.ITR };
								}
								else if (xDir == -1)
								{
									xDir		= 0;
									yDir		= 1;
									lookingFor	= new List<Corner>() { Corner.OTL, Corner.ITR };
								}
							}
							break;
							case Corner.OTL:
							{
								if (yDir == 1)
								{
									xDir		= 1;
									yDir		= 0;
									lookingFor	= new List<Corner>() { Corner.OTR, Corner.IBR };
								}
								else if (xDir == -1)
								{
									xDir		= 0;
									yDir		= -1;
									lookingFor	= new List<Corner>() { Corner.OBL, Corner.IBR };
								}
							}
							break;
							case Corner.OTR:
							{
								if (yDir == 1)
								{
									xDir		= -1;
									yDir		= 0;
									lookingFor	= new List<Corner>() { Corner.OTL, Corner.IBL };
								}
								else if (xDir == 1)
								{
									xDir		= 0;
									yDir		= -1;
									lookingFor	= new List<Corner>() { Corner.OBR, Corner.IBL };
								}
							}
							break;
							case Corner.OBR:
							{
								if (yDir == -1)
								{
									xDir		= -1;
									yDir		= 0;
									lookingFor	= new List<Corner>() { Corner.OBL, Corner.ITL };
								}
								else if (xDir == 1)
								{
									xDir		= 0;
									yDir		= 1;
									lookingFor	= new List<Corner>() { Corner.OTR, Corner.ITL };
								}
							}
							break;
							case Corner.IBL:
							{
								if (yDir == -1)
								{
									xDir		= 1;
									yDir		= 0;
									lookingFor	= new List<Corner>() { Corner.IBR, Corner.OTR };
								}
								else if (xDir == -1)
								{
									xDir		= 0;
									yDir		= 1;
									lookingFor	= new List<Corner>() { Corner.ITL, Corner.OTR };
								}
							}
							break;
							case Corner.ITL:
							{
								if (yDir == 1)
								{
									xDir		= 1;
									yDir		= 0;
									lookingFor	= new List<Corner>() { Corner.ITR, Corner.OBR };
								}
								else if (xDir == -1)
								{
									xDir		= 0;
									yDir		= -1;
									lookingFor	= new List<Corner>() { Corner.IBL, Corner.OBR };
								}
							}
							break;
							case Corner.ITR:
							{
								if (yDir == 1)
								{
									xDir		= -1;
									yDir		= 0;
									lookingFor	= new List<Corner>() { Corner.ITL, Corner.OBL };
								}
								else if (xDir == 1)
								{
									xDir		= 0;
									yDir		= -1;
									lookingFor	= new List<Corner>() { Corner.IBR, Corner.OBL };
								}
							}
							break;
							case Corner.IBR:
							{
								if (yDir == -1)
								{
									xDir		= -1;
									yDir		= 0;
									lookingFor	= new List<Corner>() { Corner.IBL, Corner.OTL };
								}
								else if (xDir == 1)
								{
									xDir		= 0;
									yDir		= 1;
									lookingFor	= new List<Corner>() { Corner.ITR, Corner.OTL };
								}
							}
							break;
						}
					}
				}

				xPos += xDir;
				yPos += yDir;
			}

			return verts;
		}

		private void RemoveClosePoints(List<Vector2> points)
		{
			float removeThreshold = 1f;

			for (int i = 0; i < points.Count; i++)
			{
				Vector2 vert1 = points[i];
				Vector2 vert2 = points[(i + 1) % points.Count];

				if (Vector2.Distance(vert1, vert2) <= removeThreshold)
				{
					points[i] = new Vector2((vert1.x + vert2.x) / 2f, (vert1.y + vert2.y) / 2f);
 
					points.RemoveAt((i + 1) % points.Count);
				}
			}
		}

		private void SmoothPoints(List<Vector2> points)
		{
			float smoothThreshold = 20f;

			for (int j = 0; j < 3; j++)
			{
				for (int i = 0; i < points.Count; i++)
				{
					Vector2 vert1 = points[i];
					Vector2 vert2 = points[(i + 1) % points.Count];
					Vector2 vert3 = points[(i == 0 ? points.Count - 1 : i - 1)];

					if (Vector2.Distance(vert1, vert2) <= smoothThreshold && Vector2.Distance(vert1, vert3) <= smoothThreshold)
					{
						points[i] = vert1 * 0.5f + vert2 * 0.25f + vert3 * 0.25f;
					}
				}
			}
		}

		private int GetLeftMostPointIndex(List<Vector2> points)
		{
			Vector2	leftMostPoint		= new Vector2(float.MaxValue, 0f);
			int		leftMostPointIndex	= -1;

			for (int i = 0; i < points.Count; i++)
			{
				Vector2 point = points[i];

				if (point.x < leftMostPoint.x)
				{
					leftMostPoint		= point;
					leftMostPointIndex	= i;
				}
			}

			return leftMostPointIndex;
		}

		private List<Vector2> BridgeHoles(List<List<Vector2>> regionPoints, List<Vector2> leftMostPoints, List<int> leftMostPointsIndex)
		{
			List<Vector2> points = new List<Vector2>();

			// Add the outside points
			points.AddRange(regionPoints[0]);

			for (int i = 1; i < regionPoints.Count; i++)
			{
				Vector2 leftMostPoint = leftMostPoints[i];
				Vector2 leftEdgePoint = new Vector2(0, leftMostPoint.y);

				Vector2	iPoint		= new Vector2(float.MinValue, 0);
				int 	iPointIndex	= 0;

				for (int j = 0; j < points.Count; j++)
				{
					Vector2 p1 = points[j];
					Vector2 p2 = points[(j + 1) % points.Count];
					Vector2 iP;

					if (LinesIntersect(leftMostPoint, leftEdgePoint, p1, p2, out iP))
					{
						if (iP.x > iPoint.x)
						{
							iPoint		= iP;
							iPointIndex	= j + 1;
						}
					}
				}

				if (iPoint.x == float.MinValue)
				{
					Debug.LogError("Could not find intersection point");
					continue;
				}

				List<Vector2>	holePoints	= regionPoints[i];
				int				start		= leftMostPointsIndex[i];
				int				index		= start;

				points.Insert(iPointIndex, iPoint);

				do
				{
					Vector2 point = holePoints[index];

					points.Insert(iPointIndex, point);

					index = (index + 1) % holePoints.Count;
				}
				while (index != start);

				points.Insert(iPointIndex, holePoints[start]);
				points.Insert(iPointIndex, iPoint);

				// PrintPoints("Joined points", points);

			}

			return points;
		}

		private bool LinesIntersect(Vector2 l1a, Vector2 l1b, Vector2 l2a, Vector2 l2b, out Vector2 intersectionPoint)
		{
			Vector2 l1Min = new Vector2(Mathf.Min(l1a.x, l1b.x), Mathf.Min(l1a.y, l1b.y));
			Vector2 l1Max = new Vector2(Mathf.Max(l1a.x, l1b.x), Mathf.Max(l1a.y, l1b.y));
			
			Vector2 l2Min = new Vector2(Mathf.Min(l2a.x, l2b.x), Mathf.Min(l2a.y, l2b.y));
			Vector2 l2Max = new Vector2(Mathf.Max(l2a.x, l2b.x), Mathf.Max(l2a.y, l2b.y));

			// Check if the bounding boxes overlap, if not then there's no way the lines can intersect
			if (!RectanglesOverlap(l1Min, l1Max, l2Min, l2Max))
			{
				intersectionPoint = Vector2.zero;

				return false;
			}

			float l1ax_l2ax = (l1a.x - l2a.x);
			float l1ay_l2ay = (l1a.y - l2a.y);
			float l1bx_l1ax = (l1b.x - l1a.x);
			float l1by_l1ay = (l1b.y - l1a.y);
			float l2bx_l2ax = (l2b.x - l2a.x);
			float l2by_l2ay = (l2b.y - l2a.y);

			float dem = (l2by_l2ay * l1bx_l1ax - l2bx_l2ax * l1by_l1ay);

			float a = (l2bx_l2ax * l1ay_l2ay - l2by_l2ay * l1ax_l2ax) / dem;
			float b = (l1bx_l1ax * l1ay_l2ay - l1by_l1ay * l1ax_l2ax) / dem;

			intersectionPoint = new Vector2(l1a.x + a * l1bx_l1ax, l1a.y + a * l1by_l1ay);

			return a >= 0 && a <= 1 && b >= 0 && b <= 1;
		}

		private bool RectanglesOverlap(Vector2 sq1Min, Vector2 sq1Max, Vector2 sq2Min, Vector2 sq2Max)
		{
			return !((sq1Min.x > sq2Max.x) || (sq1Max.x < sq2Min.x) || (sq1Min.y > sq2Max.y) || (sq1Max.y < sq2Min.y));
		}

		private List<Vector2> GetTrianglePoints(List<Vector2> points)
		{
			List<Vector2> trianglePoints = new List<Vector2>();

			int i = 0;

			bool trimPoints = true;

			while (points.Count > 0)
			{
				if (Stopping) return null;

				if (trimPoints)
				{
					TrimPoints(points);
					trimPoints = false;
				}

				// If there are less than 3 points then there is no way to make a triangle from them
				if (points.Count < 3)
				{
					break;
				}

				i = (i % points.Count);

				Vector2 pointToTry	= points[i];
				Vector2 rPoint		= points[(i + 1) % points.Count];
				Vector2 lPoint		= points[(i == 0) ? points.Count - 1 : i - 1];

				List<Vector2> triangle = new List<Vector2>() { pointToTry, rPoint, lPoint };

				// Check if there are only 3 points left
				if (points.Count == 3)
				{
					// Add the remaining triangle
					trianglePoints.AddRange(triangle);
					break;
				}

				// Check if the triangle is inside the polygon and the triangle does not contain any other point in the polygon
				if (IsClockwise(triangle) && !ContainsAnyOtherPoint(triangle, points))
				{
					// Add the triangles points
					trianglePoints.AddRange(triangle);

					// Remove the point from the polygon
					points.RemoveAt(i);
					ProgressPointsLeft = points.Count;

					trimPoints = true;
				}
				else
				{
					// The triangle is outside the polygon, try the next point
					i++;
				}
			}

			return trianglePoints;
		}

		private void TrimPoints(List<Vector2> points)
		{
			int i = 0;

			while(i < points.Count)
			{
				Vector2 pointToTry	= points[i];
				Vector2 rPoint		= points[(i + 1) % points.Count];
				Vector2 lPoint		= points[(i == 0) ? points.Count - 1 : i - 1];

				// Check if the lPoint and rPoint are the same point
				if (CloseEnough(lPoint, rPoint) || CloseEnough(lPoint, pointToTry) || CloseEnough(rPoint, pointToTry))
				{
					// Remove the point from the polygon
					points.RemoveAt(i);
					i = 0;
				}
				// Check if the points are all on the same line
				else if ((pointToTry - lPoint).normalized == (rPoint - pointToTry).normalized)
				{
					// Remove the point from the polygon
					points.RemoveAt(i);
					i = 0;
				}
				else if ((pointToTry - lPoint).normalized == (pointToTry - rPoint).normalized)
				{
					// Remove the point from the polygon
					points.RemoveAt(i);
					i = 0;
				}
				else if (SpecialCase1(lPoint, pointToTry, rPoint, points, i))
				{
					int index = (i + 4) % points.Count;

					for (int j = 0; j < 5; j++)
					{
						points.RemoveAt(index);

						index = (index == 0) ? points.Count - 1 : index - 1;
					}
					i = 0;
				}
				else if (SpecialCase2(lPoint, pointToTry, rPoint, points, i))
				{
					int index = (i + 3) % points.Count;

					for (int j = 0; j < 4; j++)
					{
						points.RemoveAt(index);

						index = (index == 0) ? points.Count - 1 : index - 1;
					}
					i = 0;
				}
				else
				{
					i++;
				}
			}
		}

		private bool SpecialCase1(Vector2 p1, Vector2 p2, Vector2 p3, List<Vector2> points, int index)
		{
			if (points.Count >= 6)
			{
				Vector2 p1b = points[(index + 4) % points.Count];
				Vector2 p2b = points[(index + 3) % points.Count];
				Vector2 p3b = points[(index + 2) % points.Count];

				return p1 == p1b && p2 == p2b && p3 == p3b;
			}

			return false;
		}

		private bool SpecialCase2(Vector2 p1, Vector2 p2, Vector2 p3, List<Vector2> points, int index)
		{
			if (points.Count >= 6)
			{
				Vector2 p4 = points[(index + 2) % points.Count];
				Vector2 p5 = points[(index + 3) % points.Count];

				return p1 == p5 && p2 == p4;
			}

			return false;
		}

		private bool CloseEnough(Vector2 p1, Vector2 p2)
		{
			return p1 == p2;
		}

		private bool IsClockwise(List<Vector2> points)
		{
			float sum = 0;

			for (int i = 0; i < points.Count; i++)
			{
				int j = (i + 1) % points.Count;

				Vector2 p1 = points[i];
				Vector2 p2 = points[j];

				sum += (p2.x - p1.x) * (p2.y + p1.y);
			}

			return sum > 0;
		}

		private bool ContainsAnyOtherPoint(List<Vector2> triangle, List<Vector2> polygonPoints)
		{
			for (int i = 0; i < polygonPoints.Count; i++)
			{
				Vector2 point = polygonPoints[i];

				if (triangle.Contains(point))
				{
					continue;
				}

				if (Math.TriangleContainsPoint(point, triangle[0], triangle[1], triangle[2]))
				{
					return true;
				}

				Vector2 point2 = polygonPoints[(i + 1) % polygonPoints.Count];

				if (LineIntersectsTriangle(point, point2, triangle[0], triangle[1], triangle[2]))
				{
					return true;
				}
			}

			return false;
		}

		private bool LineIntersectsTriangle(Vector2 p1, Vector2 p2, Vector2 t1, Vector2 t2, Vector2 t3)
		{
			return LinesIntersect(p1, p2, t1, t2) || LinesIntersect(p1, p2, t1, t3) || LinesIntersect(p1, p2, t2, t3);
		}

		/// <summary>
		/// Returns true if the two lines intersect each other
		/// </summary>
		private bool LinesIntersect(Vector2 l1a, Vector2 l1b, Vector2 l2a, Vector2 l2b)
		{
			float a = ((l2b.x - l2a.x) * (l1a.y - l2a.y) - (l2b.y - l2a.y) * (l1a.x - l2a.x)) / ((l2b.y - l2a.y) * (l1b.x - l1a.x) - (l2b.x - l2a.x) * (l1b.y - l1a.y));
			float b = ((l1b.x - l1a.x) * (l1a.y - l2a.y) - (l1b.y - l1a.y) * (l1a.x - l2a.x)) / ((l2b.y - l2a.y) * (l1b.x - l1a.x) - (l2b.x - l2a.x) * (l1b.y - l1a.y));

			return a > 0 && a < 1 && b > 0 && b < 1;
		}

		private List<int> GetTriangleIndices(List<Vector2> trianglePoints, out List<Vector2> points)
		{
			List<int>					triangleIndices	= new List<int>();
			Dictionary<Vector2, int>	pointIndices	= new Dictionary<Vector2, int>();

			points = new List<Vector2>();

			for (int i = 0; i < trianglePoints.Count; i++)
			{
				Vector2 trianglePoint = trianglePoints[i];

				int pointIndex;

				if (pointIndices.ContainsKey(trianglePoint))
				{
					pointIndex = pointIndices[trianglePoint];
				}
				else
				{
					pointIndex = points.Count;

					points.Add(trianglePoint);

					pointIndices.Add(trianglePoint, pointIndex);
				}

				triangleIndices.Add(pointIndex);
			}

			return triangleIndices;
		}

		private void WriteLevelFiles(List<Region> regions, List<List<Vector2>> allRegionPoints, List<List<int>> allRegionTriangles, List<Color> colors)
		{
			string outPath = settings.outPath;

			if (batchMode)
			{
				outPath += "/" + System.IO.Path.GetFileNameWithoutExtension(coloredFiles[curBatchIndex]);
			}

			byte[] bytes	= WriteLevelByteFile(regions, allRegionPoints, allRegionTriangles, colors, outPath);
			string id		= GetId(bytes);

			WriteLevelTxtFile(outPath, id);
		}

		private void WriteLevelTxtFile(string outPath, string id)
		{
			// Get the resources path of the file
			int		resourcesIndex		= outPath.IndexOf("/Resources/", System.StringComparison.Ordinal);
			string	fileResourcePath	= outPath.Remove(0, resourcesIndex + "/Resources/".Length);

			string fileContents = "";

			fileContents += id;		
			fileContents += "\n" + fileResourcePath;

			System.IO.File.WriteAllText(outPath + ".txt", fileContents);
		}

		private byte[] WriteLevelByteFile(List<Region> regions, List<List<Vector2>> allRegionPoints, List<List<int>> allRegionTriangles, List<Color> colors, string outPath)
		{
			List<byte> bytes = new List<byte>();

			// Add the size of the image then the number of colors to the values list
			bytes.AddRange(System.BitConverter.GetBytes((int)settings.imageSize.x));
			bytes.AddRange(System.BitConverter.GetBytes((int)settings.imageSize.y));
			bytes.AddRange(System.BitConverter.GetBytes(colors.Count));

			// Add each color to the values list
			for (int i = 0; i < colors.Count; i++)
			{
				Color color = colors[i];

				int r = Mathf.RoundToInt(color.r * 255f);
				int g = Mathf.RoundToInt(color.g * 255f);
				int b = Mathf.RoundToInt(color.b * 255f);

				bytes.AddRange(System.BitConverter.GetBytes(r));
				bytes.AddRange(System.BitConverter.GetBytes(g));
				bytes.AddRange(System.BitConverter.GetBytes(b));
			}

			bytes.AddRange(System.BitConverter.GetBytes(regions.Count));

			for (int i = 0; i < regions.Count; i++)
			{
				Region			region			= regions[i]; 
				List<Vector2>	regionPoints	= allRegionPoints[i];
				List<int>		regionTriangles	= allRegionTriangles[i];

				bytes.AddRange(System.BitConverter.GetBytes(region.colorIndex));
				bytes.AddRange(System.BitConverter.GetBytes(region.minX));
				bytes.AddRange(System.BitConverter.GetBytes(region.minY));
				bytes.AddRange(System.BitConverter.GetBytes(region.width));
				bytes.AddRange(System.BitConverter.GetBytes(region.height));

				int numAreaWidth	= (region.bounds[2] - region.bounds[0]);
				int numAreaHeight	= (region.bounds[3] - region.bounds[1]);
				int numX			= region.minX + region.bounds[0] + Mathf.FloorToInt(numAreaWidth / 2f);
				int numY			= region.minY + region.bounds[1] + Mathf.FloorToInt(numAreaHeight / 2f);
				int numSize			= Mathf.Min(numAreaWidth, numAreaHeight);

				bytes.AddRange(System.BitConverter.GetBytes(numX));
				bytes.AddRange(System.BitConverter.GetBytes(numY));
				bytes.AddRange(System.BitConverter.GetBytes(numSize));

				bytes.AddRange(System.BitConverter.GetBytes(regionPoints.Count));

				for (int j = 0; j < regionPoints.Count; j++)
				{
					bytes.AddRange(System.BitConverter.GetBytes(regionPoints[j].x));
					bytes.AddRange(System.BitConverter.GetBytes(regionPoints[j].y));
				}

				bytes.AddRange(System.BitConverter.GetBytes(regionTriangles.Count));

				for (int j = 0; j < regionTriangles.Count; j++)
				{
					bytes.AddRange(System.BitConverter.GetBytes(regionTriangles[j]));
				}
			}

			System.IO.File.WriteAllBytes(outPath + "_bytes.bytes", bytes.ToArray());

			return bytes.ToArray();
		}

		/// <summary>
		/// Gets a hash value for the given Texture2D
		/// </summary>
		private string GetId(byte[] bytes)
		{
			// encrypt bytes
			MD5CryptoServiceProvider	md5			= new MD5CryptoServiceProvider();
			byte[]						hashBytes	= md5.ComputeHash(bytes);

			// Convert the encrypted bytes back to a string (base 16)
			string hashString = "";

			for (int i = 0; i < hashBytes.Length; i++)
			{
				hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
			}

			return hashString.PadLeft(32, '0');
		}

		#region Debug

		private void PrintRegion(Region region)
		{
			string str = "Region";

			str += "\nminX: " + region.minX;
			str += "\nminY: " + region.minY;

			for (int y = region.height - 1; y >= 0; y--)
			{
				str += "\n";

				for (int x = 0; x < region.width; x++)
				{
					bool containsPixel = (region.pixels[x][y] != null);

					str += containsPixel ? "#" : "_";
				}
			}

			Debug.Log(str);
		}

		private void PrintCells(List<List<Cell>> cells)
		{
			string str = "Cells";

			int width = cells.Count;
			int height = cells[0].Count;;

			for (int y = height - 1; y >= 0; y--)
			{
				str += "\n";

				for (int x = 0; x < width; x++)
				{
					bool containsPixel = (cells[x][y].containsPixel);

					if (containsPixel)
					{
						str += "#";
					}
					else if (cells[x][y].location == CellLocation.Inside)
					{
						str += "I";
					}
					else if (cells[x][y].location == CellLocation.Outside)
					{
						str += "O";
					}
					else if (cells[x][y].location == CellLocation.Unknown)
					{
						str += "U";
					}
				}
			}

			Debug.Log(str);
		}

		private void PrintCorners(List<List<Corner>> corners)
		{
			string str = "Corners";

			int width = corners.Count;
			int height = corners[0].Count;;

			for (int y = height - 1; y >= 0; y--)
			{
				str += "\n";

				for (int x = 0; x < width; x++)
				{
					str += corners[x][y] + "_";
				}
			}

			Debug.Log(str);
		}

		private void PrintPoints(string header, List<Vector2> points)
		{
			string str = header;

			for (int i = 0; i < points.Count; i++)
			{
				str += "\n" + points[i];
			}

			Debug.Log(str);
		}

		#endregion
	}
}
