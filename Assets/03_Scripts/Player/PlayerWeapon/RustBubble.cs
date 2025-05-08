using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RustBubble : MonoBehaviour
{
    [SerializeField] private float fadeSpeed = 1.0f;
    [SerializeField] private float groundDetectionDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private GameObject splashPrefab;

    private SpriteRenderer spriteRenderer;
    private float alpha = 1.0f;
    private bool hitGround = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Resources.Load<Sprite>("AcidDrip");
        }

        // 크기 랜덤 설정
        transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);
    }

    private void Update()
    {
        // 바닥 감지
        if (!hitGround)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, groundDetectionDistance, groundLayer))
            {
                hitGround = true;

                // 튀기는 효과 생성
                if (splashPrefab != null)
                {
                    Instantiate(splashPrefab, hit.point + Vector3.up * 0.05f, Quaternion.identity);
                }

                // 사운드 재생
                AudioSource audioSource = GetComponent<AudioSource>();
                if (audioSource != null && audioSource.clip != null)
                {
                    audioSource.pitch = Random.Range(0.8f, 1.2f);
                    audioSource.Play();
                }

                // 리지드바디 제거
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Destroy(rb);
                }
            }
        }

        // 페이드 아웃
        if (hitGround)
        {
            alpha -= fadeSpeed * Time.deltaTime;
            if (alpha <= 0)
            {
                Destroy(gameObject);
            }

            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }
}
