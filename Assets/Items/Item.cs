using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

public enum Item
{
    OIL,
    BOLT
}

struct ItemPackage 
{
    Item type;
    Image icon;
    GameObject itemPrefab;

    public static void Load(Item type) {

    }
}