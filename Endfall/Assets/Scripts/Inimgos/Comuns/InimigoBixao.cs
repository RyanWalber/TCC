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
    public float velocidade = 4f;
    public float raioDetecao = 10f;
    public float distanciaFuga = 6f;
    public float tempoEspera = 1f;
    public int vida = 3;
    public int danoNoJogador = 1;
    public float forcaEmpurrao = 8f;

    private Rigidbody2D rb;
    private bool aguardando = false;
    private Vector3 escalaOriginal;
    private Coroutine coroutineFuga;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        if (armatureComponent == null)
            armatureComponent = GetComponentInChildren<UnityArmatureComponent>();
    }

    private void Start()
    {
        escalaOriginal = transform.localScale;

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

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
            float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
            transform.localScale = new Vector3(-absX, absY, escalaOriginal.z);
            transform.rotation = Quaternion.Euler(0, 0, angulo);
        }
        else
        {
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
            obj.SendMessage("Machucar", danoNoJogador, SendMessageOptions.DontRequireReceiver);

            Rigidbody2D rbPlayer = obj.GetComponent<Rigidbody2D>();
            if (rbPlayer != null)
            {
                Vector2 direcaoImpacto = (obj.transform.position - transform.position).normalized;
                direcaoImpacto.y = Mathf.Clamp(direcaoImpacto.y + 0.3f, 0.4f, 0.8f);

                rbPlayer.linearVelocity = new Vector2(rbPlayer.linearVelocity.x, 0);
                rbPlayer.AddForce(direcaoImpacto * forcaEmpurrao, ForceMode2D.Impulse);
            }

            Fugir();
        }
        else if (obj.CompareTag("Ataque") || obj.CompareTag("Projetil") || obj.CompareTag("Tiro") || obj.CompareTag("Bala"))
        {
            TomarDano(1);
        }
    }

    public void TomarDano(int dano = 1)
    {
        ReceberDano(dano);
    }

    public void ReceberDano(int dano = 1)
    {
        if (vida <= 0) return;

        vida -= dano;
        StartCoroutine(PiscarVermelhoDragonBones());

        if (vida <= 0)
        {
            Morrer();
        }
        else
        {
            Fugir();
        }
    }

    private void Morrer()
    {
        rb.linearVelocity = Vector2.zero;
        if (GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = false;
        Destroy(gameObject, 0.15f);
    }

    private IEnumerator PiscarVermelhoDragonBones()
    {
        if (armatureComponent != null)
        {
            DragonBones.ColorTransform corVermelha = new DragonBones.ColorTransform();
            corVermelha.redMultiplier = 1f;
            corVermelha.greenMultiplier = 0f;
            corVermelha.blueMultiplier = 0f;

            DragonBones.ColorTransform corNormal = new DragonBones.ColorTransform();

            armatureComponent.color = corVermelha;
            yield return new WaitForSeconds(0.15f);

            if (armatureComponent != null)
                armatureComponent.color = corNormal;
        }
    }

    private void Fugir()
    {
        if (coroutineFuga != null)
            StopCoroutine(coroutineFuga);

        coroutineFuga = StartCoroutine(RotinaFuga());
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