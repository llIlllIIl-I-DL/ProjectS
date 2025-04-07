using UnityEngine;

public interface IEnemyState
{
    void Enter();           // 상태에 진입했을 때
    void Exit();            // 상태에서 나갈 때
    void Update();          // 매 프레임 업데이트
    void FixedUpdate();     // 물리 업데이트
    void OnTriggerEnter2D(Collider2D other);  // 트리거 충돌 감지
}