using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;
public class TownEntrance : MonoBehaviour
{
public TownData townData;
public List<string> entrySpawnPointNames = new List<string>();
bool isLoading = false;
void OnTriggerEnter2D(Collider2D other)
    {
        if(isLoading || !other.CompareTag("Player")) return;
        Interact();
    }
public void Interact()
    {
        if(townData == null || string.IsNullOrEmpty(townData.sceneName)) return;
        isLoading = true;
        BattleManager.lastTown = townData.sceneName;
        StartCoroutine(LoadTown());
    }
    IEnumerator LoadTown()
    {
        string spawnPointName = GetSpawnPointName();
        AsyncOperation operation = SceneManager.LoadSceneAsync(townData.sceneName);
        while(!operation.isDone) yield return null;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if(playerObj == null)
        {
            Debug.LogWarning("Could not find object tagged player in town");
            yield break;
        }
        if(string.IsNullOrEmpty(spawnPointName))
        {
            Debug.LogWarning($"TownEntrance for '{townData.townName}' has no entrySpawnPointNames set - player left at the scene's default position");
            yield break;
        }
        GameObject spawnPoint = GameObject.Find(spawnPointName);
        if(spawnPoint == null)
        {
            Debug.LogWarning($"Could not find spawn point '{spawnPointName}' in scene '{townData.sceneName}'.");
            yield break;
        }
        playerObj.transform.position = spawnPoint.transform.position;
    }
    public string GetSpawnPointName()
    {
        if(entrySpawnPointNames == null || entrySpawnPointNames.Count == 0) return null;
        return entrySpawnPointNames[0];
    }
}
