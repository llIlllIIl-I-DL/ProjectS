using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody2D))]
public class JellyWobble : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector3 defaultScale;
    private bool isWobbling = false;
    private float wobbleThreshold = 0.5f; // 속도 임계값

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultScale = transform.localScale;
    }

    void Update()
    {
        if (!isWobbling && rb.velocity.magnitude > wobbleThreshold)
        {
            StartCoroutine(DoWobble());
        }
    }

    private System.Collections.IEnumerator DoWobble()
    {
        isWobbling = true;

        // X, Y 방향으로 살짝 눌렸다 펴지는 느낌
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(new Vector3(defaultScale.x * 1.2f, defaultScale.y * 0.8f, 1f), 0.1f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(new Vector3(defaultScale.x * 0.9f, defaultScale.y * 1.1f, 1f), 0.1f).SetEase(Ease.InOutQuad));
        seq.Append(transform.DOScale(defaultScale, 0.1f).SetEase(Ease.OutElastic));

        yield return seq.WaitForCompletion();

        isWobbling = false;
    }
}
