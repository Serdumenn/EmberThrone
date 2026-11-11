using System;
using UnityEngine;
#if UNITY_ANDROID
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
#endif

namespace EmberThrone.Game
{
    public sealed class AdService : MonoBehaviour
    {
        const string LastInterstitialKey = "et.ads.lastInterstitial";

        public static bool SimulateRewardedInEditor = true;

        public int RewardedShown { get; private set; }
        public int InterstitialRequests { get; private set; }
        public event Action PrivacyChanged;

        bool _initialized;
        bool _initializing;
        bool _showing;
        bool _loadingInterstitial;
        bool _loadingRewarded;
        bool _consentRequested;
        bool _consentReady;
        bool _rewardEarned;
        Action _afterInterstitial;
        Action _onRewarded;
        Action<bool> _onRewardedClosed;

#if UNITY_ANDROID
        InterstitialAd _interstitial;
        RewardedAd _rewarded;
#endif

        public bool IsRewardedReady
        {
            get
            {
                if (Application.isEditor) return SimulateRewardedInEditor;
#if UNITY_ANDROID
                return _initialized && _rewarded != null && _rewarded.CanShowAd();
#else
                return false;
#endif
            }
        }

        public bool PrivacyOptionsRequired
        {
            get
            {
#if UNITY_ANDROID
                if (Application.isEditor) return false;
                return _consentReady && ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
#else
                return false;
#endif
            }
        }

        void Start()
        {
            if (!Application.isEditor) Initialize();
        }

        void OnDestroy()
        {
            DestroyInterstitial();
            DestroyRewarded();
        }

        public void ShowRewarded(Action onRewarded, Action<bool> onClosed)
        {
            if (Application.isEditor)
            {
                bool watched = SimulateRewardedInEditor;
                if (watched)
                {
                    RewardedShown++;
                    onRewarded?.Invoke();
                }
                onClosed?.Invoke(watched);
                return;
            }

#if UNITY_ANDROID
            if (_showing || _rewarded == null || !_rewarded.CanShowAd())
            {
                LoadRewarded();
                onClosed?.Invoke(false);
                return;
            }

            _showing = true;
            _rewardEarned = false;
            _onRewarded = onRewarded;
            _onRewardedClosed = onClosed;
            RewardedShown++;
            _rewarded.Show(_ => MobileAdsEventExecutor.ExecuteInUpdate(() => _rewardEarned = true));
#else
            onClosed?.Invoke(false);
#endif
        }

        public void ShowInterstitialThen(Action onComplete)
        {
            InterstitialRequests++;
            if (onComplete == null) onComplete = () => { };
            if (Application.isEditor)
            {
                onComplete();
                return;
            }

#if UNITY_ANDROID
            if (!_initialized || _showing || CooldownActive() || _interstitial == null || !_interstitial.CanShowAd())
            {
                onComplete();
                Initialize();
                LoadInterstitial();
                return;
            }

            _showing = true;
            _afterInterstitial = onComplete;
            _interstitial.Show();
#else
            onComplete();
#endif
        }

        public void ShowPrivacyOptions(Action<bool> onDone)
        {
#if UNITY_ANDROID
            if (Application.isEditor)
            {
                onDone?.Invoke(false);
                return;
            }
            ConsentForm.ShowPrivacyOptionsForm(error => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (error != null) Debug.LogWarning("[EmberThrone] Privacy options failed: " + error.Message);
                PrivacyChanged?.Invoke();
                if (ConsentInformation.CanRequestAds()) Initialize();
                onDone?.Invoke(error == null);
            }));
#else
            onDone?.Invoke(false);
#endif
        }

        void Initialize()
        {
            if (_initialized || _initializing) return;
#if UNITY_ANDROID
            if (!_consentReady)
            {
                RequestConsent();
                if (!_consentReady) return;
            }
            if (!ConsentInformation.CanRequestAds()) return;

            _initializing = true;
            MobileAds.Initialize(_ => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                _initializing = false;
                _initialized = true;
                LoadInterstitial();
                LoadRewarded();
            }));
#endif
        }

#if UNITY_ANDROID
        void RequestConsent()
        {
            if (_consentRequested) return;
            _consentRequested = true;

            ConsentInformation.Update(new ConsentRequestParameters(), updateError => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (updateError != null) Debug.LogWarning("[EmberThrone] Consent info failed: " + updateError.Message);
                ConsentForm.LoadAndShowConsentFormIfRequired(formError => MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (formError != null) Debug.LogWarning("[EmberThrone] Consent form failed: " + formError.Message);
                    _consentReady = true;
                    PrivacyChanged?.Invoke();
                    if (ConsentInformation.CanRequestAds()) Initialize();
                }));
            }));

            if (ConsentInformation.CanRequestAds())
            {
                _consentReady = true;
                Initialize();
            }
        }

        void LoadInterstitial()
        {
            if (!_initialized || _loadingInterstitial || _showing) return;
            _loadingInterstitial = true;
            DestroyInterstitial();
            InterstitialAd.Load(AdRules.InterstitialUnitId, new AdRequest(), (ad, error) => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                _loadingInterstitial = false;
                if (error != null || ad == null) return;
                _interstitial = ad;
                ad.OnAdFullScreenContentClosed += () => MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    _showing = false;
                    PlayerPrefs.SetString(LastInterstitialKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                    PlayerPrefs.Save();
                    FinishInterstitial();
                    LoadInterstitial();
                });
                ad.OnAdFullScreenContentFailed += _ => MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    _showing = false;
                    FinishInterstitial();
                    LoadInterstitial();
                });
            }));
        }

        void LoadRewarded()
        {
            if (!_initialized || _loadingRewarded) return;
            if (_rewarded != null && _rewarded.CanShowAd()) return;
            _loadingRewarded = true;
            DestroyRewarded();
            RewardedAd.Load(AdRules.RewardedUnitId, new AdRequest(), (ad, error) => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                _loadingRewarded = false;
                if (error != null || ad == null) return;
                _rewarded = ad;
                ad.OnAdFullScreenContentClosed += () => MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    _showing = false;
                    FinishRewarded();
                    LoadRewarded();
                });
                ad.OnAdFullScreenContentFailed += _ => MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    _showing = false;
                    _rewardEarned = false;
                    FinishRewarded();
                    LoadRewarded();
                });
            }));
        }
#endif

        void FinishInterstitial()
        {
            var after = _afterInterstitial;
            _afterInterstitial = null;
            after?.Invoke();
        }

        void FinishRewarded()
        {
            var onRewarded = _onRewarded;
            var onClosed = _onRewardedClosed;
            bool earned = _rewardEarned;
            _onRewarded = null;
            _onRewardedClosed = null;
            _rewardEarned = false;
            if (earned) onRewarded?.Invoke();
            onClosed?.Invoke(earned);
        }

        static bool CooldownActive()
        {
            long last = long.TryParse(PlayerPrefs.GetString(LastInterstitialKey, "0"), out long value) ? value : 0;
            if (last <= 0) return false;
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() - last < AdRules.InterstitialCooldownSeconds;
        }

        void DestroyInterstitial()
        {
#if UNITY_ANDROID
            if (_interstitial == null) return;
            _interstitial.Destroy();
            _interstitial = null;
#endif
        }

        void DestroyRewarded()
        {
#if UNITY_ANDROID
            if (_rewarded == null) return;
            _rewarded.Destroy();
            _rewarded = null;
#endif
        }
    }
}
