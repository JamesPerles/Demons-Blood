using UnityEngine;
using UnityEngine.EventSystems;

public class MenuSelect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
{
public MenuBase owner;
bool pointerInside;
static int checkedFrame = -1;
static Vector3 lastMousePosition;
static bool mouseMovedThisFrame;
public static bool MouseMoved()
    {
        if(checkedFrame != Time.frameCount)
        {
            Vector3 current = Input.mousePosition;
            mouseMovedThisFrame = (Time.frameCount - checkedFrame == 1) && (current - lastMousePosition).sqrMagnitude > 0.01f;
            lastMousePosition = current;
            checkedFrame = Time.frameCount;
        }
        return mouseMovedThisFrame;
    }
public void OnPointerEnter(PointerEventData eventData)
    {
       pointerInside = true;
       TryHoverFocus();
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
    }
    void OnDisable()
    {
        pointerInside = false;
    }
    void Update()
    {
        MouseMoved();
        if(pointerInside) TryHoverFocus();
    }
    void TryHoverFocus()
    {
        if(owner == null) return;
        if(!MouseMoved()) return;
        owner.FocusEntry(gameObject);
    }
    public void OnSelect(BaseEventData eventData)
    {
        if(owner != null) owner.EntryHighlight(gameObject);
    }
}
