using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;

public class IgnorePlatform : MonoBehaviour
{
    [SerializeField] Collider2D platformCollider;
    [SerializeField] LayerMask playerLayerMask;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayerMask) != 0)
        {
            Physics2D.IgnoreCollision(collision, platformCollider, true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayerMask) != 0)
        {
            Physics2D.IgnoreCollision(collision,platformCollider, false);
        }
    }
}
