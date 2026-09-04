using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
public class MapMenu : MonoBehaviour, ITabVisualOwner
{
public PauseMenu host;
public List<LocationData> allLocations = new List<LocationData>();
public RectTransform mapContainer;
public GameObject nodePrefab;
public Color townColor = new Color32(0x63, 0x99, 0x22, 0xFF);
public Color dungeonColor = new Color32(0xC4, 0x3B, 0x3B, 0xFF);
public Color landmarkColor = new Color32(0xE0, 0xB0, 0x3A, 0xFF);
public Color undiscoveredColor = new Color32(0x5F, 0x5E, 0x5A, 0xFF);
public Color currentRingColor = new Color32(0xD8, 0x5A, 0x30, 0xFF);
public float mapViewRadius = 30f;
public Color playerMarkerColor = new Color32(0xE8, 0xE4, 0xE0, 0xFF);
public float nodeEdgePadding = 16f;
const string colorMuted = "#8A8580";
const string colorBody = "#C9C2C2";
const string colorAccent = "#D85A30";
public Button travelButton;
public TextMeshProUGUI travelButtonLabel;
List<GameObject> spawnedNodes = new List<GameObject>();
LocationData selectedLocation;
public void OpenTab()
    {
        host.PrepareTabSwitch();
        if(mapContainer != null) mapContainer.gameObject.SetActive(true);
        host.ShowSplitPanel();
        host.SetBreadcrumbSuffix("Map");
        if(host.miniTabGroup != null) host.miniTabGroup.Hide();
        RebuildNodes();
    }
   public void HideVisuals()
    {
        if(mapContainer != null) mapContainer.gameObject.SetActive(false);
        if(travelButton != null) travelButton.gameObject.SetActive(false);
    }
    void RebuildNodes()
    {
        foreach(GameObject node in spawnedNodes) Destroy(node);
        spawnedNodes.Clear();
        if(nodePrefab == null || mapContainer == null) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(mapContainer);
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        Vector2 playerWorldPos = playerObj != null ? (Vector2)playerObj.transform.position : Vector2.zero;
        SpawnPlayerNode();
        LocationData defaultSelection = null;
        foreach(LocationData location in allLocations)
        {
            if(location == null) continue;
            bool current = !string.IsNullOrEmpty(location.sceneName) && location.sceneName == BattleManager.lastTown;
            float distance = Vector2.Distance(location.worldPosition, playerWorldPos);
            if(!current && distance > mapViewRadius) continue;
            GameObject nodeObj = Instantiate(nodePrefab, mapContainer);
            spawnedNodes.Add(nodeObj);
            RectTransform nodeRect = nodeObj.GetComponent<RectTransform>();
            PositionNodeRelative(nodeRect, location.worldPosition - playerWorldPos);
            nodeRect.SetAsLastSibling();
            EntryCard card = nodeObj.GetComponent<EntryCard>();
            if(card == null) continue;
            SetNodeVisual(card, location, current);
            if(card.button != null)
            {
                card.button.onClick.RemoveAllListeners();
                LocationData capturedTown = location;
                card.button.onClick.AddListener(() => SelectLocation(capturedTown));
            }
            if(current) defaultSelection = location;
            else if(defaultSelection == null && location.discovered) defaultSelection = location;
        }
        if(defaultSelection != null) SelectLocation(defaultSelection);
        else
        {
            if(travelButton != null) travelButton.gameObject.SetActive(false);
            if(host.detailText != null) host.detailText.text = "No locations discovered yet";
        }
       
    }
    void SpawnPlayerNode()
    {
        GameObject nodeObj = Instantiate(nodePrefab, mapContainer);
        spawnedNodes.Add(nodeObj);
        RectTransform nodeRect = nodeObj.GetComponent<RectTransform>();
        PositionNodeRelative(nodeRect, Vector2.zero);
        nodeRect.SetAsLastSibling();
        EntryCard card = nodeObj.GetComponent<EntryCard>();
        if(card == null) return;
        if(card.borderImage != null) card.borderImage.color = playerMarkerColor;
        if(card.titleText != null)
        {
            card.titleText.text = "You";
            card.titleText.color = playerMarkerColor;
        }
        if(card.backgroundImage != null)
        {
            Color clear = playerMarkerColor;
            clear.a = 0f;
            card.backgroundImage.color = clear;
        }
        if(card.button != null) card.button.interactable = false;
    }
    void PositionNodeRelative(RectTransform nodeRect, Vector2 worldOffsetFromPlayer)
    {
        Vector2 normalized = new Vector2(
            0.5f + Mathf.Clamp(worldOffsetFromPlayer.x / mapViewRadius, -1f, 1f) * 0.5f,
            0.5f + Mathf.Clamp(worldOffsetFromPlayer.y / mapViewRadius, -1f, 1f) * 0.5f);
            PositionNode(nodeRect, normalized);
    }
    void PositionNode(RectTransform nodeRect, Vector2 normalizedPos)
    {
        Vector2 clamped01 = new Vector2(Mathf.Clamp01(normalizedPos.x), Mathf.Clamp01(normalizedPos.y));
        nodeRect.anchorMin = Vector2.zero;
        nodeRect.anchorMax = Vector2.zero;
        nodeRect.pivot = new Vector2(0.5f, 0.5f);
        Vector2 containerSize = mapContainer.rect.size;
        float x = Mathf.Clamp(clamped01.x * containerSize.x, nodeEdgePadding, Mathf.Max(nodeEdgePadding, containerSize.x - nodeEdgePadding));
        float y = Mathf.Clamp(clamped01.y * containerSize.y, nodeEdgePadding, Mathf.Max(nodeEdgePadding, containerSize.y - nodeEdgePadding));
        nodeRect.anchoredPosition = new Vector2(x, y);
    }
   Color GetTypeColor(LocationType type)
    {
        switch(type)
        {
            case LocationType.Town: return townColor;
            case LocationType.Dungeon: return dungeonColor;
            case LocationType.Landmark: return landmarkColor;
            default: return townColor;
        }
    }
    void SetNodeVisual(EntryCard card, LocationData location, bool current)
    {
        bool discovered = location.discovered;
        if(card.borderImage != null) card.borderImage.color = discovered ? GetTypeColor(location.locationType) : undiscoveredColor;
        if(card.titleText != null)
        {
            card.titleText.text = discovered ? location.locationName : location.undiscoveredHint;
            card.titleText.color = discovered ? new Color32(0xF2, 0xF2, 0xF2, 0xFF) : undiscoveredColor;
        }
        if(card.backgroundImage != null)
        {
            Color ring = currentRingColor;
            ring.a = current ? 1f : 0f;
            card.backgroundImage.color = ring;
        }
    }
    void SelectLocation(LocationData location)
    {
        selectedLocation = location;
        if(host.detailText == null) return;
        bool discovered = location.discovered;
        bool current = !string.IsNullOrEmpty(location.sceneName) && location.sceneName == BattleManager.lastTown;
        if(!discovered)
        {
            host.detailText.text = 
            $"<size=140%><color=#F2F2F2>{location.undiscoveredHint}</color></size>\n" +
            $"<size=80%><color={colorMuted}>undiscovered</color></size>";
            if(travelButton != null) travelButton.gameObject.SetActive(false);
            return;
        }
        string status = current ? "current location" : "visited";
        string result = $"<size=140%><color=#F2F2F2>{location.locationName}</color></size>\n";
        result += $"<size=80%><color={colorMuted}>{status}</color></size>\n\n";
        result += $"<color={colorBody}>{location.description}</color>\n\n";
        host.detailText.text = result;
        if(travelButton != null)
        {
            travelButton.gameObject.SetActive(!current);
            if(!current)
            {
                travelButton.interactable = true;
                if(travelButtonLabel != null) travelButtonLabel.text = "Travel Here";
                travelButton.onClick.RemoveAllListeners();
                travelButton.onClick.AddListener(() => TravelToLocation(location));
            }
        } 
    }
    void TravelToLocation(LocationData location)
    {
        if(location == null || string.IsNullOrEmpty(location.sceneName)) return;
        SceneManager.LoadScene(location.sceneName);
    }
}
