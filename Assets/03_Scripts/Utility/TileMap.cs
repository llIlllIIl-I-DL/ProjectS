using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

#if UNITY_EDITOR
[ExecuteInEditMode]
public class TilemapCenterPivot : MonoBehaviour
{
    void Reset()
    {
        CenterPivot();
    }
    [ContextMenu("Center All Tilemaps")]
    public void CenterPivot()
    {
        var tilemaps = GetComponentsInChildren<Tilemap>(true);
        foreach (var tm in tilemaps)
        {
            // Tilemap의 로컬 바운드 중심
            Vector3 localCenter = tm.localBounds.center;
            // 해당 중심을 0,0으로 보정
            tm.transform.localPosition = -localCenter;

            // TilemapRenderer 앵커를 중앙으로 설정
            var rend = tm.GetComponent<TilemapRenderer>();
            if (rend != null)
            {
                // 'tileAnchor'는 TilemapRenderer가 아닌 Tilemap에 속합니다.  
                tm.tileAnchor = new Vector3(0.5f, 0.5f, 0);
            }
        }
    }
}
#endif