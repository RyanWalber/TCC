using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class InimigoBixinho : MonoBehaviour
{
    [Header("Movimentação e Rotação")]
    public float velocidade = 3.5f;
    public float velocidadeRotacao = 10f;
    public float offsetAngulo = 180f; 
    public float raioDeteccao = 6f;
    public Vector2 offsetDeteccao;
    public Transform player;

    [Header("Vida")]
    public int vidaMaxima = 3;
    private int vidaAtual;

    [Header("Impacto no Jogador")]
    public int danoNoJogador = 1;
    public float forcaImpacto = 12f;
    public float cooldownImpacto = 0.8f;
    private float tempoProximoImpacto;

    private Rigidbody2D rb;

    private void Start()
    {
        vidaAtual = vidaMaxima;
        rb = GetComponent<Rigidbody2D>();

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Update()
    {
        if (player == null) return;

        Vector3 centroDeteccao = transform.position + (Vector3)offsetDeteccao;
        float distancia = Vector2.Distance(player.position, centroDeteccao);

        if (distancia <= raioDeteccao)
        {
            Vector2 direcao = ((Vector2)player.position - (Vector2)transform.position).normalized;

            rb.linearVelocity = direcao * velocidade;
            RotacionarParaPlayer(direcao);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void RotacionarParaPlayer(Vector2 direcao)
    {
        float angulo = (Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg) + offsetAngulo;
        Quaternion rotacaoAlvo = Quaternion.Euler(0, 0, angulo);

        transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoAlvo, velocidadeRotacao * Time.deltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TentarAplicarImpacto(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TentarAplicarImpacto(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TentarAplicarImpacto(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TentarAplicarImpacto(collision.gameObject);
    }

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
        if (vidaAtual <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 centroDeteccao = transform.position + (Vector3)offsetDeteccao;
        Gizmos.DrawWireSphere(centroDeteccao, raioDeteccao);
    }
}