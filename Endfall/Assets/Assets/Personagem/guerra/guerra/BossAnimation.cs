using UnityEngine;

public class BossAnimation : MonoBehaviour
{
    private Animator animator;
    private BossGuerraController controller;

    void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponentInParent<BossGuerraController>();
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

    public void InstanciarBarril()
    {
        if (controller != null)
        {
            controller.InstanciarBarril();
        }
    }
}