using UnityEngine;
using UnityEditor;

#if UNITY_EIDTOR
public class FixVHierarchyDefaultParent : EditorWindow
{
    [MenuItem("Tools/Fix VHierarchy Default Parent Error")]
    public static void FixVHierarchyError()
    {
        // vHierarchy의 setDefaultParentEnabled 설정을 비활성화합니다
        EditorPrefs.SetBool("vHierarchy-setDefaultParentEnabled", false);
        
        Debug.Log("VHierarchy의 'setDefaultParentEnabled' 설정이 비활성화되었습니다. 이제 오류가 해결되어야 합니다.");
        
        // 또는 메뉴를 통해 관리하려면 아래 메뉴 경로를 사용할 수 있습니다:
        // Tools > vHierarchy > Shortcuts > D to set default parent
    }
}
#endif