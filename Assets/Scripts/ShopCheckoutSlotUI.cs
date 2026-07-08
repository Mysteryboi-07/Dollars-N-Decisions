using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopCheckoutSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;

    private ConvenienceShopManager shopManager;
    private int slotIndex;
    private string currentItemName;

    public void Show(ConvenienceShopManager manager, int index, string itemName, int quantity)
    {
        shopManager = manager;
        slotIndex = index;
        currentItemName = itemName;

        gameObject.SetActive(true);
        ResolveButtons();
        DisableSlotTextRaycasts();

        if (itemNameText != null)
            itemNameText.text = itemName;

        if (quantityText != null)
            quantityText.text = quantity.ToString();

        BindButtons();
    }

    public void Hide()
    {
        shopManager = null;
        currentItemName = string.Empty;
        gameObject.SetActive(false);
        ClearButtons();
    }

    private void BindButtons()
    {
        ClearButtons();

        if (minusButton != null)
            minusButton.onClick.AddListener(DecreaseQuantity);

        if (plusButton != null)
            plusButton.onClick.AddListener(IncreaseQuantity);
    }

    private void ClearButtons()
    {
        if (minusButton != null)
            minusButton.onClick = new Button.ButtonClickedEvent();

        if (plusButton != null)
            plusButton.onClick = new Button.ButtonClickedEvent();
    }

    private void DecreaseQuantity()
    {
        Debug.Log($"[SHOP SLOT UI] {GetHierarchyPath(gameObject)} minus clicked by {GetCurrentSelectedPath()} -> {currentItemName}");
        shopManager?.ChangeItemQuantity(currentItemName, -1);
    }

    private void IncreaseQuantity()
    {
        Debug.Log($"[SHOP SLOT UI] {GetHierarchyPath(gameObject)} plus clicked by {GetCurrentSelectedPath()} -> {currentItemName}");
        shopManager?.ChangeItemQuantity(currentItemName, 1);
    }

    private void ResolveButtons()
    {
        Button[] childButtons = GetComponentsInChildren<Button>(true);

        foreach (Button childButton in childButtons)
        {
            string buttonName = childButton.gameObject.name.Trim();

            if (buttonName == "-")
                minusButton = childButton;
            else if (buttonName == "+")
                plusButton = childButton;
        }
    }

    private void DisableSlotTextRaycasts()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text text in texts)
            text.raycastTarget = false;
    }

    private string GetCurrentSelectedPath()
    {
        GameObject selectedObject = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject
            : null;

        return selectedObject != null ? GetHierarchyPath(selectedObject) : "null";
    }

    private string GetHierarchyPath(GameObject target)
    {
        if (target == null) return "null";

        string path = target.name;
        Transform current = target.transform.parent;

        while (current != null)
        {
            path = $"{current.name}/{path}";
            current = current.parent;
        }

        return path;
    }
}
