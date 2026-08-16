using UnityEngine;
using UnityEngine.UI;

public class MinigameTutorialManager : MonoBehaviour
{
    public static MinigameTutorialManager Instance { get; private set; }

    [Header("Tutorial")]
    [SerializeField] private GameObject tutorialGroup;
    [SerializeField] private GameObject[] tutorialPages;

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button endButton;

    private static bool hasCompletedTutorial;
    private int currentPageIndex;

    private void Awake()
    {
        Instance = this;
        BindButtons();
        HideTutorial();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void ShowIfNeeded()
    {
        if (hasCompletedTutorial || Instance == null) return;

        Instance.ShowTutorial();
    }

    public static void ResetTutorialForNewRun()
    {
        hasCompletedTutorial = false;
    }

    public void ShowTutorial()
    {
        if (hasCompletedTutorial) return;

        if (tutorialGroup != null)
            tutorialGroup.SetActive(true);

        GameManager.Instance?.UnlockCursor();
        ShowPage(0);
    }

    public void ShowPreviousPage()
    {
        ShowPage(currentPageIndex - 1);
    }

    public void ShowNextPage()
    {
        ShowPage(currentPageIndex + 1);
    }

    public void EndTutorial()
    {
        hasCompletedTutorial = true;
        HideTutorial();
    }

    private void ShowPage(int pageIndex)
    {
        if (tutorialPages == null || tutorialPages.Length == 0) return;

        currentPageIndex = Mathf.Clamp(pageIndex, 0, tutorialPages.Length - 1);

        for (int i = 0; i < tutorialPages.Length; i++)
        {
            if (tutorialPages[i] != null)
                tutorialPages[i].SetActive(i == currentPageIndex);
        }

        RefreshButtons();
    }

    private void HideTutorial()
    {
        if (tutorialPages != null)
        {
            foreach (GameObject tutorialPage in tutorialPages)
            {
                if (tutorialPage != null)
                    tutorialPage.SetActive(false);
            }
        }

        if (tutorialGroup != null)
            tutorialGroup.SetActive(false);
    }

    private void RefreshButtons()
    {
        bool isFirstPage = currentPageIndex <= 0;
        bool isLastPage = tutorialPages != null && currentPageIndex >= tutorialPages.Length - 1;

        if (backButton != null)
            backButton.gameObject.SetActive(!isFirstPage);

        if (nextButton != null)
            nextButton.gameObject.SetActive(!isLastPage);

        if (endButton != null)
            endButton.gameObject.SetActive(isLastPage);
    }

    private void BindButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(ShowPreviousPage);
            backButton.onClick.AddListener(ShowPreviousPage);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(ShowNextPage);
            nextButton.onClick.AddListener(ShowNextPage);
        }

        if (endButton != null)
        {
            endButton.onClick.RemoveListener(EndTutorial);
            endButton.onClick.AddListener(EndTutorial);
        }
    }
}
