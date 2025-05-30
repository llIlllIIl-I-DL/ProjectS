using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueSystem : MonoBehaviour
{
    Dictionary<int, string[]> talkData;

    bool isNext = false;
    int dialogueCount = 0;
    int contextCount = 0;

    private void Awake()
    {
        talkData = new Dictionary<int, string[]>();
        GenerateData();
    }

    void GenerateData()
    {
        talkData.Add(100, new string[] { "으아아ㅏㅏ" });

        talkData.Add(101, new string[] { "호옹이" });
    }
}
