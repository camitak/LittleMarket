using System;
using UnityEngine;

// One scene shelf's location. Products stay in the existing shelfSlots save list.
[Serializable]
public class ShelfPlacementSaveData
{
    public string shelfID;
    public Vector3 position;
    public Quaternion rotation;
}