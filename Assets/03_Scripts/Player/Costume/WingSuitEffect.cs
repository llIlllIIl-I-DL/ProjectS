// 윙슈트 효과 클래스
using UnityEngine;

public class WingSuitEffect : CostumeEffectBase
{
    public float hoverForce = 3f;  // 더 큰 힘으로 증가
    public float hoverDuration = 2.5f;  // 더 긴 지속 시간
    public float energyCostPerSecond = 10f;
    
    [Header("물리 설정")]
    public float normalGravityScale = 1f;     // 일반 중력 스케일
    public float hoverGravityScale = 0f;      // 부양 중 무중력 (0)으로 변경
    public float wingsuitGravityScale = 0.3f;  // 기존 윙슈트 모드의 중력 스케일
    public float diveGravityScale = 2.5f;      // 하강 시 증가된 중력 스케일
    public float diveForce = 5f;              // 하강 가속 힘
    
    [Header("더블 탭 설정")]
    public float doubleTapTimeThreshold = 0.3f; // 더블 탭 인식 시간 임계값
    
    [Header("디버그 옵션")]
    public bool showDebugInfo = true;

    [Header("지상 감지 설정")]
    public float groundCheckDistance = 0.2f;  // 땅 감지 거리
    public float maxSlopeAngle = 45f;         // 최대 경사면 각도
    
    private bool isHovering;
    private bool isWingsuitActive;            // 기존 윙슈트 모드 활성화 상태
    private bool isDiving;                    // 하강 모드 활성화 상태
    private float hoverTimeRemaining;
    private Rigidbody2D playerRb;
    private PlayerMovement playerMovement;
    private CollisionDetector collisionDetector;  // 추가: CollisionDetector 참조
    private float debugTimer = 0f;
    private GUIStyle debugStyle;
    private float originalGravityScale;
    
    // 더블 탭 감지용 변수
    private float lastUpKeyPressTime = 0f;
    private bool upKeyWasPressed = false;
    private float lastDownKeyPressTime = 0f;  // 아래 방향키 감지용
    private bool downKeyWasPressed = false;   // 아래 방향키 감지용

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
        
        // PlayerMovement와 CollisionDetector를 찾음
        playerMovement = FindObjectOfType<PlayerMovement>();
        collisionDetector = FindObjectOfType<CollisionDetector>();
        
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
        
