using UnityEngine;
[CreateAssetMenu(fileName = "New Alchemy Recipe", menuName = "Recipe/Alchemy")]
public class AlchemyRecipe : ScriptableObject
{
public Baggable ingredientA;
public Baggable ingredientB;
public Baggable result;
}
