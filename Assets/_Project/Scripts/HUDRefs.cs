using TMPro;
using UnityEngine;

public class HUDRefs : MonoBehaviour
{
    public static HUDRefs Instance { get; private set; }

    public TMP_Text coinText;
    public TMP_Text seedText;

    public TMP_Text selectedSeedText;
    public TMP_Text carrotCountText;
    public TMP_Text tomatoCountText;
    public TMP_Text pumpkinCountText;

    private void Awake()
    {
        Instance = this;
    }
}