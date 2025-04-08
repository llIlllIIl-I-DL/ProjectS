using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적의 상태를 관리하는 상태 머신 클래스
/// </summary>
public class EnemyStateMachine
{
    #region Properties and Fields
    
    /// <summary>
    /// 현재 활성화된 상태
    /// </summary>
    private IEnemyState currentState;

    /// <summary>
    /// 현재 상태 접근자 (읽기 전용)
    /// </summary>
    public IEnemyState CurrentState => currentState;
    
    /// <summary>
    /// 상태 전환 이력 (디버깅용)
    /// </summary>
    private List<string> stateHistory = new List<string>();
    
    #endregion
    
    #region State Management
    
    /// <summary>
    /// 상태를 새로운 상태로 전환합니다.
    /// </summary>
    /// <param name="newState">전환할 새 상태</param>
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
    
    #endregion
    
    #region Update Methods
    
    /// <summary>
    /// 프레임 기반 업데이트 - 현재 상태의 Update 메서드를 호출합니다.
    /// </summary>
    public void Update()
    {
        currentState?.Update();
    }
    
    /// <summary>
    /// 물리 기반 업데이트 - 현재 상태의 FixedUpdate 메서드를 호출합니다.
    /// </summary>
    public void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }
    
    /// <summary>
    /// 트리거 이벤트 처리 - 현재 상태의 OnTriggerEnter2D 메서드를 호출합니다.
    /// </summary>
    /// <param name="other">충돌한 콜라이더</param>
    public void OnTriggerEnter2D(Collider2D other)
    {
        currentState?.OnTriggerEnter2D(other);
    }
    
    #endregion
    
    #region Utility and Debug
    
    /// <summary>
    /// 현재 상태의 이름을 반환합니다.
    /// </summary>
    /// <returns>현재 상태 이름 또는 "No State"</returns>
    public string GetCurrentStateName()
    {
        return currentState != null ? currentState.GetType().Name : "No State";
    }
    
    /// <summary>
    /// 상태 전환 이력을 배열로 반환합니다. (디버그용)
    /// </summary>
    /// <returns>상태 이름 배열</returns>
    public string[] GetStateHistory()
    {
        return stateHistory.ToArray();
    }
    
    #endregion
}