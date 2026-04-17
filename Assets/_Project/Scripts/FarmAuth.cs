using Photon.Pun;
using UnityEngine;

public static class FarmAuth
{
    public static bool TryGetLocalFarmIndex(out int farmIndex)
    {
        farmIndex = -1;

        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[FarmAuth] Not in room!");
            return false;
        }

        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(FarmAssignmentService.PROP_FARM))
        {
            Debug.LogWarning("[FarmAuth] No farm property found!");
            return false;
        }

        object v = PhotonNetwork.LocalPlayer.CustomProperties[FarmAssignmentService.PROP_FARM];

        if (v is int i)
        {
            farmIndex = i;
            Debug.Log($"[FarmAuth] Local farm: {farmIndex}");
            return true;
        }

        if (v is long l)
        {
            farmIndex = (int)l;
            Debug.Log($"[FarmAuth] Local farm (long cast): {farmIndex}");
            return true;
        }

        Debug.LogWarning($"[FarmAuth] Unknown farm type: {v?.GetType()}");
        return false;
    }

    public static bool CanEditFarm(int targetFarmIndex)
    {
        bool result = TryGetLocalFarmIndex(out int myFarm) && myFarm == targetFarmIndex;

        if (!result)
        {
            TryGetLocalFarmIndex(out int actualFarm);
            Debug.LogWarning($"[FarmAuth] CanEditFarm FAILED! Target={targetFarmIndex}, MyFarm={actualFarm}");
        }
        else
        {
            //Debug.Log($"[FarmAuth] CanEditFarm OK: Farm {targetFarmIndex}");
        }

        return result;
    }

    public static string GetLocalPlayFabId()
    {
        if (PhotonNetwork.LocalPlayer?.CustomProperties?.ContainsKey(FarmAssignmentService.PROP_PLAYFAB_ID) == true)
            return PhotonNetwork.LocalPlayer.CustomProperties[FarmAssignmentService.PROP_PLAYFAB_ID] as string;
        return AuthSession.PlayFabId;
    }
}