using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipeDatabase", menuName = "UIGame/Recipe Database")]
public class RecipeDatabase : ScriptableObject
{
    public List<RecipeData> recipes = new List<RecipeData>(); // Elenco completo delle ricette
}
