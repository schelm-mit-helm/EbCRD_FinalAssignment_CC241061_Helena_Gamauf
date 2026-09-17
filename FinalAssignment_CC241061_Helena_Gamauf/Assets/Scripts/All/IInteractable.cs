public struct PlayerInteractionContext
{
    public UnityEngine.Transform Player;
    public Inventory Inventory;
}

public interface IInteractable
{
    bool CanInteract(PlayerInteractionContext context);
    string GetPromptMessage(bool useGamepad);
    void Interact(PlayerInteractionContext context);
}
