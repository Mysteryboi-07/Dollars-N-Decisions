using UnityEngine;

public class LaptopUIManager : MonoBehaviour
{
    [Header("Home")]
    [SerializeField] private GameObject appButtonGroup;

    [Header("Apps")]
    [SerializeField] private GameObject[] appObjects;

    [Header("Startup")]
    [SerializeField] private bool showHomeOnEnable = true;

    private GameObject currentApp;

    private void OnEnable()
    {
        if (showHomeOnEnable)
            ShowHome();
    }

    public void OpenApp(GameObject appObject)
    {
        if (appObject == null) return;

        HideAllApps();

        currentApp = appObject;
        currentApp.SetActive(true);

        MinigameManager minigameManager = currentApp.GetComponentInChildren<MinigameManager>(true);

        if (minigameManager != null)
            minigameManager.LaunchNextMinigame();

        if (appButtonGroup != null)
            appButtonGroup.SetActive(false);
    }

    public void ShowHome()
    {
        HideAllApps();

        if (appButtonGroup != null)
            appButtonGroup.SetActive(true);
    }

    public void CloseCurrentApp()
    {
        if (currentApp != null)
        {
            currentApp.SetActive(false);
            currentApp = null;
        }

        if (appButtonGroup != null)
            appButtonGroup.SetActive(true);
    }

    private void HideAllApps()
    {
        currentApp = null;

        if (appObjects == null) return;

        foreach (GameObject appObject in appObjects)
        {
            if (appObject != null)
                appObject.SetActive(false);
        }
    }
}
