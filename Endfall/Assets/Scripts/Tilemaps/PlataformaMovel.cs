using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlataformaMovel : MonoBehaviour
{
    public Vector3[] pontos;
    public float velocidade = 3f;
    public float tempoDeEspera = 1f;

    private int indicePontoAtual = 0;
    private float temporizadorEspera = 0f;
    private Vector3[] pontosGlobais;
    private Rigidbody2D rb;
    private Rigidbody2D playerRb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        pontosGlobais = new Vector3[pontos.Length + 1];
        pontosGlobais[0] = transform.position;

        for (int i = 0; i < pontos.Length; i++)
        {
            pontosGlobais[i + 1] = transform.position + pontos[i];
        }
    }

    private void FixedUpdate()
    {
        if (pontosGlobais == null || pontosGlobais.Length <= 1) return;

        if (temporizadorEspera > 0)
        {
            temporizadorEspera -= Time.fixedDeltaTime;
        }
        else
        {
            Vector3 destino = pontosGlobais[indicePontoAtual];
            Vector3 novaPosicao = Vector3.MoveTowards(transform.position, destino, velocidade * Time.fixedDeltaTime);
            Vector3 delta = novaPosicao - transform.position;

            rb.MovePosition(novaPosicao);

            if (playerRb != null)
            {
                if (playerRb.linearVelocity.y > 0.1f)
                {
                    playerRb = null;
                }
                else
                {
                    Vector2 deslocamentoPlayer = playerRb.linearVelocity * Time.fixedDeltaTime;
                    playerRb.MovePosition(playerRb.position + (Vector2)delta + deslocamentoPlayer);
                }
            }

            if (Vector3.Distance(transform.position, destino) < 0.01f)
            {
                indicePontoAtual = (indicePontoAtual + 1) % pontosGlobais.Length;
                temporizadorEspera = tempoDeEspera;
            }
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
        if (collision.gameObject.CompareTag("Player"))
        {
            foreach (ContactPoint2D contato in collision.contacts)
            {
                if (contato.normal.y < -0.2f || collision.transform.position.y > transform.position.y + 0.1f)
                {
                    playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
                    return;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            playerRb = null;
        }
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