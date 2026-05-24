using UnityEngine;

namespace _Scripts
{
    public class FollowPlayer : MonoBehaviour
    {
        public Transform target;
        public float smoothSpeed = 10f;
        public float cameraDistance = 8f;
        public float cameraHeight = 12f;
        
        public Vector3 lookOffset = new Vector3(0, 2, 0);
    
        void LateUpdate()
        {
            if (!target) return;

            // Position camera behind target based on target's rotation
            Vector3 targetBackDirection = -target.forward;
            Vector3 desiredPosition = target.position + targetBackDirection * cameraDistance + Vector3.up * cameraHeight;
        
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.LookAt(target.position + lookOffset);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            if (target)
            {
                Vector3 targetPosition = target.position + lookOffset;
                Gizmos.DrawWireSphere(targetPosition, 0.2f);
                Gizmos.DrawLine(transform.position, targetPosition);
            }
        }


    }
}
