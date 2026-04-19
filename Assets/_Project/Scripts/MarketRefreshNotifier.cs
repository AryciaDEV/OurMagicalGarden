using System.Collections;
using UnityEngine;

public class MarketRefreshNotifier : MonoBehaviour
{
    [Header("Seed Market Popup")]
    public GameObject seedMarketPopup;

    [Header("Pet Market Popup")]
    public GameObject petMarketPopup;

    [Header("Services")]
    public MarketRotationService seedMarketService;
    public PetMarketRotationService petMarketService;

    private Coroutine _seedHideCo;
    private Coroutine _petHideCo;

    private const float PopupDuration = 3f;

    private void OnEnable()
    {
        if (seedMarketService != null)
            seedMarketService.OnMarketRotated += ShowSeedPopup;

        if (petMarketService != null)
            petMarketService.OnMarketRotated += ShowPetPopup;
    }

    private void OnDisable()
    {
        if (seedMarketService != null)
            seedMarketService.OnMarketRotated -= ShowSeedPopup;

        if (petMarketService != null)
            petMarketService.OnMarketRotated -= ShowPetPopup;

        if (_seedHideCo != null)
        {
            StopCoroutine(_seedHideCo);
            _seedHideCo = null;
        }

        if (_petHideCo != null)
        {
            StopCoroutine(_petHideCo);
            _petHideCo = null;
        }

        if (seedMarketPopup != null) seedMarketPopup.SetActive(false);
        if (petMarketPopup != null) petMarketPopup.SetActive(false);
    }

    private void ShowSeedPopup()
    {
        if (seedMarketPopup == null) return;

        seedMarketPopup.SetActive(true);

        if (_seedHideCo != null)
            StopCoroutine(_seedHideCo);

        _seedHideCo = StartCoroutine(HideSeedAfter());
    }

    private void ShowPetPopup()
    {
        if (petMarketPopup == null) return;

        petMarketPopup.SetActive(true);

        if (_petHideCo != null)
            StopCoroutine(_petHideCo);

        _petHideCo = StartCoroutine(HidePetAfter());
    }

    private IEnumerator HideSeedAfter()
    {
        yield return new WaitForSeconds(PopupDuration);

        if (seedMarketPopup != null)
            seedMarketPopup.SetActive(false);

        _seedHideCo = null;
    }

    private IEnumerator HidePetAfter()
    {
        yield return new WaitForSeconds(PopupDuration);

        if (petMarketPopup != null)
            petMarketPopup.SetActive(false);

        _petHideCo = null;
    }
}