        // CollisionDetector를 찾았는지 로그 출력
        if (collisionDetector != null)
        {
            Debug.Log("CollisionDetector 참조 찾음");
            
            // 지면 상태 변경 이벤트 구독
            collisionDetector.OnGroundedChanged += OnGroundedChanged;
        }
        else
        {
            Debug.LogWarning("CollisionDetector를 찾을 수 없습니다.");
        }
    }
    
    // 지면 상태 변경 이벤트 핸들러
    private void OnGroundedChanged(bool isGrounded)
    {
        // 땅에 닿았고 하강 중이면 경사면인지 확인
        if (isGrounded && isDiving)
        {
            // 경사면이 아닌 평지에 닿았을 때만 하강 모드 종료
            if (!IsOnSlope())
            {
                StopDiving();
                Debug.Log("평평한 지면에 착지하여 하강 모드 종료");
            }
            else
            {
                Debug.Log("경사면에 닿았으나 하강 모드 유지");
            }
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
        isWingsuitActive = false;
        isDiving = false;  // 하강 상태도 비활성화
        // 중력 스케일 복원
        RestoreGravity();
        // 플레이어 이동 제한 해제
        EnableVerticalMovement();
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

        // 하강 중에 땅에 닿았는지 확인 (이벤트와 별개로 매 프레임 확인)
        if (isDiving && IsGrounded())
        {
            // 경사면이 아닌 평지에 닿았을 때만 하강 모드 종료
            if (!IsOnSlope())
            {
                StopDiving();
                Debug.Log("평평한 지면에 착지하여 하강 모드 종료");
            }
            else if (debugTimer > 0.5f)
            {
                Debug.Log("경사면 위에서 하강 중");
                debugTimer = 0f;
            }
        }

        // 더블 탭 입력 처리
        HandleDoubleTapInput();

        // C키 입력 처리 (기존 윙슈트 모드)
        HandleWingsuitInput();
        
        // 부양 상태 업데이트
        UpdateHoverState();
    }

    private void TryFindPlayer()
    {
        playerMovement = FindObjectOfType<PlayerMovement>();
        collisionDetector = FindObjectOfType<CollisionDetector>();
        
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
        
        // CollisionDetector를 찾았는지 로그 출력
        if (collisionDetector != null)
        {
            Debug.Log("CollisionDetector 참조 다시 찾음");
            
            // 이벤트 중복 구독 방지를 위해 한번 구독 해제 후 다시 구독
            collisionDetector.OnGroundedChanged -= OnGroundedChanged;
            collisionDetector.OnGroundedChanged += OnGroundedChanged;
        }
    }

    private void HandleWingsuitInput()
    {
        // C키가 눌렸고 호버링 중이 아닌 경우 기존 윙슈트 모드 활성화
        if (Input.GetKeyDown(KeyCode.C) && !isHovering && !isDiving && hoverTimeRemaining > 0)
        {
            StartWingsuit();
        }
        
        // C키를 계속 누르고 있으면 윙슈트 모드 유지
        if (Input.GetKey(KeyCode.C) && isWingsuitActive && hoverTimeRemaining > 0)
        {
            // 이미 활성화되어 있으므로 유지
            if (debugTimer > 0.5f)
            {
                Debug.Log($"윙슈트 모드 유지 중: 남은 시간 = {hoverTimeRemaining:F1}초");
                debugTimer = 0f;
            }
        }
        
        // C키를 떼면 윙슈트 모드 종료
        if (Input.GetKeyUp(KeyCode.C) && isWingsuitActive)
        {
            StopWingsuit();
        }
    }

    private void HandleDoubleTapInput()
    {
        // 위쪽 방향키 더블 탭 감지 (호버링)
        bool upKeyDown = Input.GetKeyDown(KeyCode.UpArrow);
        
        if (upKeyDown)
        {
            float currentTime = Time.time;
            
            // 더블 탭 감지 로직
            if (upKeyWasPressed && (currentTime - lastUpKeyPressTime) < doubleTapTimeThreshold)
            {
                // 다른 모드가 활성화되어 있지 않고 부양 시간이 남아있으면 호버링 시작/종료
                if (!isDiving && hoverTimeRemaining > 0)
                {
                    if (isHovering)
                    {
                        StopHovering();
                    }
                    else
                    {
                        StartHovering();
                    }
                }
                upKeyWasPressed = false;
            }
            else
            {
                // 첫 번째 탭 기록
                upKeyWasPressed = true;
                lastUpKeyPressTime = currentTime;
            }
        }
        
        // 아래쪽 방향키 더블 탭 감지 (하강)
        bool downKeyDown = Input.GetKeyDown(KeyCode.DownArrow);
        
        if (downKeyDown)
        {
            float currentTime = Time.time;
            
            // 더블 탭 감지 로직
            if (downKeyWasPressed && (currentTime - lastDownKeyPressTime) < doubleTapTimeThreshold)
            {
                // 다른 모드가 활성화되어 있지 않고 부양 시간이 남아있으면 하강 시작/종료
                if (!isHovering && hoverTimeRemaining > 0)
                {
                    if (isDiving)
                    {
                        StopDiving();
                    }
                    else
                    {
                        StartDiving();
                    }
                }
                downKeyWasPressed = false;
            }
            else
            {
                // 첫 번째 탭 기록
                downKeyWasPressed = true;
                lastDownKeyPressTime = currentTime;
            }
        }
        
        // 더블 탭 감지 시간 초과 시 상태 초기화
        if (upKeyWasPressed && (Time.time - lastUpKeyPressTime) > doubleTapTimeThreshold)
        {
            upKeyWasPressed = false;
        }
        
        if (downKeyWasPressed && (Time.time - lastDownKeyPressTime) > doubleTapTimeThreshold)
        {
            downKeyWasPressed = false;
        }
    }

    private void StartWingsuit()
    {
        // 하강 중이면 먼저 종료
        if (isDiving)
        {
            StopDiving();
        }
        
        isWingsuitActive = true;
        
        // 윙슈트 시작 시 중력 스케일 변경
        if (playerRb != null)
        {
            playerRb.gravityScale = wingsuitGravityScale;
            Debug.Log($"윙슈트 모드 시작 - 중력 스케일 변경: {normalGravityScale} -> {wingsuitGravityScale}");
        }
        
        Debug.Log($"윙슈트 모드 활성화: 남은 시간 = {hoverTimeRemaining:F1}초");
    }

    private void StopWingsuit()
    {
        isWingsuitActive = false;
        
        // 윙슈트 종료 시 중력 스케일 복원
        if (playerRb != null)
        {
            playerRb.gravityScale = normalGravityScale;
            Debug.Log($"윙슈트 모드 종료 - 중력 스케일 복원: {wingsuitGravityScale} -> {normalGravityScale}");
        }
        
        Debug.Log("윙슈트 모드 종료");
    }

    private void StartHovering()
    {
        // 윙슈트 모드나 하강 모드가 활성화되어 있으면 먼저 종료
        if (isWingsuitActive)
        {
            StopWingsuit();
        }
        
        if (isDiving)
        {
            StopDiving();
        }
        
        isHovering = true;
        
        // 부양 시작 시 중력 스케일 변경 (무중력)
        if (playerRb != null)
        {
            // 수직 속도 초기화 (관성 제거)
            Vector2 velocity = playerRb.velocity;
            velocity.y = 0;
            playerRb.velocity = velocity;
            
            // 무중력 설정
            playerRb.gravityScale = hoverGravityScale;
            Debug.Log($"부양 시작 - 무중력 상태로 변경: {normalGravityScale} -> {hoverGravityScale}");
        }
        
        // 수직 이동 제한 설정
        DisableVerticalMovement();
        
        Debug.Log($"Hovering 활성화: 남은 시간 = {hoverTimeRemaining:F1}초");
    }

    private void StopHovering()
    {
        isHovering = false;
        
        // 부양 종료 시 중력 스케일 복원
        if (playerRb != null)
        {
            playerRb.gravityScale = normalGravityScale;
            Debug.Log($"부양 종료 - 중력 스케일 복원: {hoverGravityScale} -> {normalGravityScale}");
        }
        
        // 수직 이동 제한 해제
        EnableVerticalMovement();
        
        Debug.Log("Hovering 종료");
    }
    
    private void StartDiving()
    {
        // 윙슈트 모드나 호버링이 활성화되어 있으면 먼저 종료
        if (isWingsuitActive)
        {
            StopWingsuit();
        }
        
        if (isHovering)
        {
            StopHovering();
        }
        
        isDiving = true;
        
        // 하강 시작 시 중력 스케일 증가
        if (playerRb != null)
        {
            // 하강 중력 설정
            playerRb.gravityScale = diveGravityScale;
            Debug.Log($"하강 시작 - 중력 스케일 증가: {normalGravityScale} -> {diveGravityScale}");
        }
        
        Debug.Log($"하강 모드 활성화: 남은 시간 = {hoverTimeRemaining:F1}초");
    }
    
    private void StopDiving()
    {
        isDiving = false;
        
        // 하강 종료 시 중력 스케일 복원
        if (playerRb != null)
        {
            playerRb.gravityScale = normalGravityScale;
            Debug.Log($"하강 종료 - 중력 스케일 복원: {diveGravityScale} -> {normalGravityScale}");
        }
        
        Debug.Log("하강 모드 종료");
    }

    private void DisableVerticalMovement()
    {
        if (playerMovement != null)
        {
            Debug.Log("수직 이동 제한 설정됨 - 좌우 이동만 가능");
        }
    }

    private void EnableVerticalMovement()
    {
        if (playerMovement != null)
        {
            Debug.Log("수직 이동 제한 해제됨");
        }
    }

    private void UpdateHoverState()
    {
        // 부양 또는 윙슈트 모드 중이면 에너지 소모 및 시간 감소 (하강 모드는 에너지 소모 없음)
        if (isHovering || isWingsuitActive)
        {
            float energyCost = energyCostPerSecond * Time.deltaTime;

            // TODO: 에너지 시스템 연결
            // if (EnergySystem.UseEnergy(energyCost))
            {
                hoverTimeRemaining -= Time.deltaTime;
                if (hoverTimeRemaining <= 0)
                {
                    hoverTimeRemaining = 0;
                    
                    // 시간이 다 되면 모든 모드 종료
                    if (isHovering)
                    {
                        StopHovering();
                        Debug.Log("부양 시간 소진으로 Hovering 종료");
                    }
                    
                    if (isWingsuitActive)
                    {
                        StopWingsuit();
                        Debug.Log("부양 시간 소진으로 윙슈트 모드 종료");
                    }
                }
            }
        }
        // 하강 모드 중에 시간 초과된 경우 확인
        else if (isDiving && hoverTimeRemaining <= 0)
        {
            StopDiving();
            Debug.Log("부양 시간 소진으로 하강 모드 종료");
        }
        else if (!isHovering && !isWingsuitActive && !isDiving)
        {
            // 특수 모드가 아니면 시간 회복
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
            Debug.Log($"WingSuitEffect 상태: isHovering={isHovering}, isWingsuit={isWingsuitActive}, isDiving={isDiving}, hoverTimeRemaining={hoverTimeRemaining:F1}, 현재 중력 스케일={playerRb?.gravityScale}");
            debugTimer = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (isHovering && playerRb != null)
        {
            // 호버링 중에는 좌우 이동만 가능하도록 수직 속도 제어
            Vector2 velocity = playerRb.velocity;
            velocity.y = 0; // 수직 속도를 0으로 유지
            playerRb.velocity = velocity;
            
            if (debugTimer > 0.2f)
            {
                Debug.Log($"호버링 중: 현재 속도={playerRb.velocity}, 현재 위치={playerRb.transform.position.y:F1}");
                debugTimer = 0f;
            }
        }
        else if (isWingsuitActive && playerRb != null)
        {
            // 윙슈트 모드에서는 중력에 반하는 힘 적용
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
                Debug.Log($"윙슈트 모드: 부양 힘 적용={force.y:F1}, 중력 스케일={gravityScale}, 실제 중력={gravityForce}");
                Debug.Log($"현재 속도: {playerRb.velocity.y:F1}, 현재 위치: {playerRb.transform.position.y:F1}");
                debugTimer = 0f;
            }
        }
        else if (isDiving && playerRb != null)
        {
            // 하강 모드에서는 아래로 향하는 힘 적용
            Vector2 force = Vector2.down * diveForce;
            
            // 경사면 위에 있을 경우, 경사면 방향으로 힘 적용
            if (IsGrounded() && IsOnSlope())
            {
                // 경사면의 방향 벡터 구하기
                RaycastHit2D hit = Physics2D.Raycast(
                    playerRb.position,
                    Vector2.down,
                    groundCheckDistance * 1.5f,
                    1 << LayerMask.NameToLayer("Ground")
                );
                
                if (hit.collider != null)
                {
                    // 경사면을 따라 내려가는 방향 벡터 계산
                    Vector2 slopeDirection = new Vector2(
                        -hit.normal.y,
                        hit.normal.x
                    ).normalized;
                    
                    // 벡터의 방향이 올바른지 확인 (항상 아래쪽 방향으로 가도록)
                    if (slopeDirection.y > 0)
                        slopeDirection = -slopeDirection;
                        
                    // 경사면 방향으로 힘 적용
                    force = slopeDirection * diveForce * 1.2f;
                    
                    if (debugTimer > 0.2f)
                    {
                        Debug.DrawRay(playerRb.position, force, Color.red, 0.1f);
                        Debug.Log($"경사면 방향으로 힘 적용: {force}");
                        debugTimer = 0f;
                    }
                }
            }
            
            // 중력 설정 확인
            float gravityScale = playerRb.gravityScale;
            float gravityForce = gravityScale * Physics2D.gravity.y;
            
            // 하강 가속을 위한 추가 힘 적용
            playerRb.AddForce(force, ForceMode2D.Force);
            
            // 속도 제한 (선택적) - 최대 하강 속도를 제한
            if (playerRb.velocity.y < -20f)
            {
                Vector2 clampedVelocity = playerRb.velocity;
                clampedVelocity.y = -20f;
                playerRb.velocity = clampedVelocity;
            }
            
            if (debugTimer > 0.2f)
            {
                Debug.Log($"하강 모드: 하강 힘 적용={force.y:F1}, 중력 스케일={gravityScale}, 실제 중력={gravityForce}");
                Debug.Log($"현재 속도: {playerRb.velocity.y:F1}, 현재 위치: {playerRb.transform.position.y:F1}");
                debugTimer = 0f;
            }
        }
    }
    
    // 활성화될 때마다 호출
    private void OnEnable()
    {
        Debug.Log($"WingSuitEffect가 활성화되었습니다. 설정된 부양 힘: {hoverForce}");
        
        // 활성화될 때 이벤트 구독
        if (collisionDetector != null)
        {
            collisionDetector.OnGroundedChanged -= OnGroundedChanged;
            collisionDetector.OnGroundedChanged += OnGroundedChanged;
        }
    }
    
    // 비활성화될 때 호출
    private void OnDisable()
    {
        // 중력 스케일 복원
        RestoreGravity();
        // 수직 이동 제한 해제
        EnableVerticalMovement();
        
        // 비활성화될 때 이벤트 구독 해제
        if (collisionDetector != null)
        {
            collisionDetector.OnGroundedChanged -= OnGroundedChanged;
        }
        
        Debug.Log("WingSuitEffect 비활성화로 중력 스케일 복원 및 이동 제한 해제");
    }
    
    // 스크립트가 파괴될 때 호출
    private void OnDestroy()
    {
        // 스크립트가 파괴될 때 이벤트 구독 해제
        if (collisionDetector != null)
        {
            collisionDetector.OnGroundedChanged -= OnGroundedChanged;
        }
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
        string statusText = "";
        
        if (isHovering)
        {
            statusText = "윙슈트 상태: 호버링 중 (무중력)";
        }
        else if (isWingsuitActive)
        {
            statusText = "윙슈트 상태: 윙슈트 모드 (낙하 지연)";
        }
        else if (isDiving)
        {
            if (IsGrounded() && IsOnSlope())
                statusText = "윙슈트 상태: 하강 모드 (경사면 활강)";
            else
                statusText = "윙슈트 상태: 하강 모드 (빠르게 하강)";
        }
        else
        {
            statusText = "윙슈트 상태: 비활성화";
        }
        
        return statusText + $"\n남은 시간: {hoverTimeRemaining:F1}/{hoverDuration:F1}초\n" +
               $"중력 스케일: {(playerRb != null ? playerRb.gravityScale.ToString("F2") : "N/A")}\n" +
               "조작: 위쪽 화살표 더블 탭 = 호버링, 아래쪽 화살표 더블 탭 = 하강, C키 = 윙슈트 모드";
    }

    // 땅에 닿았는지 확인하는 메서드
    private bool IsGrounded()
    {
        // CollisionDetector가 있으면 그것을 사용
        if (collisionDetector != null)
        {
            return collisionDetector.IsGrounded;
        }
        
        
        return false;
    }

    // 경사면 위에 있는지 확인하는 메서드
    private bool IsOnSlope()
    {
        if (collisionDetector == null || !IsGrounded())
            return false;
            
        // CollisionDetector로부터 지면의 법선 벡터 정보를 얻어오기 위한 레이캐스트
        RaycastHit2D hit = Physics2D.Raycast(
            playerRb.position,
            Vector2.down,
            groundCheckDistance * 1.5f,
            1 << LayerMask.NameToLayer("Ground") // Ground 레이어로 설정
        );
            
        if (hit.collider != null)
        {
            // 지면의 법선 벡터와 위쪽 방향 벡터의 각도 계산
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                
            // 디버그 정보 표시
            if (showDebugInfo && hit.collider != null)
            {
                Debug.DrawRay(hit.point, hit.normal, Color.green, 0.1f);
                
                if (debugTimer > 0.5f)
                {
                    Debug.Log($"경사면 각도: {slopeAngle}°, 최대 각도: {maxSlopeAngle}°");
                    debugTimer = 0f;
                }
            }
                
            // 설정된 최대 경사면 각도보다 크면 경사면으로 판단
            return slopeAngle > 1f && slopeAngle <= maxSlopeAngle;
        }
            
        return false;
    }
}