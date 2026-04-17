using UnityEngine;

public class UIPanelToggle : MonoBehaviour
{
    [Header("Paneller")]
    public GameObject panel1;     // MarketPanel
    public GameObject panel2;     // Envanter
    public GameObject panel3;
    public GameObject panel4;
    public GameObject panel5;
    public GameObject panel6;
    public GameObject panel7;
    public GameObject panel8;

    [Header("Tuþlar")]
    public KeyCode toggleKey1 = KeyCode.M;
    public KeyCode toggleKey2 = KeyCode.I;
    public KeyCode toggleKey3 = KeyCode.P;
    public KeyCode toggleKey4 = KeyCode.O;
    public KeyCode toggleKey5 = KeyCode.T;
    public KeyCode toggleKey6 = KeyCode.R;
    public KeyCode toggleKey7 = KeyCode.Return;
    public KeyCode toggleKey8 = KeyCode.Escape;

    [Header("Ses Ayarlarý")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip panelOpenSound;
    [SerializeField] private AudioClip panelCloseSound;

    private GameObject currentlyOpenPanel;

    private void Start()
    {
        if (panel1) panel1.SetActive(false);
        if (panel2) panel2.SetActive(false);
        if (panel3) panel3.SetActive(false);
        if (panel4) panel4.SetActive(false);
        if (panel5) panel5.SetActive(false);
        if (panel6) panel6.SetActive(false);
        if (panel7) panel7.SetActive(false);
        if (panel8) panel8.SetActive(false);

        currentlyOpenPanel = null;

        // AudioSource kontrolü
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey1))
            TogglePanel(panel1, 1);

        if (Input.GetKeyDown(toggleKey2))
            TogglePanel(panel2, 2);

        if (Input.GetKeyDown(toggleKey3))
            TogglePanel(panel3, 3);

        if (Input.GetKeyDown(toggleKey4))
            TogglePanel(panel4, 4);

        if (Input.GetKeyDown(toggleKey5))
            TogglePanel(panel5, 5);

        if (Input.GetKeyDown(toggleKey6))
            TogglePanel(panel6, 6);

        if (Input.GetKeyDown(toggleKey7))
            TogglePanel(panel7, 7);

