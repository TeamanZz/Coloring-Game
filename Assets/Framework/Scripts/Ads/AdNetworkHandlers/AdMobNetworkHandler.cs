using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if BBG_ADMOB
using GoogleMobileAds.Api;
#endif

namespace BizzyBeeGames
{
	public class AdMobNetworkHandler : AdNetworkHandler
	{
		#region Member Variables

		#if BBG_ADMOB
		private BannerView			bannerView;
		private InterstitialAd		interstitialAd;
		private RewardBasedVideoAd	rewardBasedVideo;
		private bool				showBannerAd;
		private float				bannerHeight;
		#endif

		#endregion

		#region Properties

		protected override string LogTag { get { return "AdMob"; } }

		private string AppId				{ get { return MobileAdsSettings.Instance.adMobConfig.AppId; } }
		private string BannerAdUnitId		{ get { return MobileAdsSettings.Instance.adMobConfig.BannerPlacementId; } }
		private string InterstitialAdUnitId	{ get { return MobileAdsSettings.Instance.adMobConfig.InterstitialPlacementId; } }
		private string RewardAdUnitId		{ get { return MobileAdsSettings.Instance.adMobConfig.RewardPlacementId; } }

		#endregion

		#region Protected Methods

		protected override void DoInitialize()
		{
			GameDebugManager.Log(LogTag, "Initializing AdMob, App Id: " + AppId);

			#if BBG_ADMOB

			// Initialize AdMob
			GoogleMobileAds.Api.MobileAds.Initialize(AppId);

			isInitialized = true;

			#if UNITY_EDITOR

			GameDebugManager.LogWarning(LogTag, "AdMob has initialized but will not load any ads in the Unity Editor. To test AdMob ads, please run the game on a Android or iOS device.");
			
			disabled = true;

			#else

			// Make sure an instance of InvokeOnMainThread is created
			InvokeOnMainThread.CreateInstance();
			 
			// If we are using AdMob for banner ads then pre-load one now
			if (bannerAdsEnabled)
			{
				GameDebugManager.Log(LogTag, "Banner ads enabled, Unit Id: " + BannerAdUnitId);

				showBannerAd = MobileAdsSettings.Instance.adMobConfig.ShowBannerOnStart;

				PreLoadBannerAd();
			}

			// If we are using AdMob for interstitial ads then pre-load one now
			if (interstitialAdsEnabled)
			{
				GameDebugManager.Log(LogTag, "Interstitial ads enabled, Unit Id: " + InterstitialAdUnitId);

				PreLoadInterstitialAd();
			}

			// If we are using AdMob for reward ads then pre-load one now
			if (rewardAdsEnabled)
			{
				GameDebugManager.Log(LogTag, "Reward ads enabled, Unit Id: " + RewardAdUnitId);

				rewardBasedVideo = RewardBasedVideoAd.Instance;

				SetRewardAdEvents();

				PreLoadRewardAd();
			}

			#endif // UNITY_EDITOR

			#else

			GameDebugManager.LogError(LogTag, "AdMob has not been setup in Mobile Ads Settings");

			#endif //BBG_ADMOB
		}

		protected override void DoLoadBannerAd()
		{
			#if BBG_ADMOB
			// Only load a new banner ad if the ad stat is none
			if (BannerAdState == AdState.None)
			{
				CreateBannerAd();
			}
			#endif
		}

		protected override void DoShowBannerAd()
		{
			#if BBG_ADMOB
			showBannerAd = true;

			switch (BannerAdState)
			{
				case AdState.None:
					CreateBannerAd();
					break;
				case AdState.Loaded:
					bannerView.Show();
					BannerAdState = AdState.Shown;
					NotifyBannerAdShown();
					break;
				default:
					GameDebugManager.Log(LogTag, "DoShowBannerAd: Nothing will happen because BannerAdState is: " + BannerAdState);
					break;
			}

			#endif
		}

		protected override void DoHideBannerAd()
		{
			#if BBG_ADMOB
			showBannerAd = false;

			if (BannerAdState == AdState.Shown)
			{
				bannerView.Hide();
				BannerAdState = AdState.Loaded;
				NotifyBannerAdHidden();
			}
			else
			{
				GameDebugManager.Log(LogTag, "DoHideBannerAd: Nothing will happen because BannerAdState is: " + BannerAdState);
			}
			#endif
		}

		protected override void DoLoadInterstitialAd()
		{
			#if BBG_ADMOB
			#if UNITY_IOS
			// On iOS InterstitialAd can only be used once, if another ad is request it needs to be destroyed and a new one created
			DestroyLoadedInterstitialAd();
			#endif

			if (interstitialAd == null)
			{
				// Create a new InterstitialAd object and attach the event callbakcs
				interstitialAd = new InterstitialAd(InterstitialAdUnitId);

				interstitialAd.OnAdLoaded				+= InterstitialAdLoaded;
				interstitialAd.OnAdFailedToLoad			+= InterstitialAdFailedToLoad;
				interstitialAd.OnAdOpening				+= InterstitialAdOpening;
				interstitialAd.OnAdClosed				+= InterstitialAdClosed;
			}

			NotifyInterstitialAdLoading();

			// Load an ad
			interstitialAd.LoadAd(CreateAdRequestBuilder().Build());
			#endif
		}

