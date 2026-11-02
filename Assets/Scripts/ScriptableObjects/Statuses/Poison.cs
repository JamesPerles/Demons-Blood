using UnityEngine;
public class Poison : WeakenDot
{
    public Poison() { statusName = "Poison"; }
    protected override float OutputWeight(HitKind kind) => kind == HitKind.Magic ? 1f : (kind == HitKind.Fusion ? 0.5f : 0f);
    protected override Element[] CuringElements => new Element[] { Element.Nature };
}
