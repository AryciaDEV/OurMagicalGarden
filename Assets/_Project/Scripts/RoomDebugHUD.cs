using System.Text;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class RoomDebugHUD : MonoBehaviourPunCallbacks
{
    public TMP_Text text;

    private void Update()
    {
        if (!text) return;

        var sb = new StringBuilder();
        sb.AppendLine($"InRoom: {PhotonNetwork.InRoom}");
        sb.AppendLine($"PlayersInRoom: {PhotonNetwork.CurrentRoom?.PlayerCount}");
        sb.AppendLine($"IsMaster: {PhotonNetwork.IsMasterClient}");

        if (PhotonNetwork.InRoom)
        {
            sb.AppendLine("PlayerList:");
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                sb.AppendLine($"- Actor:{p.ActorNumber} Nick:{p.NickName}");
            }
        }

        text.text = sb.ToString();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[HUD] Player entered: {newPlayer.ActorNumber} {newPlayer.NickName}");
    }
}