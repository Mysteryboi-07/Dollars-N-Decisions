using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HouseIntroTutorialManager : MonoBehaviour
{
    [Serializable]
    public class TutorialSection
    {
        public GameObject sectionGroup;
        public TMP_Text[] textObjects;
        public string animationTriggerAfterSection;
        public float animationDurationAfterSection;
    }

    [Header("Tutorial")]
    [SerializeField] private TutorialSection[] sections;
    [SerializeField] private float lettersPerSecond = 15f;
    [SerializeField] private Button advanceButton;

    [Header("Camera Animation")]
    [SerializeField] private Animator tutorialCameraAnimator;

    private Action onTutorialFinished;
    private Coroutine tutorialRoutine;
    private bool isTyping;
    private bool skipTyping;
    private bool waitingForClick;
    private bool advanceRequested;

    private void Awake()
    {
        SetAllSectionsActive(false);
    }

    private void OnEnable()
    {
        if (advanceButton != null)
            advanceButton.onClick.AddListener(RequestAdvance);
    }

    private void OnDisable()
    {
        if (advanceButton != null)
            advanceButton.onClick.RemoveListener(RequestAdvance);
    }

    private void Update()
    {
        if (tutorialRoutine == null) return;
        if (!advanceRequested && !WasAdvancePressed()) return;

        if (isTyping)
        {
            skipTyping = true;
            advanceRequested = false;
            return;
        }

        waitingForClick = false;
        advanceRequested = false;
    }

    public void RequestAdvance()
    {
        advanceRequested = true;
    }

    public void BeginTutorial(Action finishedCallback)
    {
        if (tutorialRoutine != null) return;

        onTutorialFinished = finishedCallback;
        tutorialRoutine = StartCoroutine(PlayTutorialRoutine());
    }

    private IEnumerator PlayTutorialRoutine()
    {
        GameManager.Instance?.UnlockCursor();
        GameManager.Instance?.ShowStatsUI();
        GameManager.Instance?.SetHouseEventVisible(true);

        SetAllSectionsActive(false);

        for (int i = 0; i < sections.Length; i++)
        {
            TutorialSection section = sections[i];

            if (section == null) continue;

            yield return PlaySection(section);
            yield return PlayAnimationAfterSection(section);
        }

        tutorialRoutine = null;
        onTutorialFinished?.Invoke();
    }

    private IEnumerator PlaySection(TutorialSection section)
    {
        if (section.sectionGroup != null)
            section.sectionGroup.SetActive(true);

        SetTextsActive(section, false);

        if (section.textObjects != null)
        {
            for (int i = 0; i < section.textObjects.Length; i++)
            {
                TMP_Text textObject = section.textObjects[i];

                if (textObject == null) continue;

                textObject.gameObject.SetActive(true);
                yield return TypeText(textObject);
                yield return WaitForAdvanceClick();
            }
        }

        if (section.sectionGroup != null)
            section.sectionGroup.SetActive(false);
    }

    private IEnumerator TypeText(TMP_Text textObject)
    {
        string fullText = textObject.text;
        isTyping = true;
        skipTyping = false;

        textObject.text = string.Empty;

        if (lettersPerSecond <= 0f)
        {
            textObject.text = fullText;
        }
        else
        {
            float secondsPerLetter = 1f / lettersPerSecond;

            for (int i = 0; i < fullText.Length; i++)
            {
                if (skipTyping)
                    break;

                textObject.text = fullText.Substring(0, i + 1);
                yield return new WaitForSeconds(secondsPerLetter);
            }
        }

        textObject.text = fullText;
        isTyping = false;
        skipTyping = false;
    }

    private IEnumerator WaitForAdvanceClick()
    {
        waitingForClick = true;

        while (waitingForClick)
            yield return null;
    }

    private IEnumerator PlayAnimationAfterSection(TutorialSection section)
    {
        if (tutorialCameraAnimator == null ||
            string.IsNullOrWhiteSpace(section.animationTriggerAfterSection))
        {
            yield break;
        }

        tutorialCameraAnimator.SetTrigger(section.animationTriggerAfterSection);

        if (section.animationDurationAfterSection > 0f)
            yield return new WaitForSeconds(section.animationDurationAfterSection);
    }

    private void SetAllSectionsActive(bool isActive)
    {
        if (sections == null) return;

        foreach (TutorialSection section in sections)
        {
            if (section == null) continue;

            if (section.sectionGroup != null)
                section.sectionGroup.SetActive(isActive);
        }
    }

    private void SetTextsActive(TutorialSection section, bool isActive)
    {
        if (section.textObjects == null) return;

        foreach (TMP_Text textObject in section.textObjects)
        {
            if (textObject != null)
                textObject.gameObject.SetActive(isActive);
        }
    }

    private bool WasAdvancePressed()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        if (Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame ||
             Keyboard.current.enterKey.wasPressedThisFrame))
        {
            return true;
        }

        return false;
    }
}
