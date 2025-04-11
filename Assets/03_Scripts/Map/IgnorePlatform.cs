using UnityEngine;

public class IgnorePlatform : MonoBehaviour
{
    [SerializeField] Collider2D platformCollider;
    [SerializeField] LayerMask playerLayerMask;
    [SerializeField] LayerMask ladderLayerMask;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayerMask) != 0)
        {
            Physics2D.IgnoreCollision(collision.GetComponent<Collider2D>(), platformCollider, true);
            Debug.Log($"플레이어와 플랫폼 충돌 무시: {collision.gameObject.name} - {platformCollider.name}");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayerMask) != 0)
        {
            Physics2D.IgnoreCollision(collision.GetComponent<Collider2D>(), platformCollider, false);
        }
    }

    private bool IsPlayerOnLadder(GameObject player)
    {
        Collider2D[] ladders = Physics2D.OverlapCircleAll(player.transform.position, 0.5f, ladderLayerMask);
        return ladders.Length > 0;
    }

    private void FixedUpdate()
    {
        Collider2D[] players = Physics2D.OverlapCircleAll(transform.position, 3f, playerLayerMask);
        
        foreach (var player in players)
        {
            if (IsPlayerOnLadder(player.gameObject))
            {
                Physics2D.IgnoreCollision(player.GetComponent<Collider2D>(), platformCollider, true);
            }
        }
    }
}
