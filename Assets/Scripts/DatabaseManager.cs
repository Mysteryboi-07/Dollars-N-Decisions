using Firebase.Database;
using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }

    private DatabaseReference rootRef;
    private string currentUserId;

    public bool HasUser => !string.IsNullOrEmpty(currentUserId);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        rootRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    public void SetCurrentUserId(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return;

        currentUserId = userId;
        Debug.Log("[DBM] User ID set: " + currentUserId);
    }

    public void ClearCurrentUser()
    {
        currentUserId = null;
    }

    public void SaveUserProfile(string email)
    {
        if (!HasUser)
        {
            Debug.LogWarning("[DBM] No user ID set.");
            return;
        }

        rootRef
            .Child("Users")
            .Child(currentUserId)
            .Child("Profile")
            .Child("Email")
            .SetValueAsync(email);
    }
}