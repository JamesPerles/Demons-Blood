using UnityEngine;
using UnityEngine.SceneManagement;
public class LoadMainMenu : MonoBehaviour
{
    void Awake()
    {
        if(SceneFader.instance != null) SceneFader.instance.LoadScene("Main Menu");
       else SceneManager.LoadScene("Main Menu");
    }
}
