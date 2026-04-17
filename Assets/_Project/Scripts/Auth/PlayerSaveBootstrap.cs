using System.Collections;
using Photon.Pun;
using UnityEngine;

// DEVRE DISI - FarmRestoreCoordinator kullaniliyor
public class PlayerSaveBootstrap : MonoBehaviourPunCallbacks
{
    private void Awake()
    {
        Debug.Log("[PlayerSaveBootstrap] Disabled - using FarmRestoreCoordinator instead");
        enabled = false;
    }
}