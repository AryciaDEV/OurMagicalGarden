using UnityEngine;
using UnityEngine.UI;

public class SocialMediaReward : MonoBehaviour
{
    [Header("Sosyal Medya Butonlarý")]
    [SerializeField] private Button instagramButton;
    [SerializeField] private Button tiktokButton;
    [SerializeField] private Button youtubeButton;
    [SerializeField] private Button discordButton;

    [Header("URL'ler")]
    [SerializeField] private string instagramUrl = "https://www.instagram.com/...";
    [SerializeField] private string tiktokUrl = "https://www.tiktok.com/@...";
    [SerializeField] private string youtubeUrl = "https://www.youtube.com/...";
    [SerializeField] private string discordUrl = "https://discord.gg/...";

    private void Start()
    {
        // Butonlarý dinle
        if (instagramButton != null)
            instagramButton.onClick.AddListener(() => OpenUrl(instagramUrl, "Instagram"));

        if (tiktokButton != null)
            tiktokButton.onClick.AddListener(() => OpenUrl(tiktokUrl, "TikTok"));

        if (youtubeButton != null)
            youtubeButton.onClick.AddListener(() => OpenUrl(youtubeUrl, "YouTube"));

        if (discordButton != null)
            discordButton.onClick.AddListener(() => OpenUrl(discordUrl, "Discord"));
    }

    private void OpenUrl(string url, string platformName)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning($"[SocialMedia] {platformName} URL'si ayarlanmamýþ!");
            return;
        }

        Application.OpenURL(url);
        Debug.Log($"[SocialMedia] {platformName} sayfasý açýldý: {url}");
    }
}