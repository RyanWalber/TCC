using UnityEngine;

public class BossAnimation : MonoBehaviour
{
    private Animator animator;
    private BossGuerraController controller;

    void Awake()
    {
        animator = GetComponent<Animator>();
        // Pega a referência do controller que está no pai (GuerraBoss)
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

    // Método que vai aparecer no Animation Event
    public void InstanciarBarril()
    {
        if (controller != null)
        {
            controller.InstanciarBarril();
        }
    }
}