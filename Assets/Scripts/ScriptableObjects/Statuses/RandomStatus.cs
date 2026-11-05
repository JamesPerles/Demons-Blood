using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RandomStatus : StatusEffect
{
  [System.Serializable]
  public class Entry
    {
        public StatusEffect status;
        [Range(0, 100)] public int chance = 50;
    }
    public List<Entry> statuses = new List<Entry>();
    public override IEnumerator Apply(object caster, object target)
    {
        List<Entry> order = new List<Entry>(statuses);
        for(int i = order.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Entry swap = order[i]; order[i] = order[j]; order[j] = swap;
        }
        StatusResult result = new StatusResult();
        foreach(Entry entry in order)
        {
            if(entry == null || entry.status == null) continue;
            yield return InflictStatusEffect.TryInflict(caster, target, entry.status, entry.chance, result, false);
            if(result.applied) yield break;
        }
    }
}