		protected override void DoShowInterstitialAd()
		{
			#if BBG_ADMOB
			NotifyInterstitialAdShowing();
			interstitialAd.Show();
			#endif
		}

		protected override void DoLoadRewardAd()
		{
			#if BBG_ADMOB
			NotifyRewardAdLoading();
			rewardBasedVideo.LoadAd(CreateAdRequestBuilder().Build(), RewardAdUnitId);
			#endif
		}

		protected override void DoShowRewardAd()
		{
			#if BBG_ADMOB
			NotifyRewardAdShowing();
			rewardBasedVideo.Show();
			#endif
		}

		protected override void DoAdsRemoved(bool dontRemoveRewardAds)
		{
			#if BBG_ADMOB
			if (bannerView != null)
			{
				bannerView.Destroy();
				bannerView = null;
			}

			if (interstitialAd != null)
			{
				interstitialAd.Destroy();
				interstitialAd = null;
			}

			if (rewardAdsEnabled && !dontRemoveRewardAds)
			{
				RemoveRewardAdEvents();
				rewardBasedVideo = null;
			}
			#endif
		}

		protected override void ConsentStatusUpdated()
		{
			// Consent status will be set next time an ad loads
		}

		protected override float DoGetBannerHeightInPixels()
		{
			#if BBG_ADMOB && !UNITY_EDITOR

			if (bannerHeight == 0)
			{
				if (bannerView != null)
				{
					bannerHeight = bannerView.GetHeightInPixels();
				}
				else
				{
					BannerView tempView = new BannerView(BannerAdUnitId, GetBannerAdSize(), AdPosition.Bottom);

					bannerHeight = tempView.GetHeightInPixels();

					tempView.Destroy();
				}
			}

			return bannerHeight;

			#else

			return 0f;

			#endif
		}

		protected override MobileAdsSettings.BannerPosition DoGetBannerPosition()
		{
			return MobileAdsSettings.Instance.adMobConfig.BannerPosition;
		}

		#endregion

		#region Private Methods

		#if BBG_ADMOB

		/// <summary>
		/// Creates and loads a new BannerView
		/// </summary>
		private void CreateBannerAd()
		{
			BannerAdState = AdState.Loading;

			NotifyBannerAdLoading();

			bannerView = new BannerView(BannerAdUnitId, GetBannerAdSize(), GetAdMobBannerPosition());
		
			bannerView.OnAdLoaded		+= BannerAdLoaded;
			bannerView.OnAdFailedToLoad	+= BannerAdFailedToLoad;

			bannerView.LoadAd(CreateAdRequestBuilder().Build());
			bannerView.Hide();
		}

		/// <summary>
		/// Adds the event callback methods to the reward ad instance
		/// </summary>
		private void SetRewardAdEvents()
		{
			if (rewardBasedVideo != null)
			{
				rewardBasedVideo.OnAdLoaded			+= RewardAdLoaded;
				rewardBasedVideo.OnAdFailedToLoad	+= RewardAdFailedToLoad;
				rewardBasedVideo.OnAdRewarded		+= RewardAdRewarded;
				rewardBasedVideo.OnAdOpening		+= RewardAdOpening;
				rewardBasedVideo.OnAdClosed			+= RewardAdClosed;
			}
		}

		/// <summary>
		/// Adds the event callback methods to the reward ad instance
		/// </summary>
		private void RemoveRewardAdEvents()
		{
			if (rewardBasedVideo != null)
			{
				rewardBasedVideo.OnAdLoaded			-= RewardAdLoaded;
				rewardBasedVideo.OnAdFailedToLoad	-= RewardAdFailedToLoad;
				rewardBasedVideo.OnAdRewarded		-= RewardAdRewarded;
				rewardBasedVideo.OnAdClosed			-= RewardAdClosed;
			}
		}

		/// <summary>
		/// Gets the AdSize that is set in the settings
		/// </summary>
		private AdSize GetBannerAdSize()
		{
			switch (MobileAdsSettings.Instance.adMobConfig.BannerSize)
			{
				case MobileAdsSettings.AdMobConfig.Config.BannerSize.Banner:
					return AdSize.Banner;
				case MobileAdsSettings.AdMobConfig.Config.BannerSize.IABBanner:
					return AdSize.IABBanner;
				case MobileAdsSettings.AdMobConfig.Config.BannerSize.Leaderboard:
					return AdSize.Leaderboard;
				case MobileAdsSettings.AdMobConfig.Config.BannerSize.MediumRectangle:
					return AdSize.MediumRectangle;
				case MobileAdsSettings.AdMobConfig.Config.BannerSize.SmartBanner:
					return AdSize.SmartBanner;
			}

			return AdSize.Banner;
		}

