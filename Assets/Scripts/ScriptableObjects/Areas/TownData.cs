using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "New Town", menuName = "Town")]
public class TownData : ScriptableObject
{
public string townName;
[TextArea] public string description;
public string sceneName;
public bool unlockedForFastTravel;
public List<Quest> associatedQuests = new List<Quest>();
public List<ShopStats> shops = new List<ShopStats>();
public List<NPC> npcs = new List<NPC>();
public Vector2 mapPosition;
public string undiscoveredHint = "? ? ?";
public List<TownData> connectedTowns = new List<TownData>();
}
