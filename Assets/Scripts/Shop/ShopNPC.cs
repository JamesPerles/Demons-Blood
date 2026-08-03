using UnityEngine;
public class ShopNPC : MonoBehaviour, IInteractable
{
public ShopStats shopStats;
public void Interact()
    {
        if(ShopMenu.instance == null) {Debug.LogError("No ShopMenu in Scene"); return;}
       if(shopStats == null) {Debug.LogWarning("ShopNPC has no ShopStats assigned"); return;}
        ShopMenu.instance.Open(shopStats);
    }
}
