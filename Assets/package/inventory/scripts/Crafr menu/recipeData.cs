using UnityEngine;

[CreateAssetMenu(fileName = "Recipe", menuName = "UIGame/Recipe")]
public class RecipeData : ScriptableObject
{
    [Header("Risultato")]
    public int ResultItemID;                 // ID dell’oggetto risultante
    public string ResultNameOverride;        // (Opzionale) Nome mostrato, altrimenti usa quello dell'ItemData
    public Sprite ResultSpriteOverride;      // (Opzionale) Sprite mostrato, altrimenti quello dell'ItemData

    [Header("Ingredienti (max 3)")]
    [Tooltip("ItemID degli ingredienti. Usa 0 per indicare slot vuoto.")]
    public int[] IngredientIDs = new int[3];       // Esempio: [Bottiglia, Slime, Slime]
    [Tooltip("Quantità necessaria per craft per ciascun indice di ingrediente.")]
    public int[] IngredientCounts = new int[3] { 0, 0, 0 };

    // True se l’indice non contiene un ingrediente valido
    public bool IsEmptyIndex(int i) => i < 0 || i >= IngredientIDs.Length || IngredientIDs[i] == 0 || IngredientCounts[i] <= 0;
}
