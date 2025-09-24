using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [Header("Dati")]
    [SerializeField] private ItemDatabase itemDB;            // Database generale degli oggetti
    [SerializeField] private ShopCatalog catalog;            // (Opzionale) Catalogo con prezzi personalizzati

    [Header("Manager")]
    [SerializeField] private sSurv1MenuManager inventory;    // Manager inventario per inserire acquisti
    [SerializeField] private sSurv1TaskbarManager taskbar;   // (Opzionale) Taskbar come overflow
    [SerializeField] private PlayerWallet wallet;            // Portafoglio del giocatore

    [Header("Tab")]
    [SerializeField] private GameObject buyTabRoot;          // Radice UI tab Compra
    [SerializeField] private GameObject sellTabRoot;         // Radice UI tab Vendi

    // -------- COMPRA (Sinistra) --------
    [Header("Compra: Catalogo (Sinistra)")]
    [SerializeField] private TMP_InputField buySearchField;  // Campo ricerca testi
    [SerializeField] private ScrollRect buyScroll;           // Scroll del catalogo
    [SerializeField] private RectTransform buyContent;       // Contenitore delle celle
    [SerializeField] private GridLayoutGroup buyGrid;        // Layout a griglia
    [SerializeField] private GameObject catalogCellPrefab;   // Prefab cella catalogo (con ShopBuyItemCell)

    // -------- COMPRA (Destra) --------
    [Header("Compra: Dettagli (Destra)")]
    [SerializeField] private Image buyIcon;                  // Icona item selezionato
    [SerializeField] private TextMeshProUGUI buyName;        // Nome item selezionato
    [SerializeField] private TextMeshProUGUI buyPriceEach;   // Prezzo unitario
    [SerializeField] private TextMeshProUGUI buyTotalPrice;  // Prezzo totale (qty * unitario)

    [SerializeField] private BuyQuantityButton qtyLeftBtn;   // Bottone diminuisci quantità (tap/hold)
    [SerializeField] private BuyQuantityButton qtyRightBtn;  // Bottone aumenta quantità (tap/hold)
    [SerializeField] private TextMeshProUGUI qtyText;        // Etichetta quantità corrente
    [SerializeField, Min(1)] private int clickStep = 1;      // Passo per click singolo
    [SerializeField, Min(1)] private int holdStepPerSec = 10;// Passo per secondo durante hold

    [SerializeField] private Button buyButton;               // Pulsante acquista

    // -------- VENDI --------
    [Header("Vendi")]
    [SerializeField] private List<InventorySlot> sellSlots = new List<InventorySlot>(4); // Slot dove mettere gli oggetti da vendere
    [SerializeField] private TextMeshProUGUI sellTotalText;                              // Totale valore degli oggetti negli slot di vendita
    [SerializeField] private Button sellButton;                                          // Pulsante vendi

    // Stato interno per il catalogo e la selezione
    private readonly List<GameObject> _catalogCells = new();   // Celle generate del catalogo
    private int _selectedItemId = -1;                          // ID dell'oggetto selezionato per l'acquisto
    private int _qty = 1;                                      // Quantità richiesta

    // Alla comparsa della UI: collega gli eventi, resetta stato e ricostruisce la lista
    private void OnEnable()
    {
        if (buySearchField) buySearchField.onValueChanged.AddListener(_ => RebuildCatalog());

        // Configura i bottoni quantità (tap e hold)
        if (qtyLeftBtn)
        {
            qtyLeftBtn.onClickTap.RemoveAllListeners();
            qtyLeftBtn.onRepeat.RemoveAllListeners();
            qtyLeftBtn.onClickTap.AddListener(() => ChangeQty(-clickStep));                          // tap: decremento piccolo
            int tick = Mathf.Max(1, Mathf.RoundToInt(holdStepPerSec * qtyLeftBtn.repeatInterval));   // calcola step per tick di hold
            qtyLeftBtn.onRepeat.AddListener(() => ChangeQty(-tick));                                 // hold: decremento continuo
        }
        if (qtyRightBtn)
        {
            qtyRightBtn.onClickTap.RemoveAllListeners();
            qtyRightBtn.onRepeat.RemoveAllListeners();
            qtyRightBtn.onClickTap.AddListener(() => ChangeQty(+clickStep));                           // tap: incremento piccolo
            int tick = Mathf.Max(1, Mathf.RoundToInt(holdStepPerSec * qtyRightBtn.repeatInterval));    // step per tick
            qtyRightBtn.onRepeat.AddListener(() => ChangeQty(+tick));                                  // hold: incremento continuo
        }

        if (buyButton)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(PerformBuy); // esegue l'acquisto
        }

        // Aggiorna i totali vendita quando cambia qualcosa in uno slot
        InventorySlot.OnSlotContentsChanged += OnAnySellSlotChanged;

        if (sellButton)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(PerformSell); // esegue la vendita
        }

        _qty = 1;              // reset quantità
        RebuildCatalog();      // ricostruisci lista a sinistra
        RefreshBuyRight();     // aggiorna pannello a destra
        RefreshSellTotals();   // calcola totale vendite
    }

    // Alla scomparsa della UI: pulizia listener per evitare leak/doppi binding
    private void OnDisable()
    {
        if (buySearchField) buySearchField.onValueChanged.RemoveAllListeners();

        if (qtyLeftBtn)
        {
            qtyLeftBtn.onClickTap.RemoveAllListeners();
            qtyLeftBtn.onRepeat.RemoveAllListeners();
        }
        if (qtyRightBtn)
        {
            qtyRightBtn.onClickTap.RemoveAllListeners();
            qtyRightBtn.onRepeat.RemoveAllListeners();
        }
        if (buyButton) buyButton.onClick.RemoveAllListeners();

        InventorySlot.OnSlotContentsChanged -= OnAnySellSlotChanged;

        if (sellButton) sellButton.onClick.RemoveAllListeners();
    }

    #region buySection

    // Ricostruisce il catalogo applicando il filtro di ricerca
    private void RebuildCatalog()
    {
        // Pulisce celle precedenti
        foreach (var go in _catalogCells) Destroy(go);
        _catalogCells.Clear();

        // Testo filtro in minuscolo e senza spazi superflui
        string filter = (buySearchField ? buySearchField.text : "").Trim().ToLowerInvariant();

        // Itera tutti gli item del DB e crea una cella per quelli che matchano il filtro
        foreach (var data in itemDB.items)
        {
            if (!data) continue;
            string nm = data.ItemName ?? "—";
            if (!string.IsNullOrEmpty(filter) && !nm.ToLowerInvariant().Contains(filter))
                continue;

            var go = Instantiate(catalogCellPrefab, buyContent, false);

            // Usa lo script della cella per popolare icona/nome/prezzo + click handler
            var cell = go.GetComponent<ShopBuyItemCell>();
            if (cell != null) cell.Init(this, data);
            else Debug.LogWarning("Catalog cell prefab missing ShopBuyItemCell.", go);

            _catalogCells.Add(go);
        }

        // Forza aggiornamento layout e altezza del contenitore in base al numero di celle
        LayoutRebuilder.ForceRebuildLayoutImmediate(buyContent);
        AdjustContentHeight(buyContent, buyGrid, _catalogCells.Count);
        if (buyScroll) buyScroll.verticalNormalizedPosition = 1f;
    }

    // Calcola e imposta l'altezza del contenuto per mostrare tutte le righe della griglia
    private void AdjustContentHeight(RectTransform content, GridLayoutGroup grid, int itemCount)
    {
        if (!content || !grid) return;

        var pad = grid.padding;
        var cell = grid.cellSize;
        var spacing = grid.spacing;

        // Determina il numero di colonne (vincolate o calcolate dallo spazio)
        int cols;
        if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount && grid.constraintCount > 0)
            cols = grid.constraintCount;
        else
        {
            float availW = content.rect.width - pad.left - pad.right + spacing.x;
            float unitW = cell.x + spacing.x;
            cols = Mathf.Max(1, Mathf.FloorToInt(availW / unitW));
        }

        // Numero righe richieste per contenere tutte le celle
        int rows = (itemCount == 0) ? 0 : Mathf.CeilToInt(itemCount / (float)cols);

        // Altezza totale = padding + righe*(altezza cella) + spazi tra righe
        float totalH = pad.top + pad.bottom;
        if (rows > 0) totalH += rows * cell.y + (rows - 1) * spacing.y;

        // Applica la nuova size.y al content
        var size = content.sizeDelta;
        size.y = totalH;
        content.sizeDelta = size;
    }

    // Seleziona un item da acquistare e resetta la quantità a 1
    public void SelectBuyItem(int itemId)
    {
        _selectedItemId = itemId;
        _qty = 1;
        RefreshBuyRight();
    }

    // Incrementa/decrementa la quantità richiesta entro limiti ragionevoli
    private void ChangeQty(int delta)
    {
        _qty = Mathf.Clamp(_qty + delta, 1, 9999);
        RefreshBuyRight();
    }

    // Aggiorna pannello destro (icona, nome, prezzi, interazione pulsante)
    private void RefreshBuyRight()
    {
        var data = itemDB.FindById(_selectedItemId);
        if (!data)
        {
            // Nessun item selezionato: pulisci UI e disabilita acquisto
            if (buyIcon) buyIcon.sprite = null;
            if (buyName) buyName.text = "Select Item";
            if (buyPriceEach) buyPriceEach.text = "Select Item";
            if (buyTotalPrice) buyTotalPrice.text = "Select Item";
            if (qtyText) qtyText.text = "0";
            if (buyButton) buyButton.interactable = false;
            return;
        }

        // Mostra dati base
        if (buyIcon) buyIcon.sprite = data.ItemSprite;
        if (buyName) buyName.text = data.ItemName;

        // Calcola prezzo unitario e totale
        int priceEach = GetBuyPrice(data.ItemID);
        if (buyPriceEach) buyPriceEach.text = "Item price:" + priceEach.ToString("N0") + "$";
        if (qtyText) qtyText.text = _qty.ToString();

        long total = (long)priceEach * _qty;
        if (buyTotalPrice) buyTotalPrice.text = "Total price:" + total.ToString("N0") + "$";

        // Abilita il bottone solo se si può permettere il costo (se wallet è assegnato)
        bool canAfford = wallet == null || wallet.Balance >= total;
        if (buyButton) buyButton.interactable = canAfford;
    }

    // Restituisce il prezzo di acquisto: prima dal catalogo (se presente), altrimenti dal DB
    private int GetBuyPrice(int itemId)
    {
        if (catalog != null)
        {
            var e = catalog.FindById(itemId);
            if (e != null) return Mathf.Max(0, e.price);
        }
        var d = itemDB.FindById(itemId);
        return d ? Mathf.Max(0, d.BuyPrice) : 0;
    }

    // Restituisce il prezzo di vendita: usa il SellPrice del DB
    private int GetSellPrice(int itemId)
    {
        // Se vuoi un moltiplicatore (es. 50% del buy), applicalo qui
        var d = itemDB.FindById(itemId);
        return d ? Mathf.Max(0, d.SellPrice) : 0;
    }

    // Procedura di acquisto: verifica fondi, spazio, inserisce negli slot e scala il denaro
    private void PerformBuy()
    {
        var data = itemDB.FindById(_selectedItemId);
        if (!data) return;

        int priceEach = GetBuyPrice(data.ItemID);
        if (priceEach <= 0) return;

        // Richiesta iniziale
        int request = _qty;

        // Limita dalla disponibilità del wallet
        int affordable = wallet ? Mathf.FloorToInt(wallet.Balance / (float)priceEach) : request;
        int toBuy = Mathf.Min(request, affordable);
        if (toBuy <= 0)
        {
            Debug.LogWarning("[Shop] Not enough money.");
            RefreshBuyRight();
            return;
        }

        // Prova a piazzare gli item acquistati (inventario poi taskbar)
        int remaining = toBuy;
        if (inventory != null) remaining = inventory.AddItemToInventory(data.ItemID, remaining);
        if (remaining > 0 && taskbar != null) remaining = taskbar.AddItemToTaskbar(data.ItemID, remaining);

        int placed = toBuy - remaining;
        if (placed <= 0)
        {
            Debug.LogWarning("[Shop] No space for purchased items.");
            RefreshBuyRight();
            return;
        }

        // Scala i soldi solo per gli oggetti effettivamente inseriti
        int cost = placed * priceEach;
        if (wallet != null)
        {
            // Usa TrySpend se esiste, altrimenti fallback (vedi metodo dedicato)
            if (!walletTrySpend(wallet, cost))
            {
                Debug.LogWarning("[Shop] Wallet spend failed unexpectedly.");
                // (Opzionale) rollback degli item aggiunti
            }
        }

        RefreshBuyRight();
    }

    // Compatibilità con diversi PlayerWallet: usa TrySpend se presente, altrimenti fallback
    private bool walletTrySpend(PlayerWallet w, int amount)
    {
        if (w == null) return true;
        try
        {
            // Prova a trovare ed eseguire TrySpend(int)
            var m = typeof(PlayerWallet).GetMethod("TrySpend", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (m != null)
            {
                return (bool)m.Invoke(w, new object[] { amount });
            }
        }
        catch { /* ignora eccezioni riflessione */ }

        // se non c'è TrySpend, non possiamo scalare con Add(-) (nel tuo PlayerWallet Add impedisce negativi)
        if (w.Balance >= amount)
        {
            Debug.LogWarning("PlayerWallet.TrySpend mancante; aggiungilo per una gestione corretta.");
            return true;
        }
        return false;
    }

    #endregion

    #region sellSection
    // Ogni volta che cambia il contenuto di uno slot, ricalcola il totale vendita
    private void OnAnySellSlotChanged(InventorySlot s) => RefreshSellTotals();

    // Scorre gli slot di vendita, calcola il valore totale e abilita/disabilita il pulsante
    private void RefreshSellTotals()
    {
        long total = 0;
        foreach (var s in sellSlots)
        {
            if (!s || !s.Item) continue;
            var it = s.Item;
            int each = GetSellPrice(it.ItemID);
            total += (long)each * it.Quantity;
        }
        if (sellTotalText)
        {
            if (total != 0)
            {
                sellTotalText.text = "Sell items for:" + total.ToString("N0") + "$";
            }
            else 
            {
                sellTotalText.text = "Insert items to sell";
            }

        }
        if (sellButton) sellButton.interactable = total > 0;
    }

    // Esegue la vendita: rimuove gli oggetti dagli slot, somma il denaro e aggiorna UI
    private void PerformSell()
    {
        long total = 0;
        foreach (var s in sellSlots)
        {
            if (!s || !s.Item) continue;
            var it = s.Item;
            int each = GetSellPrice(it.ItemID);
            total += (long)each * it.Quantity;

            Destroy(it.gameObject);
            s.Clear();
        }

        // Aggiunge il denaro ottenuto
        if (wallet != null && total > 0)
        {
            // PlayerWallet.Add esiste già
            wallet.Add((int)Mathf.Min(int.MaxValue, total));
        }

        RefreshSellTotals();
        RefreshBuyRight();
    }

    #endregion

    // Mostra tab Compra e nasconde tab Vendi
    public void ShowBuyTab()
    {
        if (buyTabRoot) buyTabRoot.SetActive(true);
        if (sellTabRoot) sellTabRoot.SetActive(false);
        RefreshBuyRight();
    }

    // Mostra tab Vendi e nasconde tab Compra
    public void ShowSellTab()
    {
        if (buyTabRoot) buyTabRoot.SetActive(false);
        if (sellTabRoot) sellTabRoot.SetActive(true);
        RefreshSellTotals();
    }
}
