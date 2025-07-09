using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneSelector : MonoBehaviour
{
    public Button mouseButton;
    public Button mobileButton;
    public Button exitButton;

    void Start()
    {
        mouseButton.onClick.AddListener(LoadMouseScene);
        mobileButton.onClick.AddListener(LoadMobileScene);
        exitButton.onClick.AddListener(ExitGame);
    }

    public void LoadMouseScene()
    {
        // Carica la scena per l'uso con mouse (PC)
        SceneManager.LoadScene("SampleScene");
    }

    public void LoadMobileScene()
    {
        // Carica la scena per il touch (Android / iOS)
        SceneManager.LoadScene("ScenaMobile");
    }

    public void ExitGame()
    {
        // Funziona sia su PC che su Android (ma non in editor)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}