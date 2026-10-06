using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
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

    public void DefinirNoChao(bool estaNoChao)
    {
        if (animator != null)
        {
            animator.SetBool("isGrounded", estaNoChao);
        }
    }

    /// <summary>
    /// Força a animação de pulo a reiniciar do frame 0 (Ideal para Pulo Duplo)
    /// </summary>
    /// <summary>
    /// Reinicia a animação de pulo correta (parada ou andando) do frame 0 no pulo duplo.
    /// </summary>
    public void ReiniciarPulo(bool estaAndando)
    {
        if (animator == null) return;

        string nomeAnimacao = estaAndando ? "kayapulando" : "kayapulandoparada";
        animator.Play(nomeAnimacao, 0, 0f);
    }

    public void AjustarVelocidadePulo(float multiplicador)
    {
        if (animator != null)
        {
            animator.SetFloat("jumpSpeed", multiplicador);
        }
    }

    public void AtualizarEstados(bool estaAndando, bool estaNoChao, bool estaDandoDash = false)
    {
        if (animator == null) return;

        animator.SetBool("isWalking", estaAndando);
        animator.SetBool("isGrounded", estaNoChao);
        animator.SetBool("isDashing", estaDandoDash);
    }
}