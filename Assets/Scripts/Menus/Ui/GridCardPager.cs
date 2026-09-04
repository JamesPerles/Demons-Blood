using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class CardGridSpec
{
    public string title;
    public string subText;
    public string detail;
    public System.Action onSelect;
    public bool enabled;
    public CardGridSpec(string title, string subText, string detail, System.Action onSelect, bool enabled = true)
    {
        this.title = title;
        this.subText = subText;
        this.detail = detail;
        this.onSelect = onSelect;
        this.enabled = enabled;
    }
}
public class GridCardPager
{
    public int columns;
    public int rows;
    public float rowSpacing;
    public int PerPage => Mathf.Max(1, columns) * Mathf.Max(1, rows);
    public Color defaultBorderColor = new Color32(0x3A, 0x16, 0x16, 0xFF);
    public Color defaultBackgroundColor = new Color32(0x24, 0x10, 0x10, 0xFF);
    public Color defaultTitleColor = new Color32(0xC9, 0xC2, 0xC2, 0xFF);
    readonly GameObject cardPrefab;
    readonly Transform grid;
    readonly PauseMenu host;
    List<GameObject> spawnedCards = new List<GameObject>();
    List<Transform> spawnedRows = new List<Transform>();
    Transform currentRow;
    int cardsInCurrentRow;
    List<CardGridSpec> allSpecs = new List<CardGridSpec>();
    List<CardGridSpec> currentPageSpecs = new List<CardGridSpec>();
    int currentPage;
    public GridCardPager(GameObject cardPrefab, Transform grid, PauseMenu host, int columns = 3, int rows = 3, float rowSpacing = 100f)
    {
        this.cardPrefab = cardPrefab;
        this.grid = grid;
        this.host = host;
        this.columns = columns;
        this.rows = rows;
        this.rowSpacing = rowSpacing;
    }
    public List<GameObject> SpawnedCards => spawnedCards;
    public List<CardGridSpec> CurrentPageSpecs => currentPageSpecs;
    public int CurrentPage => currentPage;
    public int MaxPage => allSpecs.Count == 0 ? 0 : (allSpecs.Count - 1) / PerPage;
    public int TotalCount => allSpecs.Count;
    public void SetSpecs(List<CardGridSpec> specs, int page = 0)
    {
        allSpecs = specs ?? new List<CardGridSpec>();
        currentPage = Mathf.Clamp(page, 0, MaxPage);
        Rebuild();
    }
    public void NextPage()
    {
        currentPage = currentPage >= MaxPage ? 0 : currentPage + 1;
        Rebuild();
    }
    public void PreviousPage()
    {
        currentPage = currentPage <= 0 ? MaxPage : currentPage - 1;
        Rebuild();
    }
    public void SelectFirstOnPage()
    {
        if(spawnedCards.Count == 0 || currentPageSpecs.Count == 0) return;
        host?.EntryHighlight(spawnedCards[0]);
        currentPageSpecs[0].onSelect?.Invoke();
    }
    public void Clear()
    {
        foreach(GameObject card in spawnedCards) if(card != null) Object.Destroy(card);
        spawnedCards.Clear();
        foreach(Transform row in spawnedRows) if(row != null) Object.Destroy(row.gameObject);
        spawnedRows.Clear();
        currentRow = null;
        cardsInCurrentRow = 0;
    }
    void Rebuild()
    {
        Clear();
        if(cardPrefab == null || grid == null) return;
        int start = currentPage * PerPage;
        int count = Mathf.Min(PerPage, allSpecs.Count - start);
        for(int i = start; i < start + count; i++) SpawnCard(allSpecs[i]);
        currentPageSpecs = allSpecs.GetRange(start < 0 ? 0 : start, Mathf.Max(0, count));
        if(host != null && host.pageText != null)
        host.pageText.text = MaxPage > 0 ? $"page {currentPage + 1} / {MaxPage + 1}" : "";
    }
    Transform GetRow()
    {
        if(currentRow == null || cardsInCurrentRow >= Mathf.Max(1, columns))
        {
            GameObject rowObj = new GameObject("Row", typeof(RectTransform));
            rowObj.transform.SetParent(grid, false);
            RectTransform rowRect = rowObj.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(0f, 1f);
            rowRect.pivot = new Vector2(0f, 1f);
            HorizontalLayoutGroup rowLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = rowSpacing;
            rowLayout.childAlignment = TextAnchor.UpperLeft;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childScaleWidth = true;
            rowLayout.childScaleHeight = true;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            currentRow = rowObj.transform;
            spawnedRows.Add(currentRow);
            cardsInCurrentRow = 0;
        }
        cardsInCurrentRow++;
        return currentRow;
    }
    void SpawnCard(CardGridSpec spec)
    {
        Transform row = GetRow();
        GameObject cardObj = Object.Instantiate(cardPrefab, row);
        spawnedCards.Add(cardObj);
        EntryCard card = cardObj.GetComponent<EntryCard>();
        if(card == null) return;
        if(card.titleText != null) 
        {
            card.titleText.text = spec.title;
            card.titleText.color = defaultTitleColor;
        }
        if(card.subText != null) card.subText.text = spec.subText;
        if(card.borderImage != null) card.borderImage.color = defaultBorderColor;
        if(card.backgroundImage != null)
        {
            Color bg = defaultBackgroundColor;
            bg.a = 0f;
            card.backgroundImage.color = bg;
        }
        MenuOption option = new MenuOption(spec.title, () => { }) { description = spec.detail };
        if(host != null) host.RegisterEntry(cardObj, option);
        if(card.button != null)
        {
            card.button.interactable = spec.enabled;
            card.button.onClick.RemoveAllListeners();
            if(spec.enabled)
            {
                GameObject capturedCard = cardObj;
                System.Action onSelect = spec.onSelect;
                card.button.onClick.AddListener(() =>
                {
                    host?.EntryHighlight(capturedCard);
                    onSelect?.Invoke();
                });
            }
        }
        UpdateRowSize(row);
    }
    void UpdateRowSize(Transform row)
    {
        RectTransform rowRect = row as RectTransform;
        if(rowRect == null) return;
        float totalWidth = 0f;
        float maxHeight = 0f;
        int childCount = row.childCount;
        for(int i = 0; i < childCount; i++)
        {
            RectTransform childRect = row.GetChild(i) as RectTransform;
            if(childRect == null) continue;
            totalWidth += childRect.rect.width;
            maxHeight = Mathf.Max(maxHeight, childRect.rect.height);
        }
        if(childCount > 1) totalWidth += rowSpacing * (childCount - 1);
        rowRect.sizeDelta = new Vector2(totalWidth, maxHeight);
    }
}
