using UnityEngine;

public class BossAnimation : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void DefinirBravo(bool estaBravo)
    {
        if (animator != null)
        {
            animator.SetBool("estaBravo", estaBravo);
        }
    }

    public void DispararAtaque()
    {
        if (animator != null)
        {
            animator.SetTrigger("atacar");
        }
    }
}