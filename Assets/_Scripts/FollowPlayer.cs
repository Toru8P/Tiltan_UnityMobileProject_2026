using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [Header("Follow Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 3f, -6f);
    [SerializeField, Tooltip("Higher = snappier follow")] private float followSpeed = 8f;
    [SerializeField] private bool smoothFollow = true;
    [SerializeField, Tooltip("When true camera will rotate to look at the player")] private bool lookAtPlayer = true;
    [SerializeField, Tooltip("Optional local look offset (head height)")] private Vector3 lookOffset = new Vector3(0f, 1.5f, 0f);

    private Transform camTransform;

    void Start()
    {
        camTransform = transform;

        // Try to find player by tag if none assigned
        if (playerTransform == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null)
                playerTransform = go.transform;
        }

        if (playerTransform == null)
            Debug.LogWarning("FollowPlayer: no playerTransform assigned and no GameObject with tag 'Player' found.");
    }

    void LateUpdate()
    {
        if (playerTransform == null)
            return;

        Vector3 desiredPosition = playerTransform.position + offset;

        if (smoothFollow)
        {
            // Damped smoothing that is frame-rate independent
            float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            camTransform.position = Vector3.Lerp(camTransform.position, desiredPosition, t);
        }
        else
        {
            camTransform.position = desiredPosition;
        }

        if (lookAtPlayer)
        {
            camTransform.LookAt(playerTransform.position + lookOffset);
        }
    }

#if UNITY_EDITOR
    // Draw offset gizmo in editor for easier tuning
    void OnDrawGizmosSelected()
    {
        if (playerTransform == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(playerTransform.position, playerTransform.position + offset);
        Gizmos.DrawSphere(playerTransform.position + offset, 0.1f);
    }
#endif
}
