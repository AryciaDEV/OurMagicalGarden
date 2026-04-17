using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlotHoverUI : MonoBehaviour
{
    public RectTransform panel;
    public TMP_Text titleText;
    public TMP_Text infoText;
    public Button actionButton;
    public TMP_Text actionButtonText;

    public void SetActive(bool on)
    {
        if (panel) panel.gameObject.SetActive(on);
    }
}