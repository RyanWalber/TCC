using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private bool estavaNoChaoAnteriormente;

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

        // Atualiza os parâmetros normais
        animator.SetBool("isWalking", estaAndando);
        animator.SetBool("isGrounded", estaNoChao);
        animator.SetBool("isDashing", estaDandoDash);

        // FORÇA O CORTE DA ANIMAÇÃO DE PULO NO MOMENTO EXATO DA ATERRISSAGEM
        if (estaNoChao && !estavaNoChaoAnteriormente)
        {
            if (estaAndando)
            {
                // Força o início imediato do ciclo de corrida
                animator.Play("kayandando", 0, 0f);
            }
            else
            {
                // Força o início imediato do estado ocioso
                animator.Play("kayaociosa", 0, 0f);
            }
        }

        estavaNoChaoAnteriormente = estaNoChao;
    }
}