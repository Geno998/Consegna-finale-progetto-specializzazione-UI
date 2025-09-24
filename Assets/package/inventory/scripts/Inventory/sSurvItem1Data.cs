using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// Tipologie di item per equip/slot
public enum ItemType
{
    notGear,
    gearSlot1,
    gearSlot2,
    gearSlot3,
    gearSlot4
}

[CreateAssetMenu(fileName = "Data", menuName = "UIGame/ItemData", order = 1)]
public class sSurv1ItemData : ScriptableObject
{
    // Campi configurabili da Inspector
    [SerializeField] private int itemID;
    [SerializeField] private int maxStackSize;
    [SerializeField] private Sprite itemSprite;
    [SerializeField] private string itemName;
    [SerializeField] private ItemType itemType;

    [Min(0)][SerializeField] private int buyPrice = 0;
    [Min(0)][SerializeField] private int sellPrice = 0;

    // Proprietà di sola lettura per accesso runtime
    [DoNotSerialize] public int ItemID { get { return itemID; } }
    [DoNotSerialize] public int MaxStackSize { get { return maxStackSize; } }
    [DoNotSerialize] public Sprite ItemSprite { get { return itemSprite; } }
    [DoNotSerialize] public string ItemName { get { return itemName; } }
    [DoNotSerialize] public ItemType ItemType { get { return itemType; } }
    [DoNotSerialize] public int BuyPrice { get { return buyPrice; } }
    [DoNotSerialize] public int SellPrice { get { return sellPrice; } }

#if UNITY_EDITOR
    // Moltiplicatore di default per derivare il prezzo di vendita dal prezzo di acquisto
    private const float defaultSellMultiplier = 0.5f;

    private void OnValidate()
    {
        // mantieni i prezzi non negativi
        buyPrice = Mathf.Max(0, BuyPrice);
        sellPrice = Mathf.Max(0, SellPrice);

        // Se SellPrice è 0 ma BuyPrice impostato, ricava automaticamente una prima volta.
        // Rimuovi queste righe se preferisci compilare sempre SellPrice manualmente.
        if (BuyPrice > 0 && SellPrice == 0)
            sellPrice = Mathf.CeilToInt(BuyPrice * defaultSellMultiplier);
    }
#endif
}
