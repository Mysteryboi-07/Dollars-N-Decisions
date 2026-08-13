using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeadlineDashTaskSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text taskText;
    [SerializeField] private GameObject selectedPanel;
    [SerializeField] private Button button;

    private DeadlineDashManager deadlineDashManager;
    private int taskIndex;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    public void Show(DeadlineDashManager manager, int index, int displayNumber, bool isSelected)
    {
        deadlineDashManager = manager;
        taskIndex = index;
        gameObject.SetActive(true);

        if (taskText != null)
            taskText.text = $"Task #{displayNumber:00}";

        SetSelected(isSelected);

        if (button != null)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(SelectTask);
            button.interactable = true;
        }
    }

    public void Hide()
    {
        deadlineDashManager = null;
        gameObject.SetActive(false);
        SetSelected(false);

        if (button != null)
            button.onClick = new Button.ButtonClickedEvent();
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedPanel != null)
            selectedPanel.SetActive(isSelected);
    }

    private void SelectTask()
    {
        deadlineDashManager?.SelectTask(taskIndex);
    }
}
