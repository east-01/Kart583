using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

/** The item atlas will hold all item data for each item enum. Properties are filled
  *   in the GameplayManager of the scene, where this ItemAtlas script should belong. */
public class ItemAtlas : MonoBehaviour
{
    /** It's very important that we match the enum index (i.e. OIL = 0) to the list index so that RetrieveData() is fast. */
    [Header("IMPORTANT NOTE: Match enum index to list index")] public List<ItemDataPackage> items;
    public ItemDataPackage RetrieveData(Item item) 
    {
        return items[(int)item];
    }
}

[Serializable]
public struct ItemDataPackage
{
    public Item item;
    public Sprite itemIcon;
    public GameObject worldItem;

    public ItemDataPackage(Item item, Sprite itemIcon, GameObject worldItem)
    {
        this.item = item;
        this.itemIcon = itemIcon;
        this.worldItem = worldItem;
    }
}