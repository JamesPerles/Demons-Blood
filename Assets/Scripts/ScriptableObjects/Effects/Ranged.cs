using UnityEngine;
public class Ranged : WeaponEffect
{
  public static bool IsRanged(Equipment weapon)
  {
    if(weapon == null || weapon.effects == null) return false;
    foreach(Effect effect in weapon.effects)
    if(effect is Ranged) return true;
    return false;
  }
} 
