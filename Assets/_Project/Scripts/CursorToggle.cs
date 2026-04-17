using Photon.Pun;
using UnityEngine;

public class CursorToggle : MonoBehaviourPun
{
    public bool startLocked = true;

    private bool _locked;

    private void Start()
    {
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

        SetLocked(startLocked);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1)) // Right click
        {
            SetLocked(!_locked);
        }
    }

    private void SetLocked(bool locked)
    {
        _locked = locked;

        if (_locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}