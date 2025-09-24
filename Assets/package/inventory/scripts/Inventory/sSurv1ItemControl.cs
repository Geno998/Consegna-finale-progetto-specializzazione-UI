using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class sSurv1ItemControl : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI quantityText;

    [Header("Drag Settings")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform dragLayer;
    [SerializeField] private float longPressSeconds = 0.4f;
    [SerializeField] private Vector2 fullStackPickupOffset = new Vector2(12f, -12f);

    [Header("Prefab")]
    [SerializeField] private GameObject itemPrefab; // assign your item prefab here!

    // Dati base dell’item (ScriptableObject) e proprietà utili
    public sSurv1ItemData itemData { get; private set; }
    public int ItemID => itemData ? itemData.ItemID : _itemId;
    public int Quantity => _quantity;
    public int MaxStack => _maxStack;

    // Slot d’origine per le operazioni di drag & drop
    public InventorySlot DragOriginSlot { get; set; }

    // Stato quantità/stack e fallback per ID/stack massimo
    private int _quantity;
    private int _maxStack = 99;
    private int _itemId = -1;

    // Cache componenti utili per prestazioni
    private RectTransform _rt;
    private CanvasGroup _cg;

    // Flag e dati per gestione input/drag
    private bool _isDragging;
    private bool _leftDown;
    private bool _rightDown;
    private float _downTime;
    private Vector2 _pressLocal;
    private Vector2 _visualOffset;

    // ===== API =====

    // Inizializza l’oggetto item con i suoi dati e la quantità iniziale
    public void OnItemCreate(sSurv1ItemData data, int startQuantity)
    {
        itemData = data;
        _itemId = data ? data.ItemID : -1;
        _maxStack = data ? Mathf.Max(1, data.MaxStackSize) : _maxStack;

        if (!icon) icon = GetComponentInChildren<Image>(true);
        if (icon) icon.sprite = data ? data.ItemSprite : null;

        if (!quantityText) quantityText = GetComponentInChildren<TextMeshProUGUI>(true);

        SetQuantity(startQuantity);
    }

    // Imposta la quantità (clamp a [0, MaxStack]) e aggiorna la label
    public void SetQuantity(int newQty)
    {
        _quantity = Mathf.Clamp(newQty, 0, MaxStack);
        if (quantityText) quantityText.text = (_quantity > 1) ? _quantity.ToString() : "";
    }

    // Aggiunge all’attuale stack, ritorna quanto NON è entrato
    public int AddToStack(int amount)
    {
        int space = MaxStack - _quantity;
        int toAdd = Mathf.Min(space, amount);
        _quantity += toAdd;
        SetQuantity(_quantity);
        return amount - toAdd;
    }

    // Rimuove dall’attuale stack, ritorna quanto effettivamente rimosso
    public int RemoveFromStack(int amount)
    {
        int removed = Mathf.Min(_quantity, amount);
        _quantity -= removed;
        SetQuantity(_quantity);
        return removed;
    }

    // Cache componenti e setup predefiniti (Canvas/DragLayer)
    private void Awake()
    {
        _rt = transform as RectTransform;
        _cg = GetComponent<CanvasGroup>();
        if (!_cg) _cg = gameObject.AddComponent<CanvasGroup>();

        if (!canvas) canvas = FindObjectOfType<Canvas>();
        if (!dragLayer)
        {
            var go = GameObject.Find("DragLayer");
            if (go) dragLayer = go.transform as RectTransform;
        }
    }

    // Memorizza stato al momento del click (tasto, tempo, offset visuale)
    public void OnPointerDown(PointerEventData e)
    {
        _downTime = Time.unscaledTime;
        _leftDown = e.button == PointerEventData.InputButton.Left;
        _rightDown = e.button == PointerEventData.InputButton.Right;
        _visualOffset = Vector2.zero;
        CachePressLocal(e);
    }

    // Resetta lo stato dei tasti al rilascio
    public void OnPointerUp(PointerEventData e)
    {
        _leftDown = _rightDown = false;
    }

    // Inizio del drag: decide quanta quantità “prendere” e prepara la scena
    public void OnBeginDrag(PointerEventData e)
    {
        if (_quantity <= 0) return;

        _isDragging = true;
        if (_cg) _cg.blocksRaycasts = false;

        // Assicura di conoscere lo slot d’origine
        if (!DragOriginSlot)
            DragOriginSlot = GetComponentInParent<InventorySlot>();

        // Determina la quantità desiderata:
        // - Click destro: metà
        // - Pressione lunga con sinistro: intero stack (offset visivo)
        // - Altrimenti: una unità
        int desired = 1;
        if (_rightDown) desired = Mathf.CeilToInt(_quantity / 2f);
        else if (_leftDown && (Time.unscaledTime - _downTime) >= longPressSeconds)
        {
            desired = _quantity;
            _visualOffset = fullStackPickupOffset;
        }
        else
        {
            _visualOffset = Vector2.zero;
        }

        // 1) Stacca QUESTO oggetto su DragLayer così lo slot d’origine si libera
        if (dragLayer) transform.SetParent(dragLayer, true);

        // 2) Se si preleva parzialmente, crea il “resto” e rimettilo nello slot d’origine
        if (desired < _quantity && DragOriginSlot)
        {
            int leftover = _quantity - desired;

            // Istanzia una copia per il rimanente
            var remainderGO = Instantiate(itemPrefab);
            var remainder = remainderGO.GetComponent<sSurv1ItemControl>();
            remainder.canvas = canvas;
            remainder.dragLayer = dragLayer;
            remainder.OnItemCreate(itemData, leftover);
            remainder.DragOriginSlot = DragOriginSlot;

            // Garantisce interattività completa al rimanente
            var remCg = remainderGO.GetComponent<CanvasGroup>();
            if (remCg) remCg.blocksRaycasts = true;
            if (remainder.icon) remainder.icon.raycastTarget = true;

            // Rimetti nell’origine usando SetItem (mantiene lo stato coerente)
            DragOriginSlot.SetItem(remainder);

            // L’oggetto trascinato rappresenta la parte prelevata
            SetQuantity(desired);
        }

        // 3) Posiziona l’oggetto trascinato sul cursore (con eventuale offset)
        UpdateDragPosition(e);
    }

    // Aggiorna la posizione durante il drag
    public void OnDrag(PointerEventData e)
    {
        if (!_isDragging) return;
        UpdateDragPosition(e);
    }

    // Fine del drag: prova a far accettare l’oggetto allo slot d’origine o riposizionalo
    public void OnEndDrag(PointerEventData e)
    {
        _isDragging = false;
        _cg.blocksRaycasts = true;

        if (dragLayer && transform.parent == dragLayer.transform)
        {
            if (DragOriginSlot != null)
            {
                bool ok = DragOriginSlot.TryAcceptDropFromCode(this);
                if (!ok) DragOriginSlot.SetItem(this);
            }
        }
    }

    // Calcola il punto locale del click rispetto al DragLayer (per un posizionamento preciso)
    private void CachePressLocal(PointerEventData e)
    {
        if (!dragLayer || !canvas) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragLayer, e.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out _pressLocal
        );
    }

    // Allinea il RectTransform dell’item alla posizione del cursore (più eventuale offset)
    private void UpdateDragPosition(PointerEventData e)
    {
        if (!dragLayer || !_rt || !canvas) return;

        Vector2 lp;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragLayer, e.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out lp
        );

        // Posiziona esattamente al mouse + offset opzionale (senza accumulo)
        _rt.anchoredPosition = lp + _visualOffset;
    }
}
