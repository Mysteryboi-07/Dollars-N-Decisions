using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EndingSceneManager : MonoBehaviour
{
    [System.Serializable]
    private class EndingScreen
    {
#pragma warning disable 0649
        public GameObject panel;
        public TMP_Text titleText;
        public TMP_Text titleShadowText;
        public TMP_Text subtitleText;
        public TMP_Text subtitleShadowText;
        public Button restartButton;
        public Button exitButton;
        public GameObject extraRevealGroup;
#pragma warning restore 0649
    }

    [Header("Panels")]
    [SerializeField] private EndingScreen winScreen;
    [SerializeField] private EndingScreen loseScreen;

    [Header("Typewriter")]
    [SerializeField] private float charactersPerSecond = 15f;

    [Header("Restart")]
    [SerializeField] private string introSceneName = "TestingScene";

    private void Start()
    {
        bool didWin = GameManager.Instance != null && GameManager.Instance.EndingWasWin;

        BindButtons(winScreen);
        BindButtons(loseScreen);
        SetScreenActive(winScreen, didWin);
        SetScreenActive(loseScreen, !didWin);

        StartCoroutine(PlayEndingText(didWin ? winScreen : loseScreen));

        GameManager.Instance?.UnlockCursor();
    }

    public void RestartGame()
    {
        GameManager.Instance?.ResetRunProgress();

        if (SceneTravelManager.Instance != null)
            SceneTravelManager.Instance.LoadScene(introSceneName);
        else
            SceneManager.LoadScene(introSceneName);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator PlayEndingText(EndingScreen screen)
    {
        if (screen == null) yield break;

        string title = GetSourceText(screen.titleText, screen.titleShadowText);
        string subtitle = GetSourceText(screen.subtitleText, screen.subtitleShadowText);

        SetInteractables(screen, false);

        if (screen.extraRevealGroup != null)
            screen.extraRevealGroup.SetActive(false);

        if (screen.titleText != null)
            screen.titleText.text = string.Empty;

        if (screen.titleShadowText != null)
            screen.titleShadowText.text = string.Empty;

        if (screen.subtitleText != null)
            screen.subtitleText.text = string.Empty;

        if (screen.subtitleShadowText != null)
            screen.subtitleShadowText.text = string.Empty;

        yield return TypeText(screen.titleText, screen.titleShadowText, title);
        yield return TypeText(screen.subtitleText, screen.subtitleShadowText, subtitle);

        if (screen.extraRevealGroup != null)
            screen.extraRevealGroup.SetActive(true);

        SetInteractables(screen, true);
    }

    private IEnumerator TypeText(TMP_Text text, TMP_Text shadowText, string fullText)
    {
        if (text == null && shadowText == null)
            yield break;

        if (string.IsNullOrEmpty(fullText))
            yield break;

        float secondsPerCharacter = charactersPerSecond > 0f ? 1f / charactersPerSecond : 0f;

        for (int i = 1; i <= fullText.Length; i++)
        {
            string visibleText = fullText.Substring(0, i);

            if (text != null)
                text.text = visibleText;

            if (shadowText != null)
                shadowText.text = visibleText;

            if (secondsPerCharacter > 0f)
                yield return new WaitForSeconds(secondsPerCharacter);
        }
    }

    private string GetSourceText(TMP_Text mainText, TMP_Text shadowText)
    {
        if (mainText != null && !string.IsNullOrEmpty(mainText.text))
            return mainText.text;

        return shadowText != null ? shadowText.text : string.Empty;
    }

    private void SetScreenActive(EndingScreen screen, bool isActive)
    {
        if (screen?.panel != null)
            screen.panel.SetActive(isActive);
    }

    private void SetInteractables(EndingScreen screen, bool isInteractable)
    {
        if (screen == null) return;

        if (screen.restartButton != null)
            screen.restartButton.interactable = isInteractable;

        if (screen.exitButton != null)
            screen.exitButton.interactable = isInteractable;
    }

    private void BindButtons(EndingScreen screen)
    {
        if (screen == null) return;

        if (screen.restartButton != null)
        {
            screen.restartButton.onClick.RemoveListener(RestartGame);
            screen.restartButton.onClick.AddListener(RestartGame);
        }

        if (screen.exitButton != null)
        {
            screen.exitButton.onClick.RemoveListener(ExitGame);
            screen.exitButton.onClick.AddListener(ExitGame);
        }
    }
}
