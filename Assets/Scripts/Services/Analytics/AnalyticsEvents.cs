using System.Collections.Generic;

/// <summary>
/// Typed analytics event methods. All event names and parameter keys are defined here
/// so no magic strings are scattered across the codebase.
/// Each method builds a parameter dictionary and forwards to AnalyticsService.
/// </summary>
public static class AnalyticsEvents
{
    // ── Event Names ────────────────────────────────────────────────────────────
    private const string EVENT_GAME_START        = "game_start";
    private const string EVENT_GAME_OVER         = "game_over";
    private const string EVENT_CHECKPOINT_RESUME = "checkpoint_resume";
    private const string EVENT_AD_WATCHED        = "ad_watched";
    private const string EVENT_POWERUP_COLLECTED = "powerup_collected";
    private const string EVENT_VEHICLE_SELECTED  = "vehicle_selected";
    private const string EVENT_MAP_SELECTED      = "map_selected";
    private const string EVENT_COIN_STREAK       = "coin_streak";
    private const string EVENT_LEVEL_DIFFICULTY   = "difficulty_reached";

    // ── Parameter Keys ─────────────────────────────────────────────────────────
    private const string PARAM_VEHICLE_ID        = "vehicle_id";
    private const string PARAM_MAP_ID            = "map_id";
    private const string PARAM_GAME_NUMBER       = "game_number";
    private const string PARAM_SCORE             = "score";
    private const string PARAM_DISTANCE          = "distance";
    private const string PARAM_HEALTH_REMAINING  = "health_remaining";
    private const string PARAM_MEDAL             = "medal";
    private const string PARAM_IS_NEW_HIGH_SCORE = "is_new_high_score";
    private const string PARAM_IS_NEW_DISTANCE   = "is_new_distance_record";
    private const string PARAM_REWARD_TYPE       = "reward_type";
    private const string PARAM_POWERUP_TYPE      = "powerup_type";
    private const string PARAM_STREAK_COUNT      = "streak_count";
    private const string PARAM_BONUS_POINTS      = "bonus_points";
    private const string PARAM_DIFFICULTY        = "difficulty";
    private const string PARAM_CHUNKS_PASSED     = "chunks_passed";

    // ── User Properties (for segmentation & future leaderboard) ────────────────
    private const string PROP_HIGHEST_SCORE      = "highest_score";
    private const string PROP_HIGHEST_DISTANCE   = "highest_distance";
    private const string PROP_TOTAL_GAMES        = "total_games_played";
    private const string PROP_FAVORITE_VEHICLE   = "favorite_vehicle";

    // ── Event Methods ──────────────────────────────────────────────────────────

    /// <summary>
    /// Fired when a game run begins (after countdown).
    /// </summary>
    public static void GameStart(string vehicleId, string mapId, int gameNumber)
    {
        AnalyticsService.LogEvent(EVENT_GAME_START, new Dictionary<string, object>
        {
            { PARAM_VEHICLE_ID, vehicleId },
            { PARAM_MAP_ID, mapId },
            { PARAM_GAME_NUMBER, gameNumber }
        });
    }

    /// <summary>
    /// Fired when the player dies and the game over screen appears.
    /// This is the most critical event for understanding player behavior.
    /// </summary>
    public static void GameOver(int score, float distance, int healthRemaining,
        string medal, string vehicleId, string mapId,
        bool isNewHighScore, bool isNewDistanceRecord)
    {
        AnalyticsService.LogEvent(EVENT_GAME_OVER, new Dictionary<string, object>
        {
            { PARAM_SCORE, score },
            { PARAM_DISTANCE, distance },
            { PARAM_HEALTH_REMAINING, healthRemaining },
            { PARAM_MEDAL, medal },
            { PARAM_VEHICLE_ID, vehicleId },
            { PARAM_MAP_ID, mapId },
            { PARAM_IS_NEW_HIGH_SCORE, isNewHighScore ? 1 : 0 },
            { PARAM_IS_NEW_DISTANCE, isNewDistanceRecord ? 1 : 0 }
        });

        // Update user properties for segmentation & future leaderboard
        if (isNewHighScore)
            AnalyticsService.SetUserProperty(PROP_HIGHEST_SCORE, score.ToString());
        if (isNewDistanceRecord)
            AnalyticsService.SetUserProperty(PROP_HIGHEST_DISTANCE, distance.ToString("F1"));
    }

    /// <summary>
    /// Fired when the player resumes from the last checkpoint.
    /// </summary>
    public static void CheckpointResume(int scoreAtCheckpoint, float distanceAtCheckpoint)
    {
        AnalyticsService.LogEvent(EVENT_CHECKPOINT_RESUME, new Dictionary<string, object>
        {
            { PARAM_SCORE, scoreAtCheckpoint },
            { PARAM_DISTANCE, distanceAtCheckpoint }
        });
    }

    /// <summary>
    /// Fired when a rewarded ad is watched to completion.
    /// </summary>
    public static void AdWatched(string rewardType)
    {
        AnalyticsService.LogEvent(EVENT_AD_WATCHED, new Dictionary<string, object>
        {
            { PARAM_REWARD_TYPE, rewardType }
        });
    }

    /// <summary>
    /// Fired when a powerup is collected during gameplay.
    /// </summary>
    public static void PowerupCollected(string powerupType)
    {
        AnalyticsService.LogEvent(EVENT_POWERUP_COLLECTED, new Dictionary<string, object>
        {
            { PARAM_POWERUP_TYPE, powerupType }
        });
    }

    /// <summary>
    /// Fired when the player selects a vehicle in the menu.
    /// </summary>
    public static void VehicleSelected(string vehicleId)
    {
        AnalyticsService.LogEvent(EVENT_VEHICLE_SELECTED, new Dictionary<string, object>
        {
            { PARAM_VEHICLE_ID, vehicleId }
        });

        AnalyticsService.SetUserProperty(PROP_FAVORITE_VEHICLE, vehicleId);
    }

    /// <summary>
    /// Fired when the player selects a map in the menu.
    /// </summary>
    public static void MapSelected(string mapId)
    {
        AnalyticsService.LogEvent(EVENT_MAP_SELECTED, new Dictionary<string, object>
        {
            { PARAM_MAP_ID, mapId }
        });
    }

    /// <summary>
    /// Fired when a coin streak ends and the bonus is committed.
    /// </summary>
    public static void CoinStreak(int streakCount, int bonusPoints)
    {
        AnalyticsService.LogEvent(EVENT_COIN_STREAK, new Dictionary<string, object>
        {
            { PARAM_STREAK_COUNT, streakCount },
            { PARAM_BONUS_POINTS, bonusPoints }
        });
    }

    /// <summary>
    /// Fired when the player reaches a new difficulty tier.
    /// </summary>
    public static void DifficultyReached(string difficulty, int chunksPassed)
    {
        AnalyticsService.LogEvent(EVENT_LEVEL_DIFFICULTY, new Dictionary<string, object>
        {
            { PARAM_DIFFICULTY, difficulty },
            { PARAM_CHUNKS_PASSED, chunksPassed }
        });
    }

    /// <summary>
    /// Update total games played user property. Call after incrementing game number.
    /// </summary>
    public static void UpdateTotalGamesPlayed(int totalGames)
    {
        AnalyticsService.SetUserProperty(PROP_TOTAL_GAMES, totalGames.ToString());
    }
}
