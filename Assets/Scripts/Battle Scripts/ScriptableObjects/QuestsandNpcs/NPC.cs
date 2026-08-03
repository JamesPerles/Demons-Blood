using UnityEngine;
public class NPC : MonoBehaviour, IInteractable
{
public string npcName;
public string[] dialogueLines;
public void Interact()
    {
        if(DialogueBox.instance == null) {Debug.LogError("No Dialogue in Scene"); return;}
        if(dialogueLines == null || dialogueLines.Length == 0) {Debug.LogWarning($"{npcName} has no lines set"); return;}
        DialogueBox.instance.StartDialogue(npcName, dialogueLines);
    }
}
