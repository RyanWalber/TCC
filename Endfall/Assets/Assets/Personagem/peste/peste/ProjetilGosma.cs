using UnityEngine;

public class ProjetilGosma : MonoBehaviour
{
    [Header("CONFIGURAÇÕES DE QUEDA")]
    public float velocidadeQueda = 12f;
    public float tempoAnimacaoImpacto = 0.4f;

    [Header("CONFIGURAÇÃO DE CHÃO")]
    [Tooltip("Digite a Tag usada no seu chão/Tilemap (Ex: Chao, Ground, Plataforma)")]
    public string tagDoChao = "Chao";

    private Rigidbody2D rb;
    private Animator anim;
    private Collider2D col;
    private bool jaImpactou = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (!jaImpactou)
        {
            // Queda 100% reta para baixo
            rb.linearVelocity = new Vector2(0f, -velocidadeQueda);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verifica se o objeto onde bateu é o Chão (por Tag ou por Layer)
        bool eChaoPorTag = collision.CompareTag(tagDoChao);
        bool eChaoPorLayer = collision.gameObject.layer == LayerMask.NameToLayer("Ground");

        // SE NÃO FOR O CHÃO (ex: BordaCamera, Boss, etc), IGNORA E CONTINUA CAINDO
        if (!eChaoPorTag && !eChaoPorLayer)
        {
            return;
        }

        // Se já bateu no chão antes, não faz nada
        if (jaImpactou) return;

        // BATEU NO CHÃO!
        jaImpactou = true;

        // Congela a gosma no chão
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        if (col != null) col.enabled = false;

        // Toca a animação de espatifar
        if (anim != null)
        {
            anim.SetTrigger("Impacto");
        }

        // Destrói após a animação terminar
        Destroy(gameObject, tempoAnimacaoImpacto);
    }
}