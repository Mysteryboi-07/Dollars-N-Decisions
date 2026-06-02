using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class MinigameManager : MonoBehaviour
{
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    [Header("Minigame UI")]
    [SerializeField] private GameObject difficultyButtonGroup;
    [SerializeField] private RectTransform levelObject;
    [SerializeField] private MinigameButtonInteract buttonPrefab;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text resultText;

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

    [Header("Events")]
    [SerializeField] private UnityEvent onMinigameClosed;

    [Header("Office Rules")]
    [SerializeField] private bool returnHomeAtEndOfOfficeDay;

    private MinigameButtonInteract activeButton;
    private Coroutine spawnRoutine;
    private Coroutine resultRoutine;
    private float currentButtonLifetime;
    private int currentMaxReward;
    private int hitTargets;
    private int resolvedTargets;
    private float roundStartTime;
    private float finalTime;
    private bool isPlaying;
    private bool shouldReturnHomeAfterResult;

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
        if (difficultyButtonGroup != null)
            difficultyButtonGroup.SetActive(true);

        if (levelObject != null)
            levelObject.gameObject.SetActive(false);

        if (resultText != null)
            resultText.gameObject.SetActive(false);

        if (progressText != null)
            progressText.gameObject.SetActive(false);
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
        roundStartTime = Time.time;
        finalTime = 0f;
        isPlaying = true;
        shouldReturnHomeAfterResult = false;

        if (resultRoutine != null)
        {
            StopCoroutine(resultRoutine);
            resultRoutine = null;
        }

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

        if (difficultyButtonGroup != null)
            difficultyButtonGroup.SetActive(false);

        if (levelObject != null)
            levelObject.gameObject.SetActive(true);

        if (progressText != null)
            progressText.gameObject.SetActive(true);

        UpdateProgressText();
        SpawnButton();
    }

    public void ClickSpawnedButton(MinigameButtonInteract clickedButton)
    {
        if (!isPlaying || clickedButton != activeButton) return;

        DestroyActiveButton(true);
        ResolveTarget(true);
    }

    private void SpawnButton()
    {
        if (buttonPrefab == null || levelObject == null) return;

        activeButton = Instantiate(buttonPrefab, levelObject);
        activeButton.Setup(this);

        RectTransform buttonRect = activeButton.GetComponent<RectTransform>();
        buttonRect.anchoredPosition = GetRandomPositionInSpawnArea(buttonRect);

        spawnRoutine = StartCoroutine(ButtonLifetimeRoutine());
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
        int earnedReward = CalculateEarnedReward();

        Debug.Log($"You hit {hitTargets}/{totalTargets} targets.");
        Debug.Log($"You have earned ${earnedReward}");
        Debug.Log($"Total time taken: {FormatTime(finalTime)}");

        GameManager.Instance?.ChangeMoney(earnedReward);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AdvanceTimePhase();
            shouldReturnHomeAfterResult = returnHomeAtEndOfOfficeDay &&
                GameManager.Instance.ShouldReturnHomeFromOffice;
        }

        StopCurrentRound();

        if (levelObject != null)
            levelObject.gameObject.SetActive(false);

        if (resultText != null)
        {
            resultText.text = $"Hits: {hitTargets}/{totalTargets}\nYou earned ${earnedReward}\nTime taken: {FormatTime(finalTime)}";
            resultText.gameObject.SetActive(true);
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

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        DestroyActiveButton(true);
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
        SpawnButton();
    }

    private int CalculateEarnedReward()
    {
        float rewardMultiplier = GetRewardMultiplier();
        return Mathf.RoundToInt(currentMaxReward * rewardMultiplier);
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
}
