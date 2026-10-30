using System.Collections;
using UnityEngine;
using DragonBones;
using Transform = UnityEngine.Transform;

[RequireComponent(typeof(Rigidbody2D))]
public class InimigoBixel : MonoBehaviour
{
    [Header("CONFIGURAÇÃO VISUAL")]
    public Transform pivoVisual;
    public UnityArmatureComponent armatureComponent;
    public string animAndar = "walk";
    public string animParado = "idle";
    public bool spriteInvertido = false;

    [Header("STATUS")]
    public int vida = 3;
    public int danoNoPlayer = 1;
    public float forcaEmpurrao = 8f;

    [Header("MOVIMENTO (Patrulha)")]
    public float velocidade = 3f;
    public float distanciaPatrulha = 5f;

    [Header("INVESTIDA")]
    [Tooltip("Distância máxima para detectar a Kaya e iniciar a investida.")]
    [Min(0f)] public float alcanceDeteccao = 5f;
    [Tooltip("Velocidade horizontal durante a investida.")]
    [Min(0f)] public float velocidadeInvestida = 8f;
    [Tooltip("Duração da investida em segundos.")]
    [Min(0.05f)] public float duracaoInvestida = 0.45f;
    [Tooltip("Tempo de espera antes de poder investir novamente.")]
    [Min(0f)] public float recargaInvestida = 2f;
    [Tooltip("Margem vertical entre os colliders do Bixel e da Kaya para detectar no mesmo nivel.")]
    [Min(0f)] public float margemAlturaDeteccao = 15f;

    private Rigidbody2D rb;
    private Collider2D colisorBixel;
    private Collider2D colisorJogador;
    private Vector3 posicaoInicial;
    private bool indoParaDireita = true;
    private bool estaMorto = false;
    private Transform jogador;
    private float tempoInvestidaRestante;
    private float tempoRecargaRestante;
    private float direcaoInvestida;

    private void Start()
    {
        posicaoInicial = transform.position;
        rb = GetComponent<Rigidbody2D>();
        colisorBixel = GetComponent<Collider2D>();
        rb.freezeRotation = true;

        if (pivoVisual == null && transform.childCount > 0)
            pivoVisual = transform.GetChild(0);

        if (armatureComponent == null)
            armatureComponent = GetComponentInChildren<UnityArmatureComponent>();

        LocalizarJogador();
        AtualizarEscala();
    }

    private void FixedUpdate()
    {
        if (estaMorto) return;

        if (tempoRecargaRestante > 0f)
            tempoRecargaRestante -= Time.fixedDeltaTime;

        if (tempoInvestidaRestante > 0f)
        {
            tempoInvestidaRestante -= Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(direcaoInvestida * velocidadeInvestida, rb.linearVelocity.y);
            return;
        }

        if (jogador == null)
            LocalizarJogador();

        if (tempoRecargaRestante <= 0f && JogadorNoAlcanceDaInvestida())
        {
            IniciarInvestida();
            return;
        }

        Patrulhar();
    }

    private void LocalizarJogador()
    {
        GameObject objeto = GameObject.FindGameObjectWithTag("Player");

        // Se a Tag estiver em um filho, usa o verdadeiro objeto da Kaya.
        if (objeto != null)
        {
            PlayerHealth vida = objeto.GetComponentInParent<PlayerHealth>();
            if (vida != null) objeto = vida.gameObject;
        }
        else
        {
            // Fallback para cenas que ainda nao configuraram a Tag.
            PlayerHealth vida = FindFirstObjectByType<PlayerHealth>();
            if (vida != null) objeto = vida.gameObject;
        }

        if (objeto == null) return;

        jogador = objeto.transform;
        colisorJogador = objeto.GetComponent<Collider2D>();
        if (colisorJogador == null)
            colisorJogador = objeto.GetComponentInChildren<Collider2D>();
    }

    private bool JogadorNoAlcanceDaInvestida()
    {
        if (jogador == null || alcanceDeteccao <= 0f) return false;

        // O movimento e horizontal: a distancia de ataque tambem deve ser
        // medida no eixo X. Nao usa a distancia entre os pivots dos rigs.
        if (Mathf.Abs(jogador.position.x - transform.position.x) > alcanceDeteccao)
            return false;

        // Antes, a diferenca Y tinha que ser <= 2 unidades, mesmo com
        // personagens/colliders de dezenas de unidades de altura.
        // Agora verifica a separacao real entre os colliders no eixo Y.
        if (colisorBixel != null && colisorJogador != null &&
            colisorBixel.enabled && colisorJogador.enabled)
        {
            Bounds inimigo = colisorBixel.bounds;
            Bounds player = colisorJogador.bounds;

            float espacoVertical = Mathf.Max(0f,
                Mathf.Max(inimigo.min.y - player.max.y,
                          player.min.y - inimigo.max.y));
            return espacoVertical <= margemAlturaDeteccao;
        }

        // Compatibilidade com um player sem Collider2D configurado.
        return Mathf.Abs(jogador.position.y - transform.position.y)
            <= Mathf.Max(margemAlturaDeteccao, alcanceDeteccao * 0.25f);
    }

