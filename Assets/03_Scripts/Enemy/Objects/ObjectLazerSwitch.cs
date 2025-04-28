using System.Collections;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 레이저에 반응하는 스위치
/// </summary>
public class LaserActivatedSwitch : MonoBehaviour, ILaserInteractable
{
    [SerializeField] private float activationThreshold = 0.5f; // 활성화 임계값
    [SerializeField] private float deactivationDelay = 1.0f;   // 비활성화 지연 시간
    [SerializeField] private GameObject activatedObject;       // 활성화할 오브젝트
    
    private float activationTimer = 0f;
    private bool isActivated = false;
    private SpriteRenderer spriteRenderer;
    
    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    private void Update()
    {
        // 타이머가 0보다 크면 감소
        if (activationTimer > 0)
        {
            activationTimer -= Time.deltaTime;
            
            // 타이머가 0이 되면 비활성화
            if (activationTimer <= 0 && isActivated)
            {
                isActivated = false;
                if (activatedObject != null)
                    activatedObject.SetActive(false);
                    
                // 색상 변경
                if (spriteRenderer != null)
                    spriteRenderer.color = Color.white;
            }
        }
    }
    
    /// <summary>
    /// 레이저 히트 처리
    /// </summary>
    public void OnLaserHit(Vector2 hitPoint, Vector2 direction)
    {
        
    }
}