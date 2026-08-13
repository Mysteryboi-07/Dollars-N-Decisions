using UnityEngine;

public class HouseSceneStartupManager : MonoBehaviour
{
    [Header("Scene Objects")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject mainUI;
    [SerializeField] private GameObject houseTutorialCamera;
    [SerializeField] private HouseIntroTutorialManager houseIntroTutorial;

    [Header("Startup")]
    [SerializeField] private bool playTutorialOnSceneStart = true;
    [SerializeField] private bool playOnlyOncePerRun = true;

    private static bool hasPlayedThisRun;

    private void Start()
    {
        if (mainUI != null)
            mainUI.SetActive(true);

        if (playTutorialOnSceneStart && (!playOnlyOncePerRun || !hasPlayedThisRun) && houseIntroTutorial != null)
        {
            StartHouseTutorial();
            return;
        }

        StartGameplay();
    }

    public static void ResetTutorialForNewRun()
    {
        hasPlayedThisRun = false;
    }

    private void StartHouseTutorial()
    {
        hasPlayedThisRun = true;

        if (playerObject != null)
            playerObject.SetActive(false);

        if (houseTutorialCamera != null)
            houseTutorialCamera.SetActive(true);

        houseIntroTutorial.BeginTutorial(StartGameplay);
    }

    private void StartGameplay()
    {
        if (houseTutorialCamera != null)
            houseTutorialCamera.SetActive(false);

        if (playerObject != null)
            playerObject.SetActive(true);

        GameManager.Instance?.LockCursor();
        GameManager.Instance?.ShowStatsUI();
        GameManager.Instance?.SetHouseEventVisible(true);
    }
}
