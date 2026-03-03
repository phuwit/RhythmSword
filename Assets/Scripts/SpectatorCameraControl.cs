using UnityEngine;

public class SpectatorCameraControl : MonoBehaviour {
    [SerializeField] private Transform target;
    [SerializeField][Range(0f, 1f)] private float positionDampingStrength = 0.875f;
    [SerializeField][Range(0f, 1f)] private float rotationDampingStrength = 0.875f;

    void Start() {
        target.GetPositionAndRotation(out Vector3 targetPosition, out Quaternion targetRotation);
        transform.SetPositionAndRotation(targetPosition, targetRotation);
    }

    void Update() {
        target.GetPositionAndRotation(out Vector3 targetPosition, out Quaternion targetRotation);
        transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
        transform.SetPositionAndRotation(
            Vector3.Lerp(position, targetPosition, 1f - positionDampingStrength),
            Quaternion.Lerp(rotation, targetRotation, 1f - rotationDampingStrength)
        );
    }
}
