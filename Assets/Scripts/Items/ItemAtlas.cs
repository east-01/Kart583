using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

/** The item atlas will hold all item data for each item enum. Properties are filled
  *   in the GameplayManager of the scene, where this ItemAtlas script should belong. */
public class ItemAtlas : MonoBehaviour
{
    public List<ItemDataPackage> items;
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