using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames
{
	public class PopupManager : SingletonComponent<PopupManager>
	{
		#region Classes

		[System.Serializable]
		private class PopupInfo
		{
			[Tooltip("The popups id, used to show the popup. Should be unique between all other popups.")]
			public string popupId = "";

			[Tooltip("The Popup component to show.")]
			public Popup popup = null;
		}

		#endregion

		#region Inspector Variables

		[SerializeField] private List<PopupInfo> popupInfos = null;

		#endregion

		#region Member Variables

		private List<Popup> activePopups;

		#endregion

		#region Unity Methods

		protected override void Awake()
		{
			base.Awake();

			activePopups = new List<Popup>();

			for (int i = 0; i < popupInfos.Count; i++)
			{
				popupInfos[i].popup.Initialize();
			}
		}

		#endregion

		#region Public Methods

		public void Show(string id)
		{
			Show(id, null, null);
		}

		public void Show(string id, object[] inData)
		{
			Show(id, inData, null);
		}

		public void Show(string id, object[] inData, Popup.PopupClosed popupClosed)
		{
			Popup popup = GetPopupById(id);

			if (popup != null)
			{
				if (popup.Show(inData, popupClosed))
				{
					activePopups.Add(popup);
				}
			}
			else
			{
				Debug.LogErrorFormat("[PopupController] Popup with id {0} does not exist", id);
			}
		}

		public bool CloseActivePopup()
		{
			if (activePopups.Count > 0)
			{
				int index = activePopups.Count - 1;

				Popup popup = activePopups[index];

				if (popup.CanAndroidBackClosePopup)
				{
					popup.Hide(true);
				}

				return true;
			}

			return false;
		}

		public void OnPopupHiding(Popup popup)
		{
			for (int i = activePopups.Count - 1; i >= 0; i--)
			{
				if (popup == activePopups[i])
				{
					activePopups.RemoveAt(i);

					break;
				}
			}
		}

		#endregion

		#region Private Methods

		private Popup GetPopupById(string id)
		{
			for (int i = 0; i < popupInfos.Count; i++)
			{
				if (id == popupInfos[i].popupId)
				{
					return popupInfos[i].popup;
				}
			}

			return null;
		}

		#endregion
	}
}