using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeadlineDashShapeButtonUI : MonoBehaviour
{
    [SerializeField] private TMP_Text shapeNameText;
    [SerializeField] private Image shapeImage;
    [SerializeField] private Button button;

    private DeadlineDashManager deadlineDashManager;
    private DeadlineDashManager.ShapeType shapeType;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    public void Show(DeadlineDashManager manager, DeadlineDashManager.ShapeType shape, Sprite shapeSprite)
    {
        deadlineDashManager = manager;
        shapeType = shape;
        gameObject.SetActive(true);

        if (shapeNameText != null)
            shapeNameText.text = DeadlineDashManager.GetShapeDisplayName(shape);

        if (shapeImage != null)
        {
            shapeImage.sprite = shapeSprite;
            shapeImage.enabled = shapeSprite != null;
        }

        if (button != null)
        {
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(ClickShape);
            button.interactable = true;
        }
    }

    public void SetInteractable(bool isInteractable)
    {
        if (button != null)
            button.interactable = isInteractable;
    }

    private void ClickShape()
    {
        deadlineDashManager?.ClickShape(shapeType);
    }
}
