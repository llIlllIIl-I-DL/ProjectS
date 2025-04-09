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
    }
}
