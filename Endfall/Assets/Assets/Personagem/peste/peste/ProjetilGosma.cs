using UnityEngine;

public class ProjetilGosma : MonoBehaviour
{
    [Header("CONFIGURAÇÃO DE QUEDA E DANO")]
    public float velocidadeQueda = 12f;
    public float dano = 15f; // Valor de dano enviado ao PlayerHealth
    public float tempoAnimacaoImpacto = 0.4f;

    [Header("TAGS")]
    public string tagDoChao = "Chao";
    public string tagDoPlayer = "Player";

    private Animator anim;
    private Collider2D col;
    private bool jaImpactou = false;

    private void Awake()
    {
        // Busca o Animator no objeto filho (Visual)
        anim = GetComponentInChildren<Animator>();
        col = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (jaImpactou) return;

        // Queda contínua no eixo Y global do mundo
        transform.position += Vector3.down * velocidadeQueda * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (jaImpactou) return;

        // 1. COLISÃO COM O JOGADOR
        if (collision.CompareTag(tagDoPlayer))
        {
            // Procura o PlayerHealth no objeto ou nos pais dele (caso o collider esteja em um filho)
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = collision.GetComponentInParent<PlayerHealth>();
            }

            if (playerHealth != null)
            {
                playerHealth.TomarDano(dano); // Dispara a barra, fúria, tremer de tela e o flash vermelho
            }

            ExecutarImpacto();
            return;
        }

        // 2. COLISÃO COM O CHÃO
        bool eChao = collision.CompareTag(tagDoChao) || collision.gameObject.layer == LayerMask.NameToLayer("Ground");
        if (eChao)
        {
            ExecutarImpacto();
        }
    }

    private void ExecutarImpacto()
    {
        jaImpactou = true;

        if (col != null) col.enabled = false;

        if (anim != null)
        {
            anim.SetTrigger("Impacto");
        }

        Destroy(gameObject, tempoAnimacaoImpacto);
    }
}