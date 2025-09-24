using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class sSurv1UIController : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private List<GameObject> menuRoots;
    [SerializeField] private GameObject taskbarRoot;
    [SerializeField] private GameObject ShopRoot;

    [Header("Optional: cancel drag on close")]
    [SerializeField] public RectTransform dragLayer;

    // Tasti scorciatoia per aprire/chiudere pannelli
    public KeyCode toggleInventoryKey = KeyCode.I;
    public KeyCode toggleShopKey = KeyCode.S;
    public KeyCode toggleCraftKey = KeyCode.C;


    [SerializeField] private ItemDatabase itemDB;
    [SerializeField] private sSurv1MenuManager inventory;
    [SerializeField] private sSurv1TaskbarManager taskbar;

    private void Update()
    {
        // Evita di intercettare tasti quando si sta scrivendo in un input
        if (IsTextInputFocused())
            return;

        if (Input.GetKeyDown(toggleInventoryKey))
        {
            ToggleMenues(menuRoots[0], menuRoots);
        }

        if (Input.GetKeyDown(toggleShopKey))
        {
            ToggleMenues(menuRoots[1], menuRoots);
        }

        if (Input.GetKeyDown(toggleCraftKey))
        {
            ToggleMenues(menuRoots[2], menuRoots);
        }
    }

    // Mostra/nasconde il menu passato e chiude tutti gli altri
    public void ToggleMenues(GameObject MenuRoot, List<GameObject> menuRoots)
    {
        bool show = !MenuRoot.activeSelf;
        MenuRoot.SetActive(show);

        foreach (GameObject root in menuRoots)
        {
            if (root != null && root != MenuRoot)
            {
                root.SetActive(false);
            }
        }



        // Sicurezza opzionale: se si chiude mentre un item è su DragLayer, riportalo allo slot d’origine
        if (!show && dragLayer != null)
        {
            for (int i = 0; i < dragLayer.childCount; i++)
            {
                var child = dragLayer.GetChild(i);
                var item = child.GetComponent<sSurv1ItemControl>();
                if (item != null)
                {
                    // Chiede allo slot d’origine di riprenderlo (merge o posizionamento)
                    if (item.DragOriginSlot != null)
                    {
                        bool accepted = item.DragOriginSlot.TryAcceptDropFromCode(item);
                        if (!accepted)
                        {
                            item.transform.SetParent(item.DragOriginSlot.transform, false);
                            (item.transform as RectTransform).anchoredPosition = Vector2.zero;
                            item.DragOriginSlot.SetItem(item);
                        }
                    }
                }
            }
        }
    }

    // Aggiunge un item e, se l’inventario è pieno, riversa l’eccesso nella taskbar
    public void AddItemWithOverflow(int itemID, int quantity)
    {
        // Non è necessario consultare qui il DB: i manager lo usano internamente
        int rem = inventory.AddItemToInventory(itemID, quantity);
        if (rem > 0) rem = taskbar.AddItemToTaskbar(itemID, rem);
        if (rem > 0) Debug.LogWarning($"Could not place {rem} of {itemID}");
    }



    // Rileva se un campo di testo (Unity o TMP) ha il focus per inibire i toggle
    private bool IsTextInputFocused()
    {
        var go = EventSystem.current?.currentSelectedGameObject;
        if (!go) return false;

        // Unity InputField
        if (go.GetComponent<UnityEngine.UI.InputField>() != null) return true;

        // TMP_InputField
        if (go.GetComponent<TMP_InputField>() != null) return true;

        return false;
    }
}