        if (Input.GetKeyDown(toggleKey8))
            TogglePanel(panel8, 8);
    }

    public void TogglePanel(GameObject panel, int panelNumber = 0)
    {
        if (!panel) return;

        // Eðer bu panel zaten açýksa -> kapat
        if (panel.activeSelf)
        {
            panel.SetActive(false);
            currentlyOpenPanel = null;
            PlayCloseSound();
        }
        else
        {
            // Eðer baþka bir panel açýksa -> önce onu kapat
            if (currentlyOpenPanel != null && currentlyOpenPanel != panel)
            {
                currentlyOpenPanel.SetActive(false);
                PlayCloseSound();
            }

            // Yeni paneli aç
            panel.SetActive(true);
            currentlyOpenPanel = panel;
            PlayOpenSound();
        }

        UpdateCursor();
    }

    // ===== SES FONKSÝYONLARI =====
    private void PlayOpenSound()
    {
        if (audioSource != null && panelOpenSound != null)
        {
            // Global SoundFXManager üzerinden çal (varsa)
            if (SoundFXManager.Instance != null)
            {
                SoundFXManager.Instance.PlaySound(panelOpenSound);
            }
            else
            {
                audioSource.PlayOneShot(panelOpenSound);
            }
        }
    }

    private void PlayCloseSound()
    {
        if (audioSource != null && panelCloseSound != null)
        {
            // Global SoundFXManager üzerinden çal (varsa)
            if (SoundFXManager.Instance != null)
            {
                SoundFXManager.Instance.PlaySound(panelCloseSound);
            }
            else
            {
                audioSource.PlayOneShot(panelCloseSound);
            }
        }
    }

    // ===== PANEL AÇMA FONKSÝYONLARI =====
    public void OpenPanel1()
    {
        if (!panel1) return;

        if (currentlyOpenPanel != null && currentlyOpenPanel != panel1)
        {
            currentlyOpenPanel.SetActive(false);
            PlayCloseSound();
        }

        if (!panel1.activeSelf)
        {
            panel1.SetActive(true);
            currentlyOpenPanel = panel1;
            PlayOpenSound();
        }
        UpdateCursor();
    }

    public void ClosePanel1()
    {
        if (!panel1) return;
        if (panel1.activeSelf)
        {
            panel1.SetActive(false);
            if (currentlyOpenPanel == panel1) currentlyOpenPanel = null;
            PlayCloseSound();
        }
        UpdateCursor();
    }

    public void OpenPanel2()
    {
        if (!panel2) return;

        if (currentlyOpenPanel != null && currentlyOpenPanel != panel2)
        {
            currentlyOpenPanel.SetActive(false);
            PlayCloseSound();
        }

        if (!panel2.activeSelf)
        {
            panel2.SetActive(true);
            currentlyOpenPanel = panel2;
            PlayOpenSound();
        }
        UpdateCursor();
    }

    public void ClosePanel2()
    {
        if (!panel2) return;
        if (panel2.activeSelf)
        {
            panel2.SetActive(false);
            if (currentlyOpenPanel == panel2) currentlyOpenPanel = null;
            PlayCloseSound();
        }
        UpdateCursor();
    }

    public void OpenPanel3()
    {
        if (!panel3) return;

        if (currentlyOpenPanel != null && currentlyOpenPanel != panel3)
        {
            currentlyOpenPanel.SetActive(false);
            PlayCloseSound();
        }

        if (!panel3.activeSelf)
        {
            panel3.SetActive(true);
            currentlyOpenPanel = panel3;
            PlayOpenSound();
        }
        UpdateCursor();
    }

    public void ClosePanel3()
    {
        if (!panel3) return;
        if (panel3.activeSelf)
        {
            panel3.SetActive(false);
            if (currentlyOpenPanel == panel3) currentlyOpenPanel = null;
            PlayCloseSound();
        }
        UpdateCursor();
    }

    public void OpenPanel4()
    {
        if (!panel4) return;

        if (currentlyOpenPanel != null && currentlyOpenPanel != panel4)
        {
            currentlyOpenPanel.SetActive(false);
            PlayCloseSound();
        }

        if (!panel4.activeSelf)
        {
            panel4.SetActive(true);
            currentlyOpenPanel = panel4;
            PlayOpenSound();
        }
        UpdateCursor();
    }

    public void ClosePanel4()
    {
        if (!panel4) return;
        if (panel4.activeSelf)
        {
            panel4.SetActive(false);
            if (currentlyOpenPanel == panel4) currentlyOpenPanel = null;
            PlayCloseSound();
        }
        UpdateCursor();
    }

    public void OpenPanel5()
    {
        if (!panel5) return;

        if (currentlyOpenPanel != null && currentlyOpenPanel != panel5)
        {
            currentlyOpenPanel.SetActive(false);
            PlayCloseSound();
        }

        if (!panel5.activeSelf)
        {
            panel5.SetActive(true);
            currentlyOpenPanel = panel5;
            PlayOpenSound();
        }
        UpdateCursor();
    }

    public void ClosePanel5()
    {
        if (!panel5) return;
        if (panel5.activeSelf)
        {
            panel5.SetActive(false);
            if (currentlyOpenPanel == panel5) currentlyOpenPanel = null;
            PlayCloseSound();
        }
        UpdateCursor();
    }

    public void OpenPanel6()
    {
        if (!panel6) return;

        if (currentlyOpenPanel != null && currentlyOpenPanel != panel6)
        {
            currentlyOpenPanel.SetActive(false);
            PlayCloseSound();
        }

        if (!panel6.activeSelf)
        {
            panel6.SetActive(true);
            currentlyOpenPanel = panel6;
            PlayOpenSound();
        }
        UpdateCursor();
    }

    public void ClosePanel6()
    {
        if (!panel6) return;
        if (panel6.activeSelf)
        {
            panel6.SetActive(false);
            if (currentlyOpenPanel == panel6) currentlyOpenPanel = null;
            PlayCloseSound();
        }
        UpdateCursor();
    }

    public void OpenPanel7()
    {
        if (!panel7) return;

        if (currentlyOpenPanel != null && currentlyOpenPanel != panel7)
        {
            currentlyOpenPanel.SetActive(false);
            PlayCloseSound();
        }

        if (!panel7.activeSelf)
        {
            panel7.SetActive(true);
            currentlyOpenPanel = panel7;
            PlayOpenSound();
        }
        UpdateCursor();
    }

    public void ClosePanel7()
    {
        if (!panel7) return;
        if (panel7.activeSelf)
        {
            panel7.SetActive(false);
            if (currentlyOpenPanel == panel7) currentlyOpenPanel = null;
            PlayCloseSound();
        }
        UpdateCursor();
    }

    public void OpenPanel8()
    {
        if (!panel8) return;

        if (currentlyOpenPanel != null && currentlyOpenPanel != panel8)
        {
            currentlyOpenPanel.SetActive(false);
            PlayCloseSound();
        }

        if (!panel8.activeSelf)
        {
            panel8.SetActive(true);
            currentlyOpenPanel = panel8;
            PlayOpenSound();
        }
        UpdateCursor();
    }

    public void ClosePanel8()
    {
        if (!panel8) return;
        if (panel8.activeSelf)
        {
            panel8.SetActive(false);
            if (currentlyOpenPanel == panel8) currentlyOpenPanel = null;
            PlayCloseSound();
        }
        UpdateCursor();
    }

    // ===== TÜM PANELLERÝ KAPAT =====
    public void CloseAllPanels()
    {
        bool wasAnyOpen = currentlyOpenPanel != null;

        if (panel1) panel1.SetActive(false);
        if (panel2) panel2.SetActive(false);
        if (panel3) panel3.SetActive(false);
        if (panel4) panel4.SetActive(false);
        if (panel5) panel5.SetActive(false);
        if (panel6) panel6.SetActive(false);
        if (panel7) panel7.SetActive(false);
        if (panel8) panel8.SetActive(false);

        currentlyOpenPanel = null;

        if (wasAnyOpen)
        {
            PlayCloseSound();
        }

        UpdateCursor();
    }

    // ===== ÝMLEÇ DURUMUNU GÜNCELLE =====
    private void UpdateCursor()
    {
        bool anyPanelOpen = (panel1 && panel1.activeSelf) ||
                            (panel2 && panel2.activeSelf) ||
                            (panel3 && panel3.activeSelf) ||
                            (panel4 && panel4.activeSelf) ||
                            (panel5 && panel5.activeSelf) ||
                            (panel6 && panel6.activeSelf) ||
                            (panel7 && panel7.activeSelf) ||
                            (panel8 && panel8.activeSelf);

        Cursor.visible = anyPanelOpen;
        Cursor.lockState = anyPanelOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }

    // ===== HALEN AÇIK OLAN PANELÝ DÖNDÜR =====
    public GameObject GetCurrentlyOpenPanel()
    {
        return currentlyOpenPanel;
    }

    // ===== BÝR PANEL AÇIK MI? =====
    public bool IsAnyPanelOpen()
    {
        return currentlyOpenPanel != null;
    }
}