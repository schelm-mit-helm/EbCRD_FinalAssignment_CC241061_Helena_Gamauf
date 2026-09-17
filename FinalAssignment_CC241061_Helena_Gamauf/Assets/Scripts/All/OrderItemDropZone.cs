using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OrderItemDropZone : MonoBehaviour, IInteractable
{
    [SerializeField] string promptMessage = "Press E to deliver item";
    [SerializeField] string gamepadPromptMessage = "Press X to deliver item";
    [SerializeField] string noItemSelectedMessage = "Select an item to deliver.";
    [SerializeField] string wrongItemMessage = "This isn't needed for the order.";
    [SerializeField] string alreadyGatheredMessage = "You already delivered this item.";
    [SerializeField] string noTaskMessage = "You don't have an order yet.";

    public bool CanInteract(Inventory inventory) =>
        inventory != null && inventory.GetSelectedItem() != null;

    bool IInteractable.CanInteract(PlayerInteractionContext context) =>
        CanInteract(context.Inventory);

    public string GetPromptMessage(bool useGamepad) =>
        useGamepad ? gamepadPromptMessage : promptMessage;

    void IInteractable.Interact(PlayerInteractionContext context) =>
        TryInteract(context.Inventory);

    public void TryInteract(Inventory inventory)
    {
        if (inventory == null)
            return;

        var item = inventory.GetSelectedItem();
        if (item == null)
        {
            ComicSpeechBoxUI.Show(noItemSelectedMessage);
            return;
        }

        if (!TryAcceptItem(item))
            return;

        inventory.TryConsumeSelectedItem();
    }

    bool TryAcceptItem(Item item)
    {
        var orderService = CustomerOrderService.Instance;
        if (orderService == null || !orderService.PlayerHasTaskList)
        {
            ComicSpeechBoxUI.Show(noTaskMessage);
            return false;
        }

        if (orderService.TryGatherItem(item, out var orderIndex))
        {
            AkUnitySoundEngine.PostEvent("Play_Deliver_Item", gameObject);
            CustomerOrderUI.Instance?.MarkItemGathered(orderIndex);
            ScoreService.EnsurePersistentInstance()?.AddDeliveryScore();
            return true;
        }

        if (orderService.IsItemFullyDelivered(item))
            ComicSpeechBoxUI.Show(alreadyGatheredMessage);
        else
            ComicSpeechBoxUI.Show(wrongItemMessage);

        return false;
    }
}
