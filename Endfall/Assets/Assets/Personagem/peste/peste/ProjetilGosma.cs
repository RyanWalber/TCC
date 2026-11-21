using UnityEngine;

public class ProjetilGosma : MonoBehaviour
{
    [Header("Audio desta gosma (inclusive ao ser cuspida)")]
    [SerializeField] private AudioInimigo audioGosma = new AudioInimigo();

    [Header("CONFIGURAÇÃO DE QUEDA E DANO")]
    public float velocidadeQueda = 12f;
    public float dano = 15f;
    public float tempoAnimacaoImpacto = 0.4f;

    [Header("TAGS")]
    public string tagDoChao = "Chao";
    public string tagDoPlayer = "Player";

    private Animator anim;
    private Collider2D col;
    private bool jaImpactou = false;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        col = GetComponent<Collider2D>();
    }

    private void Start()
    {
        audioGosma.TocarInvocacao(this);
    }

    private void Update()
    {
        if (jaImpactou) return;

        transform.position += Vector3.down * velocidadeQueda * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (jaImpactou) return;

        if (collision.CompareTag(tagDoPlayer))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = collision.GetComponentInParent<PlayerHealth>();
            }

            if (playerHealth != null)
            {
                float vidaAnterior = playerHealth.VidaAtual;
                playerHealth.TomarDanoDeInimigo(dano);
                if (playerHealth.VidaAtual < vidaAnterior)
                    audioGosma.TocarHitCausado(this, true);
            }

            ExecutarImpacto();
            return;
        }

        bool eChao = collision.CompareTag(tagDoChao) || collision.gameObject.layer == LayerMask.NameToLayer("Ground");
        if (eChao)
        {
            ExecutarImpacto();
        }
    }

    private void ExecutarImpacto()
    {
        jaImpactou = true;
        audioGosma.TocarMorte(this);

        if (col != null) col.enabled = false;

        if (anim != null)
        {
            anim.SetTrigger("Impacto");
        }

        Destroy(gameObject, tempoAnimacaoImpacto);
    }
}