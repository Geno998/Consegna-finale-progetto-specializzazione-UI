using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeListCell : MonoBehaviour
{
    [SerializeField] private Image[] ingIcons;                   // Array di 3 icone per gli ingredienti
    [SerializeField] private Image resultIcon;                   // Icona del risultato
    [SerializeField] private TextMeshProUGUI resultNameText;     // Nome del risultato

    private RecipeData data;                                     // Ricetta rappresentata da questa cella
    private CraftingUI owner;                                    // Riferimento al pannello crafting per la selezione

    public void Init(CraftingUI owner, RecipeData data, ItemDatabase db)
    {
        // Inizializza la cella con riferimenti e popola grafica
        this.owner = owner;
        this.data = data;

        // Mostra gli ingredienti disponibili (nasconde icone per slot vuoti)
        for (int i = 0; i < ingIcons.Length; i++)
        {
            var img = ingIcons[i];
            if (data.IsEmptyIndex(i))
            {
                if (img) img.gameObject.SetActive(false);
                continue;
            }
            var d = db.FindById(data.IngredientIDs[i]);
            if (img) { img.gameObject.SetActive(true); img.sprite = d ? d.ItemSprite : null; }
        }

        // Mostra il risultato con sprite/nome (override se impostati)
        var res = db.FindById(data.ResultItemID);
        if (resultIcon) resultIcon.sprite = data.ResultSpriteOverride ? data.ResultSpriteOverride : (res ? res.ItemSprite : null);
        if (resultNameText) resultNameText.text = !string.IsNullOrEmpty(data.ResultNameOverride) ? data.ResultNameOverride : (res ? res.ItemName : "—");

        // Click sulla cella: notifica al pannello di selezionare questa ricetta
        GetComponent<Button>().onClick.AddListener(() => owner.SelectRecipe(data));
    }
}
