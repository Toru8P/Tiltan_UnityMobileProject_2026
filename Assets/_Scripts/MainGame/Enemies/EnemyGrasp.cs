using UnityEngine;

namespace _Scripts.Enemies
{
    public class EnemyGrasp : MonoBehaviour
    {
        [Header("Fingers to Curl")]
        public Transform[] fingerBones;
        public Vector3 curlRotation = new Vector3(30, 0, 0);

        [Header("Thumb Bones")]
        public Transform[] thumbBones;
        public Vector3 thumbRotation = new Vector3(-20, 0, 0);

        // LateUpdate runs AFTER the animator has set bone rotations.
        // We forcibly override the finger and thumb bones to a curled "grabby" pose every frame,
        // so the zombie's hands always look menacing regardless of which animation is playing.
        private void LateUpdate()
        {
            if (fingerBones != null)
            {
                foreach (var bone in fingerBones)
                {
                    if (bone != null)
                        bone.localRotation = Quaternion.Euler(curlRotation);
                }
            }

            if (thumbBones != null)
            {
                foreach (var bone in thumbBones)
                {
                    if (bone != null)
                        bone.localRotation = Quaternion.Euler(thumbRotation);
                }
            }
        }
    }
}