using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames
{
	public class CurrencyManager : SaveableManager<CurrencyManager>
	{
		#region Classes

		[System.Serializable]
		public class Settings
		{
			public bool		showNotEnoughCurrencyPopup	= false;
			public string	notEnoughCurrencyPopupId	= "";
			public string	popupTitleText				= "";
			public string	popupMessageText			= "";
			public bool		popupHasStoreButton			= false;
			[Space]
			public bool		popupHasRewardAdButton		= false;
			public string	rewardButtonText			= "";
			public string	rewardCurrencyId			= "";
			public int		rewardAmount				= 0;
			[Space]
			public bool		showRewardAdGrantedPopup	= false;
			public string	rewardAdGrantedPopupId		= "";
			public string	rewardAdGrantedPopupTitle	= "";
			public string	rewardAdGrantedPopupMessage	= "";
		}

		[System.Serializable]
		private class CurrencyInfo
		{
			public string	id				= "";
			public int		startingAmount	= 0;
			public Settings	settings		= null;
		}

		#endregion

		#region Inspector Variables

		[SerializeField] private List<CurrencyInfo>	currencyInfos = null;

		#endregion

		#region Member Variables

		private Dictionary<string, int> currencyAmounts;

		#endregion

		#region Properties

		public override string SaveId { get { return "currency_manager"; } }

		public System.Action<string> OnCurrencyChanged { get; set; }

		#endregion

		#region Unity Methods

		protected override void Awake()
		{
			base.Awake();

			currencyAmounts = new Dictionary<string, int>();

			InitSave();
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Gets the amount of currency the player has
		/// </summary>
		public int GetAmount(string currencyId)
		{
			if (!CheckCurrencyExists(currencyId))
			{
				return 0;
			}

			return currencyAmounts[currencyId];
		}

		/// <summary>
		/// Tries to spend the curreny
		/// </summary>
		public bool TrySpend(string currencyId, int amount)
		{
			if (!CheckCurrencyExists(currencyId))
			{
				return false;
			}

			// Check if the player has enough of the currency
			if (currencyAmounts[currencyId] >= amount)
			{
				ChangeCurrency(currencyId, -amount);

				return true;
			}

			CurrencyInfo currencyInfo = GetCurrencyInfo(currencyId);

			if (currencyInfo.settings.showNotEnoughCurrencyPopup && PopupManager.Exists())
			{
				PopupManager.Instance.Show(currencyInfo.settings.notEnoughCurrencyPopupId, new object[] { currencyInfo.settings });
			}

			return false;
		}

		/// <summary>
		/// Gives the amount of currency
		/// </summary>
		public void Give(string currencyId, int amount)
		{
			if (!CheckCurrencyExists(currencyId))
			{
				return;
			}

			ChangeCurrency(currencyId, amount);
		}

		/// <summary>
		/// Gives the amount of currency, data is of the following format: "id;amount"
		/// </summary>
		public void Give(string data)
		{
			string[] stringObjs = data.Trim().Split(';');

			if (stringObjs.Length != 2)
			{
				Debug.LogErrorFormat("[CurrencyManager] Give(string data) : Data format incorrect: \"{0}\", should be \"id;amount\"", data);
				return;
			}

			string currencyId	= stringObjs[0];
			string amountStr	= stringObjs[1];

			int amount;

			if (!int.TryParse(amountStr, out amount))
			{
				Debug.LogErrorFormat("[CurrencyManager] Give(string data) : Amount must be an integer, given data: \"{0}\"", data);
				return;
			}

			if (!CheckCurrencyExists(currencyId))
			{
				return;
			}

			ChangeCurrency(currencyId, amount);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Sets the currency
		/// </summary>
		private void ChangeCurrency(string currencyId, int amount)
		{
			currencyAmounts[currencyId] += amount;

			if (OnCurrencyChanged != null)
			{
				OnCurrencyChanged(currencyId);
			}
		}

		/// <summary>
		/// Sets all the starting currency amounts
		/// </summary>
		private void SetStartingValues()
		{
			for (int i = 0; i < currencyInfos.Count; i++)
			{
				CurrencyInfo currencyInfo = currencyInfos[i];

				currencyAmounts[currencyInfo.id] = currencyInfo.startingAmount;
			}
		}

		/// <summary>
		/// Gets the CUrrencyInfo for the given id
		/// </summary>
		private CurrencyInfo GetCurrencyInfo(string currencyId)
		{
			for (int i = 0; i < currencyInfos.Count; i++)
			{
				CurrencyInfo currencyInfo = currencyInfos[i];

				if (currencyId == currencyInfo.id)
				{
					return currencyInfo;
				}
			}

			return null;
		}

		/// <summary>
		/// Checks that the currency exists
		/// </summary>
		private bool CheckCurrencyExists(string currencyId)
		{
			CurrencyInfo currencyInfo = GetCurrencyInfo(currencyId);

			if (currencyInfo == null || !currencyAmounts.ContainsKey(currencyId))
			{
				Debug.LogErrorFormat("[CurrencyManager] TrySpend : The given currencyId \"{0}\" does not exist", currencyId);

				return false;
			}

			return true;
		}

		#endregion

		#region Save Methods

		public override Dictionary<string, object> Save()
		{
			Dictionary<string, object> saveData = new Dictionary<string, object>();

			saveData["amounts"] = currencyAmounts;

			return saveData;
		}

		protected override void LoadSaveData(bool exists, JSONNode saveData)
		{
			if (!exists)
			{
				SetStartingValues();

				return;
			}

			foreach (KeyValuePair<string, JSONNode> pair in saveData["amounts"])
			{
				// Make sure the currency still exists
				if (GetCurrencyInfo(pair.Key) != null)
				{
					currencyAmounts[pair.Key] = pair.Value.AsInt;
				}
			}
		}

		#endregion
	}
}
