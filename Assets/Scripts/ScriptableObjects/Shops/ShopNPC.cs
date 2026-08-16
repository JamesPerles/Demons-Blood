using UnityEngine;
public class ShopNPC : NPC
{
public ShopStats shopStats;
    public override void Interact()
    {
        if(DialogueBox.instance == null) {Debug.LogError("No Dialogue in Scene"); return;}
        if(dialogueLines == null || dialogueLines.Length == 0)
        {
            OpenShop();
            return;
        }
        DialogueBox.instance.StartDialogue(npcName, dialogueLines, OpenShop);
    }
public void OpenShop()
    {
        if(ShopMenu.instance == null) {Debug.LogError("No ShopMenu in Scene"); return;}
       if(shopStats == null) {Debug.LogWarning("ShopNPC has no ShopStats assigned"); return;}
        ShopMenu.instance.Open(shopStats);
    }
}
