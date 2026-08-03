using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "New Skill", menuName = "Create New Skill")]
public class Skill : Learnable
{
public string skillName;
public List<Effect> effects = new List<Effect>();
}
