using System;
using System.Collections.Generic;

// Tüm save data sýnýflarý burada tek bir dosyada toplandý

[Serializable]
public class PlayerSaveData
{
    public string playerId;
    public string username;
    public bool isGuest;
    public int coins;

    public List<SeedStackData> seeds = new List<SeedStackData>();
    public List<ItemStackData> items = new List<ItemStackData>();
    public List<PetSaveData> pets = new List<PetSaveData>();
    public int equippedPetUid;

    public List<PlotSaveData> plots = new List<PlotSaveData>();
}

[Serializable]
public class SeedStackData
{
    public string seedId;
    public int count;
}

[Serializable]
public class ItemStackData
{
    public int uid;
    public string seedId;
    public float weight;
}

[Serializable]
public class PetSaveData
{
    public int uid;
    public string petId;
    public string eggId;
}

[Serializable]
public class PlotSaveData
{
    public int farmIndex;
    public int x;
    public int y;
    public bool occupied;
    public string seedId;
    public long plantUnix;
    public float growSeconds;
    public float weight;
    public int version;
}