using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Firebase Analytics provider.
/// Requires the Firebase Unity SDK to be installed.
/// When Firebase SDK is not yet installed, calls are silently no-oped so the game still compiles.
/// Once you install the SDK, define FIREBASE_ANALYTICS in Player Settings > Scripting Define Symbols.
/// </summary>
public class FirebaseAnalyticsProvider : IAnalyticsProvider
{
    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
#if FIREBASE_ANALYTICS
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var status = task.Result;
            if (status == Firebase.DependencyStatus.Available)
            {
                IsInitialized = true;
                Debug.Log("[Analytics] Firebase initialized successfully.");
            }
            else
            {
                Debug.LogError($"[Analytics] Firebase dependency error: {status}");
            }
        });
#else
        Debug.Log("[Analytics] Firebase SDK not installed (FIREBASE_ANALYTICS not defined). Events will be logged to console only.");
        IsInitialized = true;
#endif
    }

    public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
    {
#if FIREBASE_ANALYTICS
        if (!IsInitialized) return;

        if (parameters == null || parameters.Count == 0)
        {
            Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName);
        }
        else
        {
            var firebaseParams = new List<Firebase.Analytics.Parameter>();
            foreach (var kvp in parameters)
            {
                switch (kvp.Value)
                {
                    case int i:
                        firebaseParams.Add(new Firebase.Analytics.Parameter(kvp.Key, i));
                        break;
                    case long l:
                        firebaseParams.Add(new Firebase.Analytics.Parameter(kvp.Key, l));
                        break;
                    case float f:
                        firebaseParams.Add(new Firebase.Analytics.Parameter(kvp.Key, f));
                        break;
                    case double d:
                        firebaseParams.Add(new Firebase.Analytics.Parameter(kvp.Key, d));
                        break;
                    default:
                        firebaseParams.Add(new Firebase.Analytics.Parameter(kvp.Key, kvp.Value?.ToString() ?? ""));
                        break;
                }
            }
            Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName, firebaseParams.ToArray());
        }
#endif

        // Always log to console in debug builds for easy verification
        if (Debug.isDebugBuild || Application.isEditor)
        {
            string paramStr = "";
            if (parameters != null)
            {
                var parts = new List<string>();
                foreach (var kvp in parameters)
                    parts.Add($"{kvp.Key}={kvp.Value}");
                paramStr = " | " + string.Join(", ", parts);
            }
            Debug.Log($"[Analytics] {eventName}{paramStr}");
        }
    }

    public void SetUserProperty(string name, string value)
    {
#if FIREBASE_ANALYTICS
        if (!IsInitialized) return;
        Firebase.Analytics.FirebaseAnalytics.SetUserProperty(name, value);
#endif

        if (Debug.isDebugBuild || Application.isEditor)
            Debug.Log($"[Analytics] UserProperty: {name}={value}");
    }
}
