using System;
using UnityEngine;

[Serializable]
public class DeliveryBoxSaveData
{
    public int slotIndex;

    public string productID;

    public int quantity;

    public int capacity;

    public bool isOpen;

    public bool hasWorldTransform;

    public Vector3 position;

    public Quaternion rotation;
}