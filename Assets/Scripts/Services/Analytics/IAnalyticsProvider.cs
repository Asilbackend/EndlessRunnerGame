using System.Collections.Generic;

/// <summary>
/// Abstraction over any analytics backend (Firebase, Unity Analytics, etc.).
/// Implement this interface to swap providers without touching game code.
/// </summary>
public interface IAnalyticsProvider
{
    /// <summary>
    /// Initialize the provider. Called once at app start.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Log a named event with optional parameters.
    /// </summary>
    void LogEvent(string eventName, Dictionary<string, object> parameters = null);

    /// <summary>
    /// Set a persistent user property (survives across sessions).
    /// Useful for segmentation and future leaderboard identity.
    /// </summary>
    void SetUserProperty(string name, string value);

    /// <summary>
    /// Whether the provider has completed initialization.
    /// </summary>
    bool IsInitialized { get; }
}
