using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using StarterAssets;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

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

    [Header("Bag")]
    [SerializeField] private List<BagItem> bagItems = new List<BagItem>();

    private float happiness;
    private float hunger;
    private float money;
    private int day;
    private int currentDayPhase;
    private readonly int[] phaseHours = { 6, 9, 12, 15, 18, 21, 0 };

    public float Happiness => happiness;
    public float Hunger => hunger;
    public float Money => money;
    public int Day => day;
    public int CurrentDayPhase => currentDayPhase;
    public int CurrentHour => phaseHours[currentDayPhase];
    public bool CanEnterOffice => currentDayPhase > 0 && currentDayPhase < 5;
    public bool ShouldReturnHomeFromOffice => currentDayPhase >= 5;
    public IReadOnlyList<BagItem> BagItems => bagItems;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
        if (statsUIGroup != null)
            statsUIGroup.SetActive(isVisible);

        if (infoUIGroup != null)
            infoUIGroup.SetActive(isVisible);
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
        UpdateBarBottomOffset(happinessFillBar, happinessEmptyBottomOffset, happiness);
        UpdateHappinessIcon();
    }

    public void SetHunger(float value)
    {
        hunger = Mathf.Clamp(value, 0f, 100f);
        UpdateBarBottomOffset(hungerFillBar, hungerEmptyBottomOffset, hunger);
    }

    public void WakeUp()
    {
        ApplyActionStatsByName(sleepActionName);
        AdvanceDay();
        SetDayPhase(0);
        Debug.Log("[GAME] Woke up at 0600.");
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
    }

    public void AdvanceDay()
    {
        SetDay(day + 1);
    }

    public void SetDay(int value)
    {
        day = Mathf.Max(0, value);
        UpdateDayText();
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
    }

    private void ApplyAction(StatAction statAction)
    {
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
        SetClockImagesActive(false, false, false, false);

        switch (CurrentHour)
        {
            case 0:
            case 12:
                if (clock12Image != null)
                    clock12Image.SetActive(true);
                break;

            case 3:
            case 15:
                if (clock3Image != null)
                    clock3Image.SetActive(true);
                break;

            case 6:
            case 18:
                if (clock6Image != null)
                    clock6Image.SetActive(true);
                break;

            case 9:
            case 21:
                if (clock9Image != null)
                    clock9Image.SetActive(true);
                break;
        }
    }

    private void SetClockImagesActive(bool show12, bool show3, bool show6, bool show9)
    {
        if (clock12Image != null)
            clock12Image.SetActive(show12);

        if (clock3Image != null)
            clock3Image.SetActive(show3);

        if (clock6Image != null)
            clock6Image.SetActive(show6);

        if (clock9Image != null)
            clock9Image.SetActive(show9);
    }

    private void UpdateTimeOfDayIcon()
    {
        SetTimeOfDayIconsActive(false, false, false);

        if (currentDayPhase <= 1)
        {
            if (morningIcon != null)
                morningIcon.SetActive(true);
            return;
        }

        if (currentDayPhase <= 4)
        {
            if (noonIcon != null)
                noonIcon.SetActive(true);
            return;
        }

        if (eveningIcon != null)
            eveningIcon.SetActive(true);
    }

    private void SetTimeOfDayIconsActive(bool showMorning, bool showNoon, bool showEvening)
    {
        if (morningIcon != null)
            morningIcon.SetActive(showMorning);

        if (noonIcon != null)
            noonIcon.SetActive(showNoon);

        if (eveningIcon != null)
            eveningIcon.SetActive(showEvening);
    }

    private void UpdateHappinessIcon()
    {
        SetHappinessIconsActive(false, false, false, false, false, false);

        if (happiness >= 100f)
        {
            if (happiness100Icon != null)
                happiness100Icon.SetActive(true);
            return;
        }

        if (happiness >= 80f)
        {
            if (happiness80Icon != null)
                happiness80Icon.SetActive(true);
            return;
        }

        if (happiness >= 60f)
        {
            if (happiness60Icon != null)
                happiness60Icon.SetActive(true);
            return;
        }

        if (happiness >= 40f)
        {
            if (happiness40Icon != null)
                happiness40Icon.SetActive(true);
            return;
        }

        if (happiness >= 20f)
        {
            if (happiness20Icon != null)
                happiness20Icon.SetActive(true);
            return;
        }

        if (happiness0Icon != null)
            happiness0Icon.SetActive(true);
    }

    private void SetHappinessIconsActive(
        bool show100,
        bool show80,
        bool show60,
        bool show40,
        bool show20,
        bool show0)
    {
        if (happiness100Icon != null)
            happiness100Icon.SetActive(show100);

        if (happiness80Icon != null)
            happiness80Icon.SetActive(show80);

        if (happiness60Icon != null)
            happiness60Icon.SetActive(show60);

        if (happiness40Icon != null)
            happiness40Icon.SetActive(show40);

        if (happiness20Icon != null)
            happiness20Icon.SetActive(show20);

        if (happiness0Icon != null)
            happiness0Icon.SetActive(show0);
    }

    private void UpdateMoneyText()
    {
        if (moneyText != null)
            moneyText.text = FormatMoney(money);
    }

    private void UpdateDayText()
    {
        if (dayText != null)
            dayText.text = $"Day {day}";
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
