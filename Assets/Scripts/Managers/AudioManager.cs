using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private Transform sfxSourceParent;
    [SerializeField] private int sfxPoolSize = 8;

    [Header("Audio Config")]
    [SerializeField] private AudioConfigSO audioConfig;

    [Header("Volume")]
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float musicVolume = 0.7f;
    [SerializeField] private float sfxVolume = 0.8f;

    [Header("Music Settings")]
    [SerializeField] private float musicFadeDuration = 1f;

    private Queue<AudioSource> sfxPool = new Queue<AudioSource>();
    private Coroutine currentMusicFade;
    private bool isMuted = false;

    private float _storedMasterVolume = 1f;
    private float _storedMusicVolume = 1f;
    private float _storedSFXVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeMusicSource();
        InitializeSFXPool();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializeMusicSource()
    {
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
        }

        musicSource.volume = masterVolume * musicVolume;
    }

    private void InitializeSFXPool()
    {
        if (sfxSourceParent == null)
        {
            sfxSourceParent = new GameObject("SFXSources").transform;
            sfxSourceParent.SetParent(transform);
        }

        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject sfxObj = new GameObject($"SFXSource_{i}");
            sfxObj.transform.SetParent(sfxSourceParent);
            AudioSource source = sfxObj.AddComponent<AudioSource>();
            source.volume = masterVolume * sfxVolume;
            sfxPool.Enqueue(source);
        }
    }

    /// <summary>
    /// Play a music track by enum (from AudioConfig) with optional crossfade.
    /// </summary>
    public void PlayMusic(AudioEventMusic musicEvent, bool loop = true, float? customFadeDuration = null)
    {
        if (audioConfig == null)
        {
            Debug.LogError("AudioManager: AudioConfig not assigned!");
            return;
        }

        // Get config settings for this music event
        audioConfig.GetMusicSettings(musicEvent, out float configVolume, out float configPitch);

        AudioClip clip = audioConfig.GetSFXClip(musicEvent.ToAudioName());
        if (clip == null)
        {
            Debug.LogWarning($"AudioManager: Music '{musicEvent}' not found in config");
            return;
        }

        float fadeDuration = customFadeDuration ?? musicFadeDuration;

        if (currentMusicFade != null)
            StopCoroutine(currentMusicFade);

        currentMusicFade = StartCoroutine(CrossfadeMusic(clip, loop, fadeDuration, configVolume, configPitch));
    }

    /// <summary>
    /// Play a music track by name (from AudioConfig) with optional crossfade.
    /// </summary>
    public void PlayMusicByName(string musicName, bool loop = true, float? customFadeDuration = null)
    {
        if (audioConfig == null)
        {
            Debug.LogError("AudioManager: AudioConfig not assigned!");
            return;
        }

        AudioClip clip = audioConfig.GetSFXClip(musicName);
        if (clip == null)
        {
            Debug.LogWarning($"AudioManager: Music '{musicName}' not found in config");
            return;
        }

        PlayMusic(clip, loop, customFadeDuration);
    }

    /// <summary>
    /// Play a music track with optional crossfade.
    /// </summary>
    public void PlayMusic(AudioClip clip, bool loop = true, float? customFadeDuration = null)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Attempted to play null music clip");
            return;
        }

        float fadeDuration = customFadeDuration ?? musicFadeDuration;

        if (currentMusicFade != null)
            StopCoroutine(currentMusicFade);

        currentMusicFade = StartCoroutine(CrossfadeMusic(clip, loop, fadeDuration));
    }

    /// <summary>
    /// Play a sound effect by enum (from AudioConfig).
    /// </summary>
    public AudioSource PlaySFX(AudioEventSFX sfxEvent, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
    {
        if (audioConfig == null)
        {
            Debug.LogError("AudioManager: AudioConfig not assigned!");
            return null;
        }

        // Get config settings for this SFX event
        audioConfig.GetSFXSettings(sfxEvent, out float configVolume, out float configPitch);

        // Apply multipliers on top of config values
        float finalVolume = configVolume * volumeMultiplier;
        float finalPitch = configPitch * pitchMultiplier;

        AudioClip clip = audioConfig.GetSFXClip(sfxEvent.ToAudioName());
        if (clip == null)
        {
            Debug.LogWarning($"AudioManager: SFX '{sfxEvent}' not found in config");
            return null;
        }

        return PlaySFXClip(clip, finalVolume, finalPitch);
    }

    /// <summary>
    /// Play a sound effect by name (from AudioConfig).
    /// </summary>
    public AudioSource PlaySFX(string sfxName, float volume = 1f, float pitch = 1f)
    {
        if (audioConfig == null)
        {
            Debug.LogError("AudioManager: AudioConfig not assigned!");
            return null;
        }

        AudioClip clip = audioConfig.GetSFXClip(sfxName);
        if (clip == null)
        {
            Debug.LogWarning($"AudioManager: SFX '{sfxName}' not found in config");
            return null;
        }

        return PlaySFXClip(clip, volume, pitch);
    }

    /// <summary>
    /// Play a sound effect directly from an AudioClip.
    /// </summary>
    public AudioSource PlaySFXClip(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: Attempted to play null SFX clip");
            return null;
        }

        AudioSource source = GetSFXSource();
        if (source == null) return null;

        source.clip = clip;
        source.volume = masterVolume * sfxVolume * volume;
        source.pitch = pitch;
        source.PlayOneShot(clip);

        return source;
    }

    /// <summary>
    /// Stop music with optional fade out.
    /// </summary>
    public void StopMusic(float? customFadeDuration = null)
    {
        float fadeDuration = customFadeDuration ?? musicFadeDuration;

        if (currentMusicFade != null)
            StopCoroutine(currentMusicFade);

        currentMusicFade = StartCoroutine(FadeOutMusic(fadeDuration));
    }

    /// <summary>
    /// Pause current music.
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource.isPlaying)
            musicSource.Pause();
    }

    /// <summary>
    /// Resume current music.
    /// </summary>
    public void ResumeMusic()
    {
        if (!musicSource.isPlaying && musicSource.clip != null)
            musicSource.Play();
    }

    /// <summary>
    /// Set master volume (affects both music and SFX).
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    /// <summary>
    /// Set music volume (0-1).
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateMusicVolume();
    }

    /// <summary>
    /// Set SFX volume (0-1).
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateSFXVolume();
    }

    /// <summary>
    /// Mute all audio.
    /// </summary>
    public void MuteAll()
    {
        if (isMuted) return;
        isMuted = true;

        _storedMasterVolume = masterVolume;
        _storedMusicVolume = musicVolume;
        _storedSFXVolume = sfxVolume;

        masterVolume = 0f;
        UpdateAllVolumes();
    }

    /// <summary>
    /// Unmute all audio.
    /// </summary>
    public void UnmuteAll()
    {
        if (!isMuted) return;
        isMuted = false;

        masterVolume = _storedMasterVolume;
        musicVolume = _storedMusicVolume;
        sfxVolume = _storedSFXVolume;

        UpdateAllVolumes();
    }

    /// <summary>
    /// Check if audio is muted.
    /// </summary>
    public bool IsMuted => isMuted;

    /// <summary>
    /// Get current music clip.
    /// </summary>
    public AudioClip CurrentMusicClip => musicSource.clip;

    /// <summary>
    /// Check if music is playing.
    /// </summary>
    public bool IsMusicPlaying => musicSource.isPlaying;

    /// <summary>
    /// Set the pitch of the currently playing music.
    /// </summary>
    public void SetMusicPitch(float pitch)
    {
        if (musicSource != null)
        {
            musicSource.pitch = Mathf.Max(0.1f, pitch); // Clamp to avoid invalid values
        }
    }

    /// <summary>
    /// Get the current pitch of the music.
    /// </summary>
    public float GetMusicPitch()
    {
        return musicSource != null ? musicSource.pitch : 1f;
    }

    // ============ PRIVATE HELPERS ============

    private AudioSource GetSFXSource()
    {
        if (sfxPool.Count == 0)
        {
            Debug.LogWarning("AudioManager: SFX pool exhausted, returning null");
            return null;
        }

        AudioSource source = sfxPool.Dequeue();
        sfxPool.Enqueue(source);
        return source;
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip, bool loop, float duration, float configVolume = 1f, float configPitch = 1f)
    {
        if (musicSource.isPlaying && duration > 0)
        {
            yield return FadeOutMusic(duration / 2f);
        }

        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.volume = 0f;
        musicSource.pitch = configPitch;
        musicSource.Play();

        if (duration > 0)
        {
            yield return FadeInMusic(duration / 2f, configVolume);
        }
        else
        {
            musicSource.volume = masterVolume * musicVolume * configVolume;
        }

        currentMusicFade = null;
    }

    private IEnumerator FadeInMusic(float duration, float configVolume = 1f)
    {
        float elapsed = 0f;
        float targetVolume = masterVolume * musicVolume * configVolume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    private IEnumerator FadeOutMusic(float duration)
    {
        float elapsed = 0f;
        float startVolume = musicSource.volume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
    }

    private void UpdateAllVolumes()
    {
        UpdateMusicVolume();
        UpdateSFXVolume();
    }

    private void UpdateMusicVolume()
    {
        if (musicSource != null)
            musicSource.volume = masterVolume * musicVolume;
    }

    private void UpdateSFXVolume()
    {
        foreach (AudioSource source in sfxPool)
        {
            if (source != null)
                source.volume = masterVolume * sfxVolume;
        }
    }
}
