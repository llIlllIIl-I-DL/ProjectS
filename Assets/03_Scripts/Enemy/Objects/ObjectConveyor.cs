using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 컨베이어 벨트 오브젝트 - 물체, 플레이어를 이동시키는 기능
/// </summary>
public class ObjectConveyor : BaseObject
{
    // 현재 컨베이어 벨트 밀리는 효과는 작동하나 점프가 씹히거나 
    // 밀리는 방향으로 이동속도가 증가하지 않는 문제가 발생
    // PlayerMovement.cs에서 점프를 처리하는 부분을 수정해야 할 듯

    // 텍스쳐 스크롤링 코드로 구현해놓았지만 쉐이더 테스트하면서 쉐이더로 구현해서 관련 부분 주석처리

    #region Variables

    [Header("컨베이어 벨트 설정")]
    [SerializeField] private float moveSpeed;                  // 이동 속도
    [SerializeField] private Vector2 moveDirection;            // 이동 방향 // 스크롤링과 반대로 음수면 <-
    [SerializeField] private bool isActive = true;             // 활성화 상태

    [Header("시각 효과")]
    [SerializeField] private SpriteRenderer beltRenderer;
    // [SerializeField] private float textureScrollSpeed;      // 1로 설정하면 방향 <-, -1로 설정하면 방향 ->
    [SerializeField] private Material scrollingMaterial;

    [Header("물리 설정")]
    [SerializeField] private bool usePhysics = true;          // 물리 기반 이동 사용 여부
    [SerializeField] private LayerMask affectedLayers;        // 영향받는 레이어

    private Material instanceMaterial;
    // private float offset = 0;

    #endregion

    #region Unity Lifecycle

    protected override void Start()
    {
        base.Start();

        if (beltRenderer == null)
            beltRenderer = GetComponent<SpriteRenderer>();

        // 머티리얼 인스턴스 생성
        if (beltRenderer != null && scrollingMaterial != null)
        {
            instanceMaterial = new Material(scrollingMaterial);
            beltRenderer.material = instanceMaterial;
            // 타일링 값을 증가시켜 반복 패턴을 더 촘촘하게 만듦
            // instanceMaterial.SetTextureScale("_MainTex", new Vector2(2f, 1f));
        }
    }

    protected override void Update()
    {
        if (isActive)
        {
            // // 텍스처 스크롤링
            // if (instanceMaterial != null)
            // {
            //     // offset을 무한대로 증가시키고 모듈로 연산 사용하지 않음
            //     // 이렇게 하면 텍스처가 계속 스크롤됨
            //     offset += textureScrollSpeed * Time.deltaTime;

            //     // 오프셋이 너무 커지는 것을 방지 (최적화 목적)
            //     if (offset > 1000f) offset -= 1000f;

            //     instanceMaterial.SetTextureOffset("_MainTex", new Vector2(offset, 0));
            // }
        }
    }

    #endregion


    #region Interaction

    // 상호작용으로 컨베이어 토글
    protected override void OnInteract(GameObject interactor)
    {
        if (isInteractable)
        {
            isActive = !isActive;
        }
    }

    #endregion

    #region Collision Handlers

    // 컨베이어 벨트 위에 오브젝트 올라왔을 때
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!isActive) return;

        Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (rb != null && (affectedLayers & (1 << collision.gameObject.layer)) != 0)
        {
            Vector2 targetVelocity = moveDirection.normalized * moveSpeed;
            //x축만 컨베이어 영향 적용
            Vector2 newVelocity = rb.velocity;
            newVelocity.x = targetVelocity.x;
            rb.velocity = newVelocity;

        }
    }

    #endregion

    #region Public Methods

    // 방향 설정 메서드
    public void SetDirection(Vector2 newDirection)
    {
        moveDirection = newDirection.normalized;
    }

    // 속도 설정 메서드
    public void SetSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    // 활성화 상태 설정 메서드
    public void SetActive(bool active)
    {
        isActive = active;
    }

    #endregion

    #region Editor

    // 컨베이어에 굳이 기즈모가 필요할까? 방향 표시만??
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)moveDirection * 2f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    #endregion
}