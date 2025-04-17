using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDamage : MonoBehaviour
{
    public PlayerHP playerHP;
    public PlayerMovement playerMovement;

    private void Start()
    {
        // PlayerMovement가 할당되지 않았다면 자동으로 찾아보기
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.LogWarning("PlayerMovement를 찾을 수 없습니다. F키 기능이 작동하지 않을 수 있습니다.");
            }
        }

        // PlayerHP가 할당되지 않았다면 자동으로 찾아보기
        if (playerHP == null)
        {
            playerHP = FindObjectOfType<PlayerHP>();
            if (playerHP == null)
            {
                Debug.LogWarning("PlayerHP를 찾을 수 없습니다. 데미지 및 회복 기능이 작동하지 않을 수 있습니다.");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        KeyCode key = KeyCode.Space;

        if (Input.GetKeyDown(key) && playerHP != null)
        {
            playerHP.TakeDamage(10); // 데미지 값은 필요에 따라 조정하세요.
            // PlayerHP 클래스의 TakeDamage 메서드를 호출하여 데미지를 입힙니다.
            Debug.Log("데미지 10");
        }
        if (Input.GetKeyDown(KeyCode.H) && playerHP != null)
        {
            playerHP.Heal(25); // 회복 값은 필요에 따라 조정하세요.
            PlayerUI.Instance.HealHP();
            // PlayerHP 클래스의 Heal 메서드를 호출하여 회복합니다.
            Debug.Log("회복 25");
        }

        if (Input.GetKeyDown(KeyCode.G) && playerHP != null)
        {
            playerHP.IncreaseMaxHP(10); // 최대 HP 증가 값은 필요에 따라 조정하세요.
            // PlayerHP 클래스의 IncreaseMaxHP 메서드를 호출하여 최대 HP를 증가시킵니다.
            Debug.Log("최대 HP 증가 10");
        }
    }

}
