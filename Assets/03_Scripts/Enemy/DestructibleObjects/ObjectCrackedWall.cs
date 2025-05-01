using System.Collections;
using UnityEngine;

/// <summary>
/// 파괴 가능한 벽 오브젝트 - 차징 공격으로만 파괴 가능 (하지만 현재는 일반공격 가능)
/// /// </summary>
public class ObjectCrackedWall : DestructibleObject
{
    #region Variables

    [Header("벽 속성")]
    [SerializeField] private bool requiresChargedAttack = true; // 차징 공격만 허용

    [Header("파괴 효과")]
    [SerializeField] private int minBrickCount = 5; // 최소 벽돌 개수
    [SerializeField] private int maxBrickCount = 10; // 최대 벽돌 개수
    [SerializeField] private float brickForce = 5f; // 벽돌 튕김 힘
    [SerializeField] private float brickLifetime = 3f; // 벽돌 지속 시간

    #endregion

    // 특수 데미지 처리를 위한 메서드 오버라이드
    public override void TakeDamage(float damage)
    {
        // 공격 타입 식별
        // 실제 구현에서는 공격 타입을 전달받는 방식으로 변경 필요
        bool isChargedAttack = CheckIfChargedAttack();

        // 차징 공격만 허용하는 경우 && 일반 공격이면 처리하지 않음
        if (requiresChargedAttack && !isChargedAttack)
        {
            // 타격 효과만 재생 (데미지는 입히지 않음)
            PlayImpactEffect();
            return;
        }

        // 차징 공격이면 데미지 적용
        base.TakeDamage(damage);
    }

    protected override IEnumerator FlashEffect()
    {
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.gray;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;

        // 코루틴 참조 초기화
        flashCoroutine = null;
    }

    // 임시로 차징 공격을 체크하는 방법 (실제 구현 필요)
    private bool CheckIfChargedAttack()
    {
        // 여기서는 임시로 항상 true 반환
        // 실제로는 공격 정보를 통해 체크해야 함
        return true;
    }

    // 일반 타격 효과만 재생
    private void PlayImpactEffect()
    {
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position, 0.7f);
        }

        // 약간의 먼지 효과 재생 (타격만 했을 때)
        if (dustEffectPrefab != null)
        {
            GameObject dust = Instantiate(dustEffectPrefab, transform.position, Quaternion.identity);
            Destroy(dust, 1f);
        }
    }

    // 파괴 효과 재생
    public override void PlayDestructionEffect()
    {
        // 벽돌 파편 생성
        CreateBrickDebris();
        // AudioManager.Instance.PlayBGM("Test");

        // 큰 먼지 효과 생성
        if (dustEffectPrefab != null)
        {
            GameObject dustCloud = Instantiate(dustEffectPrefab, transform.position, Quaternion.identity);

            // 먼지 효과 크기를 더 크게 설정
            dustCloud.transform.localScale = new Vector3(2f, 2f, 2f);

            Destroy(dustCloud, 2f);
        }

        // 벽 무너지는 효과음 재생
        if (destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, transform.position, 1.0f);
        }

        // 파괴된 벽은 충돌체와 렌더러를 비활성화
        if (GetComponent<Collider2D>() != null)
            GetComponent<Collider2D>().enabled = false;

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    // 벽돌 파편 생성
    private void CreateBrickDebris()
    {
        if (destroyEffectPrefab == null) return;

        // 랜덤하게 벽돌 개수 결정
        int brickCount = Random.Range(minBrickCount, maxBrickCount + 1);

        for (int i = 0; i < brickCount; i++)
        {
            // 벽돌 생성 위치에 약간의 랜덤성 추가
            Vector3 spawnPos = transform.position + new Vector3(
                Random.Range(-0.7f, 0.7f),
                Random.Range(-0.7f, 0.7f),
                0f
            );

            // 벽돌 회전에 랜덤성 추가
            Quaternion rotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));

            // 벽돌 생성
            GameObject brick = Instantiate(destroyEffectPrefab, spawnPos, rotation);

            // 벽돌에 물리 효과 적용
            Rigidbody2D brickRb = brick.GetComponent<Rigidbody2D>();
            if (brickRb != null)
            {
                // 아래로 더 많이 떨어지도록 방향 조정
                Vector2 randomDir = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-1f, -0.2f)).normalized;
                brickRb.AddForce(randomDir * brickForce, ForceMode2D.Impulse);

                // 랜덤한 회전 적용
                brickRb.AddTorque(Random.Range(-8f, 8f), ForceMode2D.Impulse);
            }

            // 벽돌 지속 시간 설정
            Destroy(brick, brickLifetime);
        }
    }
}