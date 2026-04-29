using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject signupPanel;

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
        ShowLogin();
    }

    public void ShowLogin()
    {
        loginPanel.SetActive(true);
        signupPanel.SetActive(false);
        UnlockCursor();
    }

    public void ShowSignup()
    {
        loginPanel.SetActive(false);
        signupPanel.SetActive(true);
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