using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapSceneLoader : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}