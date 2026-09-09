using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New Craft Recipe", menuName = "Recipe/Craft")]
public class CraftRecipe : ScriptableObject
{
    [System.Serializable]
        public class MaterialAmount
    {
        public Materials material;
        public int amount = 1;
    }
public Equipment result;
public Item itemResult;
public List<MaterialAmount> requiredMaterials = new List<MaterialAmount>();
public int goldCost = 0;
}
