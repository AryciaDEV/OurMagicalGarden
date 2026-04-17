using UnityEngine;

public class PlotInteract : MonoBehaviour
{
    public int x;
    public int y;

    private FarmNetwork farm;
    public int farmIndex = -1;

    private void Awake()
    {
        // Erken bulmaya calis
        FindFarmIndex();
    }

    private void Start()
    {
        farm = FindFirstObjectByType<FarmNetwork>();

        if (farmIndex < 0)
        {
            farmIndex = FindFarmIndexFromParents();
        }

        if (farmIndex < 0)
        {
            Debug.LogError($"[PlotInteract] CRITICAL: Farm index not found for {name}!");
        }
        else
        {
            Debug.Log($"[PlotInteract] Initialized: Farm_{farmIndex}, Plot ({x},{y})");
        }
    }

    private void FindFarmIndex()
    {
        if (farmIndex >= 0) return;
        farmIndex = FindFarmIndexFromParents();
    }

    private int FindFarmIndexFromParents()
    {
        Transform t = transform;
        while (t != null)
        {
            if (t.name.StartsWith("Farm_"))
            {
                string s = t.name.Substring("Farm_".Length);
                if (int.TryParse(s, out int idx))
                {
                    Debug.Log($"[PlotInteract] Found farm index {idx} from parent {t.name}");
                    return idx;
                }
            }
            t = t.parent;
        }
        return -1;
    }

    // PUBLIC: Disaridan farm index almak icin
    public int GetFarmIndex()
    {
        if (farmIndex < 0) FindFarmIndex();
        return farmIndex;
    }
}