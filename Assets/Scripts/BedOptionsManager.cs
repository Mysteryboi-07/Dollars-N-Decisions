using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BedOptionsManager : MonoBehaviour
{
    public static BedOptionsManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject optionsGroup;
    [SerializeField] private Animator optionsAnimator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private TMP_Text option1Text;
    [SerializeField] private TMP_Text option2Text;
    [SerializeField] private Button option1Button;
    [SerializeField] private Button option2Button;
    [SerializeField] private Button cancelButton;

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

    public void OpenOptions()
    {
        if (option1Text != null)
            option1Text.text = "Take a nap";

        if (option2Text != null)
            option2Text.text = "Sleep until tomorrow";

        if (optionsGroup != null)
            optionsGroup.SetActive(true);

        if (optionsAnimator != null && !string.IsNullOrWhiteSpace(openTrigger))
            optionsAnimator.SetTrigger(openTrigger);

        GameManager.Instance?.FreezePlayerForUI();
        Debug.Log("[BED] Opened bed options.");
    }

    public void TakeNap()
    {
        CloseOptions(false);
        SleepManager.Instance?.TakeNap();
    }

    public void SleepUntilTomorrow()
    {
        CloseOptions(false);
        SleepManager.Instance?.GoToSleep();
    }

    public void Cancel()
    {
        CloseOptions(true);
    }

    private void CloseOptions(bool shouldRefreshPrompt)
    {
        if (optionsGroup != null)
            optionsGroup.SetActive(false);

        GameManager.Instance?.UnfreezePlayerFromUI();

        if (shouldRefreshPrompt)
            InteractionUIManager.Instance?.RefreshPrompt();

        Debug.Log("[BED] Closed bed options.");
    }

    private void BindButtons()
    {
        if (option1Button != null)
        {
            option1Button.onClick.RemoveListener(TakeNap);
            option1Button.onClick.AddListener(TakeNap);
        }

        if (option2Button != null)
        {
            option2Button.onClick.RemoveListener(SleepUntilTomorrow);
            option2Button.onClick.AddListener(SleepUntilTomorrow);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(Cancel);
            cancelButton.onClick.AddListener(Cancel);
        }
    }
}
