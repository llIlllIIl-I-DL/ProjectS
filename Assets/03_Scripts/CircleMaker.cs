using UnityEngine;

public class JellyCreator : MonoBehaviour
{
    public GameObject pointPrefab;
    public int pointCount = 8;
    public float radius = 1f;
    public Rigidbody2D centerBody;

    [ContextMenu("Create Jelly Points")]
    public void CreateJellyPoints()
    {
        Transform[] points = new Transform[pointCount];

        float angleStep = 360f / pointCount;

        // 포인트 생성 및 중심 연결
        for (int i = 0; i < pointCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            GameObject pointObj = Instantiate(pointPrefab, transform.position + (Vector3)pos, Quaternion.identity, transform);
            pointObj.name = $"Point_{i + 1}";
            Rigidbody2D rb = pointObj.GetComponent<Rigidbody2D>();

            SpringJoint2D spring = pointObj.AddComponent<SpringJoint2D>();
            spring.connectedBody = centerBody;
            spring.distance = radius;
            spring.dampingRatio = 0.8f;
            spring.frequency = 3f;

            points[i] = pointObj.transform;
        }

        // 외곽끼리 DistanceJoint로 연결
        for (int i = 0; i < pointCount; i++)
        {
            Rigidbody2D rb = points[i].GetComponent<Rigidbody2D>();
            Rigidbody2D nextRb = points[(i + 1) % pointCount].GetComponent<Rigidbody2D>(); // 마지막은 0번과 연결

            DistanceJoint2D distanceJoint = rb.gameObject.AddComponent<DistanceJoint2D>();
            distanceJoint.connectedBody = nextRb;
            distanceJoint.autoConfigureDistance = false;
            distanceJoint.distance = Vector2.Distance(rb.position, nextRb.position);
            // Removed invalid property 'dampingRatio'
            distanceJoint.maxDistanceOnly = false; // Adjusted to use a valid property
        }
    }
}
