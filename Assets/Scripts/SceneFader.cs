using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class SceneFader : MonoBehaviour
{
public static SceneFader instance { get; private set; }
public Image fadeImage;
public float fadeDuration = 0.5f;
bool isFading;
void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
        if(fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
        }
    }
    public void LoadScene(string sceneName, System.Action onLoaded = null, bool restoreMovementAfter = true)
    {
        if(isFading) return;
        StartCoroutine(FadeAndLoad(sceneName, -1, onLoaded, restoreMovementAfter));
    }
    public void LoadScene(int buildIndex, System.Action onLoaded = null, bool restoreMovementAfter = true)
    {
        if(isFading) return;
        StartCoroutine(FadeAndLoad(null, buildIndex, onLoaded, restoreMovementAfter));
    }
    IEnumerator FadeAndLoad(string sceneName, int buildIndex, System.Action onLoaded, bool restoreMovementAfter)
    {
        isFading = true;
        PlayerMovement playerMovement = FindPlayerMovement();
        if(playerMovement != null) playerMovement.enabled = false;
        yield return StartCoroutine(Fade(1f));
        AsyncOperation load = sceneName != null 
        ? SceneManager.LoadSceneAsync(sceneName) 
        : SceneManager.LoadSceneAsync(buildIndex);
        load.allowSceneActivation = false;
        while(load.progress < 0.9f) yield return null;
        load.allowSceneActivation = true;
        while(!load.isDone) yield return null;
        onLoaded?.Invoke();
        yield return StartCoroutine(Fade(0f));
        if(restoreMovementAfter)
        {
            playerMovement = FindPlayerMovement();
            if(playerMovement != null) playerMovement.enabled = true;
        }
        isFading = false;
    }
    PlayerMovement FindPlayerMovement()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        return playerObj != null ? playerObj.GetComponent<PlayerMovement>() : null;
    }
    IEnumerator Fade(float targetAlpha)
    {
        if(fadeImage == null) yield break;
        fadeImage.raycastTarget = true;
        float startAlpha = fadeImage.color.a;
        float durationTime = 0f;
        while(durationTime < fadeDuration)
        {
            durationTime += Time.unscaledDeltaTime;
            Color c = fadeImage.color;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, durationTime / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        Color finalColor = fadeImage.color;
        finalColor.a = targetAlpha;
        fadeImage.color = finalColor;
        fadeImage.raycastTarget = targetAlpha > 0.01f;
    }
}
