using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    public Animator animator;

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void SetMoving(bool isMoving)
    {
        animator.SetBool("IsMoving", isMoving);
    }

    public void PlayJump()
    {
        animator.SetTrigger("Jump");
    }

    public void PlayAttack()
    {
        animator.SetTrigger("Attack");
    }

    public void PlayHit()
    {
        animator.SetTrigger("Hit");
    }

    public void PlayDeath()
    {
        animator.SetTrigger("Death");
    }
}
