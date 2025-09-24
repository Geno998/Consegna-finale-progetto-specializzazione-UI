using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class sSurv1MenuManager : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private ItemDatabase itemDB;

    [Header("Grid")]
    [SerializeField] private RectTransform slotsRoot;
    [SerializeField] private GridLayoutGroup grid;
    [SerializeField] private int rows = 4;
    [SerializeField] private int columns = 6;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private GameObject itemPrefab;

    // Elenco degli slot generati per l’inventario
    [SerializeField] private List<InventorySlot> slots = new();
    public IReadOnlyList<InventorySlot> Slots => slots;


    private void Awake()
    {
        BuildGrid();
    }

    // Trova i dati di un item tramite ID usando il database
    private sSurv1ItemData FindItemData(int itemID)
    {
        return itemDB ? itemDB.FindById(itemID) : null;
    }

    // Aggiunge una certa quantità di un item all’inventario (riempie stack, poi slot liberi). Ritorna quanto non è entrato
    public int AddItemToInventory(int itemID, int quantity)
    {
        var data = FindItemData(itemID);
        if (data == null) return quantity;

        int remaining = quantity;

        // 1) Completa gli stack esistenti
        foreach (var slot in slots)
        {
            if (remaining <= 0) break;
            var item = slot.Item;
            if (item == null) continue;
            if (item.ItemID != itemID) continue;

            remaining = item.AddToStack(remaining);
        }

        // 2) Usa gli slot vuoti
        foreach (var slot in slots)
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

        return remaining;
    }

    // Costruisce la griglia (rows × columns) ricreando gli slot
    private void BuildGrid()
    {
        foreach (Transform child in slotsRoot)
            Destroy(child.gameObject);

        slots.Clear();

        int total = Mathf.Max(1, rows) * Mathf.Max(1, columns);
        for (int i = 0; i < total; i++)
        {
            var slotGO = Instantiate(slotPrefab, slotsRoot);
            var slot = slotGO.GetComponent<InventorySlot>();
            slots.Add(slot);
        }
    }
}
