# Audio Manager - Complete Setup Guide

## Overview
The AudioManager is a persistent, singleton-based audio system that handles both music and sound effects with clean architecture, volume control, and crossfading.

---

## Setup Instructions

### Step 1: Create the AudioManager Prefab
1. Create a new empty GameObject named `AudioManager`
2. Add the `AudioManager` component from `Scripts/Managers/AudioManager.cs`
3. The script will auto-create:
   - `MusicSource` (for background music)
   - `SFXSources` (pool of audio sources for sound effects)

### Step 2: Create AudioConfig ScriptableObject
1. Right-click in your Assets folder → Create → AudioConfig
2. Name it `GameAudioConfig` (or similar)
3. Assign it to the `AudioManager` component's `Audio Config` field

### Step 3: Add Audio Clips to AudioConfig
1. Select your `GameAudioConfig` in the Inspector
2. In the `Categories` list, add new categories:
   - `Music` (for background tracks)
   - `SFX` (for sound effects)
   - `UI` (for menu sounds)
   - etc.

3. For each category, add clips with **unique names**:
   ```
   Category: Music
   ├── Music_MainMenu
   ├── Music_Gameplay
   └── Music_GameOver

   Category: SFX
   ├── SFX_Jump
   ├── SFX_Collect
   ├── SFX_Collision
   └── SFX_MenuClick
   ```

**Important:** Clip names are case-insensitive but must be unique across ALL categories.

### Step 4: Make AudioManager Persistent
Drag the `AudioManager` prefab to `Assets/Resources/Prefabs/` (create the folder if needed) so it persists across scenes.

---

## Usage Examples

### Playing Music
```csharp
// Play music with 1 second crossfade
AudioManager.Instance.PlayMusic(audioConfig.GetMusicClip("Music_Gameplay"));

// Play music with custom fade duration
AudioManager.Instance.PlayMusic(clip, loop: true, customFadeDuration: 2f);

// Stop music with fade out
AudioManager.Instance.StopMusic(customFadeDuration: 1f);

// Pause/Resume
AudioManager.Instance.PauseMusic();
AudioManager.Instance.ResumeMusic();
```

### Playing Sound Effects
```csharp
// Play SFX by name (simplest method)
AudioManager.Instance.PlaySFX("SFX_Jump");

// Play SFX with custom volume and pitch
AudioManager.Instance.PlaySFX("SFX_Collect", volume: 0.8f, pitch: 1.2f);

// Play SFX from AudioClip directly
AudioManager.Instance.PlaySFXClip(myClip, volume: 1f, pitch: 1f);
```

### Volume Control
```csharp
// Set individual volumes (0-1)
AudioManager.Instance.SetMusicVolume(0.7f);
AudioManager.Instance.SetSFXVolume(0.8f);

// Set master volume (affects all)
AudioManager.Instance.SetMasterVolume(0.5f);

// Mute/Unmute
AudioManager.Instance.MuteAll();
AudioManager.Instance.UnmuteAll();

// Check if muted
if (AudioManager.Instance.IsMuted)
{
    // Handle muted state
}
```

### Checking Audio State
```csharp
// Get current music
AudioClip current = AudioManager.Instance.CurrentMusicClip;

// Check if music is playing
if (AudioManager.Instance.IsMusicPlaying)
{
    // Music is playing
}
```

---

## Integration with Your Game

### In GameController
```csharp
public class GameController : MonoBehaviour
{
    private void Start()
    {
        // Play background music when game starts
        AudioManager.Instance.PlayMusic(audioConfig.GetMusicClip("Music_Gameplay"));
    }

    public void GameOver()
    {
        // Play game over music
        AudioManager.Instance.PlayMusic(audioConfig.GetMusicClip("Music_GameOver"));

        // Play game over sound
        AudioManager.Instance.PlaySFX("SFX_GameOver");
    }
}
```

### In PlayerController (Obstacle Collision)
```csharp
private void OnCollided()
{
    AudioManager.Instance.PlaySFX("SFX_Collision", volume: 1f, pitch: 1.2f);
    GameController.Instance.GameOver();
}
```

### In WorldCollectible (Pickup)
```csharp
public void OnCollided()
{
    // Play collection sound with slight pitch variation
    AudioManager.Instance.PlaySFX("SFX_Collect", pitch: Random.Range(0.9f, 1.1f));
    _isCollected = true;
}
```

### In Menu/Settings
```csharp
public class SettingsPanel : MonoBehaviour
{
    public void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance.SetMusicVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance.SetSFXVolume(value);
    }

    public void OnMuteToggle(bool muted)
    {
        if (muted)
            AudioManager.Instance.MuteAll();
        else
            AudioManager.Instance.UnmuteAll();
    }
}
```

---

## Key Features

✅ **Persistent Singleton** - Survives scene changes
✅ **Auto-pooling** - Efficient SFX playback (8 concurrent sources)
✅ **Music Crossfading** - Smooth transitions between tracks
✅ **Volume Control** - Master, Music, and SFX controls
✅ **Mute System** - Toggleable mute with volume state preservation
✅ **Easy Organization** - Category-based clip management
✅ **Flexible** - Play by name OR by AudioClip reference

---

## Best Practices

1. **Naming Convention:** Use prefixes for clarity
   - `Music_` for background tracks
   - `SFX_` for effects
   - `UI_` for menu sounds

2. **Avoid Redundancy:** Don't create multiple instances of the AudioManager. The singleton handles this.

3. **Volume Levels:** Start with these defaults:
   - Master: 1.0
   - Music: 0.7 (music is usually quieter)
   - SFX: 0.8

4. **Pitch Variation:** Add natural variation to repetitive sounds:
   ```csharp
   AudioManager.Instance.PlaySFX("SFX_Step", pitch: Random.Range(0.95f, 1.05f));
   ```

5. **Wait for Clip Names:** Always double-check spelling when calling `PlaySFX()` by name (case-insensitive is fine).

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| "SFX not found" warning | Check the clip name in AudioConfig matches exactly |
| No music playing | Verify `AudioConfig` is assigned and contains clips |
| AudioManager missing | Ensure `Resources/Prefabs/` exists and prefab is there |
| Volume not changing | Call `SetMusicVolume()` or `SetSFXVolume()`, not just `masterVolume` |
| Clips overlapping | Increase `SFX Pool Size` in AudioManager (default: 8) |

---

## Next Steps

1. Create your `AudioConfig` ScriptableObject
2. Organize your audio clips into categories
3. Add clips to the config
4. Assign the config to AudioManager
5. Use the examples above to integrate into your game
6. Test and adjust volumes/crossfade times as needed
