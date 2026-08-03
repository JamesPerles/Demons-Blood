using UnityEngine;
[CreateAssetMenu(fileName = "New Alchemy Recipe", menuName = "Recipe/Alchemy")]
public class AlchemyRecipe : ScriptableObject
{
public Item ingredientA;
public Item ingredientB;
public Item result;
}
