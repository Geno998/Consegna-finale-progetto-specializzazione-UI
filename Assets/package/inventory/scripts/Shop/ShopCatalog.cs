using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopCatalog", menuName = "UIGame/Shop Catalog")]
public class ShopCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public sSurv1ItemData item;         // Riferimento ai dati dell'oggetto
        public int price;                   // Prezzo unitario personalizzato
        public bool available = true;       // Disponibilità (per attivare/disattivare rapidamente)
    }

    public List<Entry> entries = new List<Entry>(); // Elenco voci del catalogo

    // Trova una voce per ID dell'oggetto; ritorna null se non presente
    public Entry FindById(int id)
    {
        for (int i = 0; i < entries.Count; i++)
            if (entries[i]?.item && entries[i].item.ItemID == id) return entries[i];
        return null;
    }
}
