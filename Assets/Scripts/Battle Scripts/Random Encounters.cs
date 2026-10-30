using UnityEngine;
using UnityEngine.SceneManagement;
public class RandomEncounter : MonoBehaviour
{
    public int minSteps = 10;
    public int maxSteps = 30;
    int stepsTaken;
    int stepsToNextEncounter;
    Vector2Int lastTile;
    static RandomEncounter instance;
    PlayerMovement playerMovement;
    SpriteRenderer playerSprite;
    Collider2D playerCollider;
    EncounterTable currentEncounterTable;
    public static string previousSceneName;
    void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        playerMovement = GetComponent<PlayerMovement>();
        playerSprite = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();
    }
    void Start()
    {
        EncounterThreshold();
        lastTile = Vector2Int.RoundToInt(transform.position);
        currentEncounterTable = FindFirstObjectByType<EncounterTable>();
    }
    void OnDestroy() 
    {
        if(instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void EncounterThreshold()
    {
        stepsToNextEncounter = Random.Range(minSteps, maxSteps + 1);
        stepsTaken = 0;
    }
    void Update()
    {
        if (currentEncounterTable == null) return;
        Vector2Int currentTile = Vector2Int.RoundToInt(transform.position);
        if(currentTile != lastTile)
        {
            lastTile = currentTile;
            stepsTaken++;
            if(stepsTaken >= stepsToNextEncounter) StartEncounter();
        }
    }
    void StartEncounter()
    {
        if(currentEncounterTable == null) return;
        EncounterGroup selectedEncounter = currentEncounterTable.GetRandomEncounter();
        if(selectedEncounter == null) return;
        BattleManager.SelectedEncounter = selectedEncounter;
        RollEncounterType();
        previousSceneName = SceneManager.GetActiveScene().name;
        if(SceneFader.instance != null) 
        SceneFader.instance.LoadScene("BattleScene", () => {playerSprite.enabled = false; playerCollider.enabled = false; }, restoreMovementAfter: false);
        else
        {
            playerSprite.enabled = false;
            playerCollider.enabled = false;
            SceneManager.LoadScene("BattleScene");
        }
        EncounterThreshold();
    }
    void RollEncounterType()
    {
        int roll = Random.Range(0, 100);
        if(roll < 10) BattleManager.encounterType = BattleManager.EncounterType.Ambush;
        else if(roll < 20) BattleManager.encounterType = BattleManager.EncounterType.Advantage;
        else BattleManager.encounterType = BattleManager.EncounterType.Normal;
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) 
    {
        if(scene.name != "BattleScene")
        {
            playerMovement.enabled = true;
            playerSprite.enabled = true;
            playerCollider.enabled = true;
            currentEncounterTable = FindFirstObjectByType<EncounterTable>();
        }
    }
}