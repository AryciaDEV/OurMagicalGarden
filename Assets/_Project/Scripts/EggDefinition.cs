using System;
using System.Collections.Generic;
using UnityEngine;

public enum EggRarity
{
    Common, Uncommon, Rare, Epic, Legendary, Mythic, Divine
}

[Serializable]
public class EggDrop
{
    public PetDefinition pet;
    [Min(0.0001f)] public float weight = 1f; // çýkma aðýrlýðý
}

[CreateAssetMenu(menuName = "Garden/EggDefinition")]
public class EggDefinition : ScriptableObject
{
    [Header("Identity")]
    public string eggId;     // örn: "Common Egg"
    public Sprite icon;
    public EggRarity rarity;

    [Header("Economy")]
    public int price;        // egg fiyatý (sen giriyorsun)

    [Header("Rotation Stock")]
    public int minQty = 1;   // market yenilenince
    public int maxQty = 5;   // market yenilenince

    [Header("Drops")]
    public List<EggDrop> drops = new();
}