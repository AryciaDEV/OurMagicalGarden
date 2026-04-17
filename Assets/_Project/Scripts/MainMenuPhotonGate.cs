using Photon.Pun;
using UnityEngine;

public class MainMenuPhotonGate : MonoBehaviourPunCallbacks
{
    public LoginUI loginUI;

    public override void OnJoinedLobby()
    {
        Debug.Log("[MainMenuPhotonGate] Joined Lobby");
        //if (loginUI) loginUI.ShowRoomPanel();
    }
}