using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InboxEmailSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text emailNumberText;
    [SerializeField] private GameObject unreadPanel;
    [SerializeField] private Button button;

    private InboxTriageManager inboxManager;
    private int emailIndex;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    public void Show(InboxTriageManager manager, int index, int displayNumber, bool isSelected)
    {
        inboxManager = manager;
        emailIndex = index;
        gameObject.SetActive(true);

        if (emailNumberText != null)
            emailNumberText.text = $"Email #{displayNumber:00}";

        SetSelected(isSelected);

        if (button != null)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(SelectEmail);
            button.interactable = true;
        }
    }

    public void Hide()
    {
        inboxManager = null;
        gameObject.SetActive(false);
        SetSelected(false);

        if (button != null)
            button.onClick = new Button.ButtonClickedEvent();
    }

    public void SetSelected(bool isSelected)
    {
        if (unreadPanel != null)
            unreadPanel.SetActive(!isSelected);
    }

    private void SelectEmail()
    {
        inboxManager?.SelectEmail(emailIndex);
    }
}
