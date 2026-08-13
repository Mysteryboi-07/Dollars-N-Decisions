using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTravelManager : MonoBehaviour
{
    public static SceneTravelManager Instance { get; private set; }

    [SerializeField] private bool keepAcrossScenes = true;
    [SerializeField] private bool lockCursorAfterSceneLoad = true;

    private bool isLoadingScene;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (keepAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void LoadScene(string sceneName)
    {
        if (isLoadingScene || string.IsNullOrWhiteSpace(sceneName))
            return;

        isLoadingScene = true;
        SceneManager.LoadScene(sceneName);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isLoadingScene = false;

        if (lockCursorAfterSceneLoad)
            GameManager.Instance?.LockCursor();
    }
}
