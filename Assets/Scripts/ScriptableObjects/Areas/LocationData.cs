using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
public enum LocationType { Town, Dungeon, Landmark}
[CreateAssetMenu(fileName = "New Location", menuName = "Location")]
public class LocationData : ScriptableObject
{
public string locationName;
public LocationType locationType = LocationType.Town;
[TextArea] public string description;
public string sceneName;
public bool startsDiscovered;
public string travelSpawnPointName;
public static HashSet<string> discoveredNames = new HashSet<string>();
public bool IsDiscovered => startsDiscovered || discoveredNames.Contains(name);
public void Discover() { discoveredNames.Add(name); }
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
static void ResetStatics() {discoveredNames = new HashSet<string>(); }
public Vector2 worldPosition;
public string undiscoveredHint = "? ? ?";
public List<Quest> associatedQuests = new List<Quest>();
public List<ShopStats> shops = new List<ShopStats>();
public List<NPC> npcs = new List<NPC>();
public Sprite icon;
}
