using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class SkillTree
{
public string treeName = "New Tree";
public List<SkillTreePath> paths = new List<SkillTreePath>();
}
[CreateAssetMenu(fileName = "New Skill Tree Set", menuName = "Skill Tree Set")]
public class SkillTreeSet : ScriptableObject
{
    public List<SkillTree> trees = new List<SkillTree>();
    void OnValidate()
    {
        while(trees.Count < 5) trees.Add(new SkillTree());
        if(trees.Count > 5) trees.RemoveRange(5, trees.Count - 5);
    }
}
[System.Serializable]
public class SkillTreePath
{
    public string pathName;
    [UnityEngine.Range(0, 100)] public int pointsRequired = 5;
    public Learnable learnable;
}
