using UnityEngine;
using UnityEngine.SceneManagement;
public class LoadMainMenu : MonoBehaviour
{
    void Awake()
    {
        SceneManager.LoadScene("Main Menu");
    }
}
