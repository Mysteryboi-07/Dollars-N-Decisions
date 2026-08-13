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
    SceneDoor = 11,
    BusStop = 12,
    CafeteriaStore = 13,
    CafeteriaTable = 14,
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

public enum WaypointLocation
{
    None,
    Home,
    Office,
    Market,
}

public enum CafeteriaStoreType
{
    None,
    Snacks,
    Meals,
    Drinks,
}

public class InteractableTrigger : MonoBehaviour
{
    [Header("Interaction Info")]
    [SerializeField] private InteractionType interactionType = InteractionType.None;
    [SerializeField] private string promptMessage = "[E] Interact";

    [Header("Scene Travel")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private bool requireOfficeEntryAllowed;
    [SerializeField] private bool clockInBeforeTravel;
    [SerializeField] private bool clockOutBeforeTravel;

    [Header("Bus Stop")]
    [SerializeField] private WaypointLocation currentWaypoint = WaypointLocation.None;

    [Header("Cafeteria")]
    [SerializeField] private CafeteriaStoreType cafeteriaStoreType = CafeteriaStoreType.None;

    [Header("Shop Info")]
    [SerializeField] private ShopCategory shopCategory = ShopCategory.None;
    [SerializeField] private string shopItemName;
    [SerializeField] private float shopItemPrice;
    [SerializeField] private float shopItemHungerRestore;

    public InteractionType InteractionType => interactionType;
    public string PromptMessage => promptMessage;
    public string TargetSceneName => targetSceneName;
    public bool RequireOfficeEntryAllowed => requireOfficeEntryAllowed;
    public bool ClockInBeforeTravel => clockInBeforeTravel;
    public bool ClockOutBeforeTravel => clockOutBeforeTravel;
    public WaypointLocation CurrentWaypoint => currentWaypoint;
    public CafeteriaStoreType CafeteriaStoreType => cafeteriaStoreType;
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
