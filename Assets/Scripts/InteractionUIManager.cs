using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionUIManager : MonoBehaviour
{
    public static InteractionUIManager Instance { get; private set; }

    [Header("Prompt UI")]
    [SerializeField] private TMP_Text interactionText;

    [Header("Laptop Interaction")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject laptopCamera;
    [SerializeField] private GameObject laptopScreen;

    [Header("Monitor Interaction")]
    [SerializeField] private GameObject workerObject;
    [SerializeField] private GameObject monitorCamera;
    [SerializeField] private GameObject monitorScreen;

    private InteractableTrigger currentInteractable;
    private InteractionType openInteractionType = InteractionType.None;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        HidePrompt();

        if (laptopCamera != null)
            laptopCamera.SetActive(false);

        if (laptopScreen != null)
            laptopScreen.SetActive(false);

        if (workerObject != null)
            workerObject.SetActive(false);

        if (monitorCamera != null)
            monitorCamera.SetActive(false);

        if (monitorScreen != null)
            monitorScreen.SetActive(false);
    }

    private void Update()
    {
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

    // ---------- Trigger Handling ----------

    public void SetCurrentInteractable(InteractableTrigger interactable)
    {
        if (openInteractionType != InteractionType.None) return;

        currentInteractable = interactable;

        if (interactionText != null)
        {
            interactionText.text = interactable.PromptMessage;
            interactionText.gameObject.SetActive(true);
        }
    }

    public void ClearCurrentInteractable(InteractableTrigger interactable)
    {
        if (currentInteractable != interactable) return;

        currentInteractable = null;
        HidePrompt();
    }

    // ---------- Interaction Handling ----------

    private void HandleInteraction(InteractionType interactionType)
    {
        switch (interactionType)
        {
            case InteractionType.Laptop:
                OpenLaptopScreen();
                break;

            case InteractionType.HouseDoor:
                EnterHouseDoor();
                break;

            case InteractionType.Monitor:
                OpenMonitorScreen();
                break;

            default:
                Debug.Log("[INTERACTION] No interaction assigned.");
                break;
        }
    }

    // ---------- Door Interaction ----------

    private void EnterHouseDoor()
    {
        HidePrompt();
        currentInteractable = null;

        if (playerObject != null)
            playerObject.SetActive(false);

        if (workerObject != null)
            workerObject.SetActive(true);

        Debug.Log("[DOOR] Switched from player to worker.");
    }

    // ---------- Laptop Screen ----------

    private void OpenLaptopScreen()
    {
        openInteractionType = InteractionType.Laptop;

        HidePrompt();

        if (playerObject != null)
            playerObject.SetActive(false);

        if (laptopCamera != null)
            laptopCamera.SetActive(true);

        if (laptopScreen != null)
            laptopScreen.SetActive(true);

        UIManager.Instance?.UnlockCursor();

        Debug.Log("[LAPTOP] Opened laptop screen.");
    }

    public void CloseLaptopScreen()
    {
        openInteractionType = InteractionType.None;

        if (laptopScreen != null)
            laptopScreen.SetActive(false);

        if (laptopCamera != null)
            laptopCamera.SetActive(false);

        if (playerObject != null)
            playerObject.SetActive(true);

        UIManager.Instance?.LockCursor();

        if (currentInteractable != null &&
            interactionText != null)
        {
            interactionText.text = currentInteractable.PromptMessage;
            interactionText.gameObject.SetActive(true);
        }

        Debug.Log("[LAPTOP] Closed laptop screen.");
    }

    // ---------- Monitor Screen ----------

    private void OpenMonitorScreen()
    {
        openInteractionType = InteractionType.Monitor;

        HidePrompt();

        if (workerObject != null)
            workerObject.SetActive(false);

        if (monitorCamera != null)
            monitorCamera.SetActive(true);

        if (monitorScreen != null)
            monitorScreen.SetActive(true);

        UIManager.Instance?.UnlockCursor();

        Debug.Log("[MONITOR] Opened monitor screen.");
    }

    public void CloseMonitorScreen()
    {
        openInteractionType = InteractionType.None;

        if (monitorScreen != null)
            monitorScreen.SetActive(false);

        if (monitorCamera != null)
            monitorCamera.SetActive(false);

        if (workerObject != null)
            workerObject.SetActive(true);

        UIManager.Instance?.LockCursor();

        if (currentInteractable != null &&
            interactionText != null)
        {
            interactionText.text = currentInteractable.PromptMessage;
            interactionText.gameObject.SetActive(true);
        }

        Debug.Log("[MONITOR] Closed monitor screen.");
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
        }
    }

    // ---------- Helpers ----------

    private void HidePrompt()
    {
        if (interactionText != null)
            interactionText.gameObject.SetActive(false);
    }
}
