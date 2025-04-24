using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public enum BossState
{
    Idle,
    ProjectileAttack,
    SmashAttack,
    Move,
    Die,
}
public class BossStateMachine : MonoBehaviour
{
    protected IEnemyState currentState;
    private Dictionary<BossState, IEnemyState> states = new Dictionary<BossState, IEnemyState>();

    public Transform playerTransform; //플레이어 위치 참조
    public GameObject projectilePrefab; //투사체 프리팹
    public Transform firePoint; //투사체 발사 위치
    public GameObject slashEffectPrefab; // 휘두르기 이펙트 추가
    public GameObject kickEffectPrefab; // 발차기 이펙트 추가


    public void Start()
    {

        playerTransform = GameObject.FindWithTag("Player")?.transform;

        states.Add(BossState.Idle, new BossIdleState(this));
        states.Add(BossState.ProjectileAttack, new BossProjectileAttackState(this));
        states.Add(BossState.SmashAttack, new BossSmashAttackState(this));
        states.Add(BossState.Move, new BossMoveState(this));
        states.Add(BossState.Die, new BossDieState(this));

        ChangeState(BossState.Idle);
    }

    public void ChangeState(BossState state)
    {
        currentState?.Exit();
        currentState = states[state];
        currentState?.Enter();
    }

    public void Update()
    {
        currentState?.Update();
    }

    public void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 10f); // detectionRange
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 5f);  // attackRange
    }
}
