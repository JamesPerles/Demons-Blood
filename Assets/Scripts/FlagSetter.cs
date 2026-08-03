using UnityEngine;
public class FlagSetter : MonoBehaviour
{
public string flagKey;
public bool oneTimeOnly = true;
bool alreadyTriggered = false;
public void Interact()
    {
        if(oneTimeOnly && alreadyTriggered) return;
        if(FlagManager.instance == null || string.IsNullOrEmpty(flagKey)) return;
        FlagManager.instance.SetFlag(flagKey);
        alreadyTriggered = true;
    }
}
