using System.Collections;
using UnityEngine;
using DragonBones;
using Transform = UnityEngine.Transform;

[RequireComponent(typeof(Rigidbody2D))]
public class InimigoBixao : MonoBehaviour
{
    public Transform player;
    public UnityArmatureComponent armatureComponent;
    public string animVoar = "fly";

    [Header("Configurações de Ação")]
    public float velocidade = 60f;
    public float raioDetecao = 300f;
    public float distanciaFuga = 150f;
    public float tempoEspera = 1f;
    public int vida = 3;

    private Rigidbody2D rb;
    private bool aguardando = false;
    private Vector3 escalaOriginal;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
    }

    private void Start()
    {
        escalaOriginal = transform.localScale;

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (armatureComponent == null)
            armatureComponent = GetComponentInChildren<UnityArmatureComponent>();

        if (armatureComponent != null && !string.IsNullOrEmpty(animVoar))
            armatureComponent.animation.Play(animVoar, 0);
    }

    private void Update()
    {
        if (player == null) return;
        AtualizarInclinacao();
    }

    private void FixedUpdate()
    {
        if (player == null || aguardando) return;

        float distancia = Vector2.Distance(player.position, transform.position);

        if (distancia <= raioDetecao)
        {
            Vector2 direcao = ((Vector2)player.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = direcao * velocidade;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void AtualizarInclinacao()
    {
        Vector2 direcao = player.position - transform.position;
        if (direcao.sqrMagnitude < 0.001f) return;

        float absX = Mathf.Abs(escalaOriginal.x);
        float absY = Mathf.Abs(escalaOriginal.y);

        if (direcao.x >= 0)
        {
            // Player à DIREITA: Espelha X para virar a boca (desenhada pra esquerda) pro player
            float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;

            transform.localScale = new Vector3(-absX, absY, escalaOriginal.z);
            transform.rotation = Quaternion.Euler(0, 0, angulo);
        }
        else
        {
            // Player à ESQUERDA: Mantém X positivo (boca nativa p/ esquerda) e ajusta a inclinação
            float angulo = Mathf.Atan2(direcao.y, -direcao.x) * Mathf.Rad2Deg;

            transform.localScale = new Vector3(absX, absY, escalaOriginal.z);
            transform.rotation = Quaternion.Euler(0, 0, -angulo);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) => ProcessarContato(collision.gameObject);
    private void OnTriggerEnter2D(Collider2D collision) => ProcessarContato(collision.gameObject);

    private void ProcessarContato(GameObject obj)
    {
        if (aguardando) return;

        if (obj.CompareTag("Player"))
        {
            Fugir();
        }
        else if (obj.CompareTag("Ataque"))
        {
            TomarDano(1);
        }
    }

    public void TomarDano(int dano)
    {
        vida -= dano;

        if (vida <= 0)
        {
            Destroy(gameObject);
        }
        else
        {
            Fugir();
        }
    }

    private void Fugir()
    {
        StopAllCoroutines();
        StartCoroutine(RotinaFuga());
    }

    private IEnumerator RotinaFuga()
    {
        aguardando = true;
        rb.linearVelocity = Vector2.zero;

        if (player != null)
        {
            Vector2 pontoAleatorio = Random.insideUnitCircle.normalized * distanciaFuga;
            if (pontoAleatorio.y < 0) pontoAleatorio.y = Mathf.Abs(pontoAleatorio.y);
            transform.position = player.position + (Vector3)pontoAleatorio;
        }

        yield return new WaitForSeconds(tempoEspera);
        aguardando = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, raioDetecao);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, distanciaFuga);
    }
}