using UnityEngine;

public class SSS : MonoBehaviour
{
    [System.Serializable]
    private class ObjectState
    {
        public GameObject target;
        public bool activeOnAwake;
    }

    [Header("Objects To Set On Scene Start")]
    [SerializeField] private ObjectState[] objects;

    private void Awake()
    {
        ApplyObjectStates();
    }

    public void ApplyObjectStates()
    {
        foreach (ObjectState obj in objects)
        {
            if (obj.target != null)
                obj.target.SetActive(obj.activeOnAwake);
        }
    }
}