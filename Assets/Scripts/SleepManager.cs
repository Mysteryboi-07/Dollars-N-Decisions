using System.Collections;
using UnityEngine;

public class SleepManager : MonoBehaviour
{
    public static SleepManager Instance { get; private set; }

    [Header("Fade")]
    [SerializeField] private CanvasGroup sleepFadeGroup;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float holdDuration = 2f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Office Return")]
    [SerializeField] private bool useSceneTravelForOfficeReturn = true;
    [SerializeField] private string outsideSceneName = "SampleScene";

    private Coroutine activeRoutine;

    public bool IsBusy => activeRoutine != null;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (sleepFadeGroup == null) return;

        sleepFadeGroup.alpha = 0f;
        sleepFadeGroup.gameObject.SetActive(false);
    }

    public void GoToSleep()
    {
        if (activeRoutine != null) return;

        activeRoutine = StartCoroutine(SleepRoutine());
    }

    public void ReturnHomeFromOffice()
    {
        if (activeRoutine != null) return;

        activeRoutine = StartCoroutine(ReturnHomeFromOfficeRoutine());
    }

    public void SleepAfterLateHomeWork()
    {
        if (activeRoutine != null) return;

        activeRoutine = StartCoroutine(SleepAfterLateHomeWorkRoutine());
    }

    private IEnumerator SleepRoutine()
    {
        yield return FadeOutThenIn(() => GameManager.Instance?.WakeUp());
        activeRoutine = null;
        InteractionUIManager.Instance?.RefreshPrompt();
        Debug.Log("[SLEEP] Finished sleeping.");
    }

    private IEnumerator ReturnHomeFromOfficeRoutine()
    {
        yield return FadeInAndHold();

        GameManager.Instance?.ClockOutOffice();
        GameManager.Instance?.LockCursor();

        if (useSceneTravelForOfficeReturn)
        {
            SceneTravelManager.Instance?.LoadScene(outsideSceneName);
            activeRoutine = null;
            yield break;
        }

        yield return FadeOut();
        GameManager.Instance?.ShowStatsUI();
        GameManager.Instance?.LockCursor();
        activeRoutine = null;
        Debug.Log("[OFFICE] Work day ended.");
    }

    private IEnumerator SleepAfterLateHomeWorkRoutine()
    {
        yield return FadeOutThenIn(() =>
        {
            FocusViewManager.Instance?.CloseLaptop();
            GameManager.Instance?.WakeUpAtPhase(2);
        });

        activeRoutine = null;
        Debug.Log("[HOME WORK] Worked past midnight. Woke up at 1200.");
    }

    private IEnumerator FadeOutThenIn(System.Action middleAction)
    {
        yield return FadeInAndHold();
        middleAction?.Invoke();
        yield return FadeOut();
    }

    private IEnumerator FadeInAndHold()
    {
        if (sleepFadeGroup == null)
        {
            Debug.LogWarning("[SLEEP] No sleep fade CanvasGroup assigned.");
            yield break;
        }

        sleepFadeGroup.gameObject.SetActive(true);
        yield return FadeSleepOverlay(0f, 1f, fadeInDuration);
        yield return new WaitForSeconds(holdDuration);
    }

    private IEnumerator FadeOut()
    {
        if (sleepFadeGroup == null)
            yield break;

        yield return FadeSleepOverlay(1f, 0f, fadeOutDuration);
        sleepFadeGroup.gameObject.SetActive(false);
    }

    private IEnumerator FadeSleepOverlay(float startAlpha, float endAlpha, float duration)
    {
        if (duration <= 0f)
        {
            sleepFadeGroup.alpha = endAlpha;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float fadePercent = Mathf.Clamp01(elapsedTime / duration);
            sleepFadeGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, fadePercent);
            yield return null;
        }

        sleepFadeGroup.alpha = endAlpha;
    }
}
