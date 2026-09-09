using UnityEngine;
[CreateAssetMenu(fileName = "New Material", menuName = "Create New Material")]
public class Materials : Baggable
{
public string materialName;
public string description;
public int sellPrice = 0;
public int buyPrice = 0;
    public override string DisplayName => materialName;
}
