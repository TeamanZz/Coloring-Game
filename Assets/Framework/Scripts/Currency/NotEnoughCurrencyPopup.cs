using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames
{
	public class NotEnoughCurrencyPopup : Popup
	{
		#region Inspector Variables

		[Space]

		[SerializeField] private Text		titleText				= null;
		[SerializeField] private Text		messageText				= null;
		[SerializeField] private Text		rewardAdButtonText		= null;
		[SerializeField] private GameObject rewardAdButton			= null;
		[SerializeField] private GameObject storeButton				= null;
		[SerializeField] private GameObject buttonsContainer		= null;
		#if UNITY_EDITOR
		[SerializeField] private bool		testRewardAdsInEditor	= false;
		#endif

		#endregion

		#region Member Variables

		private CurrencyManager.Settings currencySettings;

		#endregion

		#region Public Methods

		public override void OnShowing(object[] inData)
		{
			base.OnShowing(inData);

			this.currencySettings = inData[0] as CurrencyManager.Settings;

			titleText.text			= currencySettings.popupTitleText;
			messageText.text		= currencySettings.popupMessageText;
			rewardAdButtonText.text = currencySettings.rewardButtonText;

			bool showStoreButton	= currencySettings.popupHasStoreButton;
			bool showRewardAdButton	= currencySettings.popupHasRewardAdButton && MobileAdsManager.Instance.AreRewardAdsEnabled;

			storeButton.SetActive(showStoreButton);
			buttonsContainer.SetActive(showRewardAdButton || showStoreButton);

			if (showRewardAdButton)
			{
				rewardAdButton.SetActive(MobileAdsManager.Instance.RewardAdState == AdNetworkHandler.AdState.Loaded);

				MobileAdsManager.Instance.OnRewardAdLoaded	+= OnRewardAdLoaded;
				MobileAdsManager.Instance.OnAdsRemoved		+= OnAdsRemoved;
			}
			else
			{
				rewardAdButton.SetActive(false);

				MobileAdsManager.Instance.OnRewardAdLoaded	-= OnRewardAdLoaded;
				MobileAdsManager.Instance.OnAdsRemoved		-= OnAdsRemoved;
			}

			#if UNITY_EDITOR
			if (testRewardAdsInEditor && currencySettings.popupHasRewardAdButton)
			{
				rewardAdButton.SetActive(true);
			}
			#endif
		}

		public void OnRewardAdButtonClick()
		{
			#if UNITY_EDITOR
			if (testRewardAdsInEditor)
			{
				OnRewardAdGranted("", 0);
				Hide(false);
				return;
			}
			#endif

			if (MobileAdsManager.Instance.RewardAdState != AdNetworkHandler.AdState.Loaded)
			{
				rewardAdButton.SetActive(false);

				Debug.LogError("[NotEnoughCurrencyPopup] The reward button was clicked but there is no ad loaded to show.");

				return;
			}

			MobileAdsManager.Instance.ShowRewardAd(OnRewardAdClosed, OnRewardAdGranted);

			Hide(false);
		}

		#endregion

		#region Private Methods

		private void OnRewardAdLoaded()
		{
			rewardAdButton.SetActive(true);
		}

		private void OnRewardAdClosed()
		{
			rewardAdButton.SetActive(false);
		}

		private void OnRewardAdGranted(string rewardId, double amount)
		{
			CurrencyManager.Instance.Give(currencySettings.rewardCurrencyId, currencySettings.rewardAmount);

			object[] popupData =
			{
				currencySettings.rewardAdGrantedPopupTitle,
				currencySettings.rewardAdGrantedPopupMessage
			};

			PopupManager.Instance.Show(currencySettings.rewardAdGrantedPopupId, popupData);
		}

		private void OnAdsRemoved()
		{
			MobileAdsManager.Instance.OnRewardAdLoaded -= OnRewardAdLoaded;

			rewardAdButton.SetActive(false);
		}

		#endregion
	}
}
