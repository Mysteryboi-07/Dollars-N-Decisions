using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaypointTravelManager : MonoBehaviour
{
    public static WaypointTravelManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject optionsGroup;
    [SerializeField] private Animator optionsAnimator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private TMP_Text option1Text;
    [SerializeField] private TMP_Text option2Text;
    [SerializeField] private Button option1Button;
    [SerializeField] private Button option2Button;
    [SerializeField] private Button exitButton;

    [Header("Waypoints")]
    [SerializeField] private Transform homeWaypoint;
    [SerializeField] private Transform officeWaypoint;
    [SerializeField] private Transform marketWaypoint;

    [Header("Player")]
    [SerializeField] private Transform playerTransform;

    private readonly WaypointLocation[] displayOrder =
    {
        WaypointLocation.Home,
        WaypointLocation.Office,
        WaypointLocation.Market
    };

    private WaypointLocation currentWaypoint = WaypointLocation.None;
    private WaypointLocation option1Destination = WaypointLocation.None;
    private WaypointLocation option2Destination = WaypointLocation.None;
    private Coroutine teleportRoutine;

    private void Awake()
    {
        Instance = this;
        BindButtons();
    }

    private void OnEnable()
    {
        BindButtons();
    }

    private void Start()
    {
        if (optionsGroup != null)
            optionsGroup.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void OpenOptions(WaypointLocation busStopWaypoint)
    {
        if (busStopWaypoint == WaypointLocation.None)
        {
            Debug.LogWarning("[WAYPOINT] Bus stop has no current waypoint assigned.");
            return;
        }

        currentWaypoint = busStopWaypoint;
        BuildOptions();

        if (optionsGroup != null)
            optionsGroup.SetActive(true);

        if (optionsAnimator != null && !string.IsNullOrWhiteSpace(openTrigger))
            optionsAnimator.SetTrigger(openTrigger);

        GameManager.Instance?.FreezePlayerForUI();
        Debug.Log($"[WAYPOINT] Opened travel options at {currentWaypoint}.");
    }

    public void ChooseOption1()
    {
        Debug.Log($"[WAYPOINT] Option 1 clicked: {option1Destination}.");
        TravelTo(option1Destination);
    }

    public void ChooseOption2()
    {
        Debug.Log($"[WAYPOINT] Option 2 clicked: {option2Destination}.");
        TravelTo(option2Destination);
    }

    public void CloseOptions()
    {
        CloseOptions(true);
    }

    private void CloseOptions(bool shouldRefreshPrompt)
    {
        if (optionsGroup != null)
            optionsGroup.SetActive(false);

        currentWaypoint = WaypointLocation.None;
        option1Destination = WaypointLocation.None;
        option2Destination = WaypointLocation.None;

        GameManager.Instance?.UnfreezePlayerFromUI();
        InteractionUIManager.Instance?.FinishBusStopInteraction(shouldRefreshPrompt);
        Debug.Log("[WAYPOINT] Closed travel options.");
    }

    private void TravelTo(WaypointLocation destination)
    {
        if (destination == WaypointLocation.None)
        {
            Debug.LogWarning("[WAYPOINT] No destination assigned to this option.");
            return;
        }

        Transform targetWaypoint = GetWaypoint(destination);

        if (targetWaypoint == null)
        {
            Debug.LogWarning($"[WAYPOINT] No transform assigned for {destination}.");
            return;
        }

        Transform activePlayer = GetPlayerTransform();

        if (activePlayer == null)
        {
            Debug.LogWarning("[WAYPOINT] No player transform found.");
            return;
        }

        if (teleportRoutine != null)
            StopCoroutine(teleportRoutine);

        teleportRoutine = StartCoroutine(TeleportRoutine(activePlayer, targetWaypoint, destination));
    }

    private IEnumerator TeleportRoutine(Transform activePlayer, Transform targetWaypoint, WaypointLocation destination)
    {
        CharacterController characterController = activePlayer.GetComponent<CharacterController>();
        Vector3 startPosition = activePlayer.position;
        Quaternion startRotation = activePlayer.rotation;

        Debug.Log($"[WAYPOINT] Teleport target player: {GetHierarchyPath(activePlayer.gameObject)}. Before: {startPosition}. Target: {targetWaypoint.position}.");

        SetCharacterControllerEnabled(characterController, false);
        activePlayer.SetPositionAndRotation(targetWaypoint.position, targetWaypoint.rotation);
        Physics.SyncTransforms();

        yield return null;

        activePlayer.SetPositionAndRotation(targetWaypoint.position, targetWaypoint.rotation);
        Physics.SyncTransforms();

        SetCharacterControllerEnabled(characterController, true);

        if (characterController != null)
            characterController.Move(Vector3.zero);

        Debug.Log($"[WAYPOINT] Travelled from {currentWaypoint} to {destination}. After: {activePlayer.position}. Started at: {startPosition}, rotation was {startRotation.eulerAngles}.");
        teleportRoutine = null;
        CloseOptions(false);
    }

    private void SetCharacterControllerEnabled(CharacterController characterController, bool isEnabled)
    {
        if (characterController != null)
            characterController.enabled = isEnabled;
    }

    private void BuildOptions()
    {
        option1Destination = WaypointLocation.None;
        option2Destination = WaypointLocation.None;

        foreach (WaypointLocation location in displayOrder)
        {
            if (location == currentWaypoint) continue;

            if (option1Destination == WaypointLocation.None)
            {
                option1Destination = location;
                continue;
            }

            option2Destination = location;
            break;
        }

        SetOptionText(option1Text, option1Destination);
        SetOptionText(option2Text, option2Destination);
    }

    private void SetOptionText(TMP_Text text, WaypointLocation destination)
    {
        if (text == null) return;

        text.text = destination == WaypointLocation.None
            ? string.Empty
            : $"Go to {GetDisplayName(destination)}";
    }

    private string GetDisplayName(WaypointLocation destination)
    {
        return destination == WaypointLocation.Home ? "Home" : destination.ToString();
    }

    private Transform GetWaypoint(WaypointLocation destination)
    {
        switch (destination)
        {
            case WaypointLocation.Home:
                return homeWaypoint;

            case WaypointLocation.Office:
                return officeWaypoint;

            case WaypointLocation.Market:
                return marketWaypoint;

            default:
                return null;
        }
    }

    private Transform GetPlayerTransform()
    {
        Transform assignedPlayer = ResolvePlayerRoot(playerTransform);

        if (assignedPlayer != null && assignedPlayer.gameObject.activeInHierarchy)
            return assignedPlayer;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return ResolvePlayerRoot(player != null ? player.transform : playerTransform);
    }

    private Transform ResolvePlayerRoot(Transform target)
    {
        if (target == null) return null;

        CharacterController characterController = target.GetComponentInParent<CharacterController>();

        if (characterController != null)
            return characterController.transform;

        return target.root != null && target.root.CompareTag("Player")
            ? target.root
            : target;
    }

    private void BindButtons()
    {
        if (option1Button != null)
        {
            option1Button.onClick.RemoveListener(ChooseOption1);
            option1Button.onClick.AddListener(ChooseOption1);
        }

        if (option2Button != null)
        {
            option2Button.onClick.RemoveListener(ChooseOption2);
            option2Button.onClick.AddListener(ChooseOption2);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(CloseOptions);
            exitButton.onClick.AddListener(CloseOptions);
        }
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
