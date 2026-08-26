using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
public class MapMenu : MonoBehaviour, ITabVisualOwner
{
public PauseMenu host;
public List<TownData> allTowns = new List<TownData>();
public RectTransform mapContainer;
public GameObject nodePrefab;
public GameObject linePrefab;
public TextMeshProUGUI legendText;
public Color currentColor = new Color32(0xD8, 0x5A, 0x30, 0xFF);
public Color discoveredColor = new Color32(0xE8, 0xE4, 0xE0, 0xFF);
public Color undiscoveredColor = new Color32(0x5F, 0x5E, 0x5A, 0xFF);
public float lineThickness = 2f;
public Color lineColor = new Color32(0x3A, 0x16, 0x16, 0xFF);
const string colorMuted = "#8A8580";
const string colorBody = "#C9C2C2";
const string colorAccent = "#D85A30";
List<GameObject> spawnedNodes = new List<GameObject>();
List<GameObject> spawnedLines = new List<GameObject>();
public void OpenTab()
    {
        host.PrepareTabSwitch();
        if(mapContainer != null) mapContainer.gameObject.SetActive(true);
        host.ShowSplitPanel();
        host.SetBreadcrumbSuffix("Map");
        if(host.miniTabGroup != null) host.miniTabGroup.Hide();
        BuildLegend();
        RebuildNodes();
    }
   public void HideVisuals()
    {
        if(mapContainer != null) mapContainer.gameObject.SetActive(false);
    }
    void BuildLegend()
    {
        if(legendText == null) return;
        legendText.text = 
        $"<color={colorAccent}>\u25CF</color> current / visited\n" +
        $"<color={colorMuted}>\u25CF</color> undiscovered";
    }
    void RebuildNodes()
    {
        foreach(GameObject node in spawnedNodes) Destroy(node);
        foreach(GameObject line in spawnedLines) Destroy(line);
        spawnedNodes.Clear();
        spawnedLines.Clear();
        if(nodePrefab == null || mapContainer == null) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(mapContainer);
        Dictionary<TownData, RectTransform> nodeRects = new Dictionary<TownData, RectTransform>();
        TownData defaultSelection = null;
        foreach(TownData town in allTowns)
        {
            if(town == null) continue;
            GameObject nodeObj = Instantiate(nodePrefab, mapContainer);
            RectTransform nodeRect = nodeObj.GetComponent<RectTransform>();
            PositionNode(nodeRect, town.mapPosition);
            nodeRect.SetAsLastSibling();
            nodeRects[town] = nodeRect;
            spawnedNodes.Add(nodeObj);
            MapNodeViewer view = nodeObj.GetComponent<MapNodeViewer>();
            if(view == null) continue;
            bool discovered = town.unlockedForFastTravel;
            bool current = !string.IsNullOrEmpty(town.sceneName) && town.sceneName == BattleManager.lastTown;
            if(view.labelText != null) view.labelText.text = discovered ? town.townName : town.undiscoveredHint;
            if(view.nodeDot != null) view.nodeDot.color = current ? currentColor : (discovered ? discoveredColor : undiscoveredColor);
            if(view.button != null)
            {
                view.button.onClick.RemoveAllListeners();
                TownData capturedTown = town;
                view.button.onClick.AddListener(() => SelectTown(capturedTown));
            }
            if(current) defaultSelection = town;
            else if(defaultSelection == null && discovered) defaultSelection = town;
        }
        HashSet<(TownData, TownData)> drawnPairs = new HashSet<(TownData, TownData)>();
        foreach(TownData town in allTowns)
        {
            if(town == null || !nodeRects.ContainsKey(town)) continue;
            foreach(TownData connected in town.connectedTowns)
            {
                if(connected == null || !nodeRects.ContainsKey(connected)) continue;
                var pairA = (town, connected);
                var pairB = (connected, town);
                if(drawnPairs.Contains(pairA) || drawnPairs.Contains(pairB)) continue;
                drawnPairs.Add(pairA);
                DrawLine(nodeRects[town], nodeRects[connected]);
            }
        }
        if(defaultSelection != null) SelectTown(defaultSelection);
        else if(host.detailText != null) host.detailText.text = "No towns discovered yet";
    }
    void PositionNode(RectTransform nodeRect, Vector2 normalizedPos)
    {
        Vector2 clamped = new Vector2(Mathf.Clamp01(normalizedPos.x), Mathf.Clamp01(normalizedPos.y));
        nodeRect.anchorMin = Vector2.zero;
        nodeRect.anchorMax = Vector2.zero;
        nodeRect.pivot = new Vector2(0.5f, 0.5f);
        Vector2 containerSize = mapContainer.rect.size;
        nodeRect.anchoredPosition = new Vector2(normalizedPos.x * containerSize.x, normalizedPos.y * containerSize.y);
    }
    void DrawLine(RectTransform from, RectTransform to)
    {
        if(linePrefab == null) return;
        GameObject lineObj = Instantiate(linePrefab, mapContainer);
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();
        Image lineImage = lineObj.GetComponent<Image>();
        if(lineImage != null) lineImage.color = lineColor;
        Vector2 diff = to.anchoredPosition - from.anchoredPosition;
        float distance = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        lineRect.anchorMin = Vector2.zero;
        lineRect.anchorMax = Vector2.zero;
        lineRect.pivot = new Vector2(0f, 0.5f);
        lineRect.anchoredPosition = from.anchoredPosition;
        lineRect.sizeDelta = new Vector2(distance, lineThickness);
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);
        lineObj.transform.SetAsFirstSibling();
        spawnedLines.Add(lineObj);
    }
    void SelectTown(TownData town)
    {
        if(host.detailText == null) return;
        bool discovered = town.unlockedForFastTravel;
        bool current = !string.IsNullOrEmpty(town.sceneName) && town.sceneName == BattleManager.lastTown;
        if(!discovered)
        {
            host.detailText.text = 
            $"<size=140%><color=#F2F2F2>{town.undiscoveredHint}</color></size>\n" +
            $"<size=80%><color={colorMuted}>undiscovered</color></size>";
            return;
        }
        string status = current ? "current location" : "visited";
        string result = $"<size=140%><color=#F2F2F2>{town.townName}</color></size>\n";
        result += $"<size=80%><color={colorMuted}>{status}</color></size>\n\n";
        result += $"<color={colorBody}>{town.description}</color>\n\n";
        result += current
        ? $"<color={colorMuted}>You are here.</color>"
        : $"<color={colorMuted}>Reach this location through its town entrance.</color>";
        host.detailText.text = result; 
    }
}
