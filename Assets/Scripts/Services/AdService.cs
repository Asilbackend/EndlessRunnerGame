using System;
using UnityEngine;
using UnityEngine.Advertisements;

/// <summary>
/// Rewarded ad service using Unity Ads SDK.
/// Attach this to a persistent GameObject in your first scene (or let it self-create).
/// Fill in your Game IDs and Ad Unit ID in the Inspector.
/// </summary>
public class AdService : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
    public static AdService Instance { get; private set; }

    [Header("Unity Ads Settings")]
    [SerializeField] private string androidGameId = "YOUR_ANDROID_GAME_ID";
    [SerializeField] private string iosGameId     = "YOUR_IOS_GAME_ID";
    [SerializeField] private string adUnitId      = "Rewarded_Android"; // change per platform if needed
    [SerializeField] private bool testMode        = true;

    private Action _onRewardEarned;
    private bool _adLoaded = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAds();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void InitializeAds()
    {
        string gameId = Application.platform == RuntimePlatform.IPhonePlayer ? iosGameId : androidGameId;
        if (!Advertisement.isInitialized)
            Advertisement.Initialize(gameId, testMode, this);
    }

    // ── IUnityAdsInitializationListener ─────────────────────────────────────

    public void OnInitializationComplete()
    {
        Debug.Log("[AdService] Unity Ads initialized.");
        LoadAd();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogWarning($"[AdService] Initialization failed: {error} — {message}");
    }

    // ── Load ─────────────────────────────────────────────────────────────────

    private void LoadAd()
    {
        _adLoaded = false;
        Advertisement.Load(adUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.Log($"[AdService] Ad loaded: {placementId}");
        _adLoaded = true;
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogWarning($"[AdService] Failed to load ad '{placementId}': {error} — {message}");
        _adLoaded = false;
    }

    // ── Show ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Show a rewarded ad. onRewardEarned is called only if the user watches to completion.
    /// </summary>
    public void ShowRewardedAd(Action onRewardEarned)
    {
        if (!_adLoaded)
        {
            Debug.LogWarning("[AdService] Ad not ready yet.");
            // Optionally notify the player with a toast/popup instead of silently failing.
            return;
        }

        _onRewardEarned = onRewardEarned;
        Advertisement.Show(adUnitId, this);
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            Debug.Log("[AdService] Rewarded ad completed — granting reward.");
            _onRewardEarned?.Invoke();
        }
        else
        {
            Debug.Log($"[AdService] Ad not completed ({showCompletionState}), no reward given.");
        }

        _onRewardEarned = null;
        LoadAd(); // pre-load the next ad
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogWarning($"[AdService] Show failed '{placementId}': {error} — {message}");
        _onRewardEarned = null;
    }

    public void OnUnityAdsShowStart(string placementId) { }
    public void OnUnityAdsShowClick(string placementId) { }
}
