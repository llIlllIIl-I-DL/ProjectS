using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InteractionKeyInput
{
    public static bool BlockAllExceptF { get; set; }

    public static bool GetKeyDown(KeyCode key)
    {
        // 대화 중 & input 키가 F가 아니라면 무시!!
        if (BlockAllExceptF && key != KeyCode.F)
        return false;

        return Input.GetKeyDown(key);
    }

}
