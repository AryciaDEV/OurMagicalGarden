using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text weightText;
    public TMP_Text priceText;
    public Button sellButton;

    [HideInInspector] public int uid;
}