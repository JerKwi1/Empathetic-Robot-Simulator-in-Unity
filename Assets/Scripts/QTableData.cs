using System;
using System.Collections.Generic;

[Serializable]
// Represents a single Q‐table entry mapping a (state, action) pair to its Q‐value.
public class QEntry
{
    public int stateKey;
    public int actionKey;
    public float value;
}

[Serializable]
// Container for serializing/deserializing the entire Q‐table as a list of QEntry objects.
public class QTableData
{
    public List<QEntry> entries = new List<QEntry>();
}