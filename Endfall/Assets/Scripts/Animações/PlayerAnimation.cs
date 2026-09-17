using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void DefinirAndando(bool estaAndando)
    {
        if (animator != null)
        {
            animator.SetBool("isWalking", estaAndando);
        }
    }

    public void DefinirDash(bool estaDandoDash)
    {
        if (animator != null)
        {
            animator.SetBool("isDashing", estaDandoDash);
        }
    }
}