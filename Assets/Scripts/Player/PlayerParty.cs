using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Playables;
public class PlayerParty: MonoBehaviour
{
    public GameObject[] playableCharacters;
    public static PlayerParty instance;
    public int maxActiveParty = 4;
    public GameObject[] ActiveParty
    {
        get
        {
            List<GameObject> active = new List<GameObject>();
            for(int i = 0; i < playableCharacters.Length; i++)
            {
                if(active.Count >= maxActiveParty) break;
                active.Add(playableCharacters[i]);
            }
            return active.ToArray();
        }
    }
    public void Swap(GameObject first, GameObject second)
    {
        int indexA = System.Array.IndexOf(playableCharacters, first);
        int indexB = System.Array.IndexOf(playableCharacters, second);
        if(indexA < 0 || indexB < 0) return;
        (playableCharacters[indexA], playableCharacters[indexB]) = (playableCharacters[indexB], playableCharacters[indexA]); 
    }
    public void SetOrder(GameObject[] newOrder){playableCharacters = newOrder;}
    void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
    void Start()
    {
        if(playableCharacters.Length > 0)
        {
            foreach (GameObject characterObject in playableCharacters)
            {
            ActiveStats character = characterObject.GetComponent<ActiveStats>();
            if(character != null) Debug.Log(character.currentName + "...is in the party with" + character.currentHP + "...HP!");
            else Debug.LogError(characterObject.name + " CharacterStats is null!!!");
            }
        }
        else Debug.LogError("No Characters Assigned");
    }
}