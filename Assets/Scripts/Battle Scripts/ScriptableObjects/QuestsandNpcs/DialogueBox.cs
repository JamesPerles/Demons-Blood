using TMPro;
using UnityEngine;
using System.Collections;
public class DialogueBox : MonoBehaviour
{
        public static DialogueBox instance;
        public GameObject panel;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI bodyText;
        public KeyCode advanceKey = KeyCode.E;
        public float charactersPerSecond = 40f;
        public bool isOpen {get; private set;} = false;
        bool isTyping = false;
        string[] currentLines;
        int currentIndex;
        Coroutine typeRoutine;
        System.Action onComplete;
        void Awake()
    {
        if(instance == null) instance = this; else Destroy(gameObject);
    }
    void Start()
    {
        if(panel != null) panel.SetActive(false);
    }
    public void Update()
    {
        if (!isOpen) return;
        if(Input.GetKeyDown(advanceKey)) Advance();
    }
    public void StartDialogue(string speakerName, string[] lines, System.Action onComplete = null)
    {
        currentLines = lines;
        currentIndex = 0;
        isOpen = true;
        this.onComplete = onComplete;
        if(panel != null) panel.SetActive(true);
        if(nameText != null) nameText.text = speakerName;
        ShowLine(currentIndex);
    }
    void Advance()
    {
        if(isTyping)
        {
            if(typeRoutine != null) StopCoroutine(typeRoutine);
            if(bodyText != null) bodyText.text = currentLines[currentIndex];
            isTyping = false;
            return;
        }
    currentIndex++;
    if(currentIndex >= currentLines.Length) {Close(); return;}
    ShowLine(currentIndex);
    }
    void ShowLine(int index)
    {
        if(typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeLine(currentLines[index]));
    }
    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        if(bodyText != null) bodyText.text = "";
        float perSec = SettingsManager.instance != null ? SettingsManager.instance.dialogueTextSpeed : charactersPerSecond;
        float delay = 1f / Mathf.Max(1f, perSec);
        for(int i = 0; i < line.Length; i++)
        {
            if(bodyText != null) bodyText.text += line[i];
            yield return new WaitForSeconds(delay);
    }
    isTyping = false;
}
void Close()
    {
        isOpen = false;
        if(panel != null) panel.SetActive(false);
        currentLines = null;
        onComplete?.Invoke();
        onComplete = null;
    }
    }
