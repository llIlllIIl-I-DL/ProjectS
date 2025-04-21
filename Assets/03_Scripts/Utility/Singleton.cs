using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 제네릭 싱글톤 
/// </summary>
/// <typeparam name="T">싱글톤 인스턴스로 만들 클래스 타입</typeparam>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<T>();
                if (_instance == null)
                {
                    GameObject singletonObj = new GameObject(typeof(T).Name);
                    _instance = singletonObj.AddComponent<T>();
                    // 루트 오브젝트이므로 DontDestroyOnLoad 호출 가능
                    DontDestroyOnLoad(singletonObj);
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            
            // 오브젝트가 다른 게임오브젝트의 자식이면 부모에서 분리
            if (transform.parent != null)
            {
                Debug.Log($"싱글톤 객체 '{gameObject.name}'는 자식 오브젝트입니다. DontDestroyOnLoad를 위해 부모에서 분리합니다.");
                transform.SetParent(null);
            }
            
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
