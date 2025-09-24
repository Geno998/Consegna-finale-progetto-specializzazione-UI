using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InventorySlot : MonoBehaviour, IDropHandler
{
    // Riferimento all'oggetto item presente nello slot (null se vuoto)
    public sSurv1ItemControl Item { get; private set; }

    // Comodità: vero se lo slot è libero
    public bool IsEmpty => Item == null;

    // Evento statico per notificare cambiamenti del contenuto dello slot
    public static System.Action<InventorySlot> OnSlotContentsChanged;
    private void NotifyChanged() => OnSlotContentsChanged?.Invoke(this);

    // Adegua il RectTransform del figlio alle dimensioni dello slot
    private void NormalizeChildRect(RectTransform rt)
    {
        if (!rt) return;
        var r = (RectTransform)transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.sizeDelta = r.rect.size;

        var img = rt.GetComponent<Image>();
        if (img) img.preserveAspect = true;
    }

    // Imposta un item nello slot e aggiorna gerarchia/visuale/origine di drag
    public void SetItem(sSurv1ItemControl item)
    {
        Item = item;
        if (item != null)
        {
            item.transform.SetParent(transform, false);
            NormalizeChildRect(item.transform as RectTransform);
            item.DragOriginSlot = this;
        }

        RebindItemRef();
        NotifyChanged();
    }

    // Svuota lo slot (non distrugge l’oggetto)
    public void Clear()
    {
        Item = null;
        NotifyChanged();
    }

    // Gestione del drop via sistema eventi di Unity
    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<sSurv1ItemControl>() : null;
        if (dragged == null) return;
        TryAcceptDropFromCode(dragged);
    }


    // Prova ad accettare un item trascinato in questo slot gestendo merge/spostamento.
    // Ritorna true se l’operazione è stata gestita qui.

    public bool TryAcceptDropFromCode(sSurv1ItemControl dragged)
    {
        if (dragged == null) return false;


        RebindItemRef();
        var origin = dragged.DragOriginSlot;
        var here = Item;

        EnsureInteractable(here);

        // --- Caso A: drop sullo stesso slot di origine ---
        if (origin == this)
        {
            if (here == null)
            {
                // Slot risultato vuoto: rimetti l’oggetto
                SetItem(dragged);
                return true;
            }

            if (here == dragged)
            {
                // Era solo stato mosso per il drag: riancora
                SetItem(dragged);
                return true;
            }

            // Stesso slot & stesso ID -> fondi sempre in 'here'
            if (here.ItemID == dragged.ItemID)
            {
                int remainder = here.AddToStack(dragged.Quantity);
                if (remainder == 0)
                {
                    // completamente unito; l’oggetto trascinato non serve più
                    Object.Destroy(dragged.gameObject);
                }
                else
                {
                    // non tutto è entrato; rimetti l’avanzo nell’oggetto trascinato e riancora nello slot
                    dragged.SetQuantity(remainder);
                    SetItem(dragged);
                }
                return true;
            }

            // Oggetti diversi nello stesso slot -> semplicemente riancora il trascinato
            SetItem(dragged);
            return true;
        }

        // --- Caso B: drop su uno slot diverso ---

        // B1) Slot target vuoto -> sposta qui l’oggetto
        if (here == null)
        {
            if (origin != null) origin.Clear();
            SetItem(dragged);
            return true;
        }

        // B2) Slot target occupato dallo STESSO ID -> FUSIONE dentro 'here'
        if (here.ItemID == dragged.ItemID)
        {
            int remainder = here.AddToStack(dragged.Quantity);
            if (remainder == 0)
            {
                // fusione completa: distruggi trascinato & svuota origine
                if (origin != null) origin.Clear();
                Object.Destroy(dragged.gameObject);
            }
            else
            {
                // non tutto entra: il trascinato trattiene il resto e torna all’origine
                dragged.SetQuantity(remainder);

                if (origin != null) origin.SetItem(dragged);
                else SetItem(dragged); // origine sconosciuta; tienilo qui (caso raro)
            }
            return true;
        }
        return false;
    }

    // Rileggi l’item figlio presente nello slot per riallineare il riferimento interno
    private void RebindItemRef()
    {
        var child = GetComponentInChildren<sSurv1ItemControl>(includeInactive: true);
        if (child != null)
        {
            Item = child;
            Item.DragOriginSlot = this;
        }
        else
        {
            Item = null;
        }
    }

    // Assicura che l’item sia interattivo (raycast abilitati) dopo operazioni di drag
    private void EnsureInteractable(sSurv1ItemControl item)
    {
        if (!item) return;
        var cg = item.GetComponent<CanvasGroup>();
        if (cg) cg.blocksRaycasts = true;

        var img = item.GetComponentInChildren<Image>(true);
        if (img) img.raycastTarget = true;
    }
}
