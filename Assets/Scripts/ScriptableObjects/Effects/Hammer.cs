public class Hammer : WeaponEffect
{
public override bool IgnoresDefend(ICombatant owner, HitContext hit) => IsActive(owner, hit);
} 
