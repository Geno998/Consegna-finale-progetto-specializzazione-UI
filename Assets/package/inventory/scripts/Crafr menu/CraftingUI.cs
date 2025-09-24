using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingUI : MonoBehaviour
{
    [Header("Dati")]
    [SerializeField] private ItemDatabase itemDB;                 // Database generale degli item
    [SerializeField] private RecipeDatabase recipeDB;             // Database con tutte le ricette

    [Header("Manager")]
    [SerializeField] private sSurv1MenuManager inventory;         // Necessita Slots + AddItemToInventory
    [SerializeField] private sSurv1TaskbarManager taskbar;        // Opzionale; espone Slots + AddItemToTaskbar

    // ------------- SINISTRA: Lista ricette -------------
    [Header("Sinistra: Lista ricette")]
    [SerializeField] private TMP_InputField searchField;          // Campo ricerca ricette per nome
    [SerializeField] private ScrollRect listScroll;               // Scroll della lista ricette
    [SerializeField] private RectTransform listContent;           // Contenitore delle celle (figlio di Viewport)
    [SerializeField] private GridLayoutGroup listGrid;            // Layout a griglia sul Content
    [SerializeField] private GameObject recipeCellPrefab;         // Prefab cella (contiene RecipeListCell + Button)

    // ------------- DESTRA: Intestazione selezione -------------
    [Header("Destra: Selezione")]
    [SerializeField] private Image selectedIcon;                  // Icona dell’oggetto risultante
    [SerializeField] private TextMeshProUGUI selectedName;        // Nome dell’oggetto risultante

    // ------------- DESTRA: Liste Have/Need -------------
    [Header("Destra: Posseduti / Richiesti")]
    [SerializeField] private RectTransform haveListRoot;          // Lista verticale “Hai”
    [SerializeField] private RectTransform needListRoot;          // Lista verticale “Ti serve”
    [SerializeField] private GameObject requirementRowPrefab;     // Prefab riga requisito (RequirementRow)

    // ------------- DESTRA: Quantità + Craft -------------
    [Header("Destra: Quantità & Craft")]
    [SerializeField] private BuyQuantityButton qtyLeftBtn;        // Decrementa quantità (tap/hold)
    [SerializeField] private BuyQuantityButton qtyRightBtn;       // Incrementa quantità (tap/hold)
    [SerializeField] private TextMeshProUGUI qtyText;             // Testo quantità selezionata
    [SerializeField, Min(1)] private int clickStep = 1;           // Passo per click singolo
    [SerializeField, Min(1)] private int holdStepPerSec = 10;     // Passo per secondo in pressione prolungata
    [SerializeField] private Button craftButton;                  // Pulsante “Craft”

    // Stato interno
    private readonly List<GameObject> _liveRecipeCells = new();   // Celle ricetta attive nella lista
    private readonly List<GameObject> _haveRows = new();          // Righe “Hai” attive
    private readonly List<GameObject> _needRows = new();          // Righe “Ti serve” attive
    private RecipeData _selected;                                  // Ricetta attualmente selezionata
    private int _qty = 1;                                         // Quantità richiesta
    private bool _canCraft;                                       // True se tutte le risorse bastano

    private void OnEnable()
    {
        // Ricostruisce la lista quando cambia il filtro
        if (searchField) searchField.onValueChanged.AddListener(_ => RebuildRecipeList());

        // Collega pulsanti quantità (tap = +/- clickStep; hold = +/- (holdStepPerSec * interval))
        if (qtyLeftBtn)
        {
            qtyLeftBtn.onClickTap.RemoveAllListeners();
            qtyLeftBtn.onRepeat.RemoveAllListeners();
            qtyLeftBtn.onClickTap.AddListener(() => ChangeQty(-clickStep));
            int leftTick = Mathf.Max(1, Mathf.RoundToInt(holdStepPerSec * qtyLeftBtn.repeatInterval));
            qtyLeftBtn.onRepeat.AddListener(() => ChangeQty(-leftTick));
        }
        if (qtyRightBtn)
        {
            qtyRightBtn.onClickTap.RemoveAllListeners();
            qtyRightBtn.onRepeat.RemoveAllListeners();
            qtyRightBtn.onClickTap.AddListener(() => ChangeQty(+clickStep));
            int rightTick = Mathf.Max(1, Mathf.RoundToInt(holdStepPerSec * qtyRightBtn.repeatInterval));
            qtyRightBtn.onRepeat.AddListener(() => ChangeQty(+rightTick));
        }

        // Avvia il crafting quando si preme il bottone
        if (craftButton)
        {
            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(Craft);
        }

        // Primo popolamento UI
        RebuildRecipeList();
        RefreshRightUI();
    }

    private void OnDisable()
    {
        // Pulisce i listener quando il pannello si disattiva
        if (searchField) searchField.onValueChanged.RemoveAllListeners();
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
        if (craftButton) craftButton.onClick.RemoveAllListeners();
    }

    // ---------------- SINISTRA ----------------
    public void SelectRecipe(RecipeData data)
    {
        // Seleziona una ricetta dalla lista e resetta la quantità
        _selected = data;
        _qty = 1;
        RefreshRightUI();
    }

    private void RebuildRecipeList()
    {
        // Rimuove celle precedenti
        foreach (var go in _liveRecipeCells) Destroy(go);
        _liveRecipeCells.Clear();

        // Applica filtro testo
        string filter = (searchField ? searchField.text : "").Trim().ToLowerInvariant();

        // Crea una cella per ogni ricetta che passa il filtro
        foreach (var r in recipeDB.recipes)
        {
            if (r == null) continue;

            var res = itemDB.FindById(r.ResultItemID);
            string name = !string.IsNullOrEmpty(r.ResultNameOverride) ? r.ResultNameOverride : (res ? res.ItemName : "—");

            if (!string.IsNullOrEmpty(filter) && !name.ToLowerInvariant().Contains(filter))
                continue;

            var go = Instantiate(recipeCellPrefab, listContent, false);
            var cell = go.GetComponent<RecipeListCell>();
            cell.Init(null, r, itemDB);

            // Click della cella = seleziona la ricetta
            var btn = go.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectRecipe(r));

            _liveRecipeCells.Add(go);
        }

        // Aggiorna layout e altezza contenuto in base al numero di celle
        LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
        AdjustContentHeight(listContent, listGrid, _liveRecipeCells.Count);
        if (listScroll) listScroll.verticalNormalizedPosition = 1f; // scroll in alto
    }

    private void AdjustContentHeight(RectTransform content, GridLayoutGroup grid, int itemCount)
    {
        // Calcola il numero di righe necessario e setta l’altezza del contenuto
        if (!content || !grid) return;

        var pad = grid.padding;
        var cell = grid.cellSize;
        var spacing = grid.spacing;

        int cols;
        if (grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount && grid.constraintCount > 0)
            cols = grid.constraintCount;
        else
        {
            float availW = content.rect.width - pad.left - pad.right + spacing.x;
            float unitW = cell.x + spacing.x;
            cols = Mathf.Max(1, Mathf.FloorToInt(availW / unitW));
        }

        int rows = (itemCount == 0) ? 0 : Mathf.CeilToInt(itemCount / (float)cols);

        float totalH = pad.top + pad.bottom;
        if (rows > 0) totalH += rows * cell.y + (rows - 1) * spacing.y;

        var size = content.sizeDelta;
        size.y = totalH;
        content.sizeDelta = size;
    }

    // ---------------- DESTRA ----------------
    private void ChangeQty(int delta)
    {
        // Cambia la quantità tenendola entro margini ragionevoli
        _qty = Mathf.Clamp(_qty + delta, 1, 9999);
        RefreshRightUI();
    }

    private void RefreshRightUI()
    {
        // Aggiorna intestazione e liste “Possedutu/Richiesti” in base alla ricetta e alla quantità
        if (_selected == null)
        {
            if (selectedIcon) selectedIcon.sprite = null;
            if (selectedName) selectedName.text = "—";
            if (qtyText) qtyText.text = "0";
            if (craftButton) craftButton.interactable = false;
            ClearReqRows();
            return;
        }

        var res = itemDB.FindById(_selected.ResultItemID);
        if (selectedIcon) selectedIcon.sprite = _selected.ResultSpriteOverride ? _selected.ResultSpriteOverride : (res ? res.ItemSprite : null);
        if (selectedName) selectedName.text = !string.IsNullOrEmpty(_selected.ResultNameOverride) ? _selected.ResultNameOverride : (res ? res.ItemName : "—");
        if (qtyText) qtyText.text = _qty.ToString();

        var haveMap = BuildHaveMap(); // mappa ID -> quantità posseduta (inventario + taskbar)
        ClearReqRows();

        bool allEnough = true;

        // Cicla max 3 ingredienti: crea righe “Hai” e “Ti serve”, colora in base alla disponibilità
        for (int i = 0; i < 3; i++)
        {
            if (_selected.IsEmptyIndex(i)) continue;

            int id = _selected.IngredientIDs[i];
            int perCraft = Mathf.Max(1, _selected.IngredientCounts[i]);
            int need = perCraft * _qty;

            var data = itemDB.FindById(id);
            string nm = data ? data.ItemName : $"ID {id}";
            Sprite ic = data ? data.ItemSprite : null;

            haveMap.TryGetValue(id, out int have);

            _haveRows.Add(SpawnReq(haveListRoot, ic, nm, have.ToString("N0"), have >= need));
            _needRows.Add(SpawnReq(needListRoot, ic, nm, need.ToString("N0"), have >= need));

            allEnough &= (have >= need);
        }

        _canCraft = allEnough;
        if (craftButton) craftButton.interactable = allEnough;
    }

    private void ClearReqRows()
    {
        // Pulisce righe precedenti per rinfrescare la UI
        foreach (var go in _haveRows) Destroy(go);
        foreach (var go in _needRows) Destroy(go);
        _haveRows.Clear();
        _needRows.Clear();
    }

    private GameObject SpawnReq(RectTransform parent, Sprite icon, string name, string amount, bool enough)
    {
        // Istanzia una riga requisito e imposta icona/nome/quantità/colore
        var go = Instantiate(requirementRowPrefab, parent, false);
        var row = go.GetComponent<RequirementRow>();
        row.Set(icon, name, amount, enough);
        return go;
    }

    private Dictionary<int, int> BuildHaveMap()
    {
        // Somma le quantità per ID item scorrendo tutti gli slot (inventario + taskbar)
        var map = new Dictionary<int, int>();

        foreach (var s in GetAllSlots())
        {
            var it = s.Item;
            if (!it) continue;
            if (!map.ContainsKey(it.ItemID)) map[it.ItemID] = 0;
            map[it.ItemID] += it.Quantity;
        }
        return map;
    }

    private IEnumerable<InventorySlot> GetAllSlots()
    {
        // Iteratore che restituisce tutti gli slot disponibili
        if (inventory != null && inventory.Slots != null)
            foreach (var s in inventory.Slots) yield return s;

        if (taskbar != null && taskbar.Slots != null)
            foreach (var s in taskbar.Slots) yield return s;
    }

    private int ComputeMaxCraftsByIngredients()
    {
        // Calcola quante volte si può craftare in base ai materiali posseduti
        if (_selected == null) return 0;
        int max = int.MaxValue;

        var have = BuildHaveMap();
        for (int i = 0; i < 3; i++)
        {
            if (_selected.IsEmptyIndex(i)) continue;
            int id = _selected.IngredientIDs[i];
            int per = Mathf.Max(1, _selected.IngredientCounts[i]);
            have.TryGetValue(id, out int v);
            int byThis = v / per;
            max = Mathf.Min(max, byThis);
        }

        return max == int.MaxValue ? 0 : max;
    }

    private void Craft()
    {
        // Esegue il crafting: piazza i risultati, poi consuma i materiali corrispondenti
        if (_selected == null || !_canCraft) return;

        int possible = ComputeMaxCraftsByIngredients();
        if (possible <= 0) { RefreshRightUI(); return; }

        int request = _qty;
        int toMake = Mathf.Min(request, possible);

        var res = itemDB.FindById(_selected.ResultItemID);
        if (!res) return;

        // Prova a collocare prima i risultati (inventario poi taskbar)
        int remaining = toMake;
        if (inventory != null) remaining = inventory.AddItemToInventory(res.ItemID, remaining);
        if (remaining > 0 && taskbar != null) remaining = taskbar.AddItemToTaskbar(res.ItemID, remaining);

        int placed = toMake - remaining;
        if (placed <= 0)
        {
            Debug.LogWarning("[Craft] No free space to place crafted items.");
            return;
        }

        // Consuma i materiali solo per la quantità effettivamente collocata
        ConsumeMaterialsFor(placed);

        RefreshRightUI();

        if (placed < request)
            Debug.Log($"[Craft] Crafted {placed}/{request} due to space or ingredients.");
    }

    private void ConsumeMaterialsFor(int amountCrafted)
    {
        // Calcola e drena le quantità richieste per ciascun ingrediente
        for (int i = 0; i < 3; i++)
        {
            if (_selected.IsEmptyIndex(i)) continue;

            int id = _selected.IngredientIDs[i];
            int need = Mathf.Max(1, _selected.IngredientCounts[i]) * amountCrafted;

            need = DrainFromSlots(inventory?.Slots, id, need);
            need = DrainFromSlots(taskbar?.Slots, id, need);
        }
    }

    private int DrainFromSlots(IReadOnlyList<InventorySlot> slots, int itemID, int need)
    {
        // Rimuove progressivamente dagli stack finché il fabbisogno non è soddisfatto
        if (slots == null || need <= 0) return need;

        for (int i = 0; i < slots.Count && need > 0; i++)
        {
            var it = slots[i].Item;
            if (it == null || it.ItemID != itemID) continue;

            int removed = it.RemoveFromStack(need);
            need -= removed;

            if (it.Quantity <= 0)
            {
                Destroy(it.gameObject);
                slots[i].Clear();
            }
        }
        return need;
    }
}
