using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MinigameButtonInteract : MonoBehaviour
{
    private MinigameManager minigameManager;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(ClickButton);
    }

    public void Setup(MinigameManager manager)
    {
        minigameManager = manager;
    }

    private void ClickButton()
    {
        minigameManager?.ClickSpawnedButton(this);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(ClickButton);
    }
}
