using UnityEngine;

public class DekeloniCamera : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 10f;

    public float cameraAngle = 45f;
    public float cameraDistance = 6f;
    public float cameraHeight = 8f;
    public float horizontalOffset = -2f;

    public Vector3 lookOffset = new Vector3(0, 2, 0);

    void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeRight;
    }

    
    void LateUpdate()
    {
        if (!target) return;

        // FIXED world-space diagonal offset (NOT using target.forward)
        Vector3 offset = new Vector3(
            -cameraDistance + horizontalOffset,
            cameraHeight,
            -cameraDistance
        );

        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        transform.LookAt(target.position + lookOffset);
    }
}
