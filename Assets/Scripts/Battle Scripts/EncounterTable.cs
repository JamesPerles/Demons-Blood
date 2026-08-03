using UnityEngine;
[System.Serializable]
public class EncounterGroup
{
    public string groupName;
    public GameObject[] enemies;
    public string spawnPointName;
}
public class EncounterTable : MonoBehaviour
{
    public EncounterGroup [] encounters;
    public EncounterGroup GetRandomEncounter()
    {
        if (encounters.Length == 0) Debug.LogError("Encounter Table Empty!");
        EncounterGroup encounter = encounters[Random.Range(0, encounters.Length)];
        return encounter;
    }
}