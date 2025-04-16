// 윙슈트 효과 클래스
using UnityEngine;

public class WingSuitEffect : CostumeEffectBase
{
    public float hoverForce = 100f;  // 더 큰 힘으로 증가
    public float hoverDuration = 5f;  // 더 긴 지속 시간
    public float energyCostPerSecond = 10f;
    
    [Header("물리 설정")]
    public float normalGravityScale = 1f;     // 일반 중력 스케일
    public float hoverGravityScale = 0f;      // 부양 중 무중력 (0)으로 변경
    public float wingsuitGravityScale = 0.3f;  // 기존 윙슈트 모드의 중력 스케일
    
    [Header("더블 탭 설정")]
    public float doubleTapTimeThreshold = 0.3f; // 더블 탭 인식 시간 임계값
    
    [Header("디버그 옵션")]
    public bool showDebugInfo = true;

    private bool isHovering;
    private bool isWingsuitActive;            // 기존 윙슈트 모드 활성화 상태
    private float hoverTimeRemaining;
    private Rigidbody2D playerRb;
    private PlayerMovement playerMovement;
    private float debugTimer = 0f;
    private GUIStyle debugStyle;
    private float originalGravityScale;
    
    // 더블 탭 감지용 변수
    private float lastUpKeyPressTime = 0f;
    private bool upKeyWasPressed = false;

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
        isWingsuitActive = false;
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

    private void HandleWingsuitInput()
    {
        // C키가 눌렸고 호버링 중이 아닌 경우 기존 윙슈트 모드 활성화
        if (Input.GetKeyDown(KeyCode.C) && !isHovering && hoverTimeRemaining > 0)
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
        // 이미 윙슈트 모드가 활성화되어 있으면 호버링 시작하지 않음
        if (isWingsuitActive)
            return;
            
        // 위쪽 방향키 감지
        bool upKeyDown = Input.GetKeyDown(KeyCode.UpArrow);
        
        // 더블 탭 감지 로직
        if (upKeyDown)
        {
            float currentTime = Time.time;
            
            // 이전에 위쪽 키가 눌렸고, 시간 임계값 내에 다시 눌렸다면 더블 탭으로 인식
            if (upKeyWasPressed && (currentTime - lastUpKeyPressTime) < doubleTapTimeThreshold)
            {
                // 호버링 상태가 아니고 부양 시간이 남아있으면 호버링 시작
                if (!isHovering && hoverTimeRemaining > 0)
                {
                    StartHovering();
                }
                // 더블 탭 상태 초기화
                upKeyWasPressed = false;
            }
            else
            {
                // 첫 번째 탭 기록
                upKeyWasPressed = true;
                lastUpKeyPressTime = currentTime;
            }
        }
        
        // 호버링 중 다시 더블 탭하면 호버링 종료
        if (isHovering && upKeyDown)
        {
            float currentTime = Time.time;
            if (upKeyWasPressed && (currentTime - lastUpKeyPressTime) < doubleTapTimeThreshold)
            {
                StopHovering();
                upKeyWasPressed = false;
            }
            else
            {
                upKeyWasPressed = true;
                lastUpKeyPressTime = currentTime;
            }
        }
        
        // 더블 탭 감지 시간 초과 시 상태 초기화
        if (upKeyWasPressed && (Time.time - lastUpKeyPressTime) > doubleTapTimeThreshold)
        {
            upKeyWasPressed = false;
        }
    }

    private void StartWingsuit()
    {
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
        // 윙슈트 모드가 활성화되어 있으면 먼저 종료
        if (isWingsuitActive)
        {
            StopWingsuit();
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

    private void DisableVerticalMovement()
    {
        if (playerMovement != null)
        {
            // PlayerMovement 스크립트에 수직 이동 제한 메소드가 있다고 가정
            // 아래 코드는 해당 메소드가 존재한다는 가정하에 작성됨
            // playerMovement.DisableVerticalMovement();
            
            // 만약 위 메소드가 없다면 playerMovement에 관련 기능을 추가해야 함
            Debug.Log("수직 이동 제한 설정됨 - 좌우 이동만 가능");
        }
    }

    private void EnableVerticalMovement()
    {
        if (playerMovement != null)
        {
            // PlayerMovement 스크립트에 수직 이동 제한 해제 메소드가 있다고 가정
            // playerMovement.EnableVerticalMovement();
            
            Debug.Log("수직 이동 제한 해제됨");
        }
    }

    private void UpdateHoverState()
    {
        // 부양 또는 윙슈트 모드 중이면 에너지 소모 및 시간 감소
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
                    
                    // 시간이 다 되면 호버링과 윙슈트 모드 모두 종료
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
            Debug.Log($"WingSuitEffect 상태: isHovering={isHovering}, isWingsuit={isWingsuitActive}, hoverTimeRemaining={hoverTimeRemaining:F1}, 현재 중력 스케일={playerRb?.gravityScale}");
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
        // 수직 이동 제한 해제
        EnableVerticalMovement();
        Debug.Log("WingSuitEffect 비활성화로 중력 스케일 복원 및 이동 제한 해제");
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
        else
        {
            statusText = "윙슈트 상태: 비활성화";
        }
        
        return statusText + $"\n남은 시간: {hoverTimeRemaining:F1}/{hoverDuration:F1}초\n" +
               $"중력 스케일: {(playerRb != null ? playerRb.gravityScale.ToString("F2") : "N/A")}\n" +
               "조작: 위쪽 화살표 더블 탭 = 호버링, C키 = 윙슈트 모드";
    }
}