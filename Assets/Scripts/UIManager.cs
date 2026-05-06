using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject startingButtons;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject signupPanel;
    [SerializeField] private GameObject namePanel;
    [SerializeField] private GameObject errorMessage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        UnlockCursor();
    }

    private void Start()
    {
        ShowStartingButtons();
    }

    public void ShowStartingButtons()
    {
        startingButtons.SetActive(true);
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);
        namePanel.SetActive(false);
        errorMessage.SetActive(false);

        UnlockCursor();
    }

    public void ShowNamePanel()
    {
        startingButtons.SetActive(false);
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);
        namePanel.SetActive(true);
        errorMessage.SetActive(false);

        UnlockCursor();
    }

    public void ShowLogin()
    {
        startingButtons.SetActive(false);
        loginPanel.SetActive(true);
        signupPanel.SetActive(false);
        namePanel.SetActive(false);
        errorMessage.SetActive(false);

        UnlockCursor();
    }

    public void ShowSignup()
    {
        startingButtons.SetActive(false);
        loginPanel.SetActive(false);
        signupPanel.SetActive(true);
        namePanel.SetActive(false);
        errorMessage.SetActive(false);

        UnlockCursor();
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}