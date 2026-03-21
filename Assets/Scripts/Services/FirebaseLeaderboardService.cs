using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// Global leaderboard service backed by Firebase Firestore.
/// Uses anonymous Firebase Authentication for a stable per-device user ID.
///
/// Setup:
///   1. Install the Firebase Unity SDK (Auth + Firestore modules).
///   2. Add FIREBASE_FIRESTORE to Player Settings > Scripting Define Symbols.
///
/// Without the define, all writes are no-ops and fetches return mock data,
/// so the game still compiles and runs during development.
/// </summary>
public class FirebaseLeaderboardService : MonoBehaviour
{
    public static FirebaseLeaderboardService Instance { get; private set; }

    /// <summary>True once Firebase Auth has signed in and Firestore is ready.</summary>
    public bool IsReady { get; private set; }

    /// <summary>True if the player has set a display name.</summary>
    public bool HasDisplayName =>
        !string.IsNullOrWhiteSpace(
            PlayerPrefsManager.GetString(PlayerPrefsKeys.PlayerDisplayName, "")
        );

    // Firestore collection names — one per leaderboard category
    private const string ColHighScore = "leaderboard_highscore";
    private const string ColDistance = "leaderboard_distance";
    private const string ColCoins = "leaderboard_coins";

    private SynchronizationContext _mainThread;

#if FIREBASE_FIRESTORE
    private Firebase.Firestore.FirebaseFirestore _db;
#endif

    // Always available regardless of define so GameController can call GetUserId()
    private string _userId;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _mainThread = SynchronizationContext.Current;
        Initialize();
    }

    private void Initialize()
    {
#if FIREBASE_FIRESTORE
        Firebase
            .FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWith(task =>
            {
                if (task.Result != Firebase.DependencyStatus.Available)
                {
                    Debug.LogError($"[Leaderboard] Firebase dependency error: {task.Result}");
                    return;
                }
                _db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
                SignInAnonymously();
            });
#else
        _userId = GetOrCreateLocalId();
        IsReady = true;
        Debug.Log("[Leaderboard] FIREBASE_FIRESTORE not defined — running with mock data.");
#endif
    }

#if FIREBASE_FIRESTORE
    private void SignInAnonymously()
    {
        var auth = Firebase.Auth.FirebaseAuth.DefaultInstance;

        if (auth.CurrentUser != null)
        {
            _userId = auth.CurrentUser.UserId;
            PlayerPrefsManager.SetString(PlayerPrefsKeys.PlayerId, _userId);
            IsReady = true;
            Debug.Log($"[Leaderboard] Reusing Firebase UID: {_userId}");
            return;
        }

        auth.SignInAnonymouslyAsync()
            .ContinueWith(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError($"[Leaderboard] Anonymous sign-in failed: {task.Exception}");
                    return;
                }
                _userId = task.Result.UserId;
                PlayerPrefsManager.SetString(PlayerPrefsKeys.PlayerId, _userId);
                IsReady = true;
                Debug.Log($"[Leaderboard] Signed in anonymously. UID: {_userId}");
            });
    }
#else
    private static string GetOrCreateLocalId()
    {
        string id = PlayerPrefsManager.GetString(PlayerPrefsKeys.PlayerId, "");
        if (!string.IsNullOrEmpty(id))
            return id;
        id = Guid.NewGuid().ToString("N").Substring(0, 12);
        PlayerPrefsManager.SetString(PlayerPrefsKeys.PlayerId, id);
        return id;
    }
#endif

    // ── Display Name ───────────────────────────────────────────────────────────

    public string GetDisplayName() =>
        PlayerPrefsManager.GetString(PlayerPrefsKeys.PlayerDisplayName, "");

    /// <summary>
    /// Save the player's chosen display name. Called from NameEntryPanel.
    /// </summary>
    public void SetDisplayName(string displayName)
    {
        displayName = displayName.Trim();
        if (string.IsNullOrEmpty(displayName))
            return;
        PlayerPrefsManager.SetString(PlayerPrefsKeys.PlayerDisplayName, displayName);
        Debug.Log($"[Leaderboard] Display name set to: {displayName}");
    }

    public string GetUserId() => _userId ?? "";

    // ── Submit ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called after every game over with the player's all-time best values.
    /// </summary>
    public void SubmitScores(int highScore, float highestDistance, int totalCoins)
    {
        if (!IsReady || !HasDisplayName)
            return;

        string name = GetDisplayName();

#if FIREBASE_FIRESTORE
        WriteDoc(ColHighScore, name, highScore);
        WriteDoc(ColDistance, name, highestDistance);
        WriteDoc(ColCoins, name, totalCoins);
#else
        Debug.Log(
            $"[Leaderboard] (mock) submit — score:{highScore}  dist:{highestDistance}  coins:{totalCoins}"
        );
#endif
    }

#if FIREBASE_FIRESTORE
    private void WriteDoc(string collection, string displayName, double value)
    {
        var data = new Dictionary<string, object>
        {
            { "displayName", displayName },
            { "value", value },
            { "updatedAt", Firebase.Firestore.FieldValue.ServerTimestamp },
        };

        _db.Collection(collection)
            .Document(_userId)
            .SetAsync(data, Firebase.Firestore.SetOptions.MergeAll)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Debug.LogError($"[Leaderboard] Write to '{collection}' failed: {t.Exception}");
            });
    }
