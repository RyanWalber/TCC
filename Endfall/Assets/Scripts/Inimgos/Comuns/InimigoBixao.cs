using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class InimigoBixao : MonoBehaviour
{
    private enum Estado { Esperando, Rodeando, Preparando, Avancando }

    [Header("Detecção e Raios")]
    public float raioAtivacao = 10f;
    public float raioRodeio = 4f;
    public Vector2 offsetDeteccao;
    public LayerMask camadaObstaculos;
    public Transform player;

    [Header("Velocidades")]
    public float velocidadeRodeio = 2f;
    public float velocidadeBote = 14f;

    [Header("Tempos do Ataque")]
    public float tempoRodeando = 3.0f;
    public float tempoPreparacao = 0.5f;
    public float tempoBote = 0.8f;
    public float cooldownAtaque = 1.0f;

    [Header("Vida e Impacto")]
    public int vidaMaxima = 5;
    private int vidaAtual;
    public int danoNoJogador = 2;
    public float forcaImpacto = 15f;
    public float cooldownImpacto = 0.8f;
    private float tempoProximoImpacto;

    private Rigidbody2D rb;
    private Estado estadoAtual = Estado.Esperando;
    private float temporizadorEstado;
    private Vector2 direcaoBote;
    private Vector3 escalaOriginal;

    private void Start()
    {
        vidaAtual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        escalaOriginal = transform.localScale;

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Update()
    {
        if (player == null) return;

        AtualizarOrientacao();

        switch (estadoAtual)
        {
            case Estado.Esperando:
                CheckDeteccao();
                break;

            case Estado.Rodeando:
                ModoRodeioArco();
                break;

            case Estado.Preparando:
                ModoPreparacao();
                break;

            case Estado.Avancando:
                ModoAvanco();
                break;
        }
    }

    private void CheckDeteccao()
    {
        Vector3 centro = transform.position + (Vector3)offsetDeteccao;
        if (Vector2.Distance(player.position, centro) <= raioAtivacao)
        {
            estadoAtual = Estado.Rodeando;
            temporizadorEstado = Time.time + tempoRodeando;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void ModoRodeioArco()
    {
        float oscilacao = Mathf.Sin(Time.time * velocidadeRodeio);
        float anguloGraus = 90f + (oscilacao * 60f);
        float anguloRad = anguloGraus * Mathf.Deg2Rad;

        Vector2 offsetArc = new Vector2(Mathf.Cos(anguloRad), Mathf.Sin(anguloRad)) * raioRodeio;
        Vector2 posicaoAlvo = (Vector2)player.position + offsetArc;

        Vector2 direcao = (posicaoAlvo - (Vector2)transform.position);
        Vector2 velocidadeDesejada = direcao * 4f;

        velocidadeDesejada = AplicarDesvioObstaculos(velocidadeDesejada);

        rb.linearVelocity = velocidadeDesejada;

        if (Time.time >= temporizadorEstado)
        {
            estadoAtual = Estado.Preparando;
            temporizadorEstado = Time.time + tempoPreparacao;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private Vector2 AplicarDesvioObstaculos(Vector2 velocidadeOriginal)
    {
        if (camadaObstaculos == 0) return velocidadeOriginal;

        float raioChecagem = 0.8f;
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, raioChecagem, velocidadeOriginal.normalized, 1.2f, camadaObstaculos);

        if (hit.collider != null)
        {
            Vector2 empurrao = hit.normal * 6f;
            return velocidadeOriginal + empurrao;
        }

        return velocidadeOriginal;
    }

    private void ModoPreparacao()
    {
        rb.linearVelocity = Vector2.zero;
        direcaoBote = ((Vector2)player.position - (Vector2)transform.position).normalized;

        if (Time.time >= temporizadorEstado)
        {
            estadoAtual = Estado.Avancando;
            temporizadorEstado = Time.time + tempoBote;
        }
    }

    private void ModoAvanco()
    {
        rb.linearVelocity = direcaoBote * velocidadeBote;

        if (Time.time >= temporizadorEstado)
        {
            VoltarParaRodeio();
        }
    }

    private void VoltarParaRodeio()
    {
        estadoAtual = Estado.Rodeando;
        temporizadorEstado = Time.time + cooldownAtaque + tempoRodeando;
    }

    private void AtualizarOrientacao()
    {
        if (Mathf.Abs(rb.linearVelocity.x) > 0.4f)
        {
            float sinal = rb.linearVelocity.x > 0 ? Mathf.Abs(escalaOriginal.x) : -Mathf.Abs(escalaOriginal.x);
            transform.localScale = new Vector3(sinal, escalaOriginal.y, escalaOriginal.z);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) => TentarAplicarImpacto(collision.gameObject);
    private void OnCollisionStay2D(Collision2D collision) => TentarAplicarImpacto(collision.gameObject);
    private void OnTriggerEnter2D(Collider2D collision) => TentarAplicarImpacto(collision.gameObject);
    private void OnTriggerStay2D(Collider2D collision) => TentarAplicarImpacto(collision.gameObject);

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

            if (estadoAtual == Estado.Avancando)
            {
                VoltarParaRodeio();
            }
        }
    }

    public void TomarDano(int quantidadeDano)
    {
        vidaAtual -= quantidadeDano;
        if (vidaAtual <= 0) Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 centro = transform.position + (Vector3)offsetDeteccao;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(centro, raioAtivacao);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centro, raioRodeio);
    }
}