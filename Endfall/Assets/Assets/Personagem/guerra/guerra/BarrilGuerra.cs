using UnityEngine;

public class BarrilGuerra : MonoBehaviour
{
    [Header("CONFIGURAÇÕES DA EXPLOSÃO")]
    public float dano = 25f;
    public float forcaEmpurrao = 50f;
    public float raioExplosao = 30f;

    [Header("TAGS")]
    public string tagDoPlayer = "Player";
    public string tagDoChao = "Chao";

    private Rigidbody2D rb;
    private bool jaExplodiu = false;
    private float tempoDesdeSpawn = 0f;
    private float tempoProtecaoChao = 0.12f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        tempoDesdeSpawn += Time.deltaTime;
    }

    // CÁLCULO BALÍSTICO DE TRAJETÓRIA
    public void Lançar(Vector3 posOrigem, Vector3 posDestino, float velocidadeHorizontal)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        rb.linearVelocity = Vector2.zero;

        float deltaX = posDestino.x - posOrigem.x;
        float deltaY = posDestino.y - posOrigem.y;

        // Garante uma distância mínima em X para não dividir por zero
        if (Mathf.Abs(deltaX) < 1f)
        {
            deltaX = deltaX >= 0 ? 1f : -1f;
        }

        float sentidoX = Mathf.Sign(deltaX);
        float vx = sentidoX * velocidadeHorizontal;

        // Tempo necessário para percorrer a distância X até o player
        float tempoDeVoo = Mathf.Abs(deltaX) / velocidadeHorizontal;

        // Pega a gravidade real da sua cena no Unity
        float g = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        if (g <= 0f) g = 9.81f;

        // Calcula a velocidade Y exata necessária para atingir a posição Y do player no tempo do voo
        float vy = (deltaY + (0.5f * g * tempoDeVoo * tempoDeVoo)) / tempoDeVoo;

        rb.linearVelocity = new Vector2(vx, vy);
        rb.AddTorque(-120f * sentidoX);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (jaExplodiu) return;

        bool ePlayer = collision.CompareTag(tagDoPlayer);
        bool eChao = collision.CompareTag(tagDoChao) || collision.gameObject.layer == LayerMask.NameToLayer("Ground");

        if (eChao && tempoDesdeSpawn < tempoProtecaoChao)
        {
            return;
        }

        if (ePlayer || eChao)
        {
            Explodir();
        }
    }

    private void Explodir()
    {
        jaExplodiu = true;

        Collider2D[] acertados = Physics2D.OverlapCircleAll(transform.position, raioExplosao);

        foreach (Collider2D hit in acertados)
        {
            if (hit.CompareTag(tagDoPlayer))
            {
                PlayerHealth hp = hit.GetComponent<PlayerHealth>() ?? hit.GetComponentInParent<PlayerHealth>();
                if (hp != null)
                {
                    hp.TomarDano(dano);
                }

                Rigidbody2D rbPlayer = hit.GetComponent<Rigidbody2D>() ?? hit.GetComponentInParent<Rigidbody2D>();
                if (rbPlayer != null)
                {
                    Vector2 direcaoEmpurrao = (hit.transform.position - transform.position).normalized;
                    direcaoEmpurrao.y = Mathf.Abs(direcaoEmpurrao.y) + 0.5f;

                    rbPlayer.linearVelocity = Vector2.zero;
                    rbPlayer.AddForce(direcaoEmpurrao.normalized * forcaEmpurrao, ForceMode2D.Impulse);
                }
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, raioExplosao);
    }
}