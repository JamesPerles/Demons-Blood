using System.Collections;
using UnityEngine;
public abstract class Effect : ScriptableObject
{
    public string effectName;
    public string description;
    public abstract IEnumerator Apply(object caster, object target);
}
