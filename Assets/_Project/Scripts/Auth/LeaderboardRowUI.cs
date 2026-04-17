using TMPro;
using UnityEngine;

public class LeaderboardRowUI : MonoBehaviour
{
    public TMP_Text rankText;
    public TMP_Text nameText;
    public TMP_Text coinsText;

    public void Bind(int rank, string nick, int coins)
    {
        if (rankText) rankText.text = rank.ToString();
        if (nameText) nameText.text = nick;
        if (coinsText) coinsText.text = coins.ToString();
    }
}