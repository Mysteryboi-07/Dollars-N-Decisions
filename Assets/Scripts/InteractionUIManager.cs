using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class InteractionUIManager : MonoBehaviour
{
    public static InteractionUIManager Instance { get; private set; }

    [Header("Prompt UI")]
    [SerializeField] private TMP_Text interactionText;

    [Header("Walker")]
    [SerializeField] private GameObject walkerObject;

    [Header("Laptop Interaction")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject laptopCamera;
    [SerializeField] private GameObject laptopScreen;

    [Header("Monitor Interaction")]
    [SerializeField] private GameObject workerObject;
    [SerializeField] private GameObject monitorCamera;
    [SerializeField] private GameObject monitorScreen;

    [Header("Shop Interaction")]
    [FormerlySerializedAs("ShopperObject")]
    [SerializeField] private GameObject shopperObject;

    [Header("Sleep Interaction")]
    [SerializeField] private CanvasGroup sleepFadeGroup;
    [SerializeField] private float sleepFadeInDuration = 1f;
    [SerializeField] private float sleepHoldDuration = 2f;
    [SerializeField] private float sleepFadeOutDuration = 1f;

    private InteractableTrigger currentInteractable;
    private InteractionType openInteractionType = InteractionType.None;
    private Coroutine sleepRoutine;
    private Coroutine officeReturnRoutine;
    private Coroutine homeWorkSleepRoutine;
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

        if (walkerObject != null)
            walkerObject.SetActive(false);

        if (shopperObject != null)
            shopperObject.SetActive(false);

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

            case InteractionType.ShopShelf:
                OpenShopShelf();
                break;

            case InteractionType.Cashier:
                UseCashier();
                break;

            case InteractionType.MarketDoor:
                UseMarketDoor();
                break;

            case InteractionType.MiniFridge:
                OpenMiniFridge();
                break;

            default:
                Debug.Log("[INTERACTION] No interaction assigned.");
                break;
        }
    }

    // ---------- Shop Interaction ----------

    private void OpenShopShelf()
    {
        if (currentInteractable == null) return;

        ConvenienceShopManager.Instance?.AddShelfItemToCart(currentInteractable);
        Debug.Log($"[SHOP] Added shelf item from {currentInteractable.ShopCategory} shelf.");
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

    public void CloseMiniFridge()
    {
        openInteractionType = InteractionType.None;

        MiniFridgeManager.Instance?.CloseFridge();
        GameManager.Instance?.ShowStatsUI();
        GameManager.Instance?.UnfreezePlayerFromUI();

        if (currentInteractable != null &&
            interactionText != null)
        {
            interactionText.text = currentInteractable.PromptMessage;
            interactionText.gameObject.SetActive(true);
        }

        Debug.Log("[FRIDGE] Closed mini fridge.");
    }

    private void UseCashier()
    {
        ConvenienceShopManager.Instance?.BeginCheckout();
        Debug.Log("[SHOP] Used cashier.");
    }

    private void UseMarketDoor()
    {
        ToggleMarketDoor();
    }

    // ---------- Door Interaction ----------

    private void EnterHouseDoor()
    {
        HidePrompt();
        currentInteractable = null;

        if (IsActive(playerObject))
        {
            SetActivePlayer(playerObject, false);
            SetActivePlayer(walkerObject, true);
            Debug.Log("[DOOR] Left room. Switched from player to walker.");
            return;
        }

        SetActivePlayer(walkerObject, false);
        SetActivePlayer(playerObject, true);
        Debug.Log("[DOOR] Entered room. Switched from walker to player.");
    }

    private void EnterOfficeDoor()
    {
        if (GameManager.Instance != null && !GameManager.Instance.CanEnterOffice)
        {
            if (IsActive(workerObject))
            {
                HidePrompt();
                currentInteractable = null;

                ExitOfficeToWalker();
                return;
            }

            if (interactionText != null)
            {
                interactionText.text = "Office opens from 0900 to 1800";
                interactionText.gameObject.SetActive(true);
            }

            Debug.Log("[DOOR] Office is closed. Come back from 0900 to 1800.");
            return;
        }

        HidePrompt();
        currentInteractable = null;

        if (IsActive(workerObject))
        {
            ExitOfficeToWalker();
            return;
        }

        SetActivePlayer(walkerObject, false);
        SetActivePlayer(workerObject, true);

        Debug.Log("[DOOR] Entered office. Switched from walker to worker.");
    }

    private void ToggleMarketDoor()
    {
        HidePrompt();
        currentInteractable = null;

        if (IsActive(shopperObject))
        {
            SetActivePlayer(shopperObject, false);
            SetActivePlayer(walkerObject, true);
            Debug.Log("[DOOR] Left market. Switched from shopper to walker.");
            return;
        }

        SetActivePlayer(walkerObject, false);
        SetActivePlayer(shopperObject, true);

        Debug.Log("[DOOR] Entered market. Switched from walker to shopper.");
    }

    private void ExitOfficeToWalker()
    {
        SetActivePlayer(workerObject, false);
        SetActivePlayer(walkerObject, true);
        Debug.Log("[DOOR] Left office. Switched from worker to walker.");
    }

    public void ReturnHomeFromOfficeWithFade()
    {
        if (officeReturnRoutine != null) return;

        officeReturnRoutine = StartCoroutine(ReturnHomeFromOfficeRoutine());
    }

    public void SleepAfterLateHomeWorkWithFade()
    {
        if (homeWorkSleepRoutine != null) return;

        homeWorkSleepRoutine = StartCoroutine(SleepAfterLateHomeWorkRoutine());
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
        GameManager.Instance?.UnlockCursor();

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
        GameManager.Instance?.LockCursor();

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
        GameManager.Instance?.UnlockCursor();

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
        GameManager.Instance?.LockCursor();

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

            case InteractionType.MiniFridge:
                CloseMiniFridge();
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
            if (monitorScreen != null)
                monitorScreen.SetActive(false);

            if (monitorCamera != null)
                monitorCamera.SetActive(false);

            openInteractionType = InteractionType.None;
            ExitOfficeToWalker();
            GameManager.Instance?.ShowStatsUI();
            GameManager.Instance?.LockCursor();

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
        ExitOfficeToWalker();

        yield return FadeSleepOverlay(1f, 0f, sleepFadeOutDuration);

        sleepFadeGroup.gameObject.SetActive(false);
        GameManager.Instance?.ShowStatsUI();
        GameManager.Instance?.LockCursor();

        isSleeping = false;
        officeReturnRoutine = null;

        Debug.Log("[OFFICE] Work day ended. Returned to outside world.");
    }

    private IEnumerator SleepAfterLateHomeWorkRoutine()
    {
        isSleeping = true;
        HidePrompt();

        if (sleepFadeGroup == null)
        {
            Debug.LogWarning("[HOME WORK] No fade CanvasGroup assigned.");
            CloseLaptopForLateHomeWork();
            GameManager.Instance?.WakeUpAtPhase(2);
            GameManager.Instance?.ShowStatsUI();
            GameManager.Instance?.LockCursor();
            isSleeping = false;
            homeWorkSleepRoutine = null;
            yield break;
        }

        sleepFadeGroup.gameObject.SetActive(true);

        yield return FadeSleepOverlay(0f, 1f, sleepFadeInDuration);
        yield return new WaitForSeconds(sleepHoldDuration);

        CloseLaptopForLateHomeWork();
        GameManager.Instance?.WakeUpAtPhase(2);

        yield return FadeSleepOverlay(1f, 0f, sleepFadeOutDuration);

        sleepFadeGroup.gameObject.SetActive(false);
        GameManager.Instance?.ShowStatsUI();
        GameManager.Instance?.LockCursor();

        isSleeping = false;
        homeWorkSleepRoutine = null;

        Debug.Log("[HOME WORK] Worked past midnight. Woke up at 1200.");
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

    private void CloseLaptopForLateHomeWork()
    {
        openInteractionType = InteractionType.None;

        if (laptopScreen != null)
            laptopScreen.SetActive(false);

        if (laptopCamera != null)
            laptopCamera.SetActive(false);

        if (playerObject != null)
            playerObject.SetActive(true);
    }

    private bool IsActive(GameObject target)
    {
        return target != null && target.activeSelf;
    }

    private void SetActivePlayer(GameObject target, bool isActive)
    {
        if (target != null)
            target.SetActive(isActive);
    }
}
