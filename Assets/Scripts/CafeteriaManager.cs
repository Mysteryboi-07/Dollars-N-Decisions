using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CafeteriaManager : MonoBehaviour
{
    public static CafeteriaManager Instance { get; private set; }

    [System.Serializable]
    public class CafeteriaFoodItem
    {
        public string itemName;
        public float price;
        public float hungerRestore;
    }

    [Header("Store UI")]
    [SerializeField] private GameObject storePanel;
    [SerializeField] private TMP_Text option1Text;
    [SerializeField] private TMP_Text option2Text;
    [SerializeField] private TMP_Text option3Text;
    [SerializeField] private Button option1Button;
    [SerializeField] private Button option2Button;
    [SerializeField] private Button option3Button;
    [SerializeField] private Button cancelButton;

    [Header("Snack Store")]
    [SerializeField] private CafeteriaFoodItem[] snackItems = new CafeteriaFoodItem[3];

    [Header("Meal Store")]
    [SerializeField] private CafeteriaFoodItem[] mealItems = new CafeteriaFoodItem[3];

    [Header("Drink Store")]
    [SerializeField] private CafeteriaFoodItem[] drinkItems = new CafeteriaFoodItem[3];

    private CafeteriaStoreType openStoreType = CafeteriaStoreType.None;

    public bool IsStoreOpen => openStoreType != CafeteriaStoreType.None;
    public bool HasCarriedFood => GameManager.Instance != null && GameManager.Instance.HasCafeteriaFood;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BindButtons();
    }

    private void Start()
    {
        if (storePanel != null)
            storePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool OpenStore(CafeteriaStoreType storeType)
    {
        if (storeType == CafeteriaStoreType.None)
        {
            Debug.LogWarning("[CAFETERIA] Store type is not assigned.");
            return false;
        }

        if (!CanBuyFromStore(storeType))
        {
            Debug.Log($"[CAFETERIA] Already bought from {storeType} during this clock-in.");
            return false;
        }

        openStoreType = storeType;

        RefreshOptionTexts();

        if (storePanel != null)
            storePanel.SetActive(true);

        GameManager.Instance?.HideStatsUI();
        GameManager.Instance?.FreezePlayerForUI();
        Debug.Log($"[CAFETERIA] Opened {openStoreType} store.");
        return true;
    }

    public bool CanBuyFromStore(CafeteriaStoreType storeType)
    {
        return GameManager.Instance != null && GameManager.Instance.CanBuyFromCafeteriaStore(storeType);
    }

    public void BuyOption1()
    {
        BuyOption(0);
    }

    public void BuyOption2()
    {
        BuyOption(1);
    }

    public void BuyOption3()
    {
        BuyOption(2);
    }

    public void CloseStore()
    {
        if (storePanel != null)
            storePanel.SetActive(false);

        openStoreType = CafeteriaStoreType.None;
        GameManager.Instance?.ShowStatsUI();
        GameManager.Instance?.UnfreezePlayerFromUI();
        InteractionUIManager.Instance?.FinishCafeteriaStoreInteraction(true);
        Debug.Log("[CAFETERIA] Closed store.");
    }

    public void ConsumeCarriedFood()
    {
        if (GameManager.Instance == null || !GameManager.Instance.HasCafeteriaFood)
        {
            Debug.Log("[CAFETERIA] No cafeteria food to eat.");
            return;
        }

        GameManager.Instance.ConsumeCafeteriaFood();
    }

    private void BuyOption(int optionIndex)
    {
        CafeteriaFoodItem item = GetItem(openStoreType, optionIndex);

        if (item == null)
        {
            Debug.LogWarning($"[CAFETERIA] No item assigned for {openStoreType} option {optionIndex + 1}.");
            return;
        }

        if (!CanBuyFromStore(openStoreType))
        {
            Debug.Log($"[CAFETERIA] Already bought from {openStoreType} during this clock-in.");
            return;
        }

        if (GameManager.Instance == null || !GameManager.Instance.TrySpendMoney(item.price))
        {
            Debug.Log($"[CAFETERIA] Could not buy {item.itemName}.");
            return;
        }

        GameManager.Instance.MarkCafeteriaStoreBought(openStoreType);
        GameManager.Instance.AddCafeteriaFood(item.hungerRestore);

        Debug.Log($"[CAFETERIA] Bought {item.itemName}. Hunger restore held until eating: {item.hungerRestore}.");
        CloseStore();
    }

    private CafeteriaFoodItem GetItem(CafeteriaStoreType storeType, int optionIndex)
    {
        CafeteriaFoodItem[] items = GetItems(storeType);

        if (items == null || optionIndex < 0 || optionIndex >= items.Length)
            return null;

        return items[optionIndex];
    }

    private CafeteriaFoodItem[] GetItems(CafeteriaStoreType storeType)
    {
        switch (storeType)
        {
            case CafeteriaStoreType.Snacks:
                return snackItems;

            case CafeteriaStoreType.Meals:
                return mealItems;

            case CafeteriaStoreType.Drinks:
                return drinkItems;

            default:
                return null;
        }
    }

    private void BindButtons()
    {
        BindButton(option1Button, BuyOption1);
        BindButton(option2Button, BuyOption2);
        BindButton(option3Button, BuyOption3);
        BindButton(cancelButton, CloseStore);
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void RefreshOptionTexts()
    {
        SetOptionText(option1Text, GetItem(openStoreType, 0));
        SetOptionText(option2Text, GetItem(openStoreType, 1));
        SetOptionText(option3Text, GetItem(openStoreType, 2));
    }

    private void SetOptionText(TMP_Text optionText, CafeteriaFoodItem item)
    {
        if (optionText == null) return;

        optionText.text = item == null
            ? "???"
            : $"{item.itemName} - {FormatMoney(item.price)}";
    }

    private string FormatMoney(float value)
    {
        return Mathf.Approximately(value % 1f, 0f) ? $"${value:0}" : $"${value:0.00}";
    }
}
