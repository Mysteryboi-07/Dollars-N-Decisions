using UnityEngine;
using UnityEngine.UI;

public class PlayingCardSlotUI : MonoBehaviour
{
    [Header("Card")]
    [SerializeField] private Image cardImage;
    [SerializeField] private float keptDropPixels = 10f;

    [Header("Click")]
    [SerializeField] private Button button;

    private DoubleOrNothingManager manager;
    private RectTransform rectTransform;
    private Vector2 startingAnchoredPosition;
    private int slotIndex;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
            startingAnchoredPosition = rectTransform.anchoredPosition;

        if (button == null)
            button = GetComponent<Button>();
    }

    public void Setup(DoubleOrNothingManager owningManager, int index)
    {
        manager = owningManager;
        slotIndex = index;

        if (button != null)
        {
            button.onClick.RemoveListener(ClickSlot);
            button.onClick.AddListener(ClickSlot);
        }
    }

    public void Show(DoubleOrNothingManager.PlayingCard card, bool isKept, bool canToggle, Sprite sprite)
    {
        gameObject.SetActive(true);

        if (cardImage != null)
        {
            cardImage.sprite = sprite;
            cardImage.enabled = sprite != null;
        }

        SetDropped(isKept);

        if (button != null)
            button.interactable = canToggle;
    }

    public void ShowBack(Sprite cardBackSprite, bool canClick = false)
    {
        gameObject.SetActive(true);

        if (cardImage != null)
        {
            cardImage.sprite = cardBackSprite;
            cardImage.enabled = cardBackSprite != null;
        }

        SetDropped(false);

        if (button != null)
            button.interactable = canClick;
    }

    public void Hide()
    {
        if (cardImage != null)
        {
            cardImage.sprite = null;
            cardImage.enabled = false;
        }

        SetDropped(false);

        if (button != null)
            button.interactable = false;
    }

    public void SetInteractable(bool isInteractable)
    {
        if (button != null)
            button.interactable = isInteractable;
    }

    private void ClickSlot()
    {
        manager?.ToggleKeepCard(slotIndex);
    }

    private void SetDropped(bool isDropped)
    {
        if (rectTransform == null) return;

        rectTransform.anchoredPosition = startingAnchoredPosition + (isDropped
            ? new Vector2(0f, -keptDropPixels)
            : Vector2.zero);
    }
}
