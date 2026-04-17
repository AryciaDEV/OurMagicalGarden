using PlayFab;
using UnityEngine;

public class PlayFabBootstrap : MonoBehaviour
{
    [SerializeField] private string titleId;

    private void Awake()
    {
        if (!string.IsNullOrWhiteSpace(titleId))
            PlayFabSettings.staticSettings.TitleId = titleId;
    }
}