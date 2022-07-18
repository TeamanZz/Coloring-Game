using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames
{
	[RequireComponent(typeof(Button))]
	public class RewardAdButton : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private string		currencyId				= "";
		[SerializeField] private int		amountToReward			= 0;
		[SerializeField] private GameObject	uiContainer				= null;
		#if UNITY_EDITOR
		[SerializeField] private bool		testInEditor			= false;
		#endif

		[Space]

		[SerializeField] private bool	showOnlyWhenCurrencyIsLow	= false;
		[SerializeField] private int	currencyShowTheshold		= 0;

		[Space]

		[SerializeField] private bool	showRewardGrantedPopup		= false;
		[SerializeField] private string	rewardGrantedPopupId		= "";
		[SerializeField] private string	rewardGrantedPopupTitle		= "";
		[SerializeField] private string	rewardGrantedPopupMessage	= "";

		#endregion

		#region Unity Methods

		private void Start()
		{
			uiContainer.SetActive(false);

			bool areRewardAdsEnabled = MobileAdsManager.Instance.AreRewardAdsEnabled;

			#if UNITY_EDITOR
			areRewardAdsEnabled = testInEditor;
			#endif

			if (areRewardAdsEnabled)
			{
				UpdateUI();

				MobileAdsManager.Instance.OnRewardAdLoaded	+= UpdateUI;
				MobileAdsManager.Instance.OnAdsRemoved		+= OnAdsRemoved;
				CurrencyManager.Instance.OnCurrencyChanged	+= OnCurrencyChanged;

				gameObject.GetComponent<Button>().onClick.AddListener(OnClicked);
			}
		}

		#endregion

		#region Private Methods

		private void OnCurrencyChanged(string changedCurrencyId)
		{
			if (currencyId == changedCurrencyId)
			{
				UpdateUI();
			}
		}

		private void UpdateUI()
		{
			bool rewardAdLoded		= MobileAdsManager.Instance.RewardAdState == AdNetworkHandler.AdState.Loaded;
			bool passShowThreshold	= (!showOnlyWhenCurrencyIsLow || CurrencyManager.Instance.GetAmount(currencyId) <= currencyShowTheshold);

			uiContainer.SetActive(rewardAdLoded && passShowThreshold);

			#if UNITY_EDITOR
			if (testInEditor)
			{
				uiContainer.SetActive(passShowThreshold);
			}
			#endif
		}

		private void OnAdsRemoved()
		{
			MobileAdsManager.Instance.OnRewardAdLoaded	-= UpdateUI;
			MobileAdsManager.Instance.OnAdsRemoved		-= OnAdsRemoved;
			CurrencyManager.Instance.OnCurrencyChanged	-= OnCurrencyChanged;

			uiContainer.SetActive(false);
		}

		private void OnClicked()
		{
			#if UNITY_EDITOR
			if (testInEditor)
			{
				OnRewardAdGranted("", 0);

				return;
			}
			#endif

			uiContainer.SetActive(false);

			MobileAdsManager.Instance.ShowRewardAd(null, OnRewardAdGranted);
		}

		private void OnRewardAdGranted(string id, double amount)
		{
			CurrencyManager.Instance.Give(currencyId, amountToReward);

			if (showRewardGrantedPopup)
			{
				object[] popupData =
				{
					rewardGrantedPopupTitle,
					rewardGrantedPopupMessage
				};

				PopupManager.Instance.Show(rewardGrantedPopupId, popupData);
			}
		}

		#endregion
	}
}
