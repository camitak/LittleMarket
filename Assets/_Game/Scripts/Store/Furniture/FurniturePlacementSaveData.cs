using System;
using UnityEngine;

[Serializable]
public class FurniturePlacementSaveData
{
    public string furnitureID;
    public string furnitureDefinitionID;
    public Vector3 position;
    public Quaternion rotation;
}