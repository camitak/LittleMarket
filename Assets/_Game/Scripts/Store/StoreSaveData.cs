using System;
using System.Collections.Generic;

[Serializable]
public class StoreSaveData
{
    public int day = 1;

    public float money = 100f;

    public float reputation = 50f;

    public List<string> unlockedProductIDs =
        new List<string>();
}