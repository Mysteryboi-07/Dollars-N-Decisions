using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MinigameButtonInteract : MonoBehaviour
{
    private BugBashManager bugBashManager;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(ClickButton);
    }

    public void Setup(BugBashManager manager)
    {
        bugBashManager = manager;
    }

    private void ClickButton()
    {
        bugBashManager?.ClickSpawnedButton(this);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(ClickButton);
    }
}
