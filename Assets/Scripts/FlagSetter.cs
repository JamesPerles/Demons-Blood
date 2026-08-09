using UnityEngine;
public class FlagSetter : MonoBehaviour, IInteractable
{
public string flagKey;
public bool oneTimeOnly = true;
bool alreadyTriggered = false;
public void Interact()
    {
        if(FlagManager.instance == null || string.IsNullOrEmpty(flagKey)) return;
        if(oneTimeOnly && (alreadyTriggered || FlagManager.instance.GetFlag(flagKey))) return;
        FlagManager.instance.SetFlag(flagKey);
        alreadyTriggered = true;
    }
}
