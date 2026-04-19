using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PetHatchedPopup : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TMP_Text nameText;
    public GameObject root;

    private void OnEnable()
    {
        var svc = PetNetworkService.Instance;
        if (svc != null)
            svc.OnLocalPetHatched += Show;
    }

    private void OnDisable()
    {
        var svc = PetNetworkService.Instance;
        if (svc != null)
            svc.OnLocalPetHatched -= Show;
    }

    public void Show(string petId)
    {
        if (root != null)
            root.SetActive(true);

        var svc = PetNetworkService.Instance;
        var def = (!string.IsNullOrWhiteSpace(petId) && svc != null)
            ? svc.GetPetDef(petId)
            : null;

        if (nameText != null)
        {
            if (def != null && !string.IsNullOrWhiteSpace(def.displayName))
                nameText.text = def.displayName;
            else if (!string.IsNullOrWhiteSpace(petId))
                nameText.text = petId;
            else
                nameText.text = "Unknown Pet";
        }

        if (iconImage != null)
        {
            bool hasIcon = def != null && def.icon != null;
            iconImage.enabled = hasIcon;
            iconImage.sprite = hasIcon ? def.icon : null;
        }
    }

    public void Close()
    {
        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}
