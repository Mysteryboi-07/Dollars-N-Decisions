using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class ConvenienceShopManager : MonoBehaviour
{
    public static ConvenienceShopManager Instance { get; private set; }

    [System.Serializable]
    public class ShopItem
    {
        public string itemName;
        public ShopCategory category;
        public float price;
        public float hungerRestore;
    }

    private class CartItem
    {
        public string itemName;
        public ShopCategory category;
        public float price;
        public float hungerRestore;
        public int quantity;
    }

    [Header("Shop Items")]
    [SerializeField] private ShopItem[] shopItems;

    [Header("Checkout UI")]
    [FormerlySerializedAs("confirmationPanel")]
    [SerializeField] private GameObject checkoutPanel;
    [FormerlySerializedAs("confirmationText")]
    [SerializeField] private TMP_Text checkoutText;
    [SerializeField] private ShopCheckoutSlotUI[] checkoutSlots;

    private readonly List<CartItem> cartItems = new List<CartItem>();
    private ShopCategory currentCategory = ShopCategory.None;

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
        if (checkoutPanel != null)
            checkoutPanel.SetActive(false);

        RefreshCheckoutPanel();
    }

    public void OpenCategory(ShopCategory category)
    {
        currentCategory = category;
        Debug.Log($"[SHOP] Browsing {currentCategory}.");
    }

    public void AddCurrentCategoryItemToCart(int categoryItemIndex)
    {
        ShopItem shopItem = GetCurrentCategoryItem(categoryItemIndex);

        if (shopItem == null)
        {
            Debug.LogWarning($"[SHOP] No item found at category index {categoryItemIndex}.");
            return;
        }

        AddItemToCart(shopItem.itemName, shopItem.category, shopItem.price, shopItem.hungerRestore);
    }

    public void AddShelfItemToCart(InteractableTrigger shelf)
    {
        if (shelf == null) return;

        if (string.IsNullOrWhiteSpace(shelf.ShopItemName))
        {
            Debug.LogWarning("[SHOP] Shelf item name is empty.");
            return;
        }

        AddItemToCart(
            shelf.ShopItemName,
            shelf.ShopCategory,
            shelf.ShopItemPrice,
            shelf.ShopItemHungerRestore
        );
        GameManager.Instance?.PlayItemAddedFeedback(shelf.ShopItemName);
    }

    public void AddItemToCartByName(string itemName)
    {
        if (shopItems == null) return;

        foreach (ShopItem shopItem in shopItems)
        {
            if (shopItem != null && shopItem.itemName == itemName)
            {
                AddItemToCart(shopItem.itemName, shopItem.category, shopItem.price, shopItem.hungerRestore);
                return;
            }
        }

        Debug.LogWarning($"[SHOP] No shop item named {itemName}.");
    }

    public void BeginCheckout()
    {
        RefreshCheckoutPanel();

        if (checkoutPanel != null)
            checkoutPanel.SetActive(true);

        GameManager.Instance?.FreezePlayerForUI();
        Debug.Log("[SHOP] Used cashier.");
    }

    public void Checkout()
    {
        BeginCheckout();
    }

    public void ConfirmCheckout()
    {
        if (cartItems.Count == 0)
        {
            Debug.Log("[SHOP] Cart is empty.");
            RefreshCheckoutPanel();
            return;
        }

        float totalCost = GetCartTotal();

        if (GameManager.Instance == null || !GameManager.Instance.TrySpendMoney(totalCost))
        {
            Debug.Log($"[SHOP] Checkout failed. Total was {FormatMoney(totalCost)}.");
            return;
        }

        foreach (CartItem cartItem in cartItems)
            GameManager.Instance.AddBagItem(cartItem.itemName, cartItem.quantity, cartItem.hungerRestore);

        Debug.Log($"[SHOP] Checkout complete. Spent {FormatMoney(totalCost)}.");
        cartItems.Clear();
        CloseCheckoutPanel();
    }

    public void CancelCheckout()
    {
        CloseCheckoutPanel();
    }

    public void ClearCart()
    {
        cartItems.Clear();
        RefreshCheckoutPanel();
        Debug.Log("[SHOP] Cart cleared.");
    }

    public void ChangeSlotQuantity(int slotIndex, int quantityChange)
    {
        if (slotIndex < 0 || slotIndex >= cartItems.Count) return;

        CartItem cartItem = cartItems[slotIndex];
        ChangeItemQuantity(cartItem.itemName, quantityChange);
    }

    public void ChangeItemQuantity(string itemName, int quantityChange)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return;

        CartItem cartItem = cartItems.Find(item => item.itemName == itemName);

        if (cartItem == null) return;

        Debug.Log($"[SHOP CART] {(quantityChange > 0 ? "Increasing" : "Decreasing")} {cartItem.itemName}. Before: {cartItem.quantity}");

        cartItem.quantity += quantityChange;

        if (cartItem.quantity <= 0)
            cartItems.Remove(cartItem);

        RefreshCheckoutPanel();
    }

    private void AddItemToCart(string itemName, ShopCategory category, float price, float hungerRestore)
    {
        CartItem cartItem = cartItems.Find(item => item.itemName == itemName);

        if (cartItem == null)
        {
            cartItem = new CartItem
            {
                itemName = itemName,
                category = category,
                price = price,
                hungerRestore = hungerRestore,
                quantity = 0
            };

            cartItems.Add(cartItem);
        }

        cartItem.quantity++;
        Debug.Log($"[SHOP] Added {itemName} to cart. Quantity: {cartItem.quantity}. Cart total: {FormatMoney(GetCartTotal())}.");
        RefreshCheckoutPanel();
    }

    private ShopItem GetCurrentCategoryItem(int categoryItemIndex)
    {
        if (shopItems == null || categoryItemIndex < 0) return null;

        int matchingIndex = 0;

        foreach (ShopItem shopItem in shopItems)
        {
            if (shopItem == null || shopItem.category != currentCategory) continue;

            if (matchingIndex == categoryItemIndex)
                return shopItem;

            matchingIndex++;
        }

        return null;
    }

    private void RefreshCheckoutPanel()
    {
        RefreshCheckoutText();
        RefreshCheckoutSlots();
    }

    private void RefreshCheckoutText()
    {
        if (checkoutText == null) return;

        if (cartItems.Count == 0)
        {
            checkoutText.text = "Basket is empty.";
            return;
        }

        checkoutText.text = $"Confirm Purchase of <u><b>{GetCartItemCount()}</b></u> items for <u><b>{FormatMoney(GetCartTotal())}</b></u>?";
    }

    private void RefreshCheckoutSlots()
    {
        if (checkoutSlots == null) return;

        for (int i = 0; i < checkoutSlots.Length; i++)
        {
            ShopCheckoutSlotUI slot = checkoutSlots[i];

            if (slot == null) continue;

            if (i >= cartItems.Count)
            {
                slot.Hide();
                continue;
            }

            CartItem cartItem = cartItems[i];
            slot.Show(this, i, cartItem.itemName, cartItem.quantity);
        }
    }

    private void CloseCheckoutPanel()
    {
        if (checkoutPanel != null)
            checkoutPanel.SetActive(false);

        GameManager.Instance?.UnfreezePlayerFromUI();
        RefreshCheckoutPanel();
    }

    private float GetCartTotal()
    {
        float totalCost = 0f;

        foreach (CartItem cartItem in cartItems)
            totalCost += cartItem.price * cartItem.quantity;

        return totalCost;
    }

    private int GetCartItemCount()
    {
        int itemCount = 0;

        foreach (CartItem cartItem in cartItems)
            itemCount += cartItem.quantity;

        return itemCount;
    }

    private string FormatMoney(float value)
    {
        return Mathf.Approximately(value % 1f, 0f) ? $"${value:0}" : $"${value:0.00}";
    }
}
