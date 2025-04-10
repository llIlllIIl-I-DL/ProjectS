using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDamage : MonoBehaviour
{
    public PlayerHP playerHP;
    // Update is called once per frame
    void Update()
    {
        KeyCode key = KeyCode.Space;

        if (Input.GetKeyDown(key))
        {
            playerHP.TakeDamage(10); // 데미지 값은 필요에 따라 조정하세요.
            // PlayerHP 클래스의 TakeDamage 메서드를 호출하여 데미지를 입힙니다.
            Debug.Log("데미지 10");
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            playerHP.Heal(25); // 회복 값은 필요에 따라 조정하세요.
            // PlayerHP 클래스의 Heal 메서드를 호출하여 회복합니다.
            Debug.Log("회복 25");
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            playerHP.IncreaseMaxHP(10); // 최대 HP 증가 값은 필요에 따라 조정하세요.
            // PlayerHP 클래스의 IncreaseMaxHP 메서드를 호출하여 최대 HP를 증가시킵니다.
            Debug.Log("최대 HP 증가 10");
        }
    }
}
