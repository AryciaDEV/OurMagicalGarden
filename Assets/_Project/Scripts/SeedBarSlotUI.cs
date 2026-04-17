using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SeedBarSlotUI : MonoBehaviour
{
    public string seedId;

    [Header("UI")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text qtyText;

    [Header("Select")]
    public Image outline;          // seçiliyken enable edilecek (Image)
    public Button selectButton;    // týklama

    public void SetSelected(bool selected)
    {
        if (outline) outline.enabled = selected;
    }
}