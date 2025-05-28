using UnityEngine;

public class PressurePlateTop : MonoBehaviour
{
    private ObjectPressurePlate parentPlate;

    public void Initialize(ObjectPressurePlate plate)
    {
        parentPlate = plate;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(parentPlate.ActivatedTag))
        {
            parentPlate.ActivePlateTop(gameObject);
            Debug.Log("윗발판에 플레이어 감지");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(parentPlate.ActivatedTag))
        {
            parentPlate.DeactivatePlateTop(gameObject);
            Debug.Log("윗발판에서 플레이어 이탈");
        }
    }
}