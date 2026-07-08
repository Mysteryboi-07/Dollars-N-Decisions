using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniFridgeManager : MonoBehaviour
{
    public static MiniFridgeManager Instance { get; private set; }

    [System.Serializable]
    public class FridgeSlot
    {
        public GameObject slotRoot;
        public TMP_Text itemNameText;
        public TMP_Text quantityText;
    }

    [Header("Fridge UI")]
    [SerializeField] private GameObject fridgePanel;
    [SerializeField] private FridgeSlot[] slots;
    [SerializeField] private bool hideEmptySlots;

    [Header("Info Panel")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private TMP_Text itemQuantityText;
    [SerializeField] private Button consumeButton;

    private string selectedItemName;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (fridgePanel != null)
            fridgePanel.SetActive(false);

        if (consumeButton != null)
        {
            consumeButton.onClick = new Button.ButtonClickedEvent();
            consumeButton.onClick.AddListener(ConsumeSelectedItem);
        }

        RefreshUI();
    }

    public void OpenFridge()
    {
        if (fridgePanel != null)
            fridgePanel.SetActive(true);

        RefreshUI();
    }

    public void CloseFridge()
    {
        selectedItemName = string.Empty;

        if (fridgePanel != null)
            fridgePanel.SetActive(false);
    }

    public void RequestClose()
    {
        if (InteractionUIManager.Instance != null)
            InteractionUIManager.Instance.CloseMiniFridge();
        else
            CloseFridge();
    }

    public void SelectSlot(int slotIndex)
    {
        GameManager gameManager = GameManager.Instance;

        if (gameManager == null ||
            slotIndex < 0 ||
            slotIndex >= gameManager.BagItems.Count)
        {
            selectedItemName = string.Empty;
            RefreshInfoPanel();
            return;
        }

        selectedItemName = gameManager.BagItems[slotIndex].itemName;
        RefreshInfoPanel();
    }

    public void ConsumeSelectedItem()
    {
        if (string.IsNullOrWhiteSpace(selectedItemName)) return;

        GameManager.Instance?.UseBagItem(selectedItemName);

        if (GetSelectedItem() == null)
            selectedItemName = string.Empty;

        RefreshUI();
    }

    private void RefreshUI()
    {
        RefreshSlots();
        RefreshInfoPanel();
    }

    private void RefreshSlots()
    {
        if (slots == null) return;

        GameManager gameManager = GameManager.Instance;
        int itemCount = gameManager != null ? gameManager.BagItems.Count : 0;

        for (int i = 0; i < slots.Length; i++)
        {
            FridgeSlot slot = slots[i];

            if (slot == null || slot.slotRoot == null) continue;

            int slotIndex = i;
            bool hasItem = slotIndex < itemCount;
            slot.slotRoot.SetActive(hasItem || !hideEmptySlots);

            Button slotButton = slot.slotRoot.GetComponent<Button>();

            if (slotButton == null)
                slotButton = slot.slotRoot.AddComponent<Button>();

            slotButton.onClick = new Button.ButtonClickedEvent();
            slotButton.interactable = hasItem;

            if (!hasItem)
            {
                if (slot.itemNameText != null)
                    slot.itemNameText.text = $"Slot {slotIndex + 1}";

                if (slot.quantityText != null)
                    slot.quantityText.text = string.Empty;

                continue;
            }

            GameManager.BagItem bagItem = gameManager.BagItems[slotIndex];

            if (slot.itemNameText != null)
                slot.itemNameText.text = bagItem.itemName;

            if (slot.quantityText != null)
                slot.quantityText.text = $"x{bagItem.quantity}";

            slotButton.onClick.AddListener(() => SelectSlot(slotIndex));
        }
    }

    private void RefreshInfoPanel()
    {
        GameManager.BagItem selectedItem = GetSelectedItem();
        bool hasSelection = selectedItem != null;

        if (infoPanel != null)
            infoPanel.SetActive(true);

        if (!hasSelection)
        {
            if (itemNameText != null)
                itemNameText.text = "???";

            if (itemDescriptionText != null)
                itemDescriptionText.text = "Select an item to view description";

            if (itemQuantityText != null)
                itemQuantityText.text = "???";

            if (consumeButton != null)
                consumeButton.interactable = false;

            return;
        }

        if (itemNameText != null)
            itemNameText.text = selectedItem.itemName;

        if (itemDescriptionText != null)
            itemDescriptionText.text = $"Restores {selectedItem.hungerRestore:0.#} Hunger";

        if (itemQuantityText != null)
            itemQuantityText.text = selectedItem.quantity.ToString();

        if (consumeButton != null)
            consumeButton.interactable = selectedItem.quantity > 0;
    }

    private GameManager.BagItem GetSelectedItem()
    {
        GameManager gameManager = GameManager.Instance;

        if (gameManager == null || string.IsNullOrWhiteSpace(selectedItemName))
            return null;

        foreach (GameManager.BagItem bagItem in gameManager.BagItems)
        {
            if (bagItem.itemName == selectedItemName)
                return bagItem;
        }

        return null;
    }
}
