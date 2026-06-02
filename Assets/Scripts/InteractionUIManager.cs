using System.Collections;
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

    [Header("Sleep Interaction")]
    [SerializeField] private CanvasGroup sleepFadeGroup;
    [SerializeField] private float sleepFadeInDuration = 1f;
    [SerializeField] private float sleepHoldDuration = 2f;
    [SerializeField] private float sleepFadeOutDuration = 1f;

    private InteractableTrigger currentInteractable;
    private InteractionType openInteractionType = InteractionType.None;
    private Coroutine sleepRoutine;
    private Coroutine officeReturnRoutine;
    private bool isSleeping;

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

        if (sleepFadeGroup != null)
        {
            sleepFadeGroup.alpha = 0f;
            sleepFadeGroup.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isSleeping) return;

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
        if (isSleeping) return;
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

            case InteractionType.OfficeDoor:
                EnterOfficeDoor();
                break;

            case InteractionType.Monitor:
                OpenMonitorScreen();
                break;

            case InteractionType.Bed:
                GoToSleep();
                break;

            default:
                Debug.Log("[INTERACTION] No interaction assigned.");
                break;
        }
    }

    // ---------- Door Interaction ----------

    private void EnterHouseDoor()
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanEnterOffice)
        {
            if (interactionText != null)
            {
                interactionText.text = "Office is closed";
                interactionText.gameObject.SetActive(true);
            }

            Debug.Log("[DOOR] Office is closed. Come back before 2100.");
            return;
        }

        HidePrompt();
        currentInteractable = null;

        if (playerObject != null)
            playerObject.SetActive(false);

        if (workerObject != null)
            workerObject.SetActive(true);

        Debug.Log("[DOOR] Switched from player to worker.");
    }

    private void EnterOfficeDoor()
    {
        HidePrompt();
        currentInteractable = null;

        if (workerObject != null)
            workerObject.SetActive(false);

        if (playerObject != null)
            playerObject.SetActive(true);

        Debug.Log("[DOOR] Switched from worker to player.");
    }

    public void ReturnHomeFromOfficeWithFade()
    {
        if (officeReturnRoutine != null) return;

        officeReturnRoutine = StartCoroutine(ReturnHomeFromOfficeRoutine());
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

        GameManager.Instance?.HideStatsUI();
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

        GameManager.Instance?.ShowStatsUI();
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

        GameManager.Instance?.HideStatsUI();
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

        GameManager.Instance?.ShowStatsUI();
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

    // ---------- Sleep Interaction ----------

    private void GoToSleep()
    {
        if (sleepRoutine != null) return;

        sleepRoutine = StartCoroutine(SleepRoutine());
    }

    private IEnumerator SleepRoutine()
    {
        isSleeping = true;
        HidePrompt();

        if (sleepFadeGroup == null)
        {
            Debug.LogWarning("[SLEEP] No sleep fade CanvasGroup assigned.");
            isSleeping = false;
            sleepRoutine = null;
            yield break;
        }

        sleepFadeGroup.gameObject.SetActive(true);

        yield return FadeSleepOverlay(0f, 1f, sleepFadeInDuration);
        yield return new WaitForSeconds(sleepHoldDuration);

        GameManager.Instance?.WakeUp();

        yield return FadeSleepOverlay(1f, 0f, sleepFadeOutDuration);

        sleepFadeGroup.gameObject.SetActive(false);

        isSleeping = false;
        sleepRoutine = null;

        if (currentInteractable != null &&
            interactionText != null)
        {
            interactionText.text = currentInteractable.PromptMessage;
            interactionText.gameObject.SetActive(true);
        }

        Debug.Log("[SLEEP] Finished sleeping.");
    }

    private IEnumerator ReturnHomeFromOfficeRoutine()
    {
        isSleeping = true;
        HidePrompt();

        if (sleepFadeGroup == null)
        {
            Debug.LogWarning("[OFFICE] No fade CanvasGroup assigned.");
            EnterOfficeDoor();
            isSleeping = false;
            officeReturnRoutine = null;
            yield break;
        }

        sleepFadeGroup.gameObject.SetActive(true);

        yield return FadeSleepOverlay(0f, 1f, sleepFadeInDuration);
        yield return new WaitForSeconds(sleepHoldDuration);

        if (monitorScreen != null)
            monitorScreen.SetActive(false);

        if (monitorCamera != null)
            monitorCamera.SetActive(false);

        openInteractionType = InteractionType.None;
        EnterOfficeDoor();

        yield return FadeSleepOverlay(1f, 0f, sleepFadeOutDuration);

        sleepFadeGroup.gameObject.SetActive(false);
        GameManager.Instance?.ShowStatsUI();
        UIManager.Instance?.LockCursor();

        isSleeping = false;
        officeReturnRoutine = null;

        Debug.Log("[OFFICE] Work day ended. Returned home.");
    }

    private IEnumerator FadeSleepOverlay(float startAlpha, float endAlpha, float duration)
    {
        if (duration <= 0f)
        {
            sleepFadeGroup.alpha = endAlpha;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float fadePercent = Mathf.Clamp01(elapsedTime / duration);
            sleepFadeGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, fadePercent);
            yield return null;
        }

        sleepFadeGroup.alpha = endAlpha;
    }

    // ---------- Helpers ----------

    private void HidePrompt()
    {
        if (interactionText != null)
            interactionText.gameObject.SetActive(false);
    }
}
