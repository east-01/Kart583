using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Unity.VisualScripting;
using UnityEngine;

/** The item atlas will hold all item data for each item enum. Properties are filled
  *   in the GameplayManager of the scene, where this ItemAtlas script should belong. */
public class ItemAtlas : MonoBehaviour
{
    private System.Random rand;

    /** It's very important that we match the enum index (i.e. OIL = 0) to the list index so that RetrieveData() is fast. */
    [Header("IMPORTANT NOTE: Match enum index to list index")] public List<ItemDataPackage> items;
    public ItemDataPackage RetrieveData(Item item) 
    {
        return items[(int)item];
    }

    public Item RollRandom() 
    {
        rand ??= new();
        
        // Calculate the total weight
        float totalWeight = 0f;
        items.ForEach(idp => totalWeight += idp.weight);

        // Generate a random value between 0 and the total weight
        float randomValue = (float)new System.Random().NextDouble() * totalWeight;

        // Iterate through the enum values and choose the one based on weights
        Item returnItem = Item.OIL;
        items.ForEach(idp => {
            randomValue -= idp.weight;
            if (randomValue <= 0f)
                returnItem = idp.item;
        });

        // This should not happen, but if it does, return the last enum value
        return items[items.Count-1].item;
    }
}

[Serializable]
public struct ItemDataPackage
{
    public Item item;
    public float weight;
    public Sprite itemIcon;
    public GameObject worldItem;

    public ItemDataPackage(Item item, float weight, Sprite itemIcon, GameObject worldItem)
    {
        this.item = item;
        this.weight = weight;
        this.itemIcon = itemIcon;
        this.worldItem = worldItem;
    }
}