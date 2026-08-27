using System.Collections.Generic;
using UnityEngine;
public class BestiaryManager : MonoBehaviour
{
    public static BestiaryManager instance;
    public List<EnemyStats> allEnemies = new List<EnemyStats>();
    HashSet<int> discoveredEnemies = new HashSet<int>();
    void Awake()
    {
        if(instance == null) { instance = this; DontDestroyOnLoad(gameObject);}
        else Destroy(gameObject);
    }
        public void Discover(EnemyStats enemy)
    {
        if(enemy == null) return;
        discoveredEnemies.Add(enemy.enemyID);
    }
    public bool IsDiscovered(EnemyStats enemy) => enemy != null && discoveredEnemies.Contains(enemy.enemyID);
    public List<int> GetDiscoveredIDs() => new List<int>(discoveredEnemies);
    public void LoadDiscovered(List<int> ids) { discoveredEnemies = new HashSet<int>(ids);}
    }
