using UnityEngine;

public class FarmSpawnPoints : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    public Transform GetSpawn(int farmIndex)
    {
        if (farmIndex >= 0 && farmIndex < spawnPoints.Length)
            return spawnPoints[farmIndex];

        Debug.LogWarning($"[FarmSpawnPoints] Spawn point for farm {farmIndex} not found");
        return null;
    }

    private void OnValidate()
    {
        if (spawnPoints == null || spawnPoints.Length != 8)
        {
            Debug.LogWarning("[FarmSpawnPoints] Should have exactly 8 spawn points (Farm_0 to Farm_7)");
        }
    }
}