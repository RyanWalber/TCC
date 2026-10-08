using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossPeste : MonoBehaviour
{
    [Header("CONFIGURAÇÃO VISUAL")]
    public bool arteOriginalOlhaDireita = false;

    [Header("STATUS")]
    public int vida = 10;
    private int vidaMaxima;
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
    private bool estaMorto = false;
    private bool olhandoParaDireita;

    private float timerAtaque;
    private float proximoIntervaloAtaque;

    private SpriteRenderer[] renderers;
    private Color[] coresOriginais;

    private void Start()
    {
        posicaoInicial = transform.position;
        vidaMaxima = vida;

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        olhandoParaDireita = arteOriginalOlhaDireita;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        renderers = GetComponentsInChildren<SpriteRenderer>();
        if (renderers != null && renderers.Length > 0)
        {
            coresOriginais = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                coresOriginais[i] = renderers[i].color;
            }
        }

        AtualizarOrientacao();
        CalcularProximoCooldown();
    }

    private void Update()
    {
        if (estaMorto) return;

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
        float percentualVida = Mathf.Clamp01((float)vida / vidaMaxima);

        float tempoBase = Mathf.Lerp(tempoAtaqueVidaBaixa, tempoAtaqueVidaCheia, percentualVida);

        proximoIntervaloAtaque = tempoBase * Random.Range(0.8f, 1.2f);
    }

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

    public void TomarDano(int dano = 1)
    {
        if (estaMorto) return;

        vida -= dano;
        StartCoroutine(PiscarVermelho());

        if (vida <= 0) Morrer();
    }

    private IEnumerator PiscarVermelho()
    {
        if (renderers != null && renderers.Length > 0)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].color = Color.red;
            }

            yield return new WaitForSeconds(0.15f);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].color = coresOriginais[i];
            }
        }
    }

    private void Morrer()
    {
        if (estaMorto) return;
        estaMorto = true;

        if (GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = false;
        rb.simulated = false;

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 centro = Application.isPlaying ? posicaoInicial : transform.position;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            new Vector3(centro.x - distanciaPatrulha, centro.y, centro.z),
            new Vector3(centro.x + distanciaPatrulha, centro.y, centro.z)
        );

        // Área visual indicando o alcance em X onde o Boss aceita cuspir no Player
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(tolerânciaSobrePlayer * 2f, 2f, 0f));
    }
}