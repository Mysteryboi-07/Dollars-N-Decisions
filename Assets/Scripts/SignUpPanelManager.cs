using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Database;

public class SignupPanelManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;
    [SerializeField] private TMP_Text errorMessage;

    private FirebaseAuth auth;

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        if (errorMessage != null)
            errorMessage.gameObject.SetActive(false);
    }

    public void Signup()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text;
        string confirmPassword = confirmPasswordInput.text;

        if (string.IsNullOrEmpty(email) || !email.Contains("@") || !email.Contains("."))
        {
            ShowError("Invalid e-mail address.");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowError("Please enter a password.");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("Password must be at least 6 characters.");
            return;
        }

        if (password != confirmPassword)
        {
            ShowError("Passwords do not match.");
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    string msg = task.Exception?.Flatten().InnerExceptions[0].Message;
                    ShowError("Signup failed:\n" + msg);
                    return;
                }

                FirebaseUser user = task.Result.User;

                FirebaseDatabase.DefaultInstance.RootReference
                    .Child("Users")
                    .Child(user.UserId)
                    .Child("Profile")
                    .Child("Email")
                    .SetValueAsync(email);

                ClearFields();
                HideError();

                UIManager.Instance?.ShowLogin();

                Debug.Log("[SIGNUP] Success: " + user.UserId);
            });
    }

    private void ShowError(string message)
    {
        errorMessage.text = message;
        errorMessage.gameObject.SetActive(true);
    }

    private void HideError()
    {
        errorMessage.gameObject.SetActive(false);
    }

    private void ClearFields()
    {
        emailInput.text = "";
        passwordInput.text = "";
        confirmPasswordInput.text = "";
    }
}