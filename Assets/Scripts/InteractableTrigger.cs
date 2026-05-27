using UnityEngine;

public enum InteractionType
{
    None,
    Laptop,
    HouseDoor,
    Monitor,
}

public class InteractableTrigger : MonoBehaviour
{
    [Header("Interaction Info")]
    [SerializeField] private InteractionType interactionType = InteractionType.None;
    [SerializeField] private string promptMessage = "[E] Interact";

    public InteractionType InteractionType => interactionType;
    public string PromptMessage => promptMessage;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        InteractionUIManager.Instance?.SetCurrentInteractable(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        InteractionUIManager.Instance?.ClearCurrentInteractable(this);
    }
}
