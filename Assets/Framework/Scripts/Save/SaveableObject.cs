using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames
{
	public abstract class SaveableObject : ISaveable
	{
		#region Abstract Properties / Methods

		public abstract string SaveId		{ get; }
		public abstract string SaveVersion	{ get; }

		public abstract Dictionary<string, object> Save();

		#endregion

		#region Properties

		public bool ShouldSave { get; set; }

		#endregion

		#region Public Methods

		public void Destroy()
		{
			SaveManager.Instance.Unregister(this);
		}

		#endregion

		#region Protected Methods

		protected void Register()
		{
			SaveManager.Instance.Register(this);
		}

		#endregion
	}
}
