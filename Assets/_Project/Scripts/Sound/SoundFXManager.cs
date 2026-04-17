using UnityEngine;
using UnityEngine.Audio;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager Instance { get; private set; }

    [Header("Audio Source - Tüm sesler buradan çalacak")]
    [SerializeField] private AudioSource audioSource;

    [Header("Audio Mixer (Opsiyonel)")]
    [SerializeField] private AudioMixerGroup soundFxMixerGroup;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultVolume = 0.5f;

    private float currentVolume;
    private const string VOLUME_PREF_KEY = "SoundFXVolume";

    // Pool için ek AudioSource'lar (aynı anda çok ses için)
    private AudioSource[] audioSourcePool;
    private int currentPoolIndex = 0;
    [SerializeField] private int poolSize = 5;

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
        SetupAudioPool();
        LoadVolume();
    }

    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        if (soundFxMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = soundFxMixerGroup;
        }
    }

    // YENİ: AudioSource Pool (aynı anda çok ses çalmak için)
    private void SetupAudioPool()
    {
        audioSourcePool = new AudioSource[poolSize];
        audioSourcePool[0] = audioSource;

        for (int i = 1; i < poolSize; i++)
        {
            var newSource = gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            newSource.loop = false;

            if (soundFxMixerGroup != null)
            {
                newSource.outputAudioMixerGroup = soundFxMixerGroup;
            }

            audioSourcePool[i] = newSource;
        }
    }

    // YENİ: Boşta olan AudioSource bul
    private AudioSource GetAvailableAudioSource()
    {
        // Önce boşta olanı bul
        for (int i = 0; i < poolSize; i++)
        {
            if (!audioSourcePool[i].isPlaying)
            {
                return audioSourcePool[i];
            }
        }

        // Hepsi doluysa sıradakini kullan (en eski ses kesilecek)
        currentPoolIndex = (currentPoolIndex + 1) % poolSize;
        return audioSourcePool[currentPoolIndex];
    }

    /// <summary>
    /// SES ÇAL - Pitch desteği ile
    /// </summary>
    public void PlaySound(AudioClip clip, float volume = -1f)
    {
        PlaySoundWithPitch(clip, volume, 1f);
    }

    /// <summary>
    /// SES ÇAL - Pitch ve Volume kontrolü ile (FootstepFX için)
    /// </summary>
    public void PlaySoundWithPitch(AudioClip clip, float volume = -1f, float pitch = 1f)
    {
        if (clip == null) return;

        float playVolume = volume >= 0 ? volume : 1f;
        // Global volume uygula (0-1 arası)
        playVolume *= currentVolume;

        AudioSource source = GetAvailableAudioSource();
        source.pitch = pitch;
        source.PlayOneShot(clip, playVolume);
    }

    /// <summary>
    /// GLOBAL SES SEVİYESİNİ AYARLA (0-1 aralığı)
    /// </summary>
    public void SetVolume(float volume)
    {
        currentVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(VOLUME_PREF_KEY, currentVolume);
        PlayerPrefs.Save();

        Debug.Log($"[SoundFXManager] Tüm sesler {currentVolume * 100}% seviyesine ayarlandı");
    }

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
    }
}