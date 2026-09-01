using System.Collections;
using UnityEngine;
using DragonBones;
using Transform = UnityEngine.Transform;

[RequireComponent(typeof(Rigidbody2D))]
public class InimigoBixao : MonoBehaviour
{
    [Header("CONFIGURAÇÃO VISUAL")]
    public Transform pivoVisual;
    public UnityArmatureComponent armatureComponent;
    public string animVoar = "fly";
    public bool spriteInvertido = false;

    [Header("Referências e Detecção")]
    public Transform player;
    public float raioAtivacao = 15f;

    [Header("Movimento e Fuga")]
    public float velocidadePerseguicao = 6f;
    public float raioFugaMin = 6f;
    public float raioFugaMax = 10f;
    public float tempoEsperaAposFuga = 1f;

    [Header("Impacto e Vida")]
    public float forcaImpacto = 12f;
    public int vidaMaxima = 5;
    private int vidaAtual;

    private Rigidbody2D rb;
    private bool emEspera = false;
    private bool estaMorto = false;
    private Vector3 escalaOriginalPivo;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
    }

    private void Start()
    {
        vidaAtual = vidaMaxima;

        if (pivoVisual == null && transform.childCount > 0)
            pivoVisual = transform.GetChild(0);

        escalaOriginalPivo = (pivoVisual != null) ? pivoVisual.localScale : transform.localScale;

        if (armatureComponent == null)
            armatureComponent = GetComponentInChildren<UnityArmatureComponent>();

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        TocarAnimacao(animVoar);
    }

    private void Update()
    {
        if (estaMorto || player == null) return;
        AtualizarOrientacao();
    }

    private void FixedUpdate()
    {
        if (estaMorto || player == null || emEspera) return;

        float distancia = Vector2.Distance(player.position, transform.position);

        if (distancia <= raioAtivacao)
        {
            Vector2 direcao = ((Vector2)player.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = direcao * velocidadePerseguicao;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) => ProcessarContato(collision.gameObject);

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ProcessarContato(collision.gameObject);

        if (collision.CompareTag("Ataque"))
        {
            TomarDano(1);
        }
    }

    private void ProcessarContato(GameObject obj)
    {
        if (estaMorto || emEspera || !obj.CompareTag("Player")) return;

        Rigidbody2D rbPlayer = obj.GetComponent<Rigidbody2D>();
        if (rbPlayer != null)
        {
            Vector2 direcaoImpacto = (obj.transform.position - transform.position).normalized;
            direcaoImpacto.y = Mathf.Clamp(direcaoImpacto.y + 0.3f, 0.4f, 1f);
            rbPlayer.linearVelocity = Vector2.zero;
            rbPlayer.AddForce(direcaoImpacto * forcaImpacto, ForceMode2D.Impulse);
        }

        Fugir();
    }

    public void TomarDano(int dano)
    {
        if (estaMorto) return;

        vidaAtual -= dano;
        StartCoroutine(PiscarVermelhoDragonBones());

        if (vidaAtual <= 0)
        {
            Morrer();
        }
        else
        {
            Fugir();
        }
    }

    private void Fugir()
    {
        if (estaMorto) return;
        StopAllCoroutines();
        StartCoroutine(RotinaFuga());
    }

    private IEnumerator RotinaFuga()
    {
        emEspera = true;
        rb.linearVelocity = Vector2.zero;

        if (player != null)
        {
            Vector2 direcaoAleatoria = Random.insideUnitCircle.normalized;
            if (direcaoAleatoria.y < 0.2f) direcaoAleatoria.y = Mathf.Abs(direcaoAleatoria.y) + 0.3f;

            float distancia = Random.Range(raioFugaMin, raioFugaMax);
            transform.position = player.position + (Vector3)(direcaoAleatoria * distancia);
        }

        yield return new WaitForSeconds(tempoEsperaAposFuga);
        emEspera = false;
    }

    private void AtualizarOrientacao()
    {
        if (pivoVisual == null || player == null) return;

        bool playerADireita = player.position.x >= transform.position.x;
        float direcaoX = playerADireita ? 1f : -1f;

        if (spriteInvertido) direcaoX *= -1f;

        pivoVisual.localScale = new Vector3(
            Mathf.Abs(escalaOriginalPivo.x) * direcaoX,
            escalaOriginalPivo.y,
            escalaOriginalPivo.z
        );
    }

    private void TocarAnimacao(string nomeAnimacao)
    {
        if (armatureComponent == null || armatureComponent.armature == null || string.IsNullOrEmpty(nomeAnimacao)) return;

        if (armatureComponent.animation.lastAnimationName != nomeAnimacao)
        {
            armatureComponent.animation.Play(nomeAnimacao, 0);
        }
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
            armatureComponent.color = corNormal;
        }
    }

    private void Morrer()
    {
        if (estaMorto) return;
        estaMorto = true;

        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;
        if (GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = false;
        Destroy(gameObject);
    }
}