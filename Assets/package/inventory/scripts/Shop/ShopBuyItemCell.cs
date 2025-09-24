using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopBuyItemCell : MonoBehaviour
{
    [SerializeField] private Image icon;                 // Icona dell'oggetto
    [SerializeField] private TextMeshProUGUI nameText;   // Nome dell'oggetto
    [SerializeField] private TextMeshProUGUI priceText;  // Prezzo unitario mostrato

    private sSurv1ItemData data;                         // Dati dell'oggetto (ScriptableObject)
    private ShopUI owner;                                // Riferimento alla ShopUI che gestisce la selezione

    // Inizializza e popola i campi della cella, aggiungendo il comportamento di selezione
    public void Init(ShopUI owner, sSurv1ItemData item)
    {
        this.owner = owner;
        data = item;

        if (icon) icon.sprite = item.ItemSprite;
        if (nameText) nameText.text = item.ItemName;
        if (priceText) priceText.text = item.BuyPrice.ToString();

        // Al click sulla cella, notifica lo shop della selezione dell'item
        GetComponent<Button>().onClick.AddListener(() => owner.SelectBuyItem(item.ItemID));
    }
}