#endif

    // ── Fetch ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches the top <paramref name="limit"/> entries for the given category.
    /// <paramref name="onComplete"/> is always invoked on the main thread.
    /// </summary>
    public void FetchTopScores(
        LeaderboardCategory category,
        int limit,
        Action<List<LeaderboardEntry>> onComplete
    )
    {
        if (!IsReady)
        {
            _mainThread.Post(_ => onComplete?.Invoke(new List<LeaderboardEntry>()), null);
            return;
        }

#if FIREBASE_FIRESTORE
        string col = CollectionFor(category);
        _db.Collection(col)
            .OrderByDescending("value")
            .Limit(limit)
            .GetSnapshotAsync()
            .ContinueWith(task =>
            {
                List<LeaderboardEntry> entries;

                if (task.IsFaulted)
                {
                    Debug.LogError($"[Leaderboard] Fetch '{col}' failed: {task.Exception}");
                    entries = new List<LeaderboardEntry>();
                }
                else
                {
                    entries = new List<LeaderboardEntry>();
                    int rank = 1;
                    string myId = _userId ?? "";
                    foreach (var doc in task.Result.Documents)
                    {
                        string name = doc.ContainsField("displayName")
                            ? doc.GetValue<string>("displayName")
                            : "Unknown";
                        double value = doc.ContainsField("value")
                            ? doc.GetValue<double>("value")
                            : 0.0;
                        entries.Add(
                            new LeaderboardEntry
                            {
                                Rank = rank++,
                                DisplayName = name,
                                Value = value,
                                IsCurrentPlayer = doc.Id == myId,
                            }
                        );
                    }
                }

                _mainThread.Post(_ => onComplete?.Invoke(entries), null);
            });
#else
        // Mock data for UI development
        var mock = new List<LeaderboardEntry>();
        string myName = GetDisplayName();
        double[] values =
            category == LeaderboardCategory.LongestRun
                ? new double[] { 9500, 6200, 4100, 2300, 1800 }
                : new double[] { 85000, 62000, 41500, 27000, 14000 };

        for (int i = 0; i < Mathf.Min(limit, values.Length); i++)
        {
            bool isMe = i == 2;
            mock.Add(
                new LeaderboardEntry
                {
                    Rank = i + 1,
                    DisplayName = isMe ? myName : $"Player_{(i + 1):D4}",
                    Value = values[i],
                    IsCurrentPlayer = isMe,
                }
            );
        }

        _mainThread.Post(_ => onComplete?.Invoke(mock), null);
#endif
    }

    // ── Player Rank ────────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches the current player's rank for a given category by counting how many
    /// players have a strictly higher value (requires Firebase SDK 10+ for Count aggregation).
    /// <paramref name="onComplete"/> is always called on the main thread with a
    /// LeaderboardEntry containing the player's rank, name, and value.
    /// </summary>
    public void FetchPlayerRank(LeaderboardCategory category,
                                Action<LeaderboardEntry> onComplete)
    {
        if (!IsReady || !HasDisplayName)
        {
            _mainThread.Post(_ => onComplete?.Invoke(null), null);
            return;
        }

#if FIREBASE_FIRESTORE
        // Get the player's local best value — this is always up to date because
        // GameController writes to PlayerPrefs before calling SubmitScores.
        double myValue = LocalValueFor(category);
        string col     = CollectionFor(category);

        // Count how many players score strictly above ours → rank = count + 1
        _db.Collection(col)
           .WhereGreaterThan("value", myValue)
           .Count
           .GetSnapshotAsync()
           .ContinueWith(task =>
           {
               LeaderboardEntry entry;
               if (task.IsFaulted)
               {
                   Debug.LogError($"[Leaderboard] Rank count '{col}' failed: {task.Exception}");
                   entry = null;
               }
               else
               {
                   int rank = (int)task.Result.Count + 1;
                   entry = new LeaderboardEntry
                   {
                       Rank            = rank,
                       DisplayName     = GetDisplayName(),
                       Value           = myValue,
                       IsCurrentPlayer = true
                   };
               }
               _mainThread.Post(_ => onComplete?.Invoke(entry), null);
           });
#else
        // Mock: pretend the player is ranked 42nd
        double mockValue = LocalValueFor(category);
        var mockEntry = new LeaderboardEntry
        {
            Rank            = 42,
            DisplayName     = GetDisplayName(),
            Value           = mockValue,
            IsCurrentPlayer = true
        };
        _mainThread.Post(_ => onComplete?.Invoke(mockEntry), null);
#endif
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static double LocalValueFor(LeaderboardCategory category) => category switch
    {
        LeaderboardCategory.HighScore  => PlayerPrefsManager.GetInt(PlayerPrefsKeys.HighestScore, 0),
        LeaderboardCategory.LongestRun => PlayerPrefsManager.GetFloat(PlayerPrefsKeys.HighestDistance, 0f),
        LeaderboardCategory.TotalCoins => PlayerPrefsManager.GetInt(PlayerPrefsKeys.TotalCoinsCollected, 0),
        _                              => 0
    };

    private static string CollectionFor(LeaderboardCategory cat) =>
        cat switch
        {
            LeaderboardCategory.HighScore  => ColHighScore,
            LeaderboardCategory.LongestRun => ColDistance,
            LeaderboardCategory.TotalCoins => ColCoins,
            _                              => ColHighScore,
        };
}

// ── Shared data types ──────────────────────────────────────────────────────────

public enum LeaderboardCategory
{
    HighScore,
    LongestRun,
    TotalCoins,
}

public class LeaderboardEntry
{
    public int Rank;
    public string DisplayName;
    public double Value;
    public bool IsCurrentPlayer;
}
