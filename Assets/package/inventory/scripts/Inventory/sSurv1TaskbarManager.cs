using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class sSurv1TaskbarManager : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private ItemDatabase itemDB;          // sorgente dati centrale

    [Header("Grid")]
    [SerializeField] private RectTransform slotsRoot;      // contenitore della taskbar
    [SerializeField] private GridLayoutGroup grid;         // componente Grid sul contenitore
    [SerializeField] private int columns = 8;              // 1 riga × N colonne

    [Header("Prefabs")]
    [SerializeField] private GameObject slotPrefab;        // stesso prefab degli slot inventario
    [SerializeField] private GameObject itemPrefab;        // stesso prefab degli item

    /// <summary>Lista runtime degli slot presenti nella taskbar.</summary>
    public List<InventorySlot> Slots { get; private set; } = new();

    private void Awake()
    {
        BuildGrid();
    }


    // Ricostruisce la taskbar come griglia 1 × columns. Mantiene eventuali figli decorativi:
    // elimina solo i GameObject che sono InventorySlot generati in precedenza.

    public void BuildGrid()
    {
        // Rimuove solo i figli che sono slot (non toccare elementi decorativi/testi)
        for (int i = slotsRoot.childCount - 1; i >= 0; i--)
        {
            var child = slotsRoot.GetChild(i);
            if (child.GetComponent<InventorySlot>() != null)
                Destroy(child.gameObject);
        }

        Slots.Clear();

        // Configura la griglia per una singola riga
        if (grid != null)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            grid.constraintCount = 1;
        }

        // Genera gli slot
        for (int i = 0; i < Mathf.Max(1, columns); i++)
        {
            var go = Instantiate(slotPrefab, slotsRoot, false);
            var slot = go.GetComponent<InventorySlot>();
            Slots.Add(slot);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(slotsRoot);
    }


    // Cambia il numero di colonne della taskbar a runtime e ricostruisce la griglia.

    public void SetColumns(int newColumns)
    {
        columns = Mathf.Max(1, newColumns);
        BuildGrid();
    }


    // Aggiunge item nella taskbar: prima completa gli stack esistenti con lo stesso ID,
    // poi riempie gli slot vuoti con nuovi stack. Ritorna la quantità non inserita.

    public int AddItemToTaskbar(int itemID, int quantity)
    {
        if (quantity <= 0) return 0;
        var data = FindItemData(itemID);
        if (data == null) return quantity;

        int remaining = quantity;

        // 1) Completa gli stack esistenti
        foreach (var slot in Slots)
        {
            if (remaining <= 0) break;
            var item = slot.Item;
            if (item == null) continue;
            if (item.ItemID != itemID) continue;

            remaining = item.AddToStack(remaining);
        }

        // 2) Colloca nei primi slot vuoti
        foreach (var slot in Slots)
        {
            if (remaining <= 0) break;
            if (slot.Item != null) continue;

            var go = Instantiate(itemPrefab);
            var it = go.GetComponent<sSurv1ItemControl>();
            it.OnItemCreate(data, 0);

            int toPut = Mathf.Min(remaining, data.MaxStackSize);
            it.SetQuantity(toPut);
            slot.SetItem(it);

            remaining -= toPut;
        }

        return remaining; // > 0 se la taskbar è piena
    }


    // Comodità: prova a spostare un intero stack da uno slot qualsiasi alla taskbar.
    // Se trova stesso ID, unisce; se trova slot vuoto, sposta.
    // Ritorna true se ha spostato/fuso almeno in parte.

    public bool TryAcceptFromSlot(InventorySlot fromSlot)
    {
        if (fromSlot == null || fromSlot.Item == null) return false;

        var moving = fromSlot.Item;

        // 1) Prova a fondere con stack esistenti dello stesso ID
        foreach (var slot in Slots)
        {
            if (slot.Item == null) continue;
            if (slot.Item.ItemID != moving.ItemID) continue;

            int remainder = slot.Item.AddToStack(moving.Quantity);
            if (remainder == 0)
            {
                fromSlot.Clear();
                Destroy(moving.gameObject);
                return true;
            }
            else
            {
                moving.SetQuantity(remainder);
            }
        }

        // 2) Sposta nel primo slot vuoto disponibile
        foreach (var slot in Slots)
        {
            if (slot.Item != null) continue;

            fromSlot.Clear();
            slot.SetItem(moving);
            return true;
        }

        return false; // nessuno spazio disponibile
    }


    // Accesso semplice al database per ottenere i dati dell’item
    private sSurv1ItemData FindItemData(int id)
    {
        return itemDB ? itemDB.FindById(id) : null;
    }
}
