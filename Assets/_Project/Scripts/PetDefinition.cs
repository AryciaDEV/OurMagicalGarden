using UnityEngine;

public enum PetBonusType
{
    None = 0,
    GrowTimeReductionPercent = 1,   // ekim süresi azaltma %
    MoveSpeedPercent = 2,           // koþma hýzý %
    SellPriceBonusPercent = 3       // satýþ bonusu %
}

[System.Serializable]
public class PetBonus
{
    public PetBonusType type = PetBonusType.None;
    [Tooltip("Percent value. Example: 2.5 means %2.5")]
    public float value = 0f;
}

[CreateAssetMenu(menuName = "Garden/PetDefinition")]
public class PetDefinition : ScriptableObject
{
    [Header("Identity")]
    public string petId;         // örn: "Unicorn"
    public string displayName;   // UI’da görünen isim (istersen petId ile ayný)
    public Sprite icon;

    [Header("Prefab (3D)")]
    public GameObject prefab; // follower olarak spawn edilecek

    [Header("Bonuses (up to 3)")]
    public PetBonus bonus1;
    public PetBonus bonus2;
    public PetBonus bonus3;

    public string GetBonusLineSingleText()
    {
        System.Collections.Generic.List<string> parts = new();

        void Add(PetBonus b)
        {
            if (b == null) return;
            if (b.type == PetBonusType.None) return;
            if (Mathf.Abs(b.value) < 0.0001f) return;

            string label = b.type switch
            {
                PetBonusType.GrowTimeReductionPercent => "GrowTime",
                PetBonusType.MoveSpeedPercent => "MoveSpeed",
                PetBonusType.SellPriceBonusPercent => "SellBonus",
                _ => b.type.ToString()
            };

            parts.Add($"{label} +{b.value}%");
        }

        Add(bonus1);
        Add(bonus2);
        Add(bonus3);

        return parts.Count == 0 ? "-" : string.Join(" | ", parts);
    }
}

