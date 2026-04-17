using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Audio Source - Müzik buradan çalacak")]
    [SerializeField] private AudioSource musicSource;

    [Header("Audio Mixer (Opsiyonel)")]
    [SerializeField] private AudioMixerGroup musicMixerGroup;

    [Header("Music Settings")]
    [SerializeField] private AudioClip[] musicTracks;
    [SerializeField] private bool playOnStart = true;

    // DÜZELTİLDİ: Inspector'da 0-1 aralığı göster
    [Range(0f, 1f)]
    [SerializeField] private float defaultVolume = 0.5f;

    private float currentVolume;
    private int currentTrackIndex = 0;
    private const string VOLUME_PREF_KEY = "MusicVolume";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupAudioSource();
        LoadVolume();
    }

    private void SetupAudioSource()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }
        }

        musicSource.loop = false;
        musicSource.playOnAwake = false;

        if (musicMixerGroup != null)
        {
            musicSource.outputAudioMixerGroup = musicMixerGroup;
        }
    }

    private void Start()
    {
        if (playOnStart && musicTracks.Length > 0)
        {
            Play();
        }
    }

    public void Play()
    {
        if (musicTracks.Length == 0) return;
        PlayTrack(0);
    }

    public void PlayTrack(int trackIndex)
    {
        if (trackIndex < 0 || trackIndex >= musicTracks.Length) return;

        currentTrackIndex = trackIndex;
        musicSource.clip = musicTracks[currentTrackIndex];
        musicSource.volume = currentVolume;
        musicSource.Play();

        CancelInvoke(nameof(NextTrack));
        Invoke(nameof(NextTrack), musicSource.clip.length);
    }

    public void NextTrack()
    {
        int nextIndex = (currentTrackIndex + 1) % musicTracks.Length;
        PlayTrack(nextIndex);
    }

    public void Stop()
    {
        musicSource.Stop();
    }

    // DÜZELTİLDİ: 0-1 aralığı netleştirildi
    public void SetVolume(float volume)
    {
        currentVolume = Mathf.Clamp01(volume);
        musicSource.volume = currentVolume;

        PlayerPrefs.SetFloat(VOLUME_PREF_KEY, currentVolume);
        PlayerPrefs.Save();

        Debug.Log($"[MusicManager] Müzik sesi: {currentVolume * 100}%");
    }

    // DÜZELTİLDİ: 0-1 döndürür
    public float GetVolume()
    {
        return currentVolume;
    }

    private void LoadVolume()
    {
        if (PlayerPrefs.HasKey(VOLUME_PREF_KEY))
        {
            currentVolume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY);
        }
        else
        {
            currentVolume = defaultVolume;
        }

        currentVolume = Mathf.Clamp01(currentVolume);
        musicSource.volume = currentVolume;
    }
}