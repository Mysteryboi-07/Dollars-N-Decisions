using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartingButtons : MonoBehaviour
{

    [Header("Camera Animator")]
    [SerializeField] private Animator cameraAnimator;

    [Header("Animation Triggers")]
    [SerializeField] private string part1Trigger = "Part1";
    [SerializeField] private string part2Trigger = "Part2";

    [Header("Animation Timing")]
    [SerializeField] private float part1Duration = 1.5f;
    [SerializeField] private float part2Duration = 1.5f;

    private bool isTransitioning;

    public void ContinueAfterName()
    {
        if (isTransitioning) return;

        StartCoroutine(PlayPart2ThenLoadGame());
    }

    private IEnumerator PlayPart1ThenShowName()
    {
        isTransitioning = true;

        cameraAnimator.SetTrigger(part1Trigger);

        yield return new WaitForSeconds(part1Duration);

        UIManager.Instance?.ShowNamePanel();

        isTransitioning = false;
    }

    private IEnumerator PlayPart2ThenLoadGame()
    {
        isTransitioning = true;

        cameraAnimator.SetTrigger(part2Trigger);

        yield return new WaitForSeconds(part2Duration);

        SceneManager.LoadScene("GameScene");
    }
}