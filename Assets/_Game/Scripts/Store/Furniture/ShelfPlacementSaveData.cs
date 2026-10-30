using System;
using UnityEngine;

[Serializable]
public class ShelfPlacementSaveData
{
    public string shelfID;
    public Vector3 position;
    public Quaternion rotation;

    // Added in version 19.
    public bool purchasedByPlayer;

    // Added in version 20.
    // Empty on old version-19 furniture records.
    public string furnitureDefinitionID;
}