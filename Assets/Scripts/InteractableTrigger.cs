using UnityEngine;

public enum InteractionType
{
    None = 0,
    Laptop = 1,
    HouseDoor = 2,
    OfficeDoor = 3,
    Monitor = 4,
    Bed = 5,
    ShopShelf = 6,
    Cashier = 7,
    MiniFridge = 9,
    MarketDoor = 10,
}

public enum ShopCategory
{
    None,
    Cereal,
    CupNoodles,
    Snacks,
    Drinks,
    Fruits,
}

public class InteractableTrigger : MonoBehaviour
{
    [Header("Interaction Info")]
    [SerializeField] private InteractionType interactionType = InteractionType.None;
    [SerializeField] private string promptMessage = "[E] Interact";

    [Header("Shop Info")]
    [SerializeField] private ShopCategory shopCategory = ShopCategory.None;
    [SerializeField] private string shopItemName;
    [SerializeField] private float shopItemPrice;
    [SerializeField] private float shopItemHungerRestore;

    public InteractionType InteractionType => interactionType;
    public string PromptMessage => promptMessage;
    public ShopCategory ShopCategory => shopCategory;
    public string ShopItemName => shopItemName;
    public float ShopItemPrice => shopItemPrice;
    public float ShopItemHungerRestore => shopItemHungerRestore;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        InteractionUIManager.Instance?.SetCurrentInteractable(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        InteractionUIManager.Instance?.ClearCurrentInteractable(this);
    }
}
