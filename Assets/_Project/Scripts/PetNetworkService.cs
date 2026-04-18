using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PetNetworkService : MonoBehaviourPunCallbacks
{
    public static PetNetworkService Instance { get; private set; }

    private const string P_COINS = "coins";
    private const string P_EQUIPPED_PETID = "petEquippedId";

    [Header("Defs")]
    public List<EggDefinition> eggDefs = new();
    public List<PetDefinition> petDefs = new();

    private Dictionary<string, EggDefinition> _eggById;
    private Dictionary<string, PetDefinition> _petById;

    private readonly Dictionary<int, Dictionary<int, PetItem>> _inv = new();
    private readonly Dictionary<int, int> _nextUid = new();
    private readonly Dictionary<int, int> _equippedUid = new();

    public event Action OnLocalInventoryChanged;
    public event Action OnLocalEquippedChanged;

    public AudioClip myAudioClip;

    private void Awake()
    {
        Instance = this;

        _eggById = new Dictionary<string, EggDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in eggDefs)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.eggId)) continue;
            _eggById[e.eggId.Trim()] = e;
        }

        _petById = new Dictionary<string, PetDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in petDefs)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.petId)) continue;
            _petById[p.petId.Trim()] = p;
        }
    }

    public void RestoreFromSaveAsMaster(int actorNumber, List<PetSaveData> pets, int equippedUid)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (actorNumber <= 0) return;

        if (!_inv.TryGetValue(actorNumber, out var dict))
        {
            dict = new Dictionary<int, PetItem>();
            _inv[actorNumber] = dict;
        }
        else
        {
            dict.Clear();
        }

        int maxUid = 0;

        if (pets != null)
        {
            foreach (var p in pets)
            {
                if (p == null) continue;

                var petItem = new PetItem
                {
                    uid = p.uid,
                    petId = p.petId,
                    eggId = p.eggId
                };

                dict[petItem.uid] = petItem;
                if (petItem.uid > maxUid) maxUid = petItem.uid;
            }
        }

        _nextUid[actorNumber] = maxUid + 1;

        if (equippedUid > 0 && dict.ContainsKey(equippedUid))
            _equippedUid[actorNumber] = equippedUid;
        else
            _equippedUid[actorNumber] = 0;
    }

    public void SyncLocalEquippedToPhotonProps()
    {
        if (!PhotonNetwork.InRoom) return;
        if (PlayerPetInventory.Local == null) return;

        int uid = PlayerPetInventory.Local.EquippedUid;
        string petId = "";

        if (uid > 0)
        {
            var item = PlayerPetInventory.Local.GetByUid(uid);
            if (item != null)
                petId = item.petId ?? "";
        }

        PhotonNetwork.LocalPlayer.SetCustomProperties(
            new Hashtable { { "petEquippedId", petId } }
        );
    }

    public void RequestRestoreFromSaveForLocal(List<PetSaveData> pets, int equippedUid)
    {
        if (!PhotonNetwork.InRoom) return;
        if (PhotonNetwork.LocalPlayer == null) return;

        if (PlayerPetInventory.Local != null)
        {
            PlayerPetInventory.Local.ClearAllFromSave();

            if (pets != null)
            {
                foreach (var p in pets)
                {
                    if (p == null) continue;
                    PlayerPetInventory.Local.LocalAdd(new PetItem
                    {
                        uid = p.uid,
                        petId = p.petId,
                        eggId = p.eggId
                    });
                }
            }

            if (equippedUid > 0)
                PlayerPetInventory.Local.LocalSetEquipped(equippedUid);
        }

        var wrapper = new PetSaveListWrapper
        {
            pets = pets ?? new List<PetSaveData>(),
            equippedUid = equippedUid
        };

        string json = JsonUtility.ToJson(wrapper);

        photonView.RPC(
            nameof(RPC_RestorePetsFromSave),
            RpcTarget.MasterClient,
            PhotonNetwork.LocalPlayer.ActorNumber,
            json
        );
    }

    [Serializable]
    private class PetSaveListWrapper
    {
        public List<PetSaveData> pets = new();
        public int equippedUid;
    }

    [PunRPC]
    private void RPC_RestorePetsFromSave(int actorNumber, string json)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var wrapper = JsonUtility.FromJson<PetSaveListWrapper>(json);
        if (wrapper == null)
        {
            Debug.LogError("[PetNetworkService] Failed to parse pet restore");
            return;
        }

        RestoreFromSaveAsMaster(actorNumber, wrapper.pets, wrapper.equippedUid);
        Debug.Log($"[PetNetworkService] Master restored pets for actor={actorNumber}");
    }

    public void RequestBuyEgg(string eggId)
    {
        if (!PhotonNetwork.InRoom) return;
        if (string.IsNullOrWhiteSpace(eggId)) return;
        photonView.RPC(nameof(RPC_RequestBuyEgg), RpcTarget.MasterClient, eggId.Trim());
    }

    [PunRPC]
    private void RPC_RequestBuyEgg(string eggId, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var sender = info.Sender;
        if (sender == null) return;

        eggId = eggId?.Trim() ?? "";
        if (!_eggById.TryGetValue(eggId, out var egg) || egg == null)
        {
            photonView.RPC(nameof(RPC_BuyEggResult), sender, false, eggId, "EggNotFound");
            return;
        }

        var room = PhotonNetwork.CurrentRoom;
        if (room?.CustomProperties == null ||
            !room.CustomProperties.TryGetValue(PetMarketRotationService.PROP_PET_MARKET, out object packedObj))
        {
            photonView.RPC(nameof(RPC_BuyEggResult), sender, false, eggId, "NoMarket");
            return;
        }

        string packed = packedObj as string ?? "";
        var items = PetMarketRotationService.Unpack(packed);

        int idx = items.FindIndex(x => string.Equals(x.eggId, eggId, StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
        {
            photonView.RPC(nameof(RPC_BuyEggResult), sender, false, eggId, "NotInRotation");
            return;
        }

        var it = items[idx];
        if (it.qty <= 0)
        {
            photonView.RPC(nameof(RPC_BuyEggResult), sender, false, eggId, "OutOfStock");
            return;
        }

        int price = Mathf.Max(0, egg.price);
        int coins = GetPlayerCoins(sender);
        if (coins < price)
        {
            photonView.RPC(nameof(RPC_BuyEggResult), sender, false, eggId, "NotEnoughCoins");
            return;
        }

        var petDef = RollPetFromEgg(egg);
        if (petDef == null || string.IsNullOrWhiteSpace(petDef.petId))
        {
            photonView.RPC(nameof(RPC_BuyEggResult), sender, false, eggId, "NoDrops");
            return;
        }

        SetPlayerCoins(sender, coins - price);

        it.qty -= 1;
        items[idx] = it;
        string newPacked = PetMarketRotationService.Pack(items);
        room.SetCustomProperties(new Hashtable { { PetMarketRotationService.PROP_PET_MARKET, newPacked } });

        int uid = Master_AddPet(sender, petDef.petId.Trim(), eggId);

        photonView.RPC(nameof(RPC_BuyEggResult), sender, true, eggId, "");
        photonView.RPC(nameof(RPC_AddPetLocal), sender, uid, petDef.petId.Trim(), eggId);

        SoundFXManager.Instance.PlaySound(myAudioClip);
    }

    private PetDefinition RollPetFromEgg(EggDefinition egg)
    {
        if (egg == null || egg.drops == null || egg.drops.Count == 0) return null;

        float total = 0f;
        foreach (var d in egg.drops)
        {
            if (d == null || d.pet == null) continue;
            total += Mathf.Max(0.0001f, d.weight);
        }
        if (total <= 0f) return null;

        float r = UnityEngine.Random.value * total;
        float acc = 0f;
        foreach (var d in egg.drops)
        {
            if (d == null || d.pet == null) continue;
            acc += Mathf.Max(0.0001f, d.weight);
            if (r <= acc) return d.pet;
        }
        return egg.drops.LastOrDefault(x => x != null && x.pet != null)?.pet;
    }

    private int Master_AddPet(Player owner, string petId, string eggId)
    {
        int actor = owner.ActorNumber;

        if (!_inv.TryGetValue(actor, out var dict))
        {
            dict = new Dictionary<int, PetItem>();
            _inv[actor] = dict;
        }

        if (!_nextUid.TryGetValue(actor, out int uid))
            uid = 1;

        dict[uid] = new PetItem { uid = uid, petId = petId, eggId = eggId };
        _nextUid[actor] = uid + 1;

        if (!_equippedUid.TryGetValue(actor, out int eq) || eq == 0)
            _equippedUid[actor] = uid;

        return uid;
    }

    [PunRPC]
    private void RPC_BuyEggResult(bool ok, string eggId, string reason)
    {
        if (!ok) Debug.Log($"[PetBuy] failed egg={eggId} reason={reason}");
        else Debug.Log($"[PetBuy] OK egg={eggId}");
    }

    [PunRPC]
    private void RPC_AddPetLocal(int uid, string petId, string eggId)
    {
        if (PlayerPetInventory.Local == null) return;

        PlayerPetInventory.Local.LocalAdd(new PetItem
        {
            uid = uid,
            petId = petId,
            eggId = eggId
        });

        OnLocalInventoryChanged?.Invoke();
    }

    [PunRPC]
    private void RPC_RemovePetLocal(int uid)
    {
        if (PlayerPetInventory.Local == null) return;
        PlayerPetInventory.Local.LocalRemove(uid);
        OnLocalInventoryChanged?.Invoke();
    }

    public void RequestEquip(int uid)
    {
        if (!PhotonNetwork.InRoom) return;
        photonView.RPC(nameof(RPC_RequestEquip), RpcTarget.MasterClient, uid);
    }

    [PunRPC]
    private void RPC_RequestEquip(int uid, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var sender = info.Sender;
        if (sender == null) return;

        int actor = sender.ActorNumber;
        if (!_inv.TryGetValue(actor, out var dict) || !dict.TryGetValue(uid, out var item))
        {
            photonView.RPC(nameof(RPC_EquipResult), sender, false, uid, "NotFound");
            return;
        }

        _equippedUid[actor] = uid;

        string petId = item.petId ?? "";
        sender.SetCustomProperties(new Hashtable { { P_EQUIPPED_PETID, petId } });

        photonView.RPC(nameof(RPC_SetEquippedLocal), sender, uid);
        photonView.RPC(nameof(RPC_EquipResult), sender, true, uid, "");
    }

    [PunRPC]
    private void RPC_SetEquippedLocal(int uid)
    {
        if (PlayerPetInventory.Local != null)
            PlayerPetInventory.Local.LocalSetEquipped(uid);

        OnLocalEquippedChanged?.Invoke();
    }

    [PunRPC]
    private void RPC_EquipResult(bool ok, int uid, string reason)
    {
        if (!ok) Debug.Log($"[PetEquip] failed uid={uid} reason={reason}");
        else Debug.Log($"[PetEquip] OK uid={uid}");
    }

    public void RequestSell(int uid)
    {
        if (!PhotonNetwork.InRoom) return;
        photonView.RPC(nameof(RPC_RequestSell), RpcTarget.MasterClient, uid);
    }

    [PunRPC]
    private void RPC_RequestSell(int uid, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var sender = info.Sender;
        if (sender == null) return;

        int actor = sender.ActorNumber;
        if (!_inv.TryGetValue(actor, out var dict) || !dict.TryGetValue(uid, out var item))
        {
            photonView.RPC(nameof(RPC_SellResult), sender, false, uid, 0, "NotFound");
            return;
        }

        int sellCoins = ComputeSellCoins(item.eggId);

        dict.Remove(uid);

        if (_equippedUid.TryGetValue(actor, out int eq) && eq == uid)
        {
            _equippedUid[actor] = 0;
            sender.SetCustomProperties(new Hashtable { { P_EQUIPPED_PETID, "" } });
            photonView.RPC(nameof(RPC_SetEquippedLocal), sender, 0);
        }

        int curCoins = GetPlayerCoins(sender);
        SetPlayerCoins(sender, curCoins + sellCoins);

        photonView.RPC(nameof(RPC_RemovePetLocal), sender, uid);
        photonView.RPC(nameof(RPC_SellResult), sender, true, uid, sellCoins, "");
    }

    private int ComputeSellCoins(string eggId)
    {
        if (string.IsNullOrWhiteSpace(eggId)) return 0;
        if (_eggById.TryGetValue(eggId.Trim(), out var egg) && egg != null)
            return Mathf.Max(0, Mathf.RoundToInt(egg.price * 0.10f));
        return 0;
    }

    [PunRPC]
    private void RPC_SellResult(bool ok, int uid, int coinsGained, string reason)
    {
        if (!ok) Debug.Log($"[PetSell] failed uid={uid} reason={reason}");
        else Debug.Log($"[PetSell] OK uid={uid} +{coinsGained}");
    }

    public (float growReducePct, float moveSpeedPct, float sellBonusPct) GetBonusesForPet(string petId)
    {
        float grow = 0f, move = 0f, sell = 0f;
        var def = GetPetDef(petId);
        if (def == null) return (0, 0, 0);

        Acc(def.bonus1);
        Acc(def.bonus2);
        Acc(def.bonus3);
        return (grow, move, sell);

        void Acc(PetBonus b)
        {
            if (b == null) return;
            switch (b.type)
            {
                case PetBonusType.GrowTimeReductionPercent: grow += b.value; break;
                case PetBonusType.MoveSpeedPercent: move += b.value; break;
                case PetBonusType.SellPriceBonusPercent: sell += b.value; break;
            }
        }
    }

    public (float growReducePct, float moveSpeedPct, float sellBonusPct) Master_GetActorEquippedBonuses(int actor)
    {
        if (!PhotonNetwork.IsMasterClient) return (0, 0, 0);
        if (actor <= 0) return (0, 0, 0);

        if (!_equippedUid.TryGetValue(actor, out int uid) || uid <= 0) return (0, 0, 0);
        if (!_inv.TryGetValue(actor, out var dict) || !dict.TryGetValue(uid, out var item) || item == null) return (0, 0, 0);

        return GetBonusesForPet(item.petId);
    }

    public PetDefinition GetPetDef(string petId)
    {
        if (string.IsNullOrWhiteSpace(petId)) return null;
        _petById.TryGetValue(petId.Trim(), out var def);
        return def;
    }

    public EggDefinition GetEggDef(string eggId)
    {
        if (string.IsNullOrWhiteSpace(eggId)) return null;
        _eggById.TryGetValue(eggId.Trim(), out var def);
        return def;
    }

    private int GetPlayerCoins(Player p)
    {
        if (p == null) return 0;
        if (p.CustomProperties != null && p.CustomProperties.TryGetValue(P_COINS, out object v))
        {
            if (v is int i) return i;
            if (v is long l) return (int)l;
        }
        return 0;
    }

    private void SetPlayerCoins(Player p, int newCoins)
    {
        if (p == null) return;
        p.SetCustomProperties(new Hashtable { { P_COINS, newCoins } });
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (otherPlayer == null) return;

        int actor = otherPlayer.ActorNumber;
        _inv.Remove(actor);
        _equippedUid.Remove(actor);
        _nextUid.Remove(actor);

        Debug.Log($"[PetNetworkService] Cleaned up pet data for actor={actor} who left the room.");
    }

    public static string GetEquippedPetIdFromPlayer(Player p)
    {
        if (p?.CustomProperties == null) return "";
        return p.CustomProperties.TryGetValue(P_EQUIPPED_PETID, out object v) ? (v as string ?? "") : "";
    }
}