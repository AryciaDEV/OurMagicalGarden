using UnityEngine;

public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Mythic,
    Divine
}

[CreateAssetMenu(menuName = "Garden/SeedDefinition")]
public class SeedDefinition : ScriptableObject
{
    [Header("Identity")]
    public string seedId;
    public Sprite icon;

    [Header("Market")]
    public int seedPrice;          // Tohum Fiyatý
    public Rarity rarity;

    [Header("Farming")]
    public int sellPrice;   // Varsayýlan Satýþ Deðeri
    public float growSeconds;      // saniye cinsinden
    public float minWeight;        // Minimum Aðýrlýk
    public float maxWeight;        // Maximum Aðýrlýk

    [Header("Growth Lift")]
    public float maxLift = 0.35f;

    [Header("Optional - Market appearance weight")]
    public float appearWeight = 1f; // istersen ileride ekstra çarpan

    [Header("Visuals")]
    public GameObject stage0Prefab;   // filiz / baþlangýç
    public GameObject stage1Prefab;   // orta büyüme
    public GameObject stage2Prefab;   // tam büyümüþ / hasat hazýr

    public Vector3 localOffset = Vector3.zero;        // plot üstünde konum düzeltme
    public Vector3 localScale = Vector3.one;
}