		private AdRequest.Builder CreateAdRequestBuilder()
		{
			AdRequest.Builder request = new AdRequest.Builder();

			for (int i = 0; i < MobileAdsSettings.Instance.adMobConfig.DeviceTestIds.Count; i++)
			{
				request.AddTestDevice(MobileAdsSettings.Instance.adMobConfig.DeviceTestIds[i]);
			}

			if (consentStatus == MobileAdsManager.ConsentType.NonPersonalized)
			{
				request.AddExtra("npa", "1");
			}

			if (MobileAdsSettings.Instance.adMobConfig.isChildDirected)
			{
				request.TagForChildDirectedTreatment(true);
			}

			return request;
		}

		private AdPosition GetAdMobBannerPosition()
		{
			// Set the ads position
			switch (MobileAdsManager.Instance.BannerAdHandler.GetBannerPosition())
			{
				case MobileAdsSettings.BannerPosition.Top:
					return AdPosition.Top;
				case MobileAdsSettings.BannerPosition.TopLeft:
					return AdPosition.TopLeft;
				case MobileAdsSettings.BannerPosition.TopRight:
					return AdPosition.TopRight;
				case MobileAdsSettings.BannerPosition.Bottom:
					return AdPosition.Bottom;
				case MobileAdsSettings.BannerPosition.BottomLeft:
					return AdPosition.BottomLeft;
				case MobileAdsSettings.BannerPosition.BottomRight:
					return AdPosition.BottomRight;
			}

			return AdPosition.Bottom;
		}

		/// <summary>
		/// Clears the current interstitial ad if one exists
		/// </summary>
		private void DestroyLoadedInterstitialAd()
		{
			if (interstitialAd != null)
			{
				interstitialAd.Destroy();
				interstitialAd = null;
			}
		}

		#region Banner Ad Events

		private void BannerAdLoaded(object sender, System.EventArgs e)
		{
			InvokeOnMainThread.Action((object[] obj) => 
			{
				BannerAdState = AdState.Loaded;
				NotifyBannerAdLoaded();

				if (showBannerAd && preLoadBannerAds)
				{
					ShowBannerAd();
				}
			});
		}

		private void BannerAdFailedToLoad(object sender, AdFailedToLoadEventArgs e)
		{
			InvokeOnMainThread.Action((object[] obj) => 
			{
				bannerView.Destroy();
				bannerView = null;

				if (BannerAdState == AdState.Shown)
				{
					NotifyBannerAdHidden();
				}

				BannerAdState = AdState.None;

				NotifyBannerAdFailedToLoad(e.Message);
			});
		}

		#endregion

		#region Interstitial Ad Events

		private void InterstitialAdLoaded(object sender, System.EventArgs e)
		{
			InvokeOnMainThread.Action((object[] obj) => 
			{
				NotifyInterstitialAdLoaded();
			});
		}

		private void InterstitialAdFailedToLoad(object sender, AdFailedToLoadEventArgs e)
		{
			InvokeOnMainThread.Action((object[] obj) => 
			{
				NotifyInterstitialAdFailedToLoad(e.Message);
			});
		}

		private void InterstitialAdClosed(object sender, System.EventArgs e)
		{
			InvokeOnMainThread.Action((object[] obj) => 
			{
				NotifyInterstitialAdClosed();

				PreLoadInterstitialAd();
			});
		}

		private void InterstitialAdOpening(object sender, System.EventArgs e)
		{
			InvokeOnMainThread.Action((object[] obj) => 
			{
				NotifyInterstitialAdShown();
			});
		}

		#endregion // Interstitial Ad Events

		#region Reward Ad Events

		private void RewardAdLoaded(object sender, System.EventArgs e)
		{
			InvokeOnMainThread.Action((object[] obj) => 
			{
				NotifyRewardAdLoaded();
			});
		}

		private void RewardAdFailedToLoad(object sender, AdFailedToLoadEventArgs e)
		{
			InvokeOnMainThread.Action((object[] obj) => 
			{
				NotifyRewardAdFailedToLoad(e.Message);
			});
		}

		private void RewardAdOpening(object sender, System.EventArgs e)
		{
			InvokeOnMainThread.Action((object[] obj) => 
			{
				NotifyRewardAdShown();
			});
		}

		private void RewardAdClosed(object sender, System.EventArgs e)
		{
			InvokeOnMainThread.Action((object[] obj) => 
			{
				NotifyRewardAdClosed();

				PreLoadRewardAd();
			});
		}

		private void RewardAdRewarded(object sender, Reward e)
		{
			InvokeOnMainThread.Action((object[] obj) => 
			{
				NotifyRewardAdGranted(e.Type, e.Amount);
			});
		}

		#endregion // Reward Ad Events

		#endif

		#endregion
	}
}
