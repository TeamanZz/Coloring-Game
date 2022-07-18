using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BizzyBeeGames
{
	public enum SaveFileStatus
	{
		Exists,
		NotExists
	}

	public class SaveManager : SingletonComponent<SaveManager>
	{
		#region Inspector Variables

		[Tooltip("If greater than 0, SaveManager will save all data every \"saveInterval\" seconds")]
		[SerializeField] private float saveInterval = 0;

		[Tooltip("Enables or disables encrypting the save files. If you change this you must clear/delete the save data or things will break.")]
		[SerializeField] private bool enableEncryption = false;
		
		#endregion // Inspector Variables

		#region Member Variables

		// Not the most secure was of storing the encryption key but works to keep most people form modifying the save data
		private const int key = 782;

		private List<ISaveable>	saveables;
		private System.DateTime	nextSaveTime;

		#endregion

		#region Properties

		/// <summary>
		/// Path to the save file on the device
		/// </summary>
		public string SaveFolderPath { get { return Application.persistentDataPath + "/SaveFiles"; } }

		/// <summary>
		/// List of registered saveables
		/// </summary>
		private List<ISaveable> Saveables
		{
			get
			{
				if (saveables == null)
				{
					saveables = new List<ISaveable>();
				}

				return saveables;
			}
		}

		#endregion

		#region Unity Methods

		private void Start()
		{
			Debug.Log("Save folder path: " + SaveFolderPath);

			SetSaveNextTime();
		}

		private void Update()
		{
			if (saveInterval > 0 && System.DateTime.UtcNow >= nextSaveTime)
			{
				Save();
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			Save();
		}

		private void OnApplicationPause(bool pause)
		{
			if (pause)
			{
				Save();
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Registers a saveable to be saved
		/// </summary>
		public void Register(ISaveable saveable)
		{
			if (Saveables.Contains(saveable))
			{
				Debug.LogWarningFormat("[SaveManager] The ISaveable {0} has already been registed", saveable.SaveId);
				return;
			}

			Saveables.Add(saveable);
		}

		/// <summary>
		/// Registers a saveable to be saved
		/// </summary>
		public void Unregister(ISaveable saveable)
		{
			// Save the Saveable if it needs saving
			Save(saveable);

			Saveables.Remove(saveable);
		}

		/// <summary>
		/// Saves any saveable that needs saving
		/// </summary>
		public void Save()
		{
			for (int i = 0; i < saveables.Count; i++)
			{
				Save(saveables[i]);
			}

			SetSaveNextTime();
		}

		/// <summary>
		/// Saves the Saveables data to a file on the device
		/// </summary>
		public void Save(ISaveable saveable)
		{
			if (saveable.ShouldSave)
			{
				// Create the save filder if it does not exist
				if (!System.IO.Directory.Exists(SaveFolderPath))
				{
					System.IO.Directory.CreateDirectory(SaveFolderPath);
				}

				try
				{
					Dictionary<string, object>	saveData		= saveable.Save();
					string						saveFilePath	= GetSaveFilePath(saveable.SaveId);

					string fileContents = Utilities.ConvertToJsonString(saveData);

					if (enableEncryption)
					{
						fileContents = Utilities.EncryptDecrypt(fileContents, key);
					}

					System.IO.File.WriteAllText(saveFilePath, fileContents);
				}
				catch (System.Exception ex)
				{
					Debug.LogError("An exception occured while saving " + saveable.SaveId);
					Debug.LogException(ex);
				}
			}
		}

		/// <summary>
		/// Loads the save data for the given Saveable
		/// </summary>
		public JSONNode LoadSave(ISaveable saveable)
		{
			return LoadSave(saveable.SaveId);
		}

		/// <summary>
		/// Loads the save data for the given Saveable
		/// </summary>
		public JSONNode LoadSave(string saveId)
		{
			string saveFilePath = GetSaveFilePath(saveId);

			if (System.IO.File.Exists(saveFilePath))
			{
				string fileContents = System.IO.File.ReadAllText(saveFilePath);

				if (enableEncryption)
				{
					fileContents = Utilities.EncryptDecrypt(fileContents, key);
				}

				return JSON.Parse(fileContents);
			}

			return null;
		}

		/// <summary>
		/// Returns true if there exists a save file with the given save id
		/// </summary>
		public bool HasSaveData(string saveId)
		{
			return System.IO.File.Exists(GetSaveFilePath(saveId));
		}

		/// <summary>
		/// Deletes the save file and un-registers the saveable
		/// </summary>
		public void DeleteSave(ISaveable saveable)
		{
			DeleteSaveFile(saveable.SaveId);
			Saveables.Remove(saveable);
		}

		/// <summary>
		/// Deletes the save file
		/// </summary>
		public void DeleteSaveFile(string saveId)
		{
			string saveFilePath = GetSaveFilePath(saveId);

			if (System.IO.File.Exists(saveFilePath))
			{
				System.IO.File.Delete(saveFilePath);
			}
			else
			{
				Debug.LogWarning("[SaveManager] Could not delete save file because it does not exist: " + saveFilePath);
			}
		}

		public static void DeleteSaveData(string[] args)
		{
			Instance.Saveables.Clear();

			DeleteSaveData();

			Application.Quit();
		}

		public static void DeleteSaveData()
		{
			System.IO.Directory.Delete(Instance.SaveFolderPath, true);

			Debug.Log("Save data deleted");
		}

#if UNITY_EDITOR

		[UnityEditor.MenuItem("Tools/Bizzy Bee Games/Delete Editor Save Data", priority = 0)]
		public static void DeleteSaveDataEditor()
		{
			DeleteSaveData();
		}

#endif

		#endregion

		#region Private Methods

		/// <summary>
		/// Returns the path to the save file for the given Saveable
		/// </summary>
		private string GetSaveFilePath(string saveId)
		{
			return string.Format("{0}/{1}.json", SaveFolderPath, saveId);
		}

		/// <summary>
		/// Sets the next save time if saveInterval is > than 0
		/// </summary>
		private void SetSaveNextTime()
		{
			if (saveInterval > 0)
			{
				nextSaveTime = System.DateTime.UtcNow.AddSeconds(saveInterval);
			}
		}

		#endregion
	}
}
