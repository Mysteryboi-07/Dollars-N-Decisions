using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DeadlineDashManager : MonoBehaviour, IWorkMinigame
{
    public enum ShapeType
    {
        Circle,
        Triangle,
        Square,
        Diamond,
        Star
    }

    [System.Serializable]
    public class DifficultySettings
    {
        public int taskCount = 3;
        public int sequenceLength = 4;
        [FormerlySerializedAs("timeLimit")]
        public float taskTimeLimit = 10f;
        public int maxReward = 10;
    }

    [System.Serializable]
    public class TaskTemplate
    {
        public string taskName;
        [TextArea(1, 3)] public string briefDescription;
    }

    [System.Serializable]
    public class ShapeVisual
    {
        public ShapeType shapeType;
        public Sprite sprite;
    }

    [System.Serializable]
    public class PatternGroup
    {
        public GameObject rootObject;
        public Image[] shapeImages;
    }

    private class RuntimeTask
    {
        public string taskName;
        public string briefDescription;
        public int originalTaskNumber;
        public readonly List<ShapeType> sequence = new List<ShapeType>();
        public int nextShapeIndex;
        public float timeRemaining;
    }

    [Header("Gameplay Groups")]
    [SerializeField] private GameObject[] gameplayGroups;

    [Header("Task List")]
    [SerializeField] private DeadlineDashTaskSlotUI[] taskSlots;
    [SerializeField] private TMP_Text taskListCountText;

    [Header("Task Detail")]
    [SerializeField] private TMP_Text taskTitleText;
    [SerializeField] private TMP_Text taskDescriptionText;
    [SerializeField] private TMP_Text timeLeftText;

    [Header("Target Pattern")]
    [SerializeField] private PatternGroup easyPattern;
    [SerializeField] private PatternGroup mediumPattern;
    [SerializeField] private PatternGroup hardPattern;

    [Header("Shape Buttons")]
    [SerializeField] private DeadlineDashShapeButtonUI[] shapeButtons;
    [SerializeField] private ShapeVisual[] shapeVisuals;

    [Header("Result")]
    [SerializeField] private TMP_Text resultScoreText;
    [SerializeField] private TMP_Text resultDetailsText;
    [SerializeField] private float resultDisplayDuration = 5f;
    [SerializeField] private UnityEvent onMinigameClosed;

    [Header("Tasks")]
    [SerializeField] private TaskTemplate[] taskPool;

    [Header("Difficulty")]
    [SerializeField] private DifficultySettings easy = new DifficultySettings { taskCount = 3, sequenceLength = 4, taskTimeLimit = 10f, maxReward = 10 };
    [SerializeField] private DifficultySettings medium = new DifficultySettings { taskCount = 4, sequenceLength = 5, taskTimeLimit = 10f, maxReward = 20 };
    [SerializeField] private DifficultySettings hard = new DifficultySettings { taskCount = 5, sequenceLength = 6, taskTimeLimit = 10f, maxReward = 30 };

    [Header("Stat Action")]
    [SerializeField] private string workActionName = "Work";

    [Header("Office Rules")]
    [SerializeField] private bool returnHomeAtEndOfOfficeDay;

    [Header("Home Rules")]
    [SerializeField] private bool sleepAfterMidnightHomeWork;

    private readonly List<RuntimeTask> activeTasks = new List<RuntimeTask>();
    private readonly List<ShapeType> currentButtonShapes = new List<ShapeType>();
    private Coroutine timerRoutine;
    private Coroutine resultRoutine;
    private DifficultySettings currentDifficulty;
    private PatternGroup currentPatternGroup;
    private int selectedTaskIndex = -1;
    private int completedTasks;
    private int correctInputs;
    private int wrongInputs;
    private int totalInputsRequired;
    private int startingDayPhase;
    private bool isPlaying;
    private bool shouldReturnHomeAfterResult;
    private bool shouldSleepAfterResult;

    private void OnEnable()
    {
        OpenMinigame();
    }

    private void OnDisable()
    {
        StopRound();
    }

    public void OpenMinigame()
    {
        StopRound();
        SetGameplayGroups(false);
        SetResultTexts(false);
        ClearTaskDetail();
        RefreshTaskSlots();
    }

    public void StartEasy()
    {
        StartRound(easy, easyPattern);
    }

    public void StartMedium()
    {
        StartRound(medium, mediumPattern);
    }

    public void StartHard()
    {
        StartRound(hard, hardPattern);
    }

    public void SelectTask(int taskIndex)
    {
        if (!isPlaying || taskIndex < 0 || taskIndex >= activeTasks.Count) return;
        if (selectedTaskIndex >= 0 && taskIndex != selectedTaskIndex) return;

        selectedTaskIndex = taskIndex;
        ShowSelectedTask();
        RefreshTaskSlots();
        ShuffleShapeButtons();
    }

    public void ClickShape(ShapeType clickedShape)
    {
        if (!isPlaying || selectedTaskIndex < 0 || selectedTaskIndex >= activeTasks.Count) return;

        RuntimeTask selectedTask = activeTasks[selectedTaskIndex];
        ShapeType expectedShape = selectedTask.sequence[selectedTask.nextShapeIndex];

        if (clickedShape == expectedShape)
        {
            correctInputs++;
            selectedTask.nextShapeIndex++;
        }
        else
        {
            wrongInputs++;
            FailSelectedTask();
            return;
        }

        if (selectedTask.nextShapeIndex >= selectedTask.sequence.Count)
        {
            CompleteSelectedTask();
            return;
        }

        ShowSelectedTask();
        ShuffleShapeButtons();
    }

    private void FailSelectedTask()
    {
        activeTasks.RemoveAt(selectedTaskIndex);
        SelectNextAvailableTask();
        RefreshTaskSlots();

        if (activeTasks.Count <= 0)
        {
            CompleteRound();
            return;
        }

        ShowSelectedTask();
        ShuffleShapeButtons();
    }

    private void StartRound(DifficultySettings difficulty, PatternGroup patternGroup)
    {
        StopRound();

        currentDifficulty = difficulty;
        currentPatternGroup = patternGroup;
        selectedTaskIndex = -1;
        completedTasks = 0;
        correctInputs = 0;
        wrongInputs = 0;
        totalInputsRequired = 0;
        shouldReturnHomeAfterResult = false;
        shouldSleepAfterResult = false;
        startingDayPhase = GameManager.Instance != null ? GameManager.Instance.CurrentDayPhase : -1;
        isPlaying = true;

        BuildTaskList();
        SetGameplayGroups(true);
        SetResultTexts(false);
        SetActivePatternGroup(currentPatternGroup);
        RefreshTaskSlots();
        UpdateTimerText();

        if (activeTasks.Count > 0)
            SelectTask(0);

        timerRoutine = StartCoroutine(TimerRoutine());
    }

    private void CompleteSelectedTask()
    {
        completedTasks++;
        activeTasks.RemoveAt(selectedTaskIndex);
        SelectNextAvailableTask();
        RefreshTaskSlots();

        if (activeTasks.Count <= 0)
        {
            CompleteRound();
            return;
        }

        ShowSelectedTask();
        ShuffleShapeButtons();
    }

    private void CompleteRound()
    {
        if (!isPlaying) return;

        isPlaying = false;

        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        int baseReward = CalculateEarnedReward();
        int earnedReward = ApplyRewardLocationMultiplier(baseReward);
        ApplyWorkConsequences();

        SetGameplayGroups(false);
        SetActivePatternGroup(null);

        if (resultScoreText != null)
        {
            resultScoreText.text = $"{correctInputs}/{totalInputsRequired}";
            resultScoreText.gameObject.SetActive(true);
        }

        if (resultDetailsText != null)
        {
            resultDetailsText.text = $"You earned ${earnedReward}";
            resultDetailsText.gameObject.SetActive(true);
        }

        GameManager.Instance?.ChangeMoney(earnedReward);
        resultRoutine = StartCoroutine(CloseAfterResultDelay());
    }

    private IEnumerator TimerRoutine()
    {
        while (isPlaying)
        {
            TickSelectedTaskTimer();
            yield return null;
        }
    }

    private IEnumerator CloseAfterResultDelay()
    {
        yield return new WaitForSeconds(resultDisplayDuration);

        resultRoutine = null;
        gameObject.SetActive(false);
        onMinigameClosed?.Invoke();

        if (shouldReturnHomeAfterResult)
            InteractionUIManager.Instance?.ReturnHomeFromOfficeWithFade();
        else if (shouldSleepAfterResult)
            InteractionUIManager.Instance?.SleepAfterLateHomeWorkWithFade();
    }

    private void BuildTaskList()
    {
        activeTasks.Clear();

        List<TaskTemplate> availableTasks = new List<TaskTemplate>();

        if (taskPool != null)
        {
            foreach (TaskTemplate taskTemplate in taskPool)
            {
                if (taskTemplate != null)
                    availableTasks.Add(taskTemplate);
            }
        }

        Shuffle(availableTasks);

        int taskCount = Mathf.Max(1, currentDifficulty.taskCount);

        for (int i = 0; i < taskCount; i++)
        {
            TaskTemplate taskTemplate = availableTasks.Count > 0
                ? availableTasks[i % availableTasks.Count]
                : null;

            RuntimeTask runtimeTask = new RuntimeTask
            {
                taskName = taskTemplate != null && !string.IsNullOrWhiteSpace(taskTemplate.taskName)
                    ? taskTemplate.taskName
                    : $"Task {i + 1}",
                briefDescription = taskTemplate != null && !string.IsNullOrWhiteSpace(taskTemplate.briefDescription)
                    ? taskTemplate.briefDescription
                    : "Click the shapes in the target order.",
                originalTaskNumber = i + 1,
                timeRemaining = Mathf.Max(1f, currentDifficulty.taskTimeLimit)
            };

            BuildShapeSequence(runtimeTask);
            activeTasks.Add(runtimeTask);
        }

        totalInputsRequired = taskCount * Mathf.Max(1, currentDifficulty.sequenceLength);
    }

    private void BuildShapeSequence(RuntimeTask runtimeTask)
    {
        runtimeTask.sequence.Clear();

        int sequenceLength = Mathf.Max(1, currentDifficulty.sequenceLength);
        ShapeType[] possibleShapes = GetAllShapeTypes();

        for (int i = 0; i < sequenceLength; i++)
        {
            int randomIndex = Random.Range(0, possibleShapes.Length);
            runtimeTask.sequence.Add(possibleShapes[randomIndex]);
        }
    }

    private void RefreshTaskSlots()
    {
        if (taskListCountText != null)
            taskListCountText.text = $"Task List ({activeTasks.Count})";

        if (taskSlots == null) return;

        for (int i = 0; i < taskSlots.Length; i++)
        {
            DeadlineDashTaskSlotUI taskSlot = taskSlots[i];

            if (taskSlot == null) continue;

            if (i >= activeTasks.Count)
            {
                taskSlot.Hide();
                continue;
            }

            RuntimeTask runtimeTask = activeTasks[i];
            taskSlot.Show(this, i, runtimeTask.originalTaskNumber, i == selectedTaskIndex);
        }
    }

    private void ShowSelectedTask()
    {
        if (selectedTaskIndex < 0 || selectedTaskIndex >= activeTasks.Count)
        {
            ClearTaskDetail();
            return;
        }

        RuntimeTask selectedTask = activeTasks[selectedTaskIndex];

        if (taskTitleText != null)
            taskTitleText.text = $"Task: {selectedTask.taskName}";

        if (taskDescriptionText != null)
            taskDescriptionText.text = selectedTask.briefDescription;

        UpdateTimerText();
        RefreshTargetPattern(selectedTask);
    }

    private void ClearTaskDetail()
    {
        if (taskTitleText != null)
            taskTitleText.text = "Task";

        if (taskDescriptionText != null)
            taskDescriptionText.text = "Select a task to begin.";

        ClearTargetPattern();
        UpdateTimerText();
    }

    private void RefreshTargetPattern(RuntimeTask selectedTask)
    {
        ClearTargetPattern();
        SetActivePatternGroup(currentPatternGroup);

        for (int i = 0; i < selectedTask.sequence.Count; i++)
        {
            ShapeType shape = selectedTask.sequence[i];

            Image[] targetShapeImages = currentPatternGroup != null
                ? currentPatternGroup.shapeImages
                : null;

            if (targetShapeImages != null && i < targetShapeImages.Length && targetShapeImages[i] != null)
            {
                targetShapeImages[i].sprite = GetShapeSprite(shape);
                targetShapeImages[i].enabled = targetShapeImages[i].sprite != null;
            }
        }
    }

    private void ClearTargetPattern()
    {
        ClearPatternGroup(easyPattern);
        ClearPatternGroup(mediumPattern);
        ClearPatternGroup(hardPattern);
    }

    private void ClearPatternGroup(PatternGroup patternGroup)
    {
        if (patternGroup == null) return;

        if (patternGroup.shapeImages == null) return;

        foreach (Image targetShapeImage in patternGroup.shapeImages)
        {
            if (targetShapeImage != null)
            {
                targetShapeImage.sprite = null;
                targetShapeImage.enabled = false;
            }
        }
    }

    private void ShuffleShapeButtons()
    {
        if (shapeButtons == null || shapeButtons.Length == 0) return;

        currentButtonShapes.Clear();
        currentButtonShapes.AddRange(GetAllShapeTypes());
        Shuffle(currentButtonShapes);

        for (int i = 0; i < shapeButtons.Length; i++)
        {
            DeadlineDashShapeButtonUI shapeButton = shapeButtons[i];

            if (shapeButton == null) continue;

            ShapeType shape = currentButtonShapes[i % currentButtonShapes.Count];
            shapeButton.Show(this, shape, GetShapeSprite(shape));
            shapeButton.SetInteractable(isPlaying);
        }
    }

    private void SelectNextAvailableTask()
    {
        if (activeTasks.Count <= 0)
        {
            selectedTaskIndex = -1;
            return;
        }

        selectedTaskIndex = Mathf.Clamp(selectedTaskIndex, 0, activeTasks.Count - 1);
    }

    private void UpdateTimerText()
    {
        if (timeLeftText != null)
        {
            if (!isPlaying || selectedTaskIndex < 0 || selectedTaskIndex >= activeTasks.Count)
            {
                timeLeftText.text = FormatTime(0f);
                return;
            }

            timeLeftText.text = FormatTime(activeTasks[selectedTaskIndex].timeRemaining);
        }
    }

    private void TickSelectedTaskTimer()
    {
        if (selectedTaskIndex < 0 || selectedTaskIndex >= activeTasks.Count)
        {
            UpdateTimerText();
            return;
        }

        RuntimeTask selectedTask = activeTasks[selectedTaskIndex];
        selectedTask.timeRemaining -= Time.deltaTime;
        UpdateTimerText();

        if (selectedTask.timeRemaining > 0f) return;

        wrongInputs++;
        FailSelectedTask();
    }

    private int CalculateEarnedReward()
    {
        if (totalInputsRequired <= 0 || currentDifficulty == null) return 0;

        float accuracy = (float)correctInputs / Mathf.Max(1, correctInputs + wrongInputs);
        float completion = (float)completedTasks / Mathf.Max(1, currentDifficulty.taskCount);
        float scorePercent = Mathf.Clamp01((accuracy * 0.7f) + (completion * 0.3f));

        return Mathf.RoundToInt(currentDifficulty.maxReward * scorePercent);
    }

    private int ApplyRewardLocationMultiplier(int baseReward)
    {
        if (!sleepAfterMidnightHomeWork || GameManager.Instance == null)
            return baseReward;

        return GameManager.Instance.ApplyHomeWorkRewardMultiplier(baseReward);
    }

    private void ApplyWorkConsequences()
    {
        if (GameManager.Instance == null) return;

        bool startedAtMidnight = startingDayPhase == 6;

        GameManager.Instance.ApplyActionStatsByName(workActionName);

        if (GameManager.Instance.CurrentDayPhase == startingDayPhase)
            GameManager.Instance.AdvanceTimePhase();
        else
            Debug.Log("[DEADLINE DASH] Time already advanced before minigame completion.");

        shouldReturnHomeAfterResult = returnHomeAtEndOfOfficeDay &&
            GameManager.Instance.ShouldReturnHomeFromOffice;
        shouldSleepAfterResult = sleepAfterMidnightHomeWork && startedAtMidnight;
    }

    private void StopRound()
    {
        isPlaying = false;

        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        if (resultRoutine != null)
        {
            StopCoroutine(resultRoutine);
            resultRoutine = null;
        }
    }

    private void SetGameplayGroups(bool isActive)
    {
        if (gameplayGroups == null) return;

        foreach (GameObject gameplayGroup in gameplayGroups)
        {
            if (gameplayGroup != null && gameplayGroup != gameObject)
                gameplayGroup.SetActive(isActive);
        }
    }

    private void SetResultTexts(bool isActive)
    {
        if (resultScoreText != null)
            resultScoreText.gameObject.SetActive(isActive);

        if (resultDetailsText != null)
            resultDetailsText.gameObject.SetActive(isActive);
    }

    private void SetActivePatternGroup(PatternGroup activePatternGroup)
    {
        SetPatternGroupRoot(easyPattern, easyPattern == activePatternGroup);
        SetPatternGroupRoot(mediumPattern, mediumPattern == activePatternGroup);
        SetPatternGroupRoot(hardPattern, hardPattern == activePatternGroup);
    }

    private void SetPatternGroupRoot(PatternGroup patternGroup, bool isActive)
    {
        if (patternGroup != null && patternGroup.rootObject != null)
            patternGroup.rootObject.SetActive(isActive);
    }

    private Sprite GetShapeSprite(ShapeType shape)
    {
        if (shapeVisuals == null) return null;

        foreach (ShapeVisual shapeVisual in shapeVisuals)
        {
            if (shapeVisual != null && shapeVisual.shapeType == shape)
                return shapeVisual.sprite;
        }

        return null;
    }

    private static ShapeType[] GetAllShapeTypes()
    {
        return new[]
        {
            ShapeType.Circle,
            ShapeType.Triangle,
            ShapeType.Square,
            ShapeType.Diamond,
            ShapeType.Star
        };
    }

    public static string GetShapeDisplayName(ShapeType shape)
    {
        switch (shape)
        {
            case ShapeType.Circle:
                return "Circle";
            case ShapeType.Triangle:
                return "Triangle";
            case ShapeType.Square:
                return "Square";
            case ShapeType.Diamond:
                return "Diamond";
            case ShapeType.Star:
                return "Star";
            default:
                return shape.ToString();
        }
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;

        return $"{minutes:00}:{remainingSeconds:00}";
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
