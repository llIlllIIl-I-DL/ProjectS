using UnityEngine;

public class NPC : BaseObject
{
    [SerializeField] private Sprite npcFaceIcon;

    [SerializeField] private GameObject interactionBtnUI;
    [SerializeField] private Transform interactionBtnUITransform;

    private GameObject interactionButtonUI;

    public bool istalking = false;


    protected override void OnInteract(GameObject interactor)
    {
        TalkToNPC();
    }

    protected override void OnTriggerExit2D(Collider2D collider2D)
    {
        base.OnTriggerExit2D (collider2D);
    }

    protected override void OnPlayerEnterRange(GameObject player)
    {
        base.OnPlayerEnterRange(player);
    }

    protected override void OnPlayerExitRange(GameObject player)
    {
        base.OnPlayerExitRange(player);
    }


    protected override void ShowInteractionPrompt()
    {
        interactionButtonUI = Instantiate(interactionBtnUI, interactionBtnUITransform);

    }

    protected override void HideInteractionPrompt()
    {
        Destroy(interactionButtonUI);
    }


    public void TalkToNPC()
    {
        UIManager.Instance.NPCTalkInteraction(npcFaceIcon);
        Destroy(interactionButtonUI);
    }
}