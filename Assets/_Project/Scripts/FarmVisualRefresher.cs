using UnityEngine;
using Photon.Pun;
using System.Collections;

public class FarmVisualRefresher : MonoBehaviourPun
{
    private FarmNetwork farm;

    private void Start()
    {
        farm = FindFirstObjectByType<FarmNetwork>();
        StartCoroutine(PeriodicVisualCheck());
    }

    private IEnumerator PeriodicVisualCheck()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);

            if (!FarmAuth.TryGetLocalFarmIndex(out int myFarm)) continue;
            if (farm == null) continue;

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var ps = farm.GetPlotState(myFarm, x, y);

                    if (ps.occupied && ps.ownerActor != PhotonNetwork.LocalPlayer.ActorNumber)
                    {
                        bool ownerExists = false;
                        foreach (var p in PhotonNetwork.PlayerList)
                        {
                            if (p.ActorNumber == ps.ownerActor)
                            {
                                ownerExists = true;
                                break;
                            }
                        }

                        if (!ownerExists && PhotonNetwork.IsMasterClient)
                        {
                            farm.GetType().GetMethod("ClearPlotLocal",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                ?.Invoke(farm, new object[] { myFarm, x, y });
                        }
                    }
                }
            }
        }
    }
}