using UnityEngine;
public class Burn : WeakenDot
{
    public Burn(){ statusName = "Burn";}
    protected override float OutputWeight(HitKind kind) => kind == HitKind.Physical ? 1f : (kind == HitKind.Fusion ? 0.5f : 0f);
    protected override Element[] CuringElements => new Element[] { Element.Water, Element.Ice };
}
