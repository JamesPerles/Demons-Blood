using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New Craft Recipe", menuName = "Recipe")]
public class CraftRecipe : ScriptableObject
{
public Equipment result;
public Item itemResult;
public List<MaterialAmount> requiredMaterials = new List<MaterialAmount>();
public int goldCost = 0;
}
