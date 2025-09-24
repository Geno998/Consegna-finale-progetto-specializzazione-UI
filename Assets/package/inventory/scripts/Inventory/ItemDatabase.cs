using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "UIGame/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Tooltip("Drag all sSurv1ItemData here (or a folder)")]
    [SerializeField] public List<sSurv1ItemData> items = new();

    // Indice a runtime per ricerche O(1) per ID
    private Dictionary<int, sSurv1ItemData> byId;

    void OnEnable()
    {
        BuildIndex();
    }

    // Ricostruisce il dizionario ID -> Dati dell’item
    private void BuildIndex()
    {
        byId = new Dictionary<int, sSurv1ItemData>(items.Count);
        foreach (var it in items)
        {
            if (it == null) continue;
            if (byId.ContainsKey(it.ItemID))
            {
                Debug.LogWarning($"[ItemDatabase] Duplicate ItemID {it.ItemID} for '{it.ItemName}'. Keeping first.");
                continue;
            }
            byId.Add(it.ItemID, it);
        }
    }

    // Chiamare se si modifica la lista durante il Play nel Editor
    public void Rebuild() => BuildIndex();

    // Trova i dati dell’item via ID (null se non presente)
    public sSurv1ItemData FindById(int id)
    {
        if (byId == null || byId.Count == 0) BuildIndex();
        return byId.TryGetValue(id, out var data) ? data : null;
    }

    // Versione TryGet: ritorna true/false e out del dato
    public bool TryGet(int id, out sSurv1ItemData data)
    {
        if (byId == null || byId.Count == 0) BuildIndex();
        return byId.TryGetValue(id, out data);
    }

    // Accesso in sola lettura alla lista completa
    public IReadOnlyList<sSurv1ItemData> All => items;
}
