using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class InboxTriageManager : MonoBehaviour, IWorkMinigame
{
    public enum EmailCategory
    {
        Urgent,
        FollowUp,
        Information,
        Spam
    }

    [System.Serializable]
    public class EmailTemplate
    {
        public string sender;
        [TextArea(3, 8)] public string body;
        public EmailCategory correctCategory;
    }

    [System.Serializable]
    public class DifficultySettings
    {
        public int emailCount = 4;
        public float emailTimeLimit = 15f;
        public int maxReward = 10;
    }

    private class RuntimeEmail
    {
        public EmailTemplate template;
        public int originalEmailNumber;
        public float timeRemaining;
    }

    [Header("Minigame UI")]
    [SerializeField] private GameObject[] gameplayGroups;
    [SerializeField] private InboxEmailSlotUI[] emailSlots;
    [FormerlySerializedAs("resultText")]
    [SerializeField] private TMP_Text resultScoreText;
    [SerializeField] private TMP_Text resultDetailsText;

    [Header("Preview UI")]
    [FormerlySerializedAs("subjectText")]
    [SerializeField] private TMP_Text emailNumberText;
    [SerializeField] private TMP_Text emailSenderText;
    [FormerlySerializedAs("bodyText")]
    [SerializeField] private TMP_Text emailContentText;
    [FormerlySerializedAs("timerText")]
    [SerializeField] private TMP_Text emailTimerText;

    [Header("Emails")]
    [SerializeField] private EmailTemplate[] emailPool;
    [SerializeField] private bool shuffleEmails = true;

    [Header("Difficulty")]
    [SerializeField] private DifficultySettings easy = new DifficultySettings { emailCount = 4, emailTimeLimit = 15f, maxReward = 10 };
    [SerializeField] private DifficultySettings medium = new DifficultySettings { emailCount = 5, emailTimeLimit = 10f, maxReward = 20 };
    [SerializeField] private DifficultySettings hard = new DifficultySettings { emailCount = 7, emailTimeLimit = 5f, maxReward = 30 };

    [Header("Result")]
    [SerializeField] private float resultDisplayDuration = 5f;
    [SerializeField] private UnityEvent onMinigameClosed;

    [Header("Stat Action")]
    [SerializeField] private string workActionName = "Work";

    [Header("Office Rules")]
    [SerializeField] private bool returnHomeAtEndOfOfficeDay;

    [Header("Home Rules")]
    [SerializeField] private bool sleepAfterMidnightHomeWork;

    private readonly List<RuntimeEmail> activeEmails = new List<RuntimeEmail>();
    private Coroutine timerRoutine;
    private Coroutine resultRoutine;
    private DifficultySettings currentDifficulty;
    private int selectedEmailIndex = -1;
    private int correctCount;
    private int wrongCount;
    private int sortedCount;
    private int totalEmailsThisRound;
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

        ClearPreview();
        RefreshSlots();
    }

    public void StartEasy()
    {
        StartRound(easy);
    }

    public void StartMedium()
    {
        StartRound(medium);
    }

    public void StartHard()
    {
        StartRound(hard);
    }

    public void SelectEmail(int emailIndex)
    {
        if (!isPlaying || emailIndex < 0 || emailIndex >= activeEmails.Count) return;
        selectedEmailIndex = emailIndex;
        ShowPreview(activeEmails[emailIndex].template);
    }

    public void SortAsUrgent()
    {
        SortSelectedEmail(EmailCategory.Urgent);
    }

    public void SortAsFollowUp()
    {
        SortSelectedEmail(EmailCategory.FollowUp);
    }

    public void SortAsInformation()
    {
        SortSelectedEmail(EmailCategory.Information);
    }

    public void SortAsSpam()
    {
        SortSelectedEmail(EmailCategory.Spam);
    }

    private void Update()
    {
        if (!isPlaying) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            SortAsUrgent();
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            SortAsFollowUp();
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            SortAsInformation();
        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
            SortAsSpam();
    }

    private void StartRound(DifficultySettings difficulty)
    {
        StopRound();

        currentDifficulty = difficulty;
        selectedEmailIndex = -1;
        correctCount = 0;
        wrongCount = 0;
        sortedCount = 0;
        totalEmailsThisRound = 0;
        shouldReturnHomeAfterResult = false;
        shouldSleepAfterResult = false;
        startingDayPhase = GameManager.Instance != null ? GameManager.Instance.CurrentDayPhase : -1;
        isPlaying = true;

        BuildEmailList(currentDifficulty.emailCount);
        totalEmailsThisRound = activeEmails.Count;

        SetGameplayGroups(true);
        SetResultTexts(false);

        ClearPreview();
        RefreshSlots();
        UpdateEmailTimerText();

        if (activeEmails.Count > 0)
            SelectEmail(0);

        timerRoutine = StartCoroutine(TimerRoutine());
    }

    private void SortSelectedEmail(EmailCategory selectedCategory)
    {
        if (!isPlaying || selectedEmailIndex < 0 || selectedEmailIndex >= activeEmails.Count) return;

        RuntimeEmail runtimeEmail = activeEmails[selectedEmailIndex];

        sortedCount++;

        if (runtimeEmail.template.correctCategory == selectedCategory)
            correctCount++;
        else
            wrongCount++;

        activeEmails.RemoveAt(selectedEmailIndex);
        SelectNextAvailableEmail();

        RefreshSlots();

        if (activeEmails.Count <= 0)
        {
            CompleteRound();
            return;
        }

        ShowPreview(activeEmails[selectedEmailIndex].template);
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

        if (resultScoreText != null)
        {
            resultScoreText.text = $"{correctCount}/{totalEmailsThisRound}";
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
            TickSelectedEmailTimer();
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

    private void BuildEmailList(int emailCount)
    {
        activeEmails.Clear();

        if (emailPool == null || emailPool.Length == 0) return;

        List<EmailTemplate> availableEmails = new List<EmailTemplate>();

        foreach (EmailTemplate emailTemplate in emailPool)
        {
            if (emailTemplate != null)
                availableEmails.Add(emailTemplate);
        }

        if (shuffleEmails)
            Shuffle(availableEmails);

        int count = Mathf.Min(emailCount, availableEmails.Count);

        for (int i = 0; i < count; i++)
            activeEmails.Add(new RuntimeEmail
            {
                template = availableEmails[i],
                originalEmailNumber = i + 1,
                timeRemaining = Mathf.Max(1f, currentDifficulty.emailTimeLimit)
            });
    }

    private void RefreshSlots()
    {
        if (emailSlots == null) return;

        for (int i = 0; i < emailSlots.Length; i++)
        {
            InboxEmailSlotUI emailSlot = emailSlots[i];

            if (emailSlot == null) continue;

            if (i >= activeEmails.Count)
            {
                emailSlot.Hide();
                continue;
            }

            emailSlot.Show(this, i, activeEmails[i].originalEmailNumber, i == selectedEmailIndex);
        }
    }

    private void ShowPreview(EmailTemplate emailTemplate)
    {
        if (emailNumberText != null)
            emailNumberText.text = $"Email #{activeEmails[selectedEmailIndex].originalEmailNumber:00}";

        if (emailSenderText != null)
            emailSenderText.text = emailTemplate.sender;

        if (emailContentText != null)
        {
            emailContentText.richText = true;
            emailContentText.text = emailTemplate.body;
        }

        UpdateEmailTimerText();
        RefreshSlotSelection();
    }

    private void ClearPreview()
    {
        if (emailNumberText != null)
            emailNumberText.text = "Email #--";

        if (emailSenderText != null)
            emailSenderText.text = "";

        if (emailContentText != null)
        {
            emailContentText.richText = true;
            emailContentText.text = "Select an email to read.";
        }

        UpdateEmailTimerText();
        RefreshSlotSelection();
    }

    private void TickSelectedEmailTimer()
    {
        if (selectedEmailIndex < 0 || selectedEmailIndex >= activeEmails.Count)
        {
            UpdateEmailTimerText();
            return;
        }

        RuntimeEmail runtimeEmail = activeEmails[selectedEmailIndex];

        runtimeEmail.timeRemaining -= Time.deltaTime;
        UpdateEmailTimerText();

        if (runtimeEmail.timeRemaining > 0f) return;

        sortedCount++;
        wrongCount++;

        activeEmails.RemoveAt(selectedEmailIndex);
        SelectNextAvailableEmail();

        RefreshSlots();

        if (activeEmails.Count <= 0)
        {
            CompleteRound();
            return;
        }

        ShowPreview(activeEmails[selectedEmailIndex].template);
    }

    private void RefreshSlotSelection()
    {
        if (emailSlots == null) return;

        for (int i = 0; i < emailSlots.Length; i++)
        {
            if (emailSlots[i] != null)
                emailSlots[i].SetSelected(i == selectedEmailIndex);
        }
    }

    private void SelectNextAvailableEmail()
    {
        if (activeEmails.Count <= 0)
        {
            selectedEmailIndex = -1;
            return;
        }

        selectedEmailIndex = Mathf.Clamp(selectedEmailIndex, 0, activeEmails.Count - 1);
    }

    private void UpdateEmailTimerText()
    {
        if (emailTimerText == null) return;

        if (!isPlaying || selectedEmailIndex < 0 || selectedEmailIndex >= activeEmails.Count)
        {
            emailTimerText.text = "";
            return;
        }

        float selectedTimeRemaining = activeEmails[selectedEmailIndex].timeRemaining;
        emailTimerText.text = Mathf.CeilToInt(Mathf.Max(0f, selectedTimeRemaining)).ToString();
    }

    private int CalculateEarnedReward()
    {
        if (totalEmailsThisRound <= 0 || currentDifficulty == null) return 0;

        float accuracy = (float)correctCount / totalEmailsThisRound;
        float completion = (float)sortedCount / totalEmailsThisRound;
        float scorePercent = Mathf.Clamp01((accuracy * 0.8f) + (completion * 0.2f));

        return Mathf.RoundToInt(currentDifficulty.maxReward * scorePercent);
    }

    private int ApplyRewardLocationMultiplier(int baseReward)
    {
        if (GameManager.Instance == null)
            return baseReward;

        return GameManager.Instance.ApplyIncomeModifiers(baseReward, sleepAfterMidnightHomeWork);
    }

    private void ApplyWorkConsequences()
    {
        if (GameManager.Instance == null) return;

        bool startedAtMidnight = startingDayPhase == 6;
        int phaseCost = GameManager.Instance.GetActionPhaseCost();

        GameManager.Instance.ApplyActionStatsByName(workActionName);

        if (GameManager.Instance.CurrentDayPhase == startingDayPhase)
            GameManager.Instance.AdvanceTimePhases(phaseCost);
        else
            Debug.Log("[INBOX TRIAGE] Time already advanced before minigame completion.");

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

    private void Shuffle(List<EmailTemplate> emailTemplates)
    {
        for (int i = 0; i < emailTemplates.Count; i++)
        {
            int randomIndex = Random.Range(i, emailTemplates.Count);
            EmailTemplate temp = emailTemplates[i];
            emailTemplates[i] = emailTemplates[randomIndex];
            emailTemplates[randomIndex] = temp;
        }
    }
}
