using UnityEngine;
using System.Collections;

public class BossIdleState : IEnemyState
{
    private BossStateMachine BossStateMachine;
    private Transform bossTransform;
    private Transform playerTransform;
    private Animator animator;

    [SerializeField] private float detectionRange = 10f; //플레이어를 감지하는 거리
    [SerializeField] private float attackRange = 5f; //근접 공격 거리

    private bool canKick = true;         // Kick 가능 여부
    private float kickCooldown = 20f;    // Kick 쿨타임 (초)

    public BossIdleState(BossStateMachine stateMachine)
    {
        BossStateMachine = stateMachine;
        bossTransform = stateMachine.transform;
        playerTransform = stateMachine.playerTransform;
        animator = stateMachine.GetComponent<Animator>();
    }

    public void Enter() // 상태에 진입했을 때
    {  
        Debug.Log("Boss Idle 상태 진입");
        
        // 애니메이션 상수를 이용하자
        animator.SetBool("IsIdle", true);
    }

    public void Exit()// 상태에서 나갈 때
    {
        animator.SetBool("IsIdle", false);
    }

    public void Update()// 매 프레임 업데이트
    {
        if (playerTransform == null || bossTransform == null) return;

        float distance = Vector3.Distance(playerTransform.position, bossTransform.position); // 계속해서 거리 확인 하기.

        Debug.Log($"{distance},{attackRange}");
        
        if(distance >= detectionRange){
            //감지 거리보다 플레이어가 멀리 있을 경우
            Debug.Log("플레이어쪽으로 이동###");
            BossStateMachine.ChangeState(BossState.Move);
        }
        else if (distance >= attackRange){
            //감지 거리보다는 가깝고 근접 공격 거리보다는 멀 경우
            Debug.Log("원거리 공격###");
            BossStateMachine.ChangeState(BossState.ProjectileAttack);
        }
        else //근접 공격 거리 안에 들어와있을 경우
        {
            Debug.Log("근거리 공격###");
            BossStateMachine.ChangeState(BossState.SlashAttack);
        }


    }

    public void FixedUpdate()// 물리 업데이트
    {

    }

    public void OnTriggerEnter2D(Collider2D other)// 트리거 충돌 감지
    {
        if (other.CompareTag("Player"))
        {
            if (BossStateMachine.CanKick)
            {
                Debug.Log("Kick 공격 진입!###");
                BossStateMachine.ChangeState(BossState.KickAttack);
            }
            else
            {
                float remain = BossStateMachine.KickCooldown - (Time.time - BossStateMachine.LastKickTime);
                Debug.Log($"Kick 쿨다운 중... 남은 시간: {remain:F1}초");
            }
        }
    }
}