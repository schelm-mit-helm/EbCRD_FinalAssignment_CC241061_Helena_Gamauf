using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractableItemPickup : MonoBehaviour
{
    [SerializeField]
    Item item;

    [SerializeField]
    MimicCakeEnemy mimicEnemy;

    [SerializeField]
    string promptMessage = "Press E to interact";

    [SerializeField]
    string gamepadPromptMessage = "Press X to interact";

    [SerializeField]
    bool destroyOnPickup;

    private void Awake()
    {
        if (mimicEnemy == null)
            mimicEnemy = GetComponent<MimicCakeEnemy>();
    }

    public string GetPromptMessage(bool useGamepad) =>
        useGamepad ? gamepadPromptMessage : promptMessage;

    public bool CanBePickedUp
    {
        get
        {
            if (mimicEnemy != null)
                return !mimicEnemy.IsAwakened;

            return item != null;
        }
    }

    public bool TryPickup(Inventory inventory)
    {
        if (mimicEnemy != null)
        {
            if (mimicEnemy.IsAwakened)
                return false;

            mimicEnemy.Awaken();
            return true;
        }

        if (!CanBePickedUp || inventory == null)
            return false;

        if (!inventory.TryPickupItem(item))
            return false;

        if (destroyOnPickup)
            Destroy(gameObject);

        return true;
    }
}