using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PetInventoryRowUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text petNameText;
    public TMP_Text bonusText;
    public TMP_Text eggRarityText;

    public Button equipButton;
    public Button sellButton;

    //public GameObject selectedOutline; // Image/Outline objesi

    [HideInInspector] public int uid;
    [HideInInspector] public string petId;
    [HideInInspector] public string eggId;

    /*
    public void SetSelected(bool selected)
    {
        if (selectedOutline) selectedOutline.SetActive(selected);
    }
    */
}