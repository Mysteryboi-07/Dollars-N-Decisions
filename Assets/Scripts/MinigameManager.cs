using UnityEngine;
using UnityEngine.Events;

public class MinigameManager : MonoBehaviour
{
    private enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    [System.Serializable]
    public class WorkMinigame
    {
        public string minigameName;
        public GameObject minigameObject;
    }

    [Header("UI")]
    [SerializeField] private GameObject difficultyButtonGroup;

    [Header("Selection Rules")]
    [SerializeField] private bool launchOnEnable = true;
    [SerializeField] private WorkMinigame[] firstRunOrder;
    [SerializeField] private WorkMinigame[] randomPool;

    [Header("Events")]
    [SerializeField] private UnityEvent onMinigameFinished;

    private static int sharedCompletedWorkSessions;
    private GameObject activeMinigame;

    private void OnEnable()
    {
        if (launchOnEnable)
            LaunchNextMinigame();
    }

    private void OnDisable()
    {
        SetDifficultyButtons(false);
        HideAllMinigames();
    }

    public void LaunchNextMinigame()
    {
        HideAllMinigames();

        WorkMinigame selectedMinigame = GetNextMinigame();

        if (selectedMinigame == null || selectedMinigame.minigameObject == null)
        {
            Debug.LogWarning("[WORK] No minigame available to launch.");
            return;
        }

        activeMinigame = selectedMinigame.minigameObject;
        ActivateMinigamePath(activeMinigame);
        SetDifficultyButtons(true);

        Debug.Log($"[WORK] Launched {selectedMinigame.minigameName}. Shared session {sharedCompletedWorkSessions}.");
    }

    public void StartEasy()
    {
        StartActiveMinigame(Difficulty.Easy);
    }

    public void StartMedium()
    {
        StartActiveMinigame(Difficulty.Medium);
    }

    public void StartHard()
    {
        StartActiveMinigame(Difficulty.Hard);
    }

    public void NotifyMinigameFinished()
    {
        sharedCompletedWorkSessions++;
        SetDifficultyButtons(false);
        HideAllMinigames();
        onMinigameFinished?.Invoke();
    }

    public void ResetWorkSessionOrder()
    {
        sharedCompletedWorkSessions = 0;
    }

    private WorkMinigame GetNextMinigame()
    {
        WorkMinigame guidedMinigame = GetGuidedMinigame();

        if (guidedMinigame != null)
            return guidedMinigame;

        return GetRandomMinigame();
    }

    private WorkMinigame GetGuidedMinigame()
    {
        if (firstRunOrder == null || sharedCompletedWorkSessions >= firstRunOrder.Length)
            return null;

        WorkMinigame guidedMinigame = firstRunOrder[sharedCompletedWorkSessions];

        if (IsValidMinigame(guidedMinigame))
            return guidedMinigame;

        return null;
    }

    private WorkMinigame GetRandomMinigame()
    {
        if (randomPool == null || randomPool.Length == 0)
            return null;

        int validCount = 0;

        foreach (WorkMinigame minigame in randomPool)
        {
            if (IsValidMinigame(minigame))
                validCount++;
        }

        if (validCount <= 0)
            return null;

        int selectedIndex = Random.Range(0, validCount);
        int currentIndex = 0;

        foreach (WorkMinigame minigame in randomPool)
        {
            if (!IsValidMinigame(minigame)) continue;

            if (currentIndex == selectedIndex)
                return minigame;

            currentIndex++;
        }

        return null;
    }

    private void HideAllMinigames()
    {
        SetMinigamesActive(firstRunOrder, false);
        SetMinigamesActive(randomPool, false);
        activeMinigame = null;
    }

    private void SetMinigamesActive(WorkMinigame[] minigames, bool isActive)
    {
        if (minigames == null) return;

        foreach (WorkMinigame minigame in minigames)
        {
            if (minigame != null && minigame.minigameObject != null)
                minigame.minigameObject.SetActive(isActive);
        }
    }

    private bool IsValidMinigame(WorkMinigame minigame)
    {
        return minigame != null && minigame.minigameObject != null;
    }

    private void StartActiveMinigame(Difficulty difficulty)
    {
        ActivateMinigamePath(activeMinigame);

        if (activeMinigame != null && !activeMinigame.activeInHierarchy)
        {
            Debug.LogWarning("[WORK] Active minigame is still inactive in the hierarchy. Check disabled parent objects.");
            return;
        }

        IWorkMinigame activeController = GetActiveMinigameController();

        if (activeController == null)
        {
            Debug.LogWarning("[WORK] Active minigame does not have a work minigame controller.");
            return;
        }

        switch (difficulty)
        {
            case Difficulty.Easy:
                activeController.StartEasy();
                break;

            case Difficulty.Medium:
                activeController.StartMedium();
                break;

            case Difficulty.Hard:
                activeController.StartHard();
                break;
        }

        SetDifficultyButtons(false);
    }

    private void ActivateMinigamePath(GameObject minigameObject)
    {
        if (minigameObject == null) return;

        if (!minigameObject.transform.IsChildOf(transform) && minigameObject.transform != transform)
        {
            minigameObject.SetActive(true);
            Debug.LogWarning("[WORK] Active minigame is not under this MinigameManager object.");
            return;
        }

        Transform current = minigameObject.transform;

        while (current != null)
        {
            current.gameObject.SetActive(true);

            if (current == transform)
                return;

            current = current.parent;
        }
    }

    private IWorkMinigame GetActiveMinigameController()
    {
        if (activeMinigame == null)
            return null;

        MonoBehaviour[] behaviours = activeMinigame.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IWorkMinigame workMinigame)
                return workMinigame;
        }

        return null;
    }

    private void SetDifficultyButtons(bool isActive)
    {
        if (difficultyButtonGroup != null)
        {
            if (!isActive && activeMinigame != null && activeMinigame.transform.IsChildOf(difficultyButtonGroup.transform))
            {
                Debug.LogWarning("[WORK] Difficulty Button Group contains the active minigame, so it was not disabled.");
                return;
            }

            difficultyButtonGroup.SetActive(isActive);
        }
    }
}
