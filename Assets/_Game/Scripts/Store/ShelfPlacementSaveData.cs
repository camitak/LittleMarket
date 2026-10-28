using System;
using UnityEngine;

[Serializable]
public class ShelfPlacementSaveData
{
    public string shelfID;
    public Vector3 position;
    public Quaternion rotation;

    // Added in version 19. Defaults to false for old version-18 saves.
    public bool purchasedByPlayer;
}