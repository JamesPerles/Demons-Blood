using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class PopupManager : MonoBehaviour
{
    public static PopupManager instance;
    public GameObject popupPrefab;
    public Transform popupParent;
    public KeyCode dismissKey = KeyCode.X;
    struct QueuedPopup
    {
        public string title;
        public string message;
        public Sprite icon;
    }
    Queue<QueuedPopup> queue = new Queue<QueuedPopup>();
    bool isShowing;
    void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void Show(string title, string message = "", Sprite icon = null)
    {
        queue.Enqueue(new QueuedPopup { title = title, message = message, icon = icon });
        if(!isShowing) StartCoroutine(ProcessQueue());
    }
    IEnumerator ProcessQueue()
    {
        isShowing = true;
        while(queue.Count > 0)
        {
            QueuedPopup next = queue.Dequeue();
            yield return StartCoroutine(ShowPopup(next));
        }
        isShowing = false;
    }
    IEnumerator ShowPopup(QueuedPopup popupData)
    {
        if(popupPrefab == null || popupParent == null) yield break;
        GameObject spawned = Instantiate(popupPrefab, popupParent);
        InfoPop popup = spawned.GetComponent<InfoPop>();
        if(popup == null)
        {
            Destroy(spawned);
            yield break;
        }
        popup.Setup(popupData.title, popupData.message, popupData.icon);
        yield return StartCoroutine(popup.FadeIn());
        yield return null;
        while(!Input.GetMouseButton(0) && !Input.GetKeyDown(dismissKey)) yield return null;
        yield return StartCoroutine(popup.FadeOutAndDestroy());
    }
}
