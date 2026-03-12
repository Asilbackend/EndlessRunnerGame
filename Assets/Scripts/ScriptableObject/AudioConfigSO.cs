using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SFXClipEntry
{
    public AudioEventSFX sfxEvent;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 2f)] public float pitch = 1f;
}

[System.Serializable]
public class MusicClipEntry
{
    public AudioEventMusic musicEvent;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 2f)] public float pitch = 1f;
}

/// <summary>
/// AudioConfig ScriptableObject for managing all game audio clips.
/// Uses enums for type-safe configuration - no string typos possible!
/// </summary>
[CreateAssetMenu(fileName = "AudioConfig", menuName = "Audio/Audio Config")]
public class AudioConfigSO : ScriptableObject
{
    public List<SFXClipEntry> sfxClips = new List<SFXClipEntry>();
    public List<MusicClipEntry> musicClips = new List<MusicClipEntry>();

    private Dictionary<string, AudioClip> clipCache;

    private void OnEnable()
    {
        BuildCache();
    }

    private void BuildCache()
    {
        clipCache = new Dictionary<string, AudioClip>();

        // Cache SFX clips by their enum names
        foreach (var entry in sfxClips)
        {
            if (entry.clip != null)
            {
                string key = entry.sfxEvent.ToAudioName().ToLower();
                if (!clipCache.ContainsKey(key))
                {
                    clipCache[key] = entry.clip;
                }
                else
                {
                    Debug.LogWarning($"AudioConfig: Duplicate clip for SFX event '{entry.sfxEvent}' found");
                }
            }
        }

        // Cache Music clips by their enum names
        foreach (var entry in musicClips)
        {
            if (entry.clip != null)
            {
                string key = entry.musicEvent.ToAudioName().ToLower();
                if (!clipCache.ContainsKey(key))
                {
                    clipCache[key] = entry.clip;
                }
                else
                {
                    Debug.LogWarning($"AudioConfig: Duplicate clip for Music event '{entry.musicEvent}' found");
                }
            }
        }
    }

    /// <summary>
    /// Get an audio clip by name (case-insensitive). Used internally by AudioManager.
    /// </summary>
    public AudioClip GetSFXClip(string clipName)
    {
        if (clipCache == null)
            BuildCache();

        string key = clipName.ToLower();
        if (clipCache.TryGetValue(key, out AudioClip clip))
        {
            return clip;
        }

        Debug.LogWarning($"AudioConfig: Clip '{clipName}' not found");
        return null;
    }

    /// <summary>
    /// Get volume and pitch for a specific SFX event.
    /// </summary>
    public void GetSFXSettings(AudioEventSFX sfxEvent, out float volume, out float pitch)
    {
        volume = 1f;
        pitch = 1f;

        foreach (var entry in sfxClips)
        {
            if (entry.sfxEvent == sfxEvent)
            {
                volume = entry.volume;
                pitch = entry.pitch;
                return;
            }
        }

        Debug.LogWarning($"AudioConfig: SFX event '{sfxEvent}' not found in config");
    }

    /// <summary>
    /// Get volume and pitch for a specific Music event.
    /// </summary>
    public void GetMusicSettings(AudioEventMusic musicEvent, out float volume, out float pitch)
    {
        volume = 1f;
        pitch = 1f;

        foreach (var entry in musicClips)
        {
            if (entry.musicEvent == musicEvent)
            {
                volume = entry.volume;
                pitch = entry.pitch;
                return;
            }
        }

        Debug.LogWarning($"AudioConfig: Music event '{musicEvent}' not found in config");
    }

    /// <summary>
    /// Get all configured clips for debugging and validation.
    /// </summary>
    public void LogAllClips()
    {
        Debug.Log("=== AudioConfig Clips ===");
        Debug.Log("--- SFX ---");
        foreach (var entry in sfxClips)
        {
            string clipName = entry.clip != null ? entry.clip.name : "NULL";
            Debug.Log($"  {entry.sfxEvent.ToAudioName()} → {clipName} (Vol: {entry.volume}, Pitch: {entry.pitch})");
        }
        Debug.Log("--- Music ---");
        foreach (var entry in musicClips)
        {
            string clipName = entry.clip != null ? entry.clip.name : "NULL";
            Debug.Log($"  {entry.musicEvent.ToAudioName()} → {clipName} (Vol: {entry.volume}, Pitch: {entry.pitch})");
        }
    }
}
