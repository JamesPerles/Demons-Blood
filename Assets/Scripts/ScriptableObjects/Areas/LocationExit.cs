using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class LocationExit : MonoBehaviour
{
    public string targetScene;
    public string targetSpawnPoint;
    public Vector3 exitOffsetDirection = Vector2.down;
    bool isLeaving = false;
    void OnTriggerEnter2D(Collider2D other)
    {
        if(isLeaving || !other.CompareTag("Player")) return;
        Leave();
    }
    public void Leave()
    {
        if(string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning($"LocationExit on '{gameObject.name}' has no targetScene set.");
            return;
        }
        isLeaving = true;
        BattleManager.lastTown = targetScene;
        StartCoroutine(LoadTargetScene());
    }
    IEnumerator LoadTargetScene()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);
        while(!operation.isDone) yield return null;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if(playerObj == null)
        {
            Debug.LogWarning($"Could not find object tagged Player after loading '{targetScene}'.");
            yield break;
        }
        Vector3 spawnPosition = playerObj.transform.position;
        if(!string.IsNullOrEmpty(targetSpawnPoint))
        {
            GameObject spawnPoint = GameObject.Find(targetSpawnPoint);
            if(spawnPoint != null) spawnPosition = spawnPoint.transform.position;
            else Debug.LogWarning($"Could not find spawn point '{targetSpawnPoint}' in scene '{targetScene}'.");
            Vector2 direction = exitOffsetDirection.sqrMagnitude > 0.0001f ? exitOffsetDirection.normalized : Vector2.down;
            Vector3 offset = (Vector3)(direction * exitOffsetDirection);
            playerObj.transform.position = spawnPosition + offset;
        }
    }
}
