using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class InimigoBixel : MonoBehaviour
{
    [Header("Inversão do Sprite")]
    public bool spriteInvertido = true;

    [Header("Patrulha")]
    public float velocidade = 2.5f;
    public Vector2 offsetChecagem = new Vector2(0.6f, -0.4f);
    public float distanciaChecagem = 0.5f;
    public LayerMask camadaChao;
    public float cooldownVirada = 0.5f;

    [Header("Animação")]
    public string parametroAndando = "isWalking";

    [Header("Vida e Impacto")]
    public int vidaMaxima = 3;
    private int vidaAtual;
    public int danoNoJogador = 1;
    public float forcaImpacto = 10f;
    public float cooldownImpacto = 0.8f;
    private float tempoProximoImpacto;

    private Rigidbody2D rb;
    private Animator anim;
    private bool movendoParaDireita = true;
    private float tempoProximaVirada;
    private Vector3 escalaOriginal;

    private void Awake()
    {
        Physics2D.queriesStartInColliders = false;
    }

    private void Start()
    {
        vidaAtual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();

        rb.freezeRotation = true;
        if (rb.gravityScale <= 0) rb.gravityScale = 3f;

        escalaOriginal = transform.localScale;
        AtualizarEscala();
    }

    private void FixedUpdate()
    {
        Patrulhar();
    }

    private void Update()
    {
        AtualizarAnimacao();
    }

    private void Patrulhar()
    {
        float direcao = movendoParaDireita ? 1f : -1f;

        rb.linearVelocity = new Vector2(direcao * velocidade, rb.linearVelocity.y);

        if (Time.time < tempoProximaVirada) return;

        Vector2 ponto = (Vector2)transform.position + new Vector2(offsetChecagem.x * direcao, offsetChecagem.y);

        bool temChao = Physics2D.Raycast(ponto, Vector2.down, distanciaChecagem, camadaChao);
        bool temParede = Physics2D.Raycast(ponto, Vector2.right * direcao, distanciaChecagem, camadaChao);

        if (!temChao || temParede)
        {
            Virar();
        }
    }

    private void Virar()
    {
        tempoProximaVirada = Time.time + cooldownVirada;
        movendoParaDireita = !movendoParaDireita;
        AtualizarEscala();
    }

    private void AtualizarEscala()
    {
        float sinal = movendoParaDireita ? Mathf.Abs(escalaOriginal.x) : -Mathf.Abs(escalaOriginal.x);
        if (spriteInvertido) sinal *= -1f;
        transform.localScale = new Vector3(sinal, escalaOriginal.y, escalaOriginal.z);
    }

    private void AtualizarAnimacao()
    {
        if (anim != null && !string.IsNullOrEmpty(parametroAndando))
        {
            bool estaAndando = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            anim.SetBool(parametroAndando, estaAndando);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) => TentarAplicarImpacto(collision.gameObject);
    private void OnCollisionStay2D(Collision2D collision) => TentarAplicarImpacto(collision.gameObject);

    private void TentarAplicarImpacto(GameObject jogadorObj)
    {
        if (!jogadorObj.CompareTag("Player") || Time.time < tempoProximoImpacto) return;

        Rigidbody2D rbPlayer = jogadorObj.GetComponent<Rigidbody2D>();
        if (rbPlayer != null)
        {
            tempoProximoImpacto = Time.time + cooldownImpacto;
            Vector2 direcaoImpacto = (jogadorObj.transform.position - transform.position).normalized;
            direcaoImpacto.y = Mathf.Clamp(direcaoImpacto.y + 0.3f, 0.4f, 0.8f);

            rbPlayer.linearVelocity = Vector2.zero;
            rbPlayer.AddForce(direcaoImpacto * forcaImpacto, ForceMode2D.Impulse);
        }
    }

    public void TomarDano(int quantidadeDano)
    {
        vidaAtual -= quantidadeDano;
        if (vidaAtual <= 0) Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        float direcao = movendoParaDireita ? 1f : -1f;
        Vector2 ponto = (Vector2)transform.position + new Vector2(offsetChecagem.x * direcao, offsetChecagem.y);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(ponto, 0.08f);
        Gizmos.DrawRay(ponto, Vector2.down * distanciaChecagem);
        Gizmos.DrawRay(ponto, Vector2.right * direcao * distanciaChecagem);
    }
}