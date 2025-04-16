// 윙슈트 효과 클래스
using UnityEngine;

public class WingSuitEffect : CostumeEffectBase
{
    public float hoverForce = 100f;  // 더 큰 힘으로 증가
    public float hoverDuration = 5f;  // 더 긴 지속 시간
    public float energyCostPerSecond = 10f;
    
    [Header("물리 설정")]
    public float normalGravityScale = 1f;     // 일반 중력 스케일
    public float hoverGravityScale = 0.3f;    // 부양 중 중력 스케일
    
    [Header("디버그 옵션")]
    public bool showDebugInfo = true;
    public KeyCode hoverKey = KeyCode.J;

    private bool isHovering;
    private float hoverTimeRemaining;
    private Rigidbody2D playerRb;
    private PlayerMovement playerMovement;
    private float debugTimer = 0f;
    private GUIStyle debugStyle;
    private float originalGravityScale;

    private void Awake()
    {
        // 디버그 스타일 초기화
        debugStyle = new GUIStyle();
        debugStyle.fontSize = 18;
        debugStyle.normal.textColor = Color.white;
    }

    private void Start()
    {
        Debug.Log("WingSuitEffect Start() 호출됨");
        hoverTimeRemaining = hoverDuration;
        
        // PlayerMovement를 찾고 Rigidbody2D 컴포넌트 가져오기
        playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerRb = playerMovement.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                originalGravityScale = playerRb.gravityScale;
                normalGravityScale = originalGravityScale; // 현재 값으로 초기화
            }
            Debug.Log($"플레이어 참조 찾음: PlayerMovement={playerMovement != null}, Rigidbody2D={playerRb != null}, 원래 중력 스케일={originalGravityScale}");
        }
        else
        {
            Debug.LogWarning("PlayerMovement를 찾을 수 없습니다.");
        }
    }

    public override void ActivateEffect()
    {
        Debug.Log("WingSuitEffect 활성화됨");
        this.enabled = true;
        hoverTimeRemaining = hoverDuration;  // 활성화 시 시간 초기화
    }

    public override void DeactivateEffect()
    {
        Debug.Log("WingSuitEffect 비활성화됨");
        isHovering = false;
        // 중력 스케일 복원
        RestoreGravity();
        this.enabled = false;
    }

    private void RestoreGravity()
    {
        if (playerRb != null)
        {
            playerRb.gravityScale = originalGravityScale;
            Debug.Log($"중력 스케일 복원: {playerRb.gravityScale}");
        }
    }

    private void Update()
    {
        debugTimer += Time.deltaTime;
        
        if (!this.enabled)
        {
            if (debugTimer > 1f)
            {
                Debug.Log("WingSuitEffect가 비활성화 상태입니다.");
                debugTimer = 0f;
            }
            return;
        }
        
        if (playerRb == null)
        {
            Debug.LogWarning("playerRb가 null입니다.");
            TryFindPlayer();  // 플레이어 재탐색 시도
            return;
        }

        // 입력 확인 (J 키)
        HandleInput();
        
        // 부양 상태 업데이트
        UpdateHoverState();
    }

    private void TryFindPlayer()
    {
        playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerRb = playerMovement.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                originalGravityScale = playerRb.gravityScale;
                normalGravityScale = originalGravityScale;
            }
            Debug.Log($"플레이어 참조 다시 찾음: PlayerMovement={playerMovement != null}, Rigidbody2D={playerRb != null}");
        }
    }

    private void HandleInput()
    {
        // 키가 눌렸고, 부양 시간이 남아있으면 부양 시작
        if (Input.GetKeyDown(hoverKey) && hoverTimeRemaining > 0)
        {
            isHovering = true;
            
            // 부양 시작 시 중력 스케일 변경
            if (playerRb != null)
            {
                playerRb.gravityScale = hoverGravityScale;
                Debug.Log($"부양 시작 - 중력 스케일 변경: {normalGravityScale} -> {hoverGravityScale}");
            }
            
            Debug.Log($"Hovering 활성화: 남은 시간 = {hoverTimeRemaining:F1}초");
        }
        
        // 키를 누르고 있고, 이미 부양 중이고, 시간이 남아있으면 부양 유지
        else if (Input.GetKey(hoverKey) && isHovering && hoverTimeRemaining > 0)
        {
            // 이미 부양 중이므로 상태 유지
            if (debugTimer > 0.5f)
            {
                Debug.Log($"Hovering 유지 중: 남은 시간 = {hoverTimeRemaining:F1}초");
                debugTimer = 0f;
            }
        }

        // 키가 떨어지면 부양 종료
        if (Input.GetKeyUp(hoverKey) && isHovering)
        {
            isHovering = false;
            
            // 부양 종료 시 중력 스케일 복원
            if (playerRb != null)
            {
                playerRb.gravityScale = normalGravityScale;
                Debug.Log($"부양 종료 - 중력 스케일 복원: {hoverGravityScale} -> {normalGravityScale}");
            }
            
            Debug.Log("키를 뗌 - Hovering 종료");
        }
    }

    private void UpdateHoverState()
    {
        // 부양 중이면 에너지 소모 및 시간 감소
        if (isHovering)
        {
            float energyCost = energyCostPerSecond * Time.deltaTime;

            // TODO: 에너지 시스템 연결
            // if (EnergySystem.UseEnergy(energyCost))
            {
                hoverTimeRemaining -= Time.deltaTime;
                if (hoverTimeRemaining <= 0)
                {
                    hoverTimeRemaining = 0;
                    isHovering = false;
                    
                    // 시간 소진으로 부양 종료 시 중력 스케일 복원
                    if (playerRb != null)
                    {
                        playerRb.gravityScale = normalGravityScale;
                        Debug.Log($"시간 소진으로 부양 종료 - 중력 스케일 복원: {hoverGravityScale} -> {normalGravityScale}");
                    }
                    
                    Debug.Log("부양 시간 소진으로 Hovering 종료");
                }
            }
        }
        else
        {
            // 부양 중이 아니면 시간 회복
            float previousTime = hoverTimeRemaining;
            hoverTimeRemaining = Mathf.Min(hoverTimeRemaining + Time.deltaTime * 0.5f, hoverDuration);
            
            // 회복량이 의미 있게 변했을 때만 로그 출력
            if (Mathf.Abs(hoverTimeRemaining - previousTime) > 0.5f)
            {
                Debug.Log($"부양 시간 회복: {hoverTimeRemaining:F1}초 / {hoverDuration:F1}초");
            }
        }
        
        // 1초마다 상태 로그 출력
        if (debugTimer > 1f)
        {
            Debug.Log($"WingSuitEffect 상태: isHovering={isHovering}, hoverTimeRemaining={hoverTimeRemaining:F1}, 현재 중력 스케일={playerRb?.gravityScale}");
            debugTimer = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (isHovering && playerRb != null)
        {
            // 중력에 반하는 힘 적용 (Force 대신 Impulse 사용)
            Vector2 force = Vector2.up * hoverForce;
            
            // 중력 설정 확인
            float gravityScale = playerRb.gravityScale;
            float gravityForce = gravityScale * Physics2D.gravity.y;
            
            // 현재 중력을 상쇄하고 추가 양력 제공
            playerRb.AddForce(force, ForceMode2D.Force);
            
            // 속도 제한 (선택적) - 최대 상승 속도를 제한
            if (playerRb.velocity.y > 10f)
            {
                Vector2 clampedVelocity = playerRb.velocity;
                clampedVelocity.y = 10f;
                playerRb.velocity = clampedVelocity;
            }
            
            if (debugTimer > 0.2f)
            {
                Debug.Log($"부양 힘 적용: {force.y:F1}, 설정된 hoverForce: {hoverForce}, 중력 스케일: {gravityScale}, 실제 중력: {gravityForce}");
                Debug.Log($"현재 속도: {playerRb.velocity.y:F1}, 현재 위치: {playerRb.transform.position.y:F1}");
                debugTimer = 0f;
            }
        }
    }
    
    // 활성화될 때마다 호출
    private void OnEnable()
    {
        Debug.Log($"WingSuitEffect가 활성화되었습니다. 설정된 부양 힘: {hoverForce}");
    }
    
    // 비활성화될 때 호출
    private void OnDisable()
    {
        // 중력 스케일 복원
        RestoreGravity();
        Debug.Log("WingSuitEffect 비활성화로 중력 스케일 복원");
    }
    
    private void OnGUI()
    {
        if (!showDebugInfo || !this.enabled) return;
        
        // 화면 크기 가져오기
        float screenHeight = Screen.height;
        
        // 화면 왼쪽 하단에 디버그 정보 표시 (여백 10픽셀)
        GUI.Label(new Rect(10, screenHeight - 90, 300, 100), GetStatusText(), debugStyle);
    }
    
    // 현재 상태 정보를 문자열로 반환
    public string GetStatusText()
    {
        return $"WingSuit 상태: {(isHovering ? "활성화됨" : "비활성화됨")}\n" +
               $"남은 시간: {hoverTimeRemaining:F1}/{hoverDuration:F1}초\n" +
               $"부양 힘: {hoverForce}\n" +
               $"중력 스케일: {(playerRb != null ? playerRb.gravityScale.ToString("F2") : "N/A")}\n" +
               $"조작: {hoverKey} 키를 눌러 부양";
    }
}