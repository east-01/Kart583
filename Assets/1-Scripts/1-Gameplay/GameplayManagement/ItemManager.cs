using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// Manages all items in race.
/// A way for racers to spawn items.
/// </summary>
[RequireComponent(typeof(GameplayManager))]
public class ItemManager : NetworkBehaviour
{

    private GameplayManager gameplayManager;

    private void Awake() 
    {
        gameplayManager = GetComponent<GameplayManager>();
    }

    [Server]
    public void SpawnItem(ItemSpawnData spawnData) 
    {
        if(spawnData.itemType == Item.NONE) {
            Debug.LogError($"Can't spawn item of type NONE");
            return;
        }

        GameObject itemPrefab = gameplayManager.ItemAtlas.RetrieveData(spawnData.itemType).worldItemPrefab;
        if(itemPrefab == null) {
            Debug.LogError($"Item type \"{spawnData.itemType}\" doesn't have a world item prefab.");
            return;
        }

        GameObject spawnedItem = Instantiate(itemPrefab);
        base.ServerManager.Spawn(spawnedItem, null, gameplayManager.gameObject.scene);
        spawnedItem.GetComponent<WorldItem>().ObserversRpcActivateItem(spawnData);

    }

}

/// <summary>
/// Addition data provided by racers to influence the spawning conditions of an item
/// </summary>
public struct ItemSpawnData {
    public string ownerUUID;
    public Item itemType;
    public Vector2 stickDirection;
}
