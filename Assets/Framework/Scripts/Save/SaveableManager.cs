using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames
{
	public abstract class SaveableManager<T> : SingletonComponent<T>, ISaveable where T : Object
	{
		#region Abstract

		public abstract string SaveId { get; }

		public abstract Dictionary<string, object> Save();

		protected abstract void LoadSaveData(bool exists, JSONNode saveData);

		#endregion // Abstract

		#region Properties

		public bool ShouldSave { get { return true; } set { } }

		#endregion

		#region Unity Methods

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (SaveManager.Exists())
			{
				SaveManager.Instance.Unregister(this);
			}
		}

		#endregion // Unity Methods

		#region Protected Methods

		protected void InitSave()
		{
			SaveManager.Instance.Register(this);

			JSONNode saveData = SaveManager.Instance.LoadSave(this);

			LoadSaveData(saveData != null, saveData);
		}

		#endregion
	}
}
