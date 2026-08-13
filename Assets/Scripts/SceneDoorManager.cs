using TMPro;
using UnityEngine;

public class SceneDoorManager : MonoBehaviour
{
    public static SceneDoorManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public bool TryUseSceneDoor(InteractableTrigger door, TMP_Text promptText)
    {
        if (door == null) return false;

        if (door.RequireOfficeEntryAllowed &&
            GameManager.Instance != null &&
            !GameManager.Instance.CanEnterOffice)
        {
            if (promptText != null)
            {
                promptText.text = GameManager.Instance.OfficeEntryBlockedMessage;
                promptText.gameObject.SetActive(true);
            }

            Debug.Log($"[DOOR] {GameManager.Instance.OfficeEntryBlockedMessage}.");
            return false;
        }

        if (door.ClockOutBeforeTravel)
            GameManager.Instance?.ClockOutOffice();

        if (door.ClockInBeforeTravel)
            GameManager.Instance?.ClockInOffice();

        if (SceneTravelManager.Instance == null)
        {
            Debug.LogWarning("[SCENE] No SceneTravelManager found.");
            return false;
        }

        SceneTravelManager.Instance.LoadScene(door.TargetSceneName);
        return true;
    }
}
