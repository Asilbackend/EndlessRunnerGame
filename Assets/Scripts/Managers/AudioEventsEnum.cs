/// <summary>
/// Enum for all audio events in the game.
/// Use these instead of string names to avoid typos and for compile-time safety.
/// </summary>
public enum AudioEventSFX
{
    // Movement
    LaneChangeLeft,
    LaneChangeRight,
    Jump,
    Land,

    // Obstacles & Collectibles
    Collision,
    Collect,
    DynamicObstacleWarning,

    // UI
    ButtonClick,
    ButtonHover,
    MenuOpen,
    MenuClose,
    SelectVehicle,

    // Checkpoint/Game Over
    GameOver,
    CheckpointRewind,

    // Powerups
    PowerupMagnet,
    PowerupHealth,
    PowerupDoubleLane,
    PowerupDoubleCoin,
    PowerupInvincibility,
    PowerupInvincibilityDeflect,
}

public enum AudioEventMusic
{
    MainMenu,
    Gameplay,
}

/// <summary>
/// Helper extension to convert enum to string for AudioManager.
/// </summary>
public static class AudioEventExtensions
{
    public static string ToAudioName(this AudioEventSFX sfx)
    {
        return $"SFX_{sfx}";
    }

    public static string ToAudioName(this AudioEventMusic music)
    {
        return $"Music_{music}";
    }
}
