using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BossVida))]
public class BossPeste : MonoBehaviour
{
    [Header("CONFIGURAÇÃO VISUAL")]
    public bool arteOriginalOlhaDireita = false;

    [Header("STATUS DE ATAQUE PLAYER")]
    public int danoNoPlayer = 1;
    public float forcaEmpurrao = 8f;

    [Header("MOVIMENTO (Voo / Patrulha)")]
    public float velocidade = 2.5f;
    public float distanciaPatrulha = 6f;
    public float balancoVertical = 0.5f;

    [Header("SISTEMA DE ATAQUE (Gosma)")]
    public GameObject prefabGosma;
    public Transform pontoBoca;

    [Tooltip("Distância máxima no eixo X para considerar que está por cima do Player")]
    public float tolerânciaSobrePlayer = 3f;

    [Tooltip("Tempo entre ataques com VIDA CHEIA (em segundos)")]
    public float tempoAtaqueVidaCheia = 4f;

    [Tooltip("Tempo entre ataques com VIDA CRÍTICA (em segundos)")]
    public float tempoAtaqueVidaBaixa = 1f;

    private Rigidbody2D rb;
    private Animator anim;
    private Transform playerTransform;
    private Vector3 posicaoInicial;
    private bool indoParaDireita = true;
    private bool olhandoParaDireita;

    private float timerAtaque;
    private float proximoIntervaloAtaque;

    // Referência para o sistema de vida universal
    private BossVida bossVida;

    private void Start()
    {
        posicaoInicial = transform.position;

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        bossVida = GetComponent<BossVida>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        olhandoParaDireita = arteOriginalOlhaDireita;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        AtualizarOrientacao();
        CalcularProximoCooldown();
    }

    private void Update()
    {
        if (bossVida != null && bossVida.EstaMorto) return;

        PatrulharEVoar();
        ChecarEExecutarAtaque();
    }

    private void PatrulharEVoar()
    {
        float limiteDireita = posicaoInicial.x + distanciaPatrulha;
        float limiteEsquerda = posicaoInicial.x - distanciaPatrulha;

        if (indoParaDireita && transform.position.x >= limiteDireita)
        {
            Virar(false);
        }
        else if (!indoParaDireita && transform.position.x <= limiteEsquerda)
        {
            Virar(true);
        }

        float direcaoX = indoParaDireita ? 1f : -1f;
        float novoX = transform.position.x + (direcaoX * velocidade * Time.deltaTime);
        float novoY = posicaoInicial.y + (Mathf.Sin(Time.time * 3f) * balancoVertical);

        transform.position = new Vector3(novoX, novoY, transform.position.z);
    }

    private void ChecarEExecutarAtaque()
    {
        timerAtaque += Time.deltaTime;

        if (timerAtaque >= proximoIntervaloAtaque)
        {
            if (EstaPorCimaDoPlayer())
            {
                if (anim != null)
                {
                    anim.SetTrigger("Ataque");
                }

                timerAtaque = 0f;
                CalcularProximoCooldown();
            }
        }
    }

    private bool EstaPorCimaDoPlayer()
    {
        if (playerTransform == null) return false;

        bool estaAlinhadoX = Mathf.Abs(transform.position.x - playerTransform.position.x) <= tolerânciaSobrePlayer;
        bool estaAcimaY = transform.position.y > playerTransform.position.y;

        return estaAlinhadoX && estaAcimaY;
    }

    private void CalcularProximoCooldown()
    {
        // Pega o percentual de vida direto do script universal
        float percentualVida = bossVida != null ? bossVida.ObterPercentualVida() : 1f;
        float tempoBase = Mathf.Lerp(tempoAtaqueVidaBaixa, tempoAtaqueVidaCheia, percentualVida);

        proximoIntervaloAtaque = tempoBase * Random.Range(0.8f, 1.2f);
    }

    // Chamado pelo Animation Event no clipe pesteatak
    public void InstanciarGosma()
    {
        if (prefabGosma != null && pontoBoca != null)
        {
            Instantiate(prefabGosma, pontoBoca.position, prefabGosma.transform.rotation);
        }
    }

    private void Virar(bool irParaDireita)
    {
        indoParaDireita = irParaDireita;
        olhandoParaDireita = irParaDireita;
        AtualizarOrientacao();
    }

    private void AtualizarOrientacao()
    {
        float anguloY = 0f;

        if (arteOriginalOlhaDireita)
        {
            anguloY = olhandoParaDireita ? 0f : 180f;
        }
        else
        {
            anguloY = olhandoParaDireita ? 180f : 0f;
        }

        transform.rotation = Quaternion.Euler(0f, anguloY, 0f);
    }

    // --- CONTATO COM O JOGADOR ---
    private void OnCollisionEnter2D(Collision2D collision) => ProcessarContatoPlayer(collision.gameObject);
    private void OnCollisionStay2D(Collision2D collision) => ProcessarContatoPlayer(collision.gameObject);
    private void OnTriggerEnter2D(Collider2D collision) => ProcessarContatoPlayer(collision.gameObject);
    private void OnTriggerStay2D(Collider2D collision) => ProcessarContatoPlayer(collision.gameObject);

    private void ProcessarContatoPlayer(GameObject obj)
    {
        if (bossVida != null && bossVida.EstaMorto) return;

        if (obj.CompareTag("Player"))
        {
            PlayerHealth playerHealth = obj.GetComponent<PlayerHealth>() ?? obj.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TomarDano(danoNoPlayer);
            }

            Rigidbody2D rbPlayer = obj.GetComponent<Rigidbody2D>() ?? obj.GetComponentInParent<Rigidbody2D>();
            if (rbPlayer != null)
            {
                float direcaoX = obj.transform.position.x >= transform.position.x ? 1f : -1f;
                Vector2 empurrao = new Vector2(direcaoX * 0.8f, 0.6f).normalized;

                rbPlayer.linearVelocity = empurrao * forcaEmpurrao;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 centro = Application.isPlaying ? posicaoInicial : transform.position;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            new Vector3(centro.x - distanciaPatrulha, centro.y, centro.z),
            new Vector3(centro.x + distanciaPatrulha, centro.y, centro.z)
        );

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(tolerânciaSobrePlayer * 2f, 2f, 0f));
    }
}