    private void IniciarInvestida()
    {
        direcaoInvestida = jogador.position.x >= transform.position.x ? 1f : -1f;
        Virar(direcaoInvestida > 0f);
        tempoInvestidaRestante = duracaoInvestida;
        tempoRecargaRestante = recargaInvestida;
        rb.linearVelocity = new Vector2(direcaoInvestida * velocidadeInvestida, rb.linearVelocity.y);
    }

    private void Update()
    {
        if (estaMorto) return;
        AtualizarAnimacao();
    }

    private void Patrulhar()
    {
        float limiteDireita = posicaoInicial.x + distanciaPatrulha;
        float limiteEsquerda = posicaoInicial.x - distanciaPatrulha;

        float direcaoX = indoParaDireita ? 1f : -1f;
        rb.linearVelocity = new Vector2(direcaoX * velocidade, rb.linearVelocity.y);

        if (indoParaDireita && transform.position.x >= limiteDireita)
        {
            Virar(false);
        }
        else if (!indoParaDireita && transform.position.x <= limiteEsquerda)
        {
            Virar(true);
        }
    }

    private void Virar(bool irParaDireita)
    {
        indoParaDireita = irParaDireita;
        AtualizarEscala();
    }

    private void AtualizarEscala()
    {
        if (pivoVisual == null) return;

        float sinal = indoParaDireita ? 1f : -1f;
        if (spriteInvertido) sinal *= -1f;

        pivoVisual.localScale = new Vector3(sinal, 1f, 1f);
    }

    private void AtualizarAnimacao()
    {
        if (armatureComponent == null) return;

        bool estaAndando = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        string animDesejada = estaAndando ? animAndar : animParado;

        if (!string.IsNullOrEmpty(animDesejada) && armatureComponent.animation.lastAnimationName != animDesejada)
        {
            armatureComponent.animation.Play(animDesejada, 0);
        }
    }

    public void ReceberDano(int dano = 1)
    {
        TomarDano(dano);
    }

    public void TomarDano(int dano = 1)
    {
        if (estaMorto) return;

        vida -= dano;
        StartCoroutine(PiscarVermelhoDragonBones());

        if (vida <= 0) Morrer();
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

        rb.linearVelocity = Vector2.zero;
        if (GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = false;
        rb.simulated = false;

        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D colisao)
    {
        ProcessarInteracao(colisao.gameObject);
    }

    private void OnCollisionStay2D(Collision2D colisao)
    {
        ProcessarInteracao(colisao.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ProcessarInteracao(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        ProcessarInteracao(other.gameObject);
    }

    private void ProcessarInteracao(GameObject obj)
    {
        if (estaMorto) return;

        if (obj.CompareTag("Player"))
        {
            AplicarDanoEEmpurrar(obj);
        }
        else if (obj.CompareTag("Ataque") || obj.CompareTag("Projetil") || obj.CompareTag("Tiro") || obj.CompareTag("Bala"))
        {
            TomarDano(1);
        }
    }

    private void AplicarDanoEEmpurrar(GameObject jogadorObj)
    {
        PlayerHealth playerHealth = jogadorObj.GetComponent<PlayerHealth>() ?? jogadorObj.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TomarDanoDeInimigo(danoNoPlayer);
        }

        Rigidbody2D rbPlayer = jogadorObj.GetComponent<Rigidbody2D>() ?? jogadorObj.GetComponentInParent<Rigidbody2D>();
        if (rbPlayer != null)
        {
            float direcaoX = jogadorObj.transform.position.x >= transform.position.x ? 1f : -1f;
            Vector2 empurrao = new Vector2(direcaoX * 0.8f, 0.6f).normalized;

            rbPlayer.linearVelocity = empurrao * forcaEmpurrao;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 centro = Application.isPlaying ? posicaoInicial : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(centro.x - distanciaPatrulha, centro.y, centro.z), new Vector3(centro.x + distanciaPatrulha, centro.y, centro.z));

        // Amarelo: faixa real de deteccao da investida no mesmo nivel.
        Collider2D colisor = Application.isPlaying ? colisorBixel : GetComponent<Collider2D>();
        Gizmos.color = Color.yellow;
        if (colisor != null)
        {
            Bounds b = colisor.bounds;
            Gizmos.DrawWireCube(
                new Vector3(transform.position.x, b.center.y, transform.position.z),
                new Vector3(alcanceDeteccao * 2f,
                            b.size.y + margemAlturaDeteccao * 2f, 0f));
        }
        else
        {
            Gizmos.DrawWireCube(
                transform.position,
                new Vector3(alcanceDeteccao * 2f, alcanceDeteccao * 0.5f, 0f));
        }
    }
}