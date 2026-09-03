using UnityEngine;
using UnityEngine.EventSystems;

public class MenuEntrySelect : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
public MenuBase owner;
public void OnPointerEnter(PointerEventData eventData)
    {
        if(owner != null) owner.EntryHighlight(gameObject);
    }
    public void OnSelect(BaseEventData eventData)
    {
        if(owner != null) owner.EntryHighlight(gameObject);
    }
}
