using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class OrderReturnInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] string promptMessage = "Press E to return to bakery";
    [SerializeField] string gamepadPromptMessage = "Press X to return to bakery";
    [SerializeField] string missingItemsMessage = "Come back when you've gathered everything!";
    [SerializeField] string destinationSceneName = SceneNames.Bakery;
    [SerializeField] bool requireCompleteOrder = true;

    bool returnSequenceRunning;

    public bool CanInteract => !returnSequenceRunning;

    bool IInteractable.CanInteract(PlayerInteractionContext context) => CanInteract;

    public string GetPromptMessage(bool useGamepad) =>
        useGamepad ? gamepadPromptMessage : promptMessage;

    void IInteractable.Interact(PlayerInteractionContext context) =>
        TryInteract(context.Player);

    public void Configure(string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(sceneName))
            destinationSceneName = sceneName;
    }

    public void TryInteract(Transform player)
    {
        if (player == null || returnSequenceRunning)
            return;
        AkUnitySoundEngine.PostEvent("Play_ButtonElevator", gameObject);
        if (requireCompleteOrder && !CanReturnToBakery())
        {
            ComicSpeechBoxUI.Show(missingItemsMessage);
            return;
        }
      
        StartCoroutine(ReturnToBakerySequence(player));
    }

    IEnumerator ReturnToBakerySequence(Transform player)
    {
        returnSequenceRunning = true;

        var playerMovement = player.GetComponent<PlayerMovement>();
        playerMovement?.SetControlsLocked(true);

        var orderService = CustomerOrderService.EnsurePersistentInstance();
        orderService.MarkReturningFromGathering();

        if (GatheringElevatorDoors.Instance != null)
        {
            yield return GatheringElevatorDoors.Instance.CloseDoors();
            yield return GatheringElevatorDoors.Instance.MovePlayerToStartPose(player);
        }

        SceneManager.LoadScene(destinationSceneName);
    }

    bool CanReturnToBakery()
    {
        var orderService = CustomerOrderService.Instance;
        return orderService != null
            && orderService.PlayerHasTaskList
            && orderService.AllItemsGathered;
    }
}
