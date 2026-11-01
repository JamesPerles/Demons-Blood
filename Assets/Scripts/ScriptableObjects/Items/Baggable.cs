using UnityEngine;
public abstract class Baggable : ScriptableObject
{
    public bool forgeable = true;
    public bool sellable = true;
    public abstract string DisplayName { get; }
    public virtual string StyledName => DisplayName;
}
