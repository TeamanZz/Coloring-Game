using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames.PictureColoring
{
	public class LoadManager : SingletonComponent<LoadManager>
	{
		#region Classes

		private class LoadingOperation
		{
			public Dictionary<int, LoadComplete>	onLoadCompleteCallbacks = null;
			public LevelData						levelData				= null;
			public LevelFileLoader					levelFileLoader			= null;
		}

		#endregion

		#region Inspector Variables

		[SerializeField] private int maxLoadedLevels = 25;

		#endregion

		#region Member Variables

		private const int MaxLoadingOperations = 4;

		private int globalId;

		private List<LoadingOperation>	queuedLoadingOperations;
		private List<LoadingOperation>	activeLoadingOperations;
		private List<LevelData>			loadedLevelsQueue;

		#endregion

		#region Delegates

		public delegate void LoadComplete(LevelData levelData, bool success);

		#endregion

		#region Unity Methods

		protected override void Awake()
		{
			base.Awake();

			queuedLoadingOperations	= new List<LoadingOperation>();
			activeLoadingOperations	= new List<LoadingOperation>();
			loadedLevelsQueue		= new List<LevelData>();

			globalId = 1;

			Application.lowMemory += OnLowMemory;
		}

		private void Update()
		{
			for (int i = 0; i < activeLoadingOperations.Count; i++)
			{
				LoadingOperation			loadingOperation	= activeLoadingOperations[i];
				LevelFileLoader.LoadResult	loadResult			= loadingOperation.levelFileLoader.Check();

				// Check if the level has finished loading
				if (loadResult != LevelFileLoader.LoadResult.InProgress)
				{
					LoadingOperationFinished(loadingOperation, true);

					// We don't want to do to much every frame so after processing a completed loading operation break out of the for loop, if there
					// are other completed loading operations they will be processed in the next update loop
					break;
				}
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Loads the level file for the level.
		/// </summary>
		public int LoadLevel(LevelData levelData, LoadComplete loadCompleteCallback)
		{
			return Load(levelData, loadCompleteCallback);
		}

		/// <summary>
		/// Cancels a load operation
		/// </summary>
		public void Cancel(string levelId, int loadId = -1)
		{
			LoadingOperation loadingOperation = GetLoadingOperation(levelId);

			if (loadingOperation != null)
			{
				if (loadId == -1)
				{
					// Load id of -1 means cancel all
					loadingOperation.onLoadCompleteCallbacks.Clear();
				}
				else
				{
					// Remove the lod complete callback
					loadingOperation.onLoadCompleteCallbacks.Remove(loadId);
				}

				// Check if there is anyone is still waiting for this level to load
				if (loadingOperation.onLoadCompleteCallbacks.Count == 0)
				{
					// No more callbacks waiting, cancel the load
					loadingOperation.levelFileLoader.Cancel();

					// Remove the cancelled operation
					activeLoadingOperations.Remove(loadingOperation);
					queuedLoadingOperations.Remove(loadingOperation);
				}
			}
		}

		#endregion

		#region Private Methods

		private int Load(LevelData levelData, LoadComplete loadCompleteCallback)
		{
			string	levelId	= levelData.Id;
			int		loadId	= globalId++;

			// Check if there is already a loading operation active for this level data
			LoadingOperation	loadingOperation	= GetLoadingOperation(levelData.Id);
			bool				isNewOperation		= false;

			if (loadingOperation == null)
			{
				// Create a new load operation
				loadingOperation = CreateLoadingOperation(levelData);

				// Set to try so LoadNextType is called and the loading operation is started
				isNewOperation = true;
			}

			// Add/Adjust the LevelData to the loaded LevelDatas queue
			AddToLoadQueue(levelData);

			// If the LevelFileData is not null then the level is already loaded
			if (levelData.LevelFileData != null)
			{
				return 0;
			}

			// Add the complete callback
			loadingOperation.onLoadCompleteCallbacks.Add(loadId, loadCompleteCallback);

			if (isNewOperation)
			{
				// Add the operation to the queued list since it might not get to start right away if the active list is at max capacity
				queuedLoadingOperations.Add(loadingOperation);

				// Try and start another queued operation
				StartQueuedLoadingOperations();
			}

			// Return the load id incase the caller needs to cancel the load at some pont
			return loadId;
		}

		/// <summary>
		/// Gets the loading operation for the level data
		/// </summary>
		private LoadingOperation GetLoadingOperation(string levelId)
		{
			for (int i = 0; i < activeLoadingOperations.Count; i++)
			{
				LoadingOperation loadingOperation = activeLoadingOperations[i];

				if (levelId == loadingOperation.levelData.Id)
				{
					return loadingOperation;
				}
			}

			for (int i = 0; i < queuedLoadingOperations.Count; i++)
			{
				LoadingOperation loadingOperation = queuedLoadingOperations[i];

				if (levelId == loadingOperation.levelData.Id)
				{
					return loadingOperation;
				}
			}

			return null;
		}

		/// <summary>
		/// Creates a new loading operation to load the level data
		/// </summary>
		private LoadingOperation CreateLoadingOperation(LevelData levelData)
		{
			LoadingOperation loadingOperation = new LoadingOperation();

			loadingOperation.levelData					= levelData;
			loadingOperation.onLoadCompleteCallbacks	= new Dictionary<int, LoadComplete>();

			return loadingOperation;
		}

		/// <summary>
		/// Adds the given LevelData to the load queue, unloads a LevelData is the load queue is now to large
		/// </summary>
		private void AddToLoadQueue(LevelData levelData)
		{
			// Remove it from the queue if it exists so we can add it to the back
			loadedLevelsQueue.Remove(levelData);

			// Check if the queue is at max capacity
			TryUnloadingLevels();

			// Add the new loaded level
			loadedLevelsQueue.Add(levelData);
		}

		private void TryUnloadingLevels()
		{
			if (maxLoadedLevels == 0) return;
			
			for (int i = loadedLevelsQueue.Count - maxLoadedLevels; i >= 0; i--)
			{
				LevelData unloadLevelData = loadedLevelsQueue[i];

				// Can only unload levels if it is not currently being laoded
				if (GetLoadingOperation(unloadLevelData.Id) == null)
				{
					// Loaded the last loaded level
					UnLoadLevel(unloadLevelData);
					loadedLevelsQueue.RemoveAt(i);
				}
			}
		}

		/// <summary>
		/// Destroies the loaded textures
		/// </summary>
		private void UnLoadLevel(LevelData levelData)
		{
			Destroy(levelData.ShareTexture);

			levelData.ShareTexture	= null;
			levelData.LevelFileData	= null;
		}

		/// <summary>
		/// Invokes the waiting callbacks to notify them that the loading operation has finished
		/// </summary>
		private void LoadingOperationFinished(LoadingOperation loadingOperation, bool success)
		{
			foreach (KeyValuePair<int, LoadComplete> pair in loadingOperation.onLoadCompleteCallbacks)
			{
				pair.Value(loadingOperation.levelData, success);
			}

			activeLoadingOperations.Remove(loadingOperation);

			// Start the next loading operation in the queued list if there is any
			StartQueuedLoadingOperations();
		}

		/// <summary>
		/// Starts any queued loading operations if there are less than the max amount of active operations
		/// </summary>
		private void StartQueuedLoadingOperations()
		{
			if (queuedLoadingOperations.Count > 0 && activeLoadingOperations.Count < MaxLoadingOperations)
			{
				// Move the loading operation from the queued to the active list
				LoadingOperation loadingOperation = queuedLoadingOperations[0];
				queuedLoadingOperations.RemoveAt(0);
				activeLoadingOperations.Add(loadingOperation);

				// Start the loading operation
				loadingOperation.levelFileLoader = new LevelFileLoader();
				loadingOperation.levelFileLoader.Start(loadingOperation.levelData);
			}
		}

		private void OnLowMemory()
		{
			maxLoadedLevels = Mathf.Max(1, loadedLevelsQueue.Count - 2);

			Debug.Log("Low memory, setting maxLoadedLevels to " + maxLoadedLevels + " (Num loaded levels == " + loadedLevelsQueue.Count + ")");

			TryUnloadingLevels();

			Resources.UnloadUnusedAssets();

			System.GC.Collect();
		}

		#endregion
	}
}
