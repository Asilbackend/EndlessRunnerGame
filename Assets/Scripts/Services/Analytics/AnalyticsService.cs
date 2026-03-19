using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton facade for analytics. Initializes the provider on Awake and exposes
/// static convenience methods so callers don't need a reference to the instance.
/// Follows the same pattern as AdService.
/// </summary>
public class AnalyticsService : MonoBehaviour
{
    public static AnalyticsService Instance { get; private set; }

    private IAnalyticsProvider _provider;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _provider = new FirebaseAnalyticsProvider();
        _provider.Initialize();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ── Static convenience methods ─────────────────────────────────────────────

    public static void LogEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        Instance?._provider?.LogEvent(eventName, parameters);
    }

    public static void SetUserProperty(string name, string value)
    {
        Instance?._provider?.SetUserProperty(name, value);
    }
}
