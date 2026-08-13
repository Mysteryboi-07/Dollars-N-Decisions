using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionUIManager : MonoBehaviour
{
    public static InteractionUIManager Instance { get; private set; }

    [Header("Prompt UI")]
    [SerializeField] private TMP_Text interactionText;
    [SerializeField] private GameObject interactionBackground;

    private InteractableTrigger currentInteractable;
    private InteractionType openInteractionType = InteractionType.None;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        HidePrompt();
    }

    private void Update()
    {
        if (SleepManager.Instance != null && SleepManager.Instance.IsBusy)
            return;

        if (openInteractionType != InteractionType.None)
        {
            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseCurrentFocusView();
            }

            return;
        }

        if (currentInteractable != null &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            HandleInteraction(currentInteractable.InteractionType);
        }
    }

    public void SetCurrentInteractable(InteractableTrigger interactable)
    {
        if (SleepManager.Instance != null && SleepManager.Instance.IsBusy)
            return;

        if (openInteractionType != InteractionType.None)
            return;

        currentInteractable = interactable;
        RefreshPrompt();
    }

    public void ClearCurrentInteractable(InteractableTrigger interactable)
    {
        if (currentInteractable != interactable) return;

        currentInteractable = null;
        HidePrompt();
    }

    public void RefreshPrompt()
    {
        if (currentInteractable == null || openInteractionType != InteractionType.None)
            return;

        string promptMessage = GetPromptMessage(currentInteractable);

        if (string.IsNullOrWhiteSpace(promptMessage))
            HidePrompt();
        else
            ShowPrompt(promptMessage);
    }

    public void CloseMiniFridge()
    {
        openInteractionType = InteractionType.None;

        MiniFridgeManager.Instance?.CloseFridge();
        GameManager.Instance?.ShowStatsUI();
        GameManager.Instance?.UnfreezePlayerFromUI();
        RefreshPrompt();

        Debug.Log("[FRIDGE] Closed mini fridge.");
    }

    public void CloseLaptopScreen()
    {
        openInteractionType = InteractionType.None;
        FocusViewManager.Instance?.CloseLaptop();
        RefreshPrompt();
    }

    public void CloseMonitorScreen()
    {
        openInteractionType = InteractionType.None;
        FocusViewManager.Instance?.CloseMonitor();
        RefreshPrompt();
    }

    public void ReturnHomeFromOfficeWithFade()
    {
        HidePrompt();
        currentInteractable = null;
        SleepManager.Instance?.ReturnHomeFromOffice();
    }

    public void SleepAfterLateHomeWorkWithFade()
    {
        HidePrompt();
        SleepManager.Instance?.SleepAfterLateHomeWork();
    }

    private void HandleInteraction(InteractionType interactionType)
    {
        switch (interactionType)
        {
            case InteractionType.Laptop:
                OpenLaptopScreen();
                break;

            case InteractionType.Monitor:
                OpenMonitorScreen();
                break;

            case InteractionType.Bed:
                GoToSleep();
                break;

            case InteractionType.ShopShelf:
                OpenShopShelf();
                break;

            case InteractionType.Cashier:
                UseCashier();
                break;

            case InteractionType.MiniFridge:
                OpenMiniFridge();
                break;

            case InteractionType.SceneDoor:
                UseSceneDoor();
                break;

            case InteractionType.BusStop:
                OpenBusStopOptions();
                break;

            case InteractionType.CafeteriaStore:
                OpenCafeteriaStore();
                break;

            case InteractionType.CafeteriaTable:
                UseCafeteriaTable();
                break;

            default:
                Debug.Log("[INTERACTION] No interaction assigned.");
                break;
        }
    }

    private void OpenLaptopScreen()
    {
        openInteractionType = InteractionType.Laptop;
        HidePrompt();
        FocusViewManager.Instance?.OpenLaptop();
    }

    private void OpenMonitorScreen()
    {
        openInteractionType = InteractionType.Monitor;
        HidePrompt();
        FocusViewManager.Instance?.OpenMonitor();
    }

    private void OpenMiniFridge()
    {
        openInteractionType = InteractionType.MiniFridge;
        HidePrompt();

        if (MiniFridgeManager.Instance == null)
        {
            openInteractionType = InteractionType.None;
            Debug.LogWarning("[FRIDGE] No MiniFridgeManager found.");
            return;
        }

        MiniFridgeManager.Instance.OpenFridge();
        GameManager.Instance?.HideStatsUI();
        GameManager.Instance?.FreezePlayerForUI();

        Debug.Log("[FRIDGE] Opened mini fridge.");
    }

    private void GoToSleep()
    {
        HidePrompt();
        SleepManager.Instance?.GoToSleep();
    }

    private void OpenShopShelf()
    {
        if (currentInteractable == null) return;

        ConvenienceShopManager.Instance?.AddShelfItemToCart(currentInteractable);
        Debug.Log($"[SHOP] Added shelf item from {currentInteractable.ShopCategory} shelf.");
    }

    private void UseCashier()
    {
        ConvenienceShopManager.Instance?.BeginCheckout();
        Debug.Log("[SHOP] Used cashier.");
    }

    private void UseSceneDoor()
    {
        if (SceneDoorManager.Instance == null)
        {
            Debug.LogWarning("[SCENE] No SceneDoorManager found.");
            return;
        }

        bool didTravel = SceneDoorManager.Instance.TryUseSceneDoor(currentInteractable, interactionText);

        if (!didTravel) return;

        currentInteractable = null;
        HidePrompt();
    }

    private void OpenBusStopOptions()
    {
        if (currentInteractable == null) return;

        if (WaypointTravelManager.Instance == null)
        {
            Debug.LogWarning("[WAYPOINT] No WaypointTravelManager found.");
            return;
        }

        openInteractionType = InteractionType.BusStop;
        HidePrompt();
        WaypointTravelManager.Instance.OpenOptions(currentInteractable.CurrentWaypoint);
    }

    public void FinishBusStopInteraction(bool shouldRefreshPrompt)
    {
        openInteractionType = InteractionType.None;

        if (shouldRefreshPrompt)
        {
            RefreshPrompt();
        }
        else
        {
            currentInteractable = null;
            HidePrompt();
        }
    }

    private void OpenCafeteriaStore()
    {
        if (currentInteractable == null) return;

        if (CafeteriaManager.Instance == null)
        {
            Debug.LogWarning("[CAFETERIA] No CafeteriaManager found.");
            return;
        }

        if (!CafeteriaManager.Instance.CanBuyFromStore(currentInteractable.CafeteriaStoreType))
        {
            RefreshPrompt();
            return;
        }

        openInteractionType = InteractionType.CafeteriaStore;
        HidePrompt();

        bool didOpen = CafeteriaManager.Instance.OpenStore(currentInteractable.CafeteriaStoreType);

        if (didOpen) return;

        openInteractionType = InteractionType.None;
        RefreshPrompt();
    }

    private void UseCafeteriaTable()
    {
        if (CafeteriaManager.Instance == null || !CafeteriaManager.Instance.HasCarriedFood)
            return;

        CafeteriaManager.Instance.ConsumeCarriedFood();
        RefreshPrompt();
    }

    public void FinishCafeteriaStoreInteraction(bool shouldRefreshPrompt)
    {
        openInteractionType = InteractionType.None;

        if (shouldRefreshPrompt)
            RefreshPrompt();
        else
            HidePrompt();
    }

    private void CloseCurrentFocusView()
    {
        switch (openInteractionType)
        {
            case InteractionType.Laptop:
                CloseLaptopScreen();
                break;

            case InteractionType.Monitor:
                CloseMonitorScreen();
                break;

            case InteractionType.MiniFridge:
                CloseMiniFridge();
                break;

            case InteractionType.BusStop:
                WaypointTravelManager.Instance?.CloseOptions();
                break;

            case InteractionType.CafeteriaStore:
                CafeteriaManager.Instance?.CloseStore();
                break;
        }
    }

    private void ShowPrompt(string message)
    {
        if (interactionText == null) return;

        interactionText.text = message;
        interactionText.gameObject.SetActive(true);

        if (interactionBackground != null)
            interactionBackground.SetActive(true);
    }

    private string GetPromptMessage(InteractableTrigger interactable)
    {
        if (interactable == null) return string.Empty;

        switch (interactable.InteractionType)
        {
            case InteractionType.CafeteriaTable:
                return CafeteriaManager.Instance != null && CafeteriaManager.Instance.HasCarriedFood
                    ? interactable.PromptMessage
                    : string.Empty;

            case InteractionType.CafeteriaStore:
                return CafeteriaManager.Instance != null &&
                       !CafeteriaManager.Instance.CanBuyFromStore(interactable.CafeteriaStoreType)
                    ? "Unavailable"
                    : interactable.PromptMessage;

            default:
                return interactable.PromptMessage;
        }
    }

    private void HidePrompt()
    {
        if (interactionText != null)
            interactionText.gameObject.SetActive(false);

        if (interactionBackground != null)
            interactionBackground.SetActive(false);
    }
}
