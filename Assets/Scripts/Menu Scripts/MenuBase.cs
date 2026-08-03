using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public abstract class MenuBase : MonoBehaviour
{
public GameObject menuDisplay;
public Transform optionsGrid;
public GameObject entryPrefab;
public GridLayoutGroup grid;
public float fontSize = 32f;
public Vector2 cellSize = new Vector2(280f, 60f);
public Vector2 spacing = new Vector2(0f, 8f);
protected List<GameObject> spawnedEntries = new List<GameObject>();
protected void SetDisplayActive(bool active)
    {
        if(menuDisplay != null) menuDisplay.SetActive(active);
        else gameObject.SetActive(active);
    }
    protected void ClearEntries()
    {
        foreach (var entry in spawnedEntries) Destroy(entry);
        spawnedEntries.Clear();
    }
    protected void ApplyGridSizing(int columns)
    {
        if(grid == null) return;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.cellSize = cellSize;
        grid.spacing = spacing;
    }
}
