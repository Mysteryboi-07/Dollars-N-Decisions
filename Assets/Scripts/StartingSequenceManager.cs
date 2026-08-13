using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class StartingSequenceManager : MonoBehaviour
{
    public static StartingSequenceManager Instance { get; private set; }

    [Header("UI Animator")]
    [SerializeField] private Animator uiAnimator;
    [SerializeField] private string uiHideTrigger = "Hide";
    [SerializeField] private float uiHideDuration = 0.28f;

    [Header("Starting Camera Animator")]
    [SerializeField] private Animator startingCamAnimator;
    [SerializeField] private string camPart1Trigger = "Part1";
    [SerializeField] private string camPart2Trigger = "Part2";
    [SerializeField] private float camPart1Duration = 0.35f;
    [SerializeField] private float camPart2Duration = 7f;

    [Header("Testing Scene Objects")]
    [FormerlySerializedAs("startingSequenceGroup")]
    [SerializeField] private GameObject introRootObject;
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject furnitureGroup;
    [SerializeField] private GameObject mainUI;
    [SerializeField] private GameObject houseTutorialCamera;
    [SerializeField] private HouseIntroTutorialManager houseIntroTutorial;

    private bool isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // ---------- Starting Buttons ----------

    public void PressStartingButton()
    {
        if (isTransitioning) return;

        StartCoroutine(HideStartingButtonsThenShowLogin());
    }

    private IEnumerator HideStartingButtonsThenShowLogin()
    {
        isTransitioning = true;

        if (uiAnimator != null)
            uiAnimator.SetTrigger(uiHideTrigger);

        yield return new WaitForSeconds(uiHideDuration);

        UIManager.Instance?.ShowLogin();

        isTransitioning = false;
    }

    // ---------- After Login Success ----------

    public void PlayCameraPart1ThenShowName()
    {
        if (isTransitioning) return;

        StartCoroutine(CameraPart1ThenShowName());
    }

    private IEnumerator CameraPart1ThenShowName()
    {
        isTransitioning = true;

        UIManager.Instance?.HideAllPanels();

        if (startingCamAnimator != null)
            startingCamAnimator.SetTrigger(camPart1Trigger);

        yield return new WaitForSeconds(camPart1Duration);

        UIManager.Instance?.ShowNamePanel();

        isTransitioning = false;
    }

    // ---------- Name Confirm ----------

    public void ConfirmName()
    {
        if (isTransitioning) return;

        StartCoroutine(CameraPart2ThenStartGame());
    }

    private IEnumerator CameraPart2ThenStartGame()
    {
        isTransitioning = true;

        UIManager.Instance?.HideAllPanels();

        if (startingCamAnimator != null)
            startingCamAnimator.SetTrigger(camPart2Trigger);

        yield return new WaitForSeconds(camPart2Duration);

        StartGameInCurrentScene();
    }

    private void StartGameInCurrentScene()
    {
        if (introRootObject != null)
            introRootObject.SetActive(false);

        if (furnitureGroup != null)
            furnitureGroup.SetActive(true);

        if (mainUI != null)
            mainUI.SetActive(true);

        if (houseIntroTutorial != null)
        {
            if (houseTutorialCamera != null)
                houseTutorialCamera.SetActive(true);

            houseIntroTutorial.BeginTutorial(FinishHouseTutorial);
            return;
        }

        FinishHouseTutorial();
    }

    private void FinishHouseTutorial()
    {
        if (houseTutorialCamera != null)
            houseTutorialCamera.SetActive(false);

        if (playerObject != null)
            playerObject.SetActive(true);

        GameManager.Instance?.LockCursor();

        GameManager.Instance?.ShowStatsUI();
        GameManager.Instance?.SetHouseEventVisible(true);
    }
}
