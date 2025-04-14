using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("아이템 움직임")]
    [SerializeField] private float floatAmplitude = 0.1f;
    [SerializeField] private float floatFrequency = 1f;
    private Vector3 startPosition;
    
    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        if (itemData != null)
        {
            UpdateVisual();
        }
        
        startPosition = transform.position;
    }
    
    private void Update()
    {
        // 아이템 위아래 움직임
        if (gameObject.activeSelf)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    public void SetItemData(ItemData data)
    {
        this.itemData = data;
        UpdateVisual();
    }
    
    private void UpdateVisual()
    {
        if (spriteRenderer != null && itemData != null && itemData.Icon != null)
        {
            spriteRenderer.sprite = itemData.Icon;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 회복 아이템인 경우 즉시 사용
            if (itemData.itemAttrivuteType == ItemAttrivuteType.HealItem)
            {
                PlayerHP playerHP = other.GetComponent<PlayerHP>();
                if (playerHP != null)
                {
                    playerHP.Heal(itemData.effectValue);
                    Debug.Log($"{itemData.ItemName}을(를) 획득하여 {itemData.effectValue}만큼 체력을 즉시 회복했습니다.");
                }
            }
            else if (itemData.itemAttrivuteType == ItemAttrivuteType.MaxHPUpItem)
            {
                PlayerHP playerHP = other.GetComponent<PlayerHP>();
                if (playerHP != null)
                {
                    playerHP.IncreaseMaxHP(itemData.effectValue);
                    Debug.Log($"{itemData.ItemName}을(를) 획득하여 최대 체력이 {itemData.effectValue}만큼 증가했습니다.");
                }
            }
            // 그 외 아이템은 인벤토리에 추가
            else if (ItemManager.Instance != null)
            {
                ItemManager.Instance.AddItem(itemData);
            }
            
            // 아이템 오브젝트 비활성화 또는 제거
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }
    }
} 