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
    [SerializeField] private Vector2 moveDirection = Vector2.right; // 이동 방향
    [SerializeField] private bool isActive = true;             // 활성화 상태
    [SerializeField] private bool canToggle = true;            // 상호작용으로 토글 가능 여부

    [Header("시각 효과")]
    [SerializeField] private SpriteRenderer beltRenderer;
    // [SerializeField] private float textureScrollSpeed; // 1로 설정하면 방향 <- -1로 설정하면 방향 ->
    [SerializeField] private Material scrollingMaterial;

    [Header("물리 설정")]
    [SerializeField] private bool usePhysics = true;          // 물리 기반 이동 사용 여부
    [SerializeField] private LayerMask affectedLayers;        // 영향받는 레이어

    private Material instanceMaterial;
    private List<Rigidbody2D> objectsOnBelt = new List<Rigidbody2D>();
    private float offset = 0;

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
            instanceMaterial.SetTextureScale("_MainTex", new Vector2(2f, 1f));
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

            // 물리 기반 아닌 경우 직접 이동
            if (!usePhysics)
            {
                MoveObjectsDirectly();
            }
        }
    }

    private void FixedUpdate()
    {
        if (isActive && usePhysics)
        {
            // 물리 이동 적용
            MoveObjectsWithPhysics();
        }
    }

    #endregion

    #region Movement Methods

    private void MoveObjectsWithPhysics()
    {
        foreach (var rb in objectsOnBelt)
        {
            if (rb != null)
            {
                // 등속 이동을 위한 힘 계산
                Vector2 targetVelocity = moveDirection.normalized * moveSpeed;
                Vector2 velocityChange = targetVelocity - rb.velocity;

                // 힘 적용
                rb.AddForce(velocityChange, ForceMode2D.Impulse);
            }
        }

        // 목록 정리 (null 항목 제거)
        objectsOnBelt.RemoveAll(rb => rb == null);
    }

    private void MoveObjectsDirectly()
    {
        // 컨베이어 벨트 위의 모든 오브젝트 찾기
        Collider2D[] colliders = Physics2D.OverlapBoxAll(
            transform.position,
            transform.localScale,
            transform.rotation.eulerAngles.z,
            affectedLayers
        );

        foreach (var collider in colliders)
        {
            if (collider.attachedRigidbody != null)
            {
                // 직접 위치 이동
                collider.transform.Translate(moveDirection.normalized * moveSpeed * Time.deltaTime);
            }
        }
    }

    #endregion

    #region Interaction

    // 상호작용으로 컨베이어 토글
    protected override void OnInteract(GameObject interactor)
    {
        if (canToggle)
        {
            isActive = !isActive;
            PlayInteractSound();

            // 활성화/비활성화 효과 (선택적)
            if (beltRenderer != null)
            {
                beltRenderer.color = isActive ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            }
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
            // 플레이어 감지
            PlayerMovement player = collision.gameObject.GetComponent<PlayerMovement>();
            if (player != null)
            {
                // player.SetOnConveyor(true);
                
                // 플레이어의 경우 입력 움직임 보존 + 컨베이어 속도 추가
                Vector2 playerVelocity = rb.velocity;
                Vector2 conveyorEffect = moveDirection.normalized * moveSpeed;
                
                // 입력 방향 속도는 유지하고 컨베이어 효과만 추가
                float inputSpeedInConveyorDirection = Vector2.Dot(playerVelocity, moveDirection.normalized);
                float effectiveSpeed = Mathf.Max(0, inputSpeedInConveyorDirection); // 음수가 되지 않도록
                
                // 최종 속도 = 입력 속도 + 컨베이어 속도
                Vector2 finalVelocity = playerVelocity + conveyorEffect;
                
                // 속도 직접 설정 (AddForce 대신)
                rb.velocity = finalVelocity;
            }
            else
            {
                // 다른 오브젝트는 기존 방식으로
                Vector2 targetVelocity = moveDirection.normalized * moveSpeed;
                rb.velocity = targetVelocity;
            }
            
            if (!objectsOnBelt.Contains(rb))
                objectsOnBelt.Add(rb);
        }
    }

    // 컨베이어 벨트에서 오브젝트 내려갔을 때
    private void OnCollisionExit2D(Collision2D collision)
    {
        Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // 플레이어라면 컨베이어 상태 해제
            PlayerMovement player = collision.gameObject.GetComponent<PlayerMovement>();
            if (player != null)
            {
                // player.SetOnConveyor(false);
            }
            
            objectsOnBelt.Remove(rb);
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

    // 에디터 시각화
    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Vector3 dir = new Vector3(moveDirection.x, moveDirection.y, 0).normalized;
        Gizmos.DrawRay(transform.position, dir * moveSpeed);
    }
    #endif

    #endregion
}