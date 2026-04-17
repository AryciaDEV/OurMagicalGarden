using TMPro;
using UnityEngine;

public class GlobalLeaderboardRowUI : MonoBehaviour
{
    public TMP_Text rankText;
    public TMP_Text nicknameText;
    public TMP_Text coinsText;

    public void Bind(int rank, string nickname, int coins)
    {
        if (rankText) rankText.text = rank.ToString();
        if (nicknameText) nicknameText.text = string.IsNullOrWhiteSpace(nickname) ? "-" : nickname;
        if (coinsText) coinsText.text = NumberShortener.Format(coins);
    }
}