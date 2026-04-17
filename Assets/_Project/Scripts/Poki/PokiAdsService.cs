using System;
using UnityEngine;

public class PokiAdsService : MonoBehaviour
{
    public static PokiAdsService Instance { get; private set; }

    public bool IsInitialized { get; private set; }
    public bool IsAdRunning { get; private set; }

    private bool _gameplayActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            PokiUnitySDK.Instance.init();
            IsInitialized = true;
            Debug.Log("[PokiAds] SDK initialized.");
        }
        catch (Exception e)
        {
            Debug.LogError("[PokiAds] SDK init failed: " + e.Message);
        }
#else
        Debug.Log("[PokiAds] Editor mode: Poki SDK mocked.");
        IsInitialized = true;
#endif
    }

    public void GameplayStart()
    {
        if (_gameplayActive) return;

        _gameplayActive = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        if (IsInitialized && !IsAdRunning)
            PokiUnitySDK.Instance.gameplayStart();
#endif
    }

    public void GameplayStop()
    {
        if (!_gameplayActive) return;

        _gameplayActive = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        if (IsInitialized && !IsAdRunning)
            PokiUnitySDK.Instance.gameplayStop();
#endif
    }

    public void ShowRewarded(Action<bool> onFinished)
    {
        if (IsAdRunning)
        {
            onFinished?.Invoke(false);
            return;
        }

        IsAdRunning = true;
        GameplayStop();

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            PokiUnitySDK.Instance.rewardedBreakCallBack = success =>
            {
                IsAdRunning = false;
                GameplayStart();
                onFinished?.Invoke(success);
            };

            PokiUnitySDK.Instance.rewardedBreak();
        }
        catch (Exception e)
        {
            Debug.LogError("[PokiAds] rewardedBreak failed: " + e.Message);
            IsAdRunning = false;
            GameplayStart();
            onFinished?.Invoke(false);
        }
#else
        Debug.Log("[PokiAds] Mock rewarded ad success.");
        IsAdRunning = false;
        GameplayStart();
        onFinished?.Invoke(true);
#endif
    }

    public void ShowCommercialBreak(Action onFinished = null)
    {
        if (IsAdRunning)
        {
            onFinished?.Invoke();
            return;
        }

        IsAdRunning = true;
        GameplayStop();

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            PokiUnitySDK.Instance.commercialBreakCallBack = () =>
            {
                IsAdRunning = false;
                GameplayStart();
                onFinished?.Invoke();
            };

            PokiUnitySDK.Instance.commercialBreak();
        }
        catch (Exception e)
        {
            Debug.LogError("[PokiAds] commercialBreak failed: " + e.Message);
            IsAdRunning = false;
            GameplayStart();
            onFinished?.Invoke();
        }
#else
        Debug.Log("[PokiAds] Mock commercial break.");
        IsAdRunning = false;
        GameplayStart();
        onFinished?.Invoke();
#endif
    }
}