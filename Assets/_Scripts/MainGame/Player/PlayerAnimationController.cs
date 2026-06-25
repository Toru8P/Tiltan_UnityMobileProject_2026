using UnityEngine;

namespace _Scripts.MainGame.Player
{
    public class PlayerAnimationController : MonoBehaviour
    {
        public Animator animator;

        // If no Animator was assigned in the Inspector, automatically find one on a child object.
        void Start()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        // Switches between idle and walk animations. Called from the movement controller whenever movement state changes.
        public void SetMoving(bool isMoving)
        {
            animator.SetBool("IsMoving", isMoving);
        }

        // Fires the jump animation one time.
        public void PlayJump()
        {
            animator.SetTrigger("Jump");
        }

        // Fires the attack animation one time. Called when the player presses the attack button.
        public void PlayAttack()
        {
            animator.SetTrigger("Attack");
        }

        // Fires the "got hit" animation one time. Reserved for when an enemy damages the player.
        public void PlayHit()
        {
            animator.SetTrigger("Hit");
        }

        // Fires the death animation one time. Reserved for when the player dies.
        public void PlayDeath()
        {
            animator.SetTrigger("Death");
        }

        // Fires the roll animation.
        public void PlayRoll()
        {
            animator.SetTrigger("Roll");
        }

        // Resets the hit trigger to prevent it from playing if it was queued during an immune state.
        public void ResetHit()
        {
            animator.ResetTrigger("Hit");
        }
    }
}
