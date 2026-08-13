using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class BugBashManager : MonoBehaviour, IWorkMinigame
{
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    [Header("Minigame UI")]
    [SerializeField] private RectTransform levelObject;
    [SerializeField] private MinigameButtonInteract buttonPrefab;
    [SerializeField] private TMP_Text progressText;
    [FormerlySerializedAs("resultText")]
    [SerializeField] private TMP_Text resultScoreText;
    [SerializeField] private TMP_Text resultDetailsText;
    [SerializeField] private TMP_Text startPromptText;
    [SerializeField] private float startPromptFadeSpeed = 2f;

    [Header("Difficulty Timers")]
    [SerializeField] private float easyButtonLifetime = 3f;
    [SerializeField] private float mediumButtonLifetime = 1.5f;
    [SerializeField] private float hardButtonLifetime = 0.5f;

    [Header("Rewards")]
    [FormerlySerializedAs("buttonsToClick")]
    [SerializeField] private int totalTargets = 25;
    [SerializeField] private int easyReward = 10;
    [SerializeField] private int mediumReward = 20;
    [SerializeField] private int hardReward = 30;
    [SerializeField] private float resultDisplayDuration = 5f;

    [Header("Stat Action")]
    [SerializeField] private string workActionName = "Work";

    [Header("Events")]
    [SerializeField] private UnityEvent onMinigameClosed;

    [Header("Office Rules")]
    [SerializeField] private bool returnHomeAtEndOfOfficeDay;

    [Header("Home Rules")]
    [SerializeField] private bool sleepAfterMidnightHomeWork;

    private MinigameButtonInteract activeButton;
    private Coroutine spawnRoutine;
    private Coroutine resultRoutine;
    private float currentButtonLifetime;
    private int currentMaxReward;
    private int hitTargets;
    private int resolvedTargets;
    private float roundStartTime;
    private float finalTime;
    private int startingDayPhase;
    private bool isPlaying;
    private bool isWaitingToStart;
    private bool shouldReturnHomeAfterResult;
    private bool shouldSleepAfterResult;

    private void OnEnable()
    {
        OpenMinigame();
    }

    private void OnDisable()
    {
        StopActiveTargetGameplay();
    }

    public void OpenMinigame()
    {
        if (levelObject != null)
            levelObject.gameObject.SetActive(false);

        SetResultTexts(false);

        if (progressText != null)
            progressText.gameObject.SetActive(false);

        if (startPromptText != null)
            startPromptText.gameObject.SetActive(false);
    }

    public void StartEasy()
    {
        StartRound(Difficulty.Easy);
    }

    public void StartMedium()
    {
        StartRound(Difficulty.Medium);
    }

    public void StartHard()
    {
        StartRound(Difficulty.Hard);
    }

    private void StartRound(Difficulty difficulty)
    {
        StopCurrentRound();

        hitTargets = 0;
        resolvedTargets = 0;
        roundStartTime = 0f;
        finalTime = 0f;
        startingDayPhase = GameManager.Instance != null
            ? GameManager.Instance.CurrentDayPhase
            : -1;
        isPlaying = false;
        isWaitingToStart = true;
        shouldReturnHomeAfterResult = false;
        shouldSleepAfterResult = false;

        if (resultRoutine != null)
        {
            StopCoroutine(resultRoutine);
            resultRoutine = null;
        }

        SetResultTexts(false);

        switch (difficulty)
        {
            case Difficulty.Easy:
                currentButtonLifetime = easyButtonLifetime;
                currentMaxReward = easyReward;
                break;

            case Difficulty.Medium:
                currentButtonLifetime = mediumButtonLifetime;
                currentMaxReward = mediumReward;
                break;

            case Difficulty.Hard:
                currentButtonLifetime = hardButtonLifetime;
                currentMaxReward = hardReward;
                break;
        }

        if (levelObject != null)
            levelObject.gameObject.SetActive(true);

        if (progressText != null)
            progressText.gameObject.SetActive(false);

        if (startPromptText != null)
        {
            startPromptText.text = "Click to start";
            startPromptText.alpha = 1f;
            startPromptText.gameObject.SetActive(true);
        }

        UpdateProgressText();
        SpawnButton(true);
    }

    public void ClickSpawnedButton(MinigameButtonInteract clickedButton)
    {
        if (clickedButton != activeButton) return;

        if (isWaitingToStart)
        {
            BeginActiveRound();
            return;
        }

        if (!isPlaying) return;

        DestroyActiveButton(true);
        ResolveTarget(true);
    }

    private void BeginActiveRound()
    {
        isWaitingToStart = false;
        isPlaying = true;
        roundStartTime = Time.time;

        DestroyActiveButton(true);

        if (startPromptText != null)
            startPromptText.gameObject.SetActive(false);

        if (progressText != null)
            progressText.gameObject.SetActive(true);

        UpdateProgressText();
        SpawnButton(false);
    }

    private void SpawnButton(bool centered)
    {
        if (buttonPrefab == null || levelObject == null) return;

        activeButton = Instantiate(buttonPrefab, levelObject);
        activeButton.Setup(this);

        RectTransform buttonRect = activeButton.GetComponent<RectTransform>();
        buttonRect.anchoredPosition = centered
            ? Vector2.zero
            : GetRandomPositionInSpawnArea(buttonRect);

        if (!centered)
            spawnRoutine = StartCoroutine(ButtonLifetimeRoutine());
    }

    private void Update()
    {
        if (!isWaitingToStart || startPromptText == null) return;

        float alpha = Mathf.PingPong(Time.time * startPromptFadeSpeed, 1f);
        startPromptText.alpha = Mathf.Lerp(0.25f, 1f, alpha);
    }

    private IEnumerator ButtonLifetimeRoutine()
    {
        yield return new WaitForSeconds(currentButtonLifetime);

        if (!isPlaying) yield break;

        DestroyActiveButton(false);
        ResolveTarget(false);
    }

    private Vector2 GetRandomPositionInSpawnArea(RectTransform buttonRect)
    {
        Vector2 areaSize = levelObject.rect.size;
        Vector2 buttonSize = buttonRect.rect.size;

        float halfWidth = Mathf.Max(0f, (areaSize.x - buttonSize.x) * 0.5f);
        float halfHeight = Mathf.Max(0f, (areaSize.y - buttonSize.y) * 0.5f);

        return new Vector2(
            Random.Range(-halfWidth, halfWidth),
            Random.Range(-halfHeight, halfHeight)
        );
    }

    private void CompleteRound()
    {
        finalTime = Time.time - roundStartTime;
        int baseReward = CalculateEarnedReward();
        int earnedReward = ApplyRewardLocationMultiplier(baseReward);

        Debug.Log($"You hit {hitTargets}/{totalTargets} targets.");
        Debug.Log($"You have earned ${earnedReward}");
        Debug.Log($"Total time taken: {FormatTime(finalTime)}");

        GameManager.Instance?.ChangeMoney(earnedReward);

        if (GameManager.Instance != null)
        {
            bool startedAtMidnight = startingDayPhase == 6;

            GameManager.Instance.ApplyActionStatsByName(workActionName);

            if (GameManager.Instance.CurrentDayPhase == startingDayPhase)
                GameManager.Instance.AdvanceTimePhase();
            else
                Debug.Log("[MINIGAME] Time already advanced before minigame completion.");

            shouldReturnHomeAfterResult = returnHomeAtEndOfOfficeDay &&
                GameManager.Instance.ShouldReturnHomeFromOffice;
            shouldSleepAfterResult = sleepAfterMidnightHomeWork && startedAtMidnight;
        }

        StopCurrentRound();

        if (levelObject != null)
            levelObject.gameObject.SetActive(false);

        if (resultScoreText != null)
        {
            resultScoreText.text = $"{hitTargets}/{totalTargets}";
            resultScoreText.gameObject.SetActive(true);
        }

        if (resultDetailsText != null)
        {
            resultDetailsText.text = $"You earned ${earnedReward}\nTime taken: {FormatTime(finalTime)}";
            resultDetailsText.gameObject.SetActive(true);
        }

        if (progressText != null)
            progressText.gameObject.SetActive(false);

        resultRoutine = StartCoroutine(CloseAfterResultDelay());
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

    private void StopCurrentRound()
    {
        StopActiveTargetGameplay();

        if (resultRoutine != null)
        {
            StopCoroutine(resultRoutine);
            resultRoutine = null;
        }
    }

    private void StopActiveTargetGameplay()
    {
        isPlaying = false;
        isWaitingToStart = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        DestroyActiveButton(true);

        if (startPromptText != null)
            startPromptText.gameObject.SetActive(false);
    }

    private void DestroyActiveButton(bool stopSpawnRoutine)
    {
        if (activeButton != null)
        {
            Destroy(activeButton.gameObject);
            activeButton = null;
        }

        if (stopSpawnRoutine && spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private void UpdateProgressText()
    {
        if (progressText != null)
            progressText.text = $"{hitTargets}/{totalTargets}";
    }

    private void ResolveTarget(bool wasHit)
    {
        if (wasHit)
            hitTargets++;

        resolvedTargets++;

        if (resolvedTargets >= totalTargets)
        {
            CompleteRound();
            return;
        }

        UpdateProgressText();
        SpawnButton(false);
    }

    private int CalculateEarnedReward()
    {
        float rewardMultiplier = GetRewardMultiplier();
        return Mathf.RoundToInt(currentMaxReward * rewardMultiplier);
    }

    private int ApplyRewardLocationMultiplier(int baseReward)
    {
        if (!sleepAfterMidnightHomeWork || GameManager.Instance == null)
            return baseReward;

        return GameManager.Instance.ApplyHomeWorkRewardMultiplier(baseReward);
    }

    private float GetRewardMultiplier()
    {
        if (hitTargets >= 25)
            return 1f;

        if (hitTargets >= 20)
            return 0.8f;

        if (hitTargets >= 15)
            return 0.6f;

        if (hitTargets >= 10)
            return 0.2f;

        return 0f;
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        float remainingSeconds = seconds % 60f;

        return $"{minutes:00}:{remainingSeconds:00.00}";
    }

    private void SetResultTexts(bool isActive)
    {
        if (resultScoreText != null)
            resultScoreText.gameObject.SetActive(isActive);

        if (resultDetailsText != null)
            resultDetailsText.gameObject.SetActive(isActive);
    }
}
