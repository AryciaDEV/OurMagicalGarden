using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class FarmAssignmentService : MonoBehaviourPunCallbacks
{
    public const string PROP_FARM = "farm";
    public const string PROP_PLAYFAB_ID = "pfid";

    private void Start()
    {
        TryAssignAllIfMaster();
    }

    private void TryAssignAllIfMaster()
    {
        if (!PhotonNetwork.InRoom) return;
        if (!PhotonNetwork.IsMasterClient) return;

        foreach (var p in PhotonNetwork.PlayerList)
            AssignFarmIfNeeded(p);
    }

    // MEVCUT KOD (satýr ~30-40 arasý) - OnPlayerEnteredRoom metodunu deðiþtir:

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // YENÝ: Önce biraz bekle ki oyuncunun custom properties'leri senkronize olsun
        StartCoroutine(DelayedAssign(newPlayer));
    }

    private System.Collections.IEnumerator DelayedAssign(Player newPlayer)
    {
        // 0.5 saniye bekle ki PlayFab ID gelsin
        yield return new WaitForSeconds(0.5f);

        // Oyuncu hala odada mý kontrol et
        if (newPlayer == null || !PhotonNetwork.CurrentRoom.Players.ContainsKey(newPlayer.ActorNumber))
            yield break;

        AssignFarmIfNeeded(newPlayer);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        TryAssignAllIfMaster();
    }

    private void AssignFarmIfNeeded(Player p)
    {
        // Zaten atama yapilmis mi?
        if (p.CustomProperties.ContainsKey(PROP_FARM))
        {
            Debug.Log($"[FarmAssignment] Player {p.ActorNumber} already has farm {(int)p.CustomProperties[PROP_FARM]}");
            return;
        }

        // PlayFab ID varsa ve daha once atama yapildiysa ayni farm'i ver
        string playFabId = GetPlayFabIdFromPlayer(p);
        if (!string.IsNullOrEmpty(playFabId))
        {
            var farmNetwork = FindFirstObjectByType<FarmNetwork>();
            if (farmNetwork != null)
            {
                var previousFarm = farmNetwork.GetFarmForPlayFabId(playFabId);
                if (previousFarm.HasValue && !IsFarmOccupied(previousFarm.Value, p.ActorNumber))
                {
                    // Eski farm'i geri ver
                    var props = new Hashtable
                    {
                        { PROP_FARM, previousFarm.Value },
                        { PROP_PLAYFAB_ID, playFabId }
                    };
                    p.SetCustomProperties(props);

                    Debug.Log($"[FarmAssignment] Reassigned Farm_{previousFarm.Value} to returning player {p.ActorNumber} (PlayFab: {playFabId})");
                    return;
                }
            }
        }

        // YENI FARM ATA
        var used = PhotonNetwork.PlayerList
            .Where(x => x.ActorNumber != p.ActorNumber)
            .Where(x => x.CustomProperties.ContainsKey(PROP_FARM))
            .Select(x =>
            {
                object v = x.CustomProperties[PROP_FARM];
                if (v is int i) return i;
                if (v is long l) return (int)l;
                return -1;
            })
            .Where(i => i >= 0)
            .ToHashSet();

        int free = -1;
        for (int i = 0; i < 8; i++)
        {
            if (!used.Contains(i))
            {
                free = i;
                break;
            }
        }

        if (free == -1)
        {
            Debug.LogWarning("[FarmAssignment] No free farm slot.");
            return;
        }

        var newProps = new Hashtable
        {
            { PROP_FARM, free }
        };

        if (!string.IsNullOrEmpty(playFabId))
        {
            newProps[PROP_PLAYFAB_ID] = playFabId;
        }

        p.SetCustomProperties(newProps);

        Debug.Log($"[FarmAssignment] Assigned Farm_{free} to Actor {p.ActorNumber}");
    }

    private string GetPlayFabIdFromPlayer(Player p)
    {
        if (p.CustomProperties.ContainsKey(PROP_PLAYFAB_ID))
            return p.CustomProperties[PROP_PLAYFAB_ID] as string;

        if (p == PhotonNetwork.LocalPlayer)
            return AuthSession.PlayFabId;

        return null;
    }

    private bool IsFarmOccupied(int farmIndex, int excludeActor)
    {
        return PhotonNetwork.PlayerList.Any(p =>
            p.ActorNumber != excludeActor &&
            p.CustomProperties.ContainsKey(PROP_FARM) &&
            (int)p.CustomProperties[PROP_FARM] == farmIndex);
    }

    public static void RegisterLocalPlayFabId(string playFabId)
    {
        if (PhotonNetwork.LocalPlayer == null) return;
        if (string.IsNullOrEmpty(playFabId)) return;

        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PROP_PLAYFAB_ID))
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
            {
                { PROP_PLAYFAB_ID, playFabId }
            });
        }
    }
}