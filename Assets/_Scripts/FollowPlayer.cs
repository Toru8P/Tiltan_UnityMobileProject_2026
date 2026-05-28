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

    // Forces the device into landscape orientation when the game starts (this is a mobile game).
    void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeRight;
    }


    // LateUpdate runs after every other Update — so the player has already moved this frame.
    // The camera computes a fixed diagonal offset from the player and smoothly slides toward that position,
    // then looks at a point slightly above the player.
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
