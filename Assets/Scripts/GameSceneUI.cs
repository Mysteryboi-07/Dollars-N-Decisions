using TMPro;
using UnityEngine;

public class GameSceneUI : MonoBehaviour
{
    [Header("Stats UI")]
    [SerializeField] private GameObject statsUIGroup;

    [Header("Happiness")]
    [SerializeField] private RectTransform happinessFillBar;
    [SerializeField] private float happinessEmptyBottomOffset;
    [SerializeField] private GameObject happiness100Icon;
    [SerializeField] private GameObject happiness80Icon;
    [SerializeField] private GameObject happiness60Icon;
    [SerializeField] private GameObject happiness40Icon;
    [SerializeField] private GameObject happiness20Icon;
    [SerializeField] private GameObject happiness0Icon;

    [Header("Hunger")]
    [SerializeField] private RectTransform hungerFillBar;
    [SerializeField] private float hungerEmptyBottomOffset;

    [Header("Info UI")]
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

    [Header("Events UI")]
    [SerializeField] private GameObject houseEventObject;
    [SerializeField] private TMP_Text houseMultiplierText;

    public GameObject StatsUIGroup => statsUIGroup;
    public RectTransform HappinessFillBar => happinessFillBar;
    public float HappinessEmptyBottomOffset => happinessEmptyBottomOffset;
    public GameObject Happiness100Icon => happiness100Icon;
    public GameObject Happiness80Icon => happiness80Icon;
    public GameObject Happiness60Icon => happiness60Icon;
    public GameObject Happiness40Icon => happiness40Icon;
    public GameObject Happiness20Icon => happiness20Icon;
    public GameObject Happiness0Icon => happiness0Icon;
    public RectTransform HungerFillBar => hungerFillBar;
    public float HungerEmptyBottomOffset => hungerEmptyBottomOffset;
    public GameObject InfoUIGroup => infoUIGroup;
    public TMP_Text MoneyText => moneyText;
    public TMP_Text DayText => dayText;
    public TMP_Text TimeText => timeText;
    public GameObject Clock12Image => clock12Image;
    public GameObject Clock3Image => clock3Image;
    public GameObject Clock6Image => clock6Image;
    public GameObject Clock9Image => clock9Image;
    public GameObject MorningIcon => morningIcon;
    public GameObject NoonIcon => noonIcon;
    public GameObject EveningIcon => eveningIcon;
    public GameObject HouseEventObject => houseEventObject;
    public TMP_Text HouseMultiplierText => houseMultiplierText;

    private void OnEnable()
    {
        if (happinessFillBar != null && happinessEmptyBottomOffset <= 0f)
            happinessEmptyBottomOffset = happinessFillBar.rect.height;

        if (hungerFillBar != null && hungerEmptyBottomOffset <= 0f)
            hungerEmptyBottomOffset = hungerFillBar.rect.height;

        GameManager.Instance?.RegisterSceneUI(this);
    }

    private void OnDisable()
    {
        GameManager.Instance?.UnregisterSceneUI(this);
    }
}
