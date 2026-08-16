using UnityEngine;
public class Interactor : MonoBehaviour
{
public KeyCode interactKey = KeyCode.E;
public float interactRange = 1f;
public float interactRadius = 0.5f;
public LayerMask interactLayer;
PlayerMovement playerMovement;
void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }
void Update()
    {
        if(DialogueBox.instance != null && DialogueBox.instance.isOpen) return;
        if(PauseMenu.instance != null && PauseMenu.instance.isOpen) return;
        if(Input.GetKeyDown(interactKey)) PlayerInteract();
    }
    void PlayerInteract()
    {
        Vector2 facing = playerMovement != null ? playerMovement.facingDirection : Vector2.down;
        Vector2 checkPoint = (Vector2) transform.position + facing.normalized * interactRange;
        Collider2D hit = Physics2D.OverlapCircle(checkPoint, interactRadius, interactLayer);
        if(hit == null) return;
        IInteractable interactable = hit.GetComponent<IInteractable>();
        if(interactable != null) interactable.Interact();
    }
}
