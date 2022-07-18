using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace BizzyBeeGames.PictureColoring
{
	public class LevelFileLoader
	{
		#region Enums

		public enum LoadResult
		{
			InProgress,
			Error,
			Complete
		}

		#endregion

		#region Member Variables

		private LevelData		levelData;
		private ResourceRequest	resourceRequest;
		private LoadWorker		loadWorker;

		#endregion

		#region Public Methods

		public void Start(LevelData levelData)
		{
			this.levelData = levelData;

			// First we need to load the byte file from resources as a TextAsset
			resourceRequest = Resources.LoadAsync<TextAsset>(levelData.ResourcesPath + "_bytes");
		}

		public LoadResult Check()
		{
			// Check if the file has finished loading from Resources
			if (resourceRequest != null && resourceRequest.isDone)
			{
				TextAsset levelBytesFile = resourceRequest.asset as TextAsset;

				resourceRequest = null;

				// Check if the bytes file was loaded successfully
				if (levelBytesFile == null)
				{
					Debug.LogErrorFormat("[LevelLoadManager] Error loading level btyes file from resources. Id: {0}, Resource path: {1}_bytes",
					                     levelData.Id, levelData.ResourcesPath);

					return LoadResult.Error;
				}

				// Start the LevelFileLoader to parse the bytes 
				loadWorker = new LoadWorker(levelBytesFile.bytes);
				loadWorker.StartWorker();

				Resources.UnloadAsset(levelBytesFile);
			}
			// Check if the level file loader has finished parsing the bytes
			else if (loadWorker != null && loadWorker.Stopped)
			{
				if (!string.IsNullOrEmpty(loadWorker.error))
				{
					Debug.LogErrorFormat("[LevelLoadManager] Error loading level file with Id {0}. Error message: {1}",
					                     levelData.Id, loadWorker.error);

					return LoadResult.Error;
				}

				// get the level file data from the worker
				levelData.LevelFileData = loadWorker.data;

				// Set the loader to null since it's no longer needed
				loadWorker = null;

				return LoadResult.Complete;
			}

			return LoadResult.InProgress;
		}

		public void Cancel()
		{
			if (loadWorker != null)
			{
				loadWorker.Stop();
			}
		}

		#endregion

		#region Worker

		private class LoadWorker : Worker
		{
			#region Member Variables

			private byte[]		levelFileBytes;
			private int			byteIndex;

			// When the worker is finished this will contain a reference to the loaded LevelFileData
			public LevelFileData data;

			#endregion

			#region Public Methods

			public LoadWorker(byte[] levelFileBytes)
			{
				this.levelFileBytes = levelFileBytes;
			}

			#endregion

			#region Protected Methods

			protected override void Begin()
			{
				byteIndex = 0;
			}

			protected override void DoWork()
			{
				LevelFileData levelFileData = new LevelFileData();

				ParseLevelFileContents(levelFileData);

				data = levelFileData;

				Stop();
			}

			#endregion

			#region Private Methods

			/// <summary>
			/// Parses the files contents
			/// </summary>
			private void ParseLevelFileContents(LevelFileData levelFileData)
			{
				// Get the images width/height
				levelFileData.imageWidth		= GetNextInt();
				levelFileData.imageHeight		= GetNextInt();

				if (Stopping) return;

				// Parse the colors in the level
				ParseColors(levelFileData);

				if (Stopping) return;

				// Add a region list for each color in the level
				ParseRegions(levelFileData);
			}

			/// <summary>
			/// Parses the colors from the contents
			/// </summary>
			private void ParseColors(LevelFileData levelFileData)
			{
				// Get the number of colors in the level
				int numColors = GetNextInt();

				// Get all the colors
				levelFileData.colors = new List<UnityEngine.Color>(numColors);

				for (int i = 0; i < numColors; i++)
				{
					if (Stopping) return;

					float r = (float)GetNextInt() / 255f;
					float g = (float)GetNextInt() / 255f;
					float b = (float)GetNextInt() / 255f;

					levelFileData.colors.Add(new UnityEngine.Color(r, g, b, 1f));
				}
			}

			/// <summary>
			/// Parses the regions from the contents
			/// </summary>
			private void ParseRegions(LevelFileData levelFileData)
			{
				int numRegions = GetNextInt();

				levelFileData.regions = new List<Region>();

				for (int i = 0; i < numRegions; i++)
				{
					if (Stopping) return;

					levelFileData.regions.Add(ParseRegion(i));
				}
			}

			/// <summary>
			/// Parses the region.
			/// </summary>
			private Region ParseRegion(int id)
			{
				int colorIndex		= GetNextInt();
				int minX			= GetNextInt();
				int minY			= GetNextInt();
				int regionWidth		= GetNextInt();
				int regionHeight	= GetNextInt();
				int numberX			= GetNextInt();
				int numberY			= GetNextInt();
				int numberSize		= GetNextInt();

				Region region = new Region();

				region.id			= id;
				region.colorIndex	= colorIndex;
				region.bounds		= new RegionBounds(minX, minY, minX + regionWidth - 1, minY + regionHeight - 1);
				region.numberX		= numberX;
				region.numberY		= numberY;
				region.numberSize	= numberSize;
				region.points		= ParseRegionPoints();
				region.triangles	= ParseRegionTriangles();

				return region;
			}

			private List<Vector2> ParseRegionPoints()
			{
				int numPoints = GetNextInt();

				List<Vector2> points = new List<Vector2>();

				for (int i = 0; i < numPoints; i++)
				{
					float x = GetNextFloat();
					float y = GetNextFloat();

					points.Add(new Vector2(x, y));
				}

				return points;
			}

			private List<int> ParseRegionTriangles()
			{
				int numTriangles = GetNextInt();

				List<int> triangles = new List<int>();

				for (int i = 0; i < numTriangles; i++)
				{
					triangles.Add(GetNextInt());
				}

				return triangles;
			}

			private int GetNextInt()
			{
				int val = System.BitConverter.ToInt32(levelFileBytes, byteIndex);

				byteIndex += 4;

				return val;
			}

			private float GetNextFloat()
			{
				float val = System.BitConverter.ToSingle(levelFileBytes, byteIndex);

				byteIndex += 4;

				return val;
			}

			#endregion
		}

		#endregion // Worker
	}
}
