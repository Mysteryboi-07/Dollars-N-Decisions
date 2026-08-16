using UnityEngine;

public class FocusViewManager : MonoBehaviour
{
    public static FocusViewManager Instance { get; private set; }

    [Header("Laptop")]
    [SerializeField] private GameObject housePlayerObject;
    [SerializeField] private GameObject laptopCamera;
    [SerializeField] private GameObject laptopScreen;

    [Header("Monitor")]
    [SerializeField] private GameObject officePlayerObject;
    [SerializeField] private GameObject monitorCamera;
    [SerializeField] private GameObject monitorScreen;

    private bool suppressMonitorPlayerRestore;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetActive(laptopCamera, false);
        SetActive(laptopScreen, false);
        SetActive(monitorCamera, false);
        SetActive(monitorScreen, false);
    }

    public void OpenLaptop()
    {
        SetActive(housePlayerObject, false);
        SetActive(laptopCamera, true);
        SetActive(laptopScreen, true);
        GameManager.Instance?.SetHouseEventVisible(false);
        GameManager.Instance?.HideStatsUI();
        GameManager.Instance?.UnlockCursor();
        MinigameTutorialManager.ShowIfNeeded();
        Debug.Log("[LAPTOP] Opened laptop screen.");
    }

    public void CloseLaptop()
    {
        SetActive(laptopScreen, false);
        SetActive(laptopCamera, false);
        SetActive(housePlayerObject, true);
        GameManager.Instance?.ShowStatsUI();
        GameManager.Instance?.SetHouseEventVisible(true);
        GameManager.Instance?.LockCursor();
        Debug.Log("[LAPTOP] Closed laptop screen.");
    }

    public void OpenMonitor()
    {
        suppressMonitorPlayerRestore = false;
        SetActive(officePlayerObject, false);
        SetActive(monitorCamera, true);
        SetActive(monitorScreen, true);
        GameManager.Instance?.HideStatsUI();
        GameManager.Instance?.UnlockCursor();
        MinigameTutorialManager.ShowIfNeeded();
        Debug.Log("[MONITOR] Opened monitor screen.");
    }

    public void CloseMonitor()
    {
        CloseMonitor(false);
    }

    public void CloseMonitorForSceneTravel()
    {
        CloseMonitor(true);
    }

    private void CloseMonitor(bool suppressPlayerRestore)
    {
        suppressMonitorPlayerRestore = suppressPlayerRestore;

        SetActive(monitorScreen, false);
        SetActive(monitorCamera, false);

        if (!suppressMonitorPlayerRestore)
        {
            SetActive(officePlayerObject, true);
            GameManager.Instance?.ShowStatsUI();
            GameManager.Instance?.LockCursor();
        }

        Debug.Log("[MONITOR] Closed monitor screen.");
    }

    private void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
            target.SetActive(isActive);
    }
}
