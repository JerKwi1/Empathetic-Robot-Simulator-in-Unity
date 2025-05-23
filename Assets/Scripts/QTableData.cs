using System;
using System.Collections.Generic;

[Serializable]
public class QEntry
{
    public int stateKey;
    public int actionKey;
    public float value;
}

[Serializable]
public class QTableData
{
    public List<QEntry> entries = new List<QEntry>();
}