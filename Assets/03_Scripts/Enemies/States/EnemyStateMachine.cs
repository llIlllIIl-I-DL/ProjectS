using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적의 상태를 관리하는 상태 머신 클래스
/// </summary>

public class EnemyStateMachine
{
    // 현재 상태
    private IEnemyState currentState;
    
    // 상태 전환 이력 (디버깅용)
    private List<string> stateHistory = new List<string>();
    
    // 상태 전환
    public void ChangeState(IEnemyState newState)
    {
        // 이전 상태가 있다면 Exit 호출
        currentState?.Exit();
        
        // 새 상태로 전환
        currentState = newState;
        
        // 상태 전환 로깅
        if (currentState != null)
        {
            string stateName = currentState.GetType().Name;
            Debug.Log($"스테이트 전환 : {stateName}");
            
            // 상태 이력 저장 (최대 10개)
            stateHistory.Add(stateName);
            if (stateHistory.Count > 10)
                stateHistory.RemoveAt(0);
        }
        
        // 새 상태 진입
        currentState?.Enter();
    }
    
    // 업데이트 로직
    public void Update()
    {
        currentState?.Update();
    }
    
    // 물리 업데이트 로직
    public void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }
    
    // 트리거 이벤트 전달
    public void OnTriggerEnter2D(Collider2D other)
    {
        currentState?.OnTriggerEnter2D(other);
    }
    
    // 현재 상태 이름 반환
    public string GetCurrentStateName()
    {
        return currentState != null ? currentState.GetType().Name : "No State";
    }
    
    // 상태 이력 반환 (디버그용)
    public string[] GetStateHistory()
    {
        return stateHistory.ToArray();
    }
}