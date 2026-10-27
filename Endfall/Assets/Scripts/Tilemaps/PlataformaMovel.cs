using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlataformaMovel : MonoBehaviour
{
    public Vector3[] pontos;
    public float velocidade = 3f;
    public float tempoDeEspera = 1f;

    [Header("Ativacao opcional")]
    [Tooltip("Se marcado, a plataforma so inicia quando a Kaya pisar em cima. Desmarcado: funciona como antes.")]
    [SerializeField] private bool iniciarAoPisar = false;
    private bool movimentoLiberado;

    private int indicePontoAtual = 0;
    private float temporizadorEspera = 0f;
    private Vector3[] pontosGlobais;
    private Rigidbody2D rb;
    public Vector2 VelocidadeAtual { get; private set; }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        movimentoLiberado = !iniciarAoPisar;

        pontosGlobais = new Vector3[pontos.Length + 1];
        pontosGlobais[0] = transform.position;

        for (int i = 0; i < pontos.Length; i++)
        {
            pontosGlobais[i + 1] = transform.position + pontos[i];
        }
    }

    private void FixedUpdate()
    {
        VelocidadeAtual = Vector2.zero;
        if (!movimentoLiberado || pontosGlobais == null || pontosGlobais.Length <= 1) return;

        if (temporizadorEspera > 0f)
        {
            temporizadorEspera -= Time.fixedDeltaTime;
            return;
        }

        Vector2 destino = pontosGlobais[indicePontoAtual];
        Vector2 atual = rb.position;
        Vector2 proximaPosicao = Vector2.MoveTowards(atual, destino,
            Mathf.Max(0f, velocidade) * Time.fixedDeltaTime);

        // So a plataforma move seu Rigidbody. A Kaya recebe a velocidade
        // como suporte no PlayerController, sem concorrencia de MovePosition.
        VelocidadeAtual = (proximaPosicao - atual) / Time.fixedDeltaTime;
        rb.MovePosition(proximaPosicao);

        if (Vector2.Distance(proximaPosicao, destino) < 0.01f)
        {
            indicePontoAtual = (indicePontoAtual + 1) % pontosGlobais.Length;
            temporizadorEspera = tempoDeEspera;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        VerificarJogador(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        VerificarJogador(collision);
    }

    private void VerificarJogador(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        // Normal do contato na plataforma aponta para baixo quando
        // o jogador esta sobre ela.
        foreach (ContactPoint2D contato in collision.contacts)
        {
            if (contato.normal.y < -0.5f)
            {
                PlayerController controle = collision.gameObject.GetComponent<PlayerController>();
                if (controle == null) return;
                controle.DefinirPlataformaMovel(this);

                if (!movimentoLiberado && iniciarAoPisar)
                {
                    movimentoLiberado = true;
                    // O ponto 0 ja e a posicao inicial. Segue diretamente
                    // para o primeiro destino quando a Kaya pisa.
                    if (pontosGlobais != null && pontosGlobais.Length > 1)
                        indicePontoAtual = 1;
                }
                return;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        PlayerController controle = collision.gameObject.GetComponent<PlayerController>();
        if (controle != null) controle.RemoverPlataformaMovel(this);
    }

    private void OnDrawGizmosSelected()
    {
        if (pontos == null) return;

        Gizmos.color = Color.cyan;
        Vector3 posicaoInicial = Application.isPlaying && pontosGlobais != null ? pontosGlobais[0] : transform.position;
        Vector3 posicaoBase = posicaoInicial;

        Gizmos.DrawWireSphere(posicaoInicial, 0.3f);

        for (int i = 0; i < pontos.Length; i++)
        {
            Vector3 posicaoAtual = Application.isPlaying ? pontosGlobais[i + 1] : transform.position + pontos[i];
            Gizmos.DrawWireSphere(posicaoAtual, 0.3f);
            Gizmos.DrawLine(posicaoBase, posicaoAtual);
            posicaoBase = posicaoAtual;
        }

        Gizmos.DrawLine(posicaoBase, posicaoInicial);
    }
}