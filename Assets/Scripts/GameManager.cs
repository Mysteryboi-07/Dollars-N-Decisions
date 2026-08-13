using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using StarterAssets;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Persistence")]
    [SerializeField] private bool keepAcrossScenes = true;

    [System.Serializable]
    public class StatAction
    {
        public string actionName;
        public float happinessChange;
        public float hungerChange;
        public bool advancesTime = true;
    }

    [System.Serializable]
    public class BagItem
    {
        public string itemName;
        public float hungerRestore;
        public int quantity;
    }

    [Header("Stats UI")]
    [SerializeField] private GameObject statsUIGroup;

    [Header("Happiness")]
    [SerializeField] private RectTransform happinessFillBar;
    [SerializeField] private float startingHappiness = 100f;
    [FormerlySerializedAs("happinessMaxHeight")]
    [SerializeField] private float happinessEmptyBottomOffset;
    [SerializeField] private GameObject happiness100Icon;
    [SerializeField] private GameObject happiness80Icon;
    [SerializeField] private GameObject happiness60Icon;
    [SerializeField] private GameObject happiness40Icon;
    [SerializeField] private GameObject happiness20Icon;
    [SerializeField] private GameObject happiness0Icon;

    [Header("Hunger")]
    [SerializeField] private RectTransform hungerFillBar;
    [SerializeField] private float startingHunger = 100f;
    [FormerlySerializedAs("hungerMaxHeight")]
    [SerializeField] private float hungerEmptyBottomOffset;

    [Header("Actions")]
    [SerializeField] private StatAction[] statActions;
    [SerializeField] private string sleepActionName = "Sleep";

    [Header("Time")]
    [FormerlySerializedAs("timeUIGroup")]
    [SerializeField] private GameObject infoUIGroup;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private GameObject clock12Image;
    [SerializeField] private GameObject clock3Image;
    [SerializeField] private GameObject clock6Image;
    [SerializeField] private GameObject clock9Image;
    [SerializeField] private GameObject morningIcon;
    [SerializeField] private GameObject noonIcon;
    [SerializeField] private GameObject eveningIcon;
    [SerializeField] private int startingDayPhase = 5;

    [Header("Progress")]
    [SerializeField] private int startingMoney = 20;
    [SerializeField] private int startingDay;

    [Header("Home Work Multiplier")]
    [SerializeField] private float minimumHomeWorkMultiplier = 0.5f;
    [SerializeField] private float maximumHomeWorkMultiplier = 2f;

    [Header("Events UI")]
    [SerializeField] private GameObject houseEventObject;
    [SerializeField] private TMP_Text houseMultiplierText;

    [Header("Bag")]
    [SerializeField] private List<BagItem> bagItems = new List<BagItem>();

    [Header("Ending")]
    [SerializeField] private int rentDueDay = 30;
    [SerializeField] private float rentAmount = 10000f;
    [SerializeField] private int debtGraceDays = 2;
    [SerializeField] private bool loadEndingSceneOnEnding = true;
    [SerializeField] private string endingSceneName = "EndingScene";

    private GameSceneUI sceneUI;
    private float happiness;
    private float hunger;
    private float money;
    private int day;
    private int currentDayPhase;
    private int officeClockInPhase = -1;
    private int currentOfficeSessionId;
    private int cafeteriaSnackPurchaseSession = -1;
    private int cafeteriaMealPurchaseSession = -1;
    private int cafeteriaDrinkPurchaseSession = -1;
    private float cafeteriaCarriedHungerRestore;
    private int cafeteriaCarriedFoodCount;
    private int debtStartedDay = -1;
    private bool endingTriggered;
    private bool endingWasWin;
    private string endingMessage;
    private bool clockedOutOfOfficeToday;
    private float houseUpgradeProgress;
    private float endingExcessMoney;
    private readonly int[] phaseHours = { 6, 9, 12, 15, 18, 21, 0 };

    public float Happiness => happiness;
    public float Hunger => hunger;
    public float Money => money;
    public int Day => day;
    public int CurrentDayPhase => currentDayPhase;
    public int CurrentHour => phaseHours[currentDayPhase];
    public int CurrentOfficeSessionId => currentOfficeSessionId;
    public bool HasCafeteriaFood => cafeteriaCarriedFoodCount > 0 && cafeteriaCarriedHungerRestore > 0f;
    public bool IsEndingTriggered => endingTriggered;
    public bool EndingWasWin => endingWasWin;
    public string EndingMessage => endingMessage;
    public float EndingExcessMoney => endingExcessMoney;
    public bool HasClockedOutOfOfficeToday => clockedOutOfOfficeToday;
    public bool CanEnterOffice => currentDayPhase > 0 && currentDayPhase < 5 && !clockedOutOfOfficeToday;
    public string OfficeEntryBlockedMessage => currentDayPhase <= 0 || currentDayPhase >= 5
        ? "Office is closed"
        : "Already clocked out for today";
    public bool ShouldReturnHomeFromOffice => currentDayPhase >= 5;
    public float HomeWorkRewardMultiplier => Mathf.Lerp(
        minimumHomeWorkMultiplier,
        maximumHomeWorkMultiplier,
        Mathf.Clamp01(houseUpgradeProgress));
    public IReadOnlyList<BagItem> BagItems => bagItems;

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

    private void Start()
    {
        if (happinessFillBar != null && happinessEmptyBottomOffset <= 0f)
            happinessEmptyBottomOffset = happinessFillBar.rect.height;

        if (hungerFillBar != null && hungerEmptyBottomOffset <= 0f)
            hungerEmptyBottomOffset = hungerFillBar.rect.height;

        SetHappiness(startingHappiness);
        SetHunger(startingHunger);
        SetMoney(startingMoney);
        SetDay(startingDay);
        SetDayPhase(startingDayPhase);
        SetStatsUIVisible(false);
        SetHouseEventVisible(false);
        UpdateHouseMultiplierText();
    }

    public void RegisterSceneUI(GameSceneUI newSceneUI)
    {
        sceneUI = newSceneUI;
        RefreshSceneUI();
    }

    public void UnregisterSceneUI(GameSceneUI oldSceneUI)
    {
        if (sceneUI == oldSceneUI)
            sceneUI = null;
    }

    public void ShowStatsUI()
    {
        SetStatsUIVisible(true);
    }

    public void HideStatsUI()
    {
        SetStatsUIVisible(false);
    }

    public void SetStatsUIVisible(bool isVisible)
    {
        GameObject activeStatsUIGroup = sceneUI != null ? sceneUI.StatsUIGroup : statsUIGroup;
        GameObject activeInfoUIGroup = sceneUI != null ? sceneUI.InfoUIGroup : infoUIGroup;

        if (activeStatsUIGroup != null)
            activeStatsUIGroup.SetActive(isVisible);

        if (activeInfoUIGroup != null)
            activeInfoUIGroup.SetActive(isVisible);
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetPlayerCursorInput(false);
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetPlayerCursorInput(true);
    }

    public void FreezePlayerForUI()
    {
        UnlockCursor();
        SetPlayerMovementInput(false);
    }

    public void UnfreezePlayerFromUI()
    {
        SetPlayerMovementInput(true);
        LockCursor();
    }

    public void ApplyAction(int actionIndex)
    {
        if (statActions == null ||
            actionIndex < 0 ||
            actionIndex >= statActions.Length)
        {
            Debug.LogWarning($"[GAME] No stat action found at index {actionIndex}.");
            return;
        }

        ApplyAction(statActions[actionIndex]);
    }

    public void ApplyActionByName(string actionName)
    {
        if (statActions == null) return;

        foreach (StatAction statAction in statActions)
        {
            if (statAction != null && statAction.actionName == actionName)
            {
                ApplyAction(statAction);
                return;
            }
        }

        Debug.LogWarning($"[GAME] No stat action named {actionName}.");
    }

    public void ApplyActionStatsByName(string actionName)
    {
        if (statActions == null) return;

        foreach (StatAction statAction in statActions)
        {
            if (statAction != null && statAction.actionName == actionName)
            {
                ApplyActionStats(statAction);
                return;
            }
        }

        Debug.LogWarning($"[GAME] No stat action named {actionName}.");
    }

    public void ChangeHappiness(float amount)
    {
        SetHappiness(happiness + amount);
    }

    public void ChangeHunger(float amount)
    {
        SetHunger(hunger + amount);
    }

    public void SetHappiness(float value)
    {
        happiness = Mathf.Clamp(value, 0f, 100f);
        UpdateBarBottomOffset(
            sceneUI != null ? sceneUI.HappinessFillBar : happinessFillBar,
            sceneUI != null ? sceneUI.HappinessEmptyBottomOffset : happinessEmptyBottomOffset,
            happiness);
        UpdateHappinessIcon();
    }

    public void SetHunger(float value)
    {
        hunger = Mathf.Clamp(value, 0f, 100f);
        UpdateBarBottomOffset(
            sceneUI != null ? sceneUI.HungerFillBar : hungerFillBar,
            sceneUI != null ? sceneUI.HungerEmptyBottomOffset : hungerEmptyBottomOffset,
            hunger);
    }

    public void WakeUp()
    {
        ApplyActionStatsByName(sleepActionName);
        AdvanceDay();
        SetDayPhase(1);
        Debug.Log("[GAME] Woke up at 0900.");
    }

    public void WakeUpAtPhase(int phaseIndex)
    {
        ApplyActionStatsByName(sleepActionName);
        AdvanceDay();
        SetDayPhase(phaseIndex);
        Debug.Log($"[GAME] Woke up at {CurrentHour:00}00.");
    }

    public void ChangeMoney(float amount)
    {
        SetMoney(money + amount);
    }

    public int ApplyHomeWorkRewardMultiplier(int baseReward)
    {
        return Mathf.RoundToInt(baseReward * HomeWorkRewardMultiplier);
    }

    public void SetHouseUpgradeProgress(float progress)
    {
        houseUpgradeProgress = Mathf.Clamp01(progress);
        UpdateHouseMultiplierText();
        Debug.Log($"[HOME] Work reward multiplier is now {HomeWorkRewardMultiplier:0.##}x.");
    }

    public void SetHouseEventVisible(bool isVisible)
    {
        GameObject activeHouseEventObject = sceneUI != null ? sceneUI.HouseEventObject : houseEventObject;

        if (activeHouseEventObject != null)
            activeHouseEventObject.SetActive(isVisible);

        if (isVisible)
            UpdateHouseMultiplierText();
    }

    public bool TrySpendMoney(float amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("[GAME] TrySpendMoney received a negative amount.");
            return false;
        }

        if (money < amount)
        {
            Debug.Log($"[GAME] Not enough money. Need {FormatMoney(amount)}, but only have {FormatMoney(money)}.");
            return false;
        }

        ChangeMoney(-amount);
        return true;
    }

    public void AddBagItem(string itemName, int quantity, float hungerRestore)
    {
        if (string.IsNullOrWhiteSpace(itemName) || quantity <= 0) return;

        BagItem existingItem = bagItems.Find(item => item.itemName == itemName);

        if (existingItem != null)
        {
            existingItem.quantity += quantity;
            existingItem.hungerRestore = hungerRestore;
            return;
        }

        bagItems.Add(new BagItem
        {
            itemName = itemName,
            hungerRestore = hungerRestore,
            quantity = quantity
        });
    }

    public void UseBagItem(string itemName)
    {
        BagItem existingItem = bagItems.Find(item => item.itemName == itemName);

        if (existingItem == null || existingItem.quantity <= 0)
        {
            Debug.Log($"[BAG] No {itemName} available.");
            return;
        }

        existingItem.quantity--;
        ChangeHunger(existingItem.hungerRestore);

        if (existingItem.quantity <= 0)
            bagItems.Remove(existingItem);

        Debug.Log($"[BAG] Used {itemName}. Hunger +{existingItem.hungerRestore}.");
    }

    public int GetBagItemQuantity(string itemName)
    {
        BagItem existingItem = bagItems.Find(item => item.itemName == itemName);
        return existingItem != null ? existingItem.quantity : 0;
    }

    public void DebugLogBag()
    {
        if (bagItems.Count == 0)
        {
            Debug.Log("[BAG] Bag is empty.");
            return;
        }

        foreach (BagItem bagItem in bagItems)
            Debug.Log($"[BAG] {bagItem.itemName} x{bagItem.quantity} - Hunger +{bagItem.hungerRestore}");
    }

    public void SetMoney(float value)
    {
        money = value;
        UpdateMoneyText();
        UpdateDebtTracking();
    }

    public void AdvanceDay()
    {
        CheckDebtDeadlineAtEndOfDay();

        if (endingTriggered) return;

        clockedOutOfOfficeToday = false;
        officeClockInPhase = -1;
        SetDay(day + 1);
        CheckRentDeadline();
    }

    public void ClockInOffice()
    {
        if (officeClockInPhase < 0)
        {
            currentOfficeSessionId++;
            ResetCafeteriaSession();
        }

        officeClockInPhase = currentDayPhase;
        Debug.Log($"[OFFICE] Clocked in at {CurrentHour:00}00.");
    }

    public void ClockOutOffice()
    {
        bool spentOfficePhase = officeClockInPhase >= 0 && currentDayPhase > officeClockInPhase;

        if (spentOfficePhase)
        {
            clockedOutOfOfficeToday = true;
            Debug.Log("[OFFICE] Clocked out for today.");
        }
        else
        {
            Debug.Log("[OFFICE] Left office without clocking out.");
        }

        officeClockInPhase = -1;
    }

    public bool CanBuyFromCafeteriaStore(CafeteriaStoreType storeType)
    {
        return storeType != CafeteriaStoreType.None &&
               GetCafeteriaStorePurchaseSession(storeType) != currentOfficeSessionId;
    }

    public void MarkCafeteriaStoreBought(CafeteriaStoreType storeType)
    {
        switch (storeType)
        {
            case CafeteriaStoreType.Snacks:
                cafeteriaSnackPurchaseSession = currentOfficeSessionId;
                break;

            case CafeteriaStoreType.Meals:
                cafeteriaMealPurchaseSession = currentOfficeSessionId;
                break;

            case CafeteriaStoreType.Drinks:
                cafeteriaDrinkPurchaseSession = currentOfficeSessionId;
                break;
        }
    }

    public void AddCafeteriaFood(float hungerRestore)
    {
        if (hungerRestore <= 0f) return;

        cafeteriaCarriedFoodCount++;
        cafeteriaCarriedHungerRestore += hungerRestore;
    }

    public void ConsumeCafeteriaFood()
    {
        if (!HasCafeteriaFood)
        {
            Debug.Log("[CAFETERIA] No cafeteria food to eat.");
            return;
        }

        ChangeHunger(cafeteriaCarriedHungerRestore);
        Debug.Log($"[CAFETERIA] Ate {cafeteriaCarriedFoodCount} item(s). Hunger +{cafeteriaCarriedHungerRestore}.");
        ClearCafeteriaFood();
    }

    public void IncreaseRentAmount(float amount)
    {
        if (amount <= 0f) return;

        rentAmount += amount;
        Debug.Log($"[GAME] Rent increased by {FormatMoney(amount)}. New rent: {FormatMoney(rentAmount)}.");
    }

    public void ResetRunProgress()
    {
        endingTriggered = false;
        endingWasWin = false;
        endingMessage = string.Empty;
        endingExcessMoney = 0f;
        debtStartedDay = -1;
        clockedOutOfOfficeToday = false;
        officeClockInPhase = -1;
        currentOfficeSessionId = 0;
        houseUpgradeProgress = 0f;
        bagItems.Clear();
        ResetCafeteriaSession();

        SetHappiness(startingHappiness);
        SetHunger(startingHunger);
        SetMoney(startingMoney);
        SetDay(startingDay);
        SetDayPhase(startingDayPhase);
        SetHouseEventVisible(false);
        HideStatsUI();
        HouseSceneStartupManager.ResetTutorialForNewRun();

        Debug.Log("[GAME] Run progress reset.");
    }

    public void SetDay(int value)
    {
        day = Mathf.Max(0, value);
        UpdateDayText();
        UpdateDebtTracking();
    }

    public void AdvanceTimePhase()
    {
        SetDayPhase(currentDayPhase + 1);
    }

    public void SetDayPhase(int phaseIndex)
    {
        currentDayPhase = Mathf.Clamp(phaseIndex, 0, phaseHours.Length - 1);
        UpdateClockImage();
        UpdateTimeOfDayIcon();
        UpdateTimeText();
    }

    private void ApplyAction(StatAction statAction)
    {
        if (endingTriggered) return;

        ApplyActionStats(statAction);

        if (statAction.advancesTime)
            AdvanceTimePhase();
    }

    private void ApplyActionStats(StatAction statAction)
    {
        ChangeHappiness(statAction.happinessChange);
        ChangeHunger(statAction.hungerChange);
        Debug.Log($"[GAME] Applied {statAction.actionName}: happiness {happiness}/100, hunger {hunger}/100");
    }

    private void UpdateBarBottomOffset(RectTransform fillBar, float emptyBottomOffset, float value)
    {
        if (fillBar == null || emptyBottomOffset <= 0f) return;

        float fillPercent = value / 100f;
        Vector2 offsetMin = fillBar.offsetMin;
        offsetMin.y = emptyBottomOffset * (1f - fillPercent);
        fillBar.offsetMin = offsetMin;
    }

    private void UpdateClockImage()
    {
        switch (CurrentHour)
        {
            case 0:
            case 12:
                SetClockImagesActive(true, false, false, false);
                break;

            case 3:
            case 15:
                SetClockImagesActive(false, true, false, false);
                break;

            case 6:
            case 18:
                SetClockImagesActive(false, false, true, false);
                break;

            case 9:
            case 21:
                SetClockImagesActive(false, false, false, true);
                break;
        }
    }

    private void SetClockImagesActive(bool show12, bool show3, bool show6, bool show9)
    {
        GameObject activeClock12Image = sceneUI != null ? sceneUI.Clock12Image : clock12Image;
        GameObject activeClock3Image = sceneUI != null ? sceneUI.Clock3Image : clock3Image;
        GameObject activeClock6Image = sceneUI != null ? sceneUI.Clock6Image : clock6Image;
        GameObject activeClock9Image = sceneUI != null ? sceneUI.Clock9Image : clock9Image;

        if (activeClock12Image != null)
            activeClock12Image.SetActive(show12);

        if (activeClock3Image != null)
            activeClock3Image.SetActive(show3);

        if (activeClock6Image != null)
            activeClock6Image.SetActive(show6);

        if (activeClock9Image != null)
            activeClock9Image.SetActive(show9);
    }

    private void UpdateTimeOfDayIcon()
    {
        if (currentDayPhase <= 1)
        {
            SetTimeOfDayIconsActive(true, false, false);
            return;
        }

        if (currentDayPhase <= 4)
        {
            SetTimeOfDayIconsActive(false, true, false);
            return;
        }

        SetTimeOfDayIconsActive(false, false, true);
    }

    private void SetTimeOfDayIconsActive(bool showMorning, bool showNoon, bool showEvening)
    {
        GameObject activeMorningIcon = sceneUI != null ? sceneUI.MorningIcon : morningIcon;
        GameObject activeNoonIcon = sceneUI != null ? sceneUI.NoonIcon : noonIcon;
        GameObject activeEveningIcon = sceneUI != null ? sceneUI.EveningIcon : eveningIcon;

        if (activeMorningIcon != null)
            activeMorningIcon.SetActive(showMorning);

        if (activeNoonIcon != null)
            activeNoonIcon.SetActive(showNoon);

        if (activeEveningIcon != null)
            activeEveningIcon.SetActive(showEvening);
    }

    private void UpdateHappinessIcon()
    {
        if (happiness >= 100f)
        {
            SetHappinessIconsActive(true, false, false, false, false, false);
            return;
        }

        if (happiness >= 80f)
        {
            SetHappinessIconsActive(false, true, false, false, false, false);
            return;
        }

        if (happiness >= 60f)
        {
            SetHappinessIconsActive(false, false, true, false, false, false);
            return;
        }

        if (happiness >= 40f)
        {
            SetHappinessIconsActive(false, false, false, true, false, false);
            return;
        }

        if (happiness >= 20f)
        {
            SetHappinessIconsActive(false, false, false, false, true, false);
            return;
        }

        SetHappinessIconsActive(false, false, false, false, false, true);
    }

    private void SetHappinessIconsActive(
        bool show100,
        bool show80,
        bool show60,
        bool show40,
        bool show20,
        bool show0)
    {
        GameObject activeHappiness100Icon = sceneUI != null ? sceneUI.Happiness100Icon : happiness100Icon;
        GameObject activeHappiness80Icon = sceneUI != null ? sceneUI.Happiness80Icon : happiness80Icon;
        GameObject activeHappiness60Icon = sceneUI != null ? sceneUI.Happiness60Icon : happiness60Icon;
        GameObject activeHappiness40Icon = sceneUI != null ? sceneUI.Happiness40Icon : happiness40Icon;
        GameObject activeHappiness20Icon = sceneUI != null ? sceneUI.Happiness20Icon : happiness20Icon;
        GameObject activeHappiness0Icon = sceneUI != null ? sceneUI.Happiness0Icon : happiness0Icon;

        if (activeHappiness100Icon != null)
            activeHappiness100Icon.SetActive(show100);

        if (activeHappiness80Icon != null)
            activeHappiness80Icon.SetActive(show80);

        if (activeHappiness60Icon != null)
            activeHappiness60Icon.SetActive(show60);

        if (activeHappiness40Icon != null)
            activeHappiness40Icon.SetActive(show40);

        if (activeHappiness20Icon != null)
            activeHappiness20Icon.SetActive(show20);

        if (activeHappiness0Icon != null)
            activeHappiness0Icon.SetActive(show0);
    }

    private void UpdateMoneyText()
    {
        TMP_Text activeMoneyText = sceneUI != null ? sceneUI.MoneyText : moneyText;

        if (activeMoneyText != null)
            activeMoneyText.text = FormatMoney(money);
    }

    private void UpdateHouseMultiplierText()
    {
        TMP_Text activeHouseMultiplierText = sceneUI != null ? sceneUI.HouseMultiplierText : houseMultiplierText;

        if (activeHouseMultiplierText != null)
            activeHouseMultiplierText.text = $"{HomeWorkRewardMultiplier:0.##}x";
    }

    private void UpdateDayText()
    {
        TMP_Text activeDayText = sceneUI != null ? sceneUI.DayText : dayText;

        if (activeDayText != null)
            activeDayText.text = $"Day {day}";
    }

    private void UpdateTimeText()
    {
        TMP_Text activeTimeText = sceneUI != null ? sceneUI.TimeText : timeText;

        if (activeTimeText != null)
            activeTimeText.text = $"{CurrentHour:00}00";
    }

    private void RefreshSceneUI()
    {
        RefreshBarBottomOffset(sceneUI != null ? sceneUI.HappinessFillBar : happinessFillBar,
            sceneUI != null ? sceneUI.HappinessEmptyBottomOffset : happinessEmptyBottomOffset,
            happiness);
        RefreshBarBottomOffset(sceneUI != null ? sceneUI.HungerFillBar : hungerFillBar,
            sceneUI != null ? sceneUI.HungerEmptyBottomOffset : hungerEmptyBottomOffset,
            hunger);
        UpdateHappinessIcon();
        UpdateClockImage();
        UpdateTimeOfDayIcon();
        UpdateMoneyText();
        UpdateDayText();
        UpdateTimeText();
        UpdateHouseMultiplierText();
    }

    private void RefreshBarBottomOffset(RectTransform fillBar, float emptyBottomOffset, float value)
    {
        UpdateBarBottomOffset(fillBar, emptyBottomOffset, value);
    }

    private void UpdateDebtTracking()
    {
        if (endingTriggered) return;

        if (money < 0f)
        {
            if (debtStartedDay < 0)
            {
                debtStartedDay = day;
                Debug.Log($"[ENDING] Debt started on Day {debtStartedDay}.");
            }

            return;
        }

        if (debtStartedDay >= 0)
            Debug.Log("[ENDING] Debt cleared.");

        debtStartedDay = -1;
    }

    private void CheckDebtDeadlineAtEndOfDay()
    {
        if (endingTriggered || debtStartedDay < 0 || money >= 0f) return;

        if (day < debtStartedDay + debtGraceDays) return;

        TriggerEnding(
            "Game Over: You stayed in debt for too long.",
            false);
    }

    private void CheckRentDeadline()
    {
        if (endingTriggered || day < rentDueDay) return;

        if (money >= rentAmount)
        {
            endingExcessMoney = money - rentAmount;
            TriggerEnding(
                $"Congratulations! Rent paid. Excess money: {FormatMoney(endingExcessMoney)}.",
                true);
            return;
        }

        TriggerEnding(
            $"Game Over: Rent was due on Day {rentDueDay}. Needed {FormatMoney(rentAmount)}.",
            false);
    }

    private void TriggerEnding(string message, bool isWin)
    {
        if (endingTriggered) return;

        endingTriggered = true;
        endingWasWin = isWin;
        endingMessage = message;

        HideStatsUI();
        UnlockCursor();
        SetPlayerMovementInput(false);

        Debug.Log($"[ENDING] {message}");

        if (!loadEndingSceneOnEnding || string.IsNullOrWhiteSpace(endingSceneName)) return;

        if (SceneTravelManager.Instance != null)
            SceneTravelManager.Instance.LoadScene(endingSceneName);
        else
            SceneManager.LoadScene(endingSceneName);
    }

    private int GetCafeteriaStorePurchaseSession(CafeteriaStoreType storeType)
    {
        switch (storeType)
        {
            case CafeteriaStoreType.Snacks:
                return cafeteriaSnackPurchaseSession;

            case CafeteriaStoreType.Meals:
                return cafeteriaMealPurchaseSession;

            case CafeteriaStoreType.Drinks:
                return cafeteriaDrinkPurchaseSession;

            default:
                return -1;
        }
    }

    private void ResetCafeteriaSession()
    {
        cafeteriaSnackPurchaseSession = -1;
        cafeteriaMealPurchaseSession = -1;
        cafeteriaDrinkPurchaseSession = -1;
        ClearCafeteriaFood();
    }

    private void ClearCafeteriaFood()
    {
        cafeteriaCarriedFoodCount = 0;
        cafeteriaCarriedHungerRestore = 0f;
    }

    private string FormatMoney(float value)
    {
        return Mathf.Approximately(value % 1f, 0f) ? $"${value:0}" : $"${value:0.00}";
    }

    private void SetPlayerCursorInput(bool isEnabled)
    {
        StarterAssetsInputs[] playerInputs = Object.FindObjectsByType<StarterAssetsInputs>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (StarterAssetsInputs playerInput in playerInputs)
        {
            playerInput.cursorLocked = isEnabled;
            playerInput.cursorInputForLook = isEnabled;
        }
    }

    private void SetPlayerMovementInput(bool isEnabled)
    {
        StarterAssetsInputs[] playerInputs = Object.FindObjectsByType<StarterAssetsInputs>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (StarterAssetsInputs playerInput in playerInputs)
        {
            if (!isEnabled)
            {
                playerInput.move = Vector2.zero;
                playerInput.look = Vector2.zero;
                playerInput.sprint = false;
            }

            playerInput.enabled = isEnabled;
        }
    }

}
