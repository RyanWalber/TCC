using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DragonBones;
using Transform = UnityEngine.Transform;

[RequireComponent(typeof(Rigidbody2D))]
public class InimigoBixel : MonoBehaviour
{
    [Header("Audio deste inimigo")]
    [SerializeField] private AudioInimigo audioInimigo = new AudioInimigo();

    [Header("Feedback visual ao receber dano")]
    [SerializeField] private FeedbackDanoDragonBones feedbackDano =
        new FeedbackDanoDragonBones();

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
    [Header("COLISAO LATERAL DA INVESTIDA")]
    [Tooltip("Distancia minima de seguranca antes de uma parede na investida.")]
    [Min(0f)] public float margemColisaoLateral = 0.1f;
    [Tooltip("Breve pausa ao bater de lado em um collider solido.")]
    [Min(0f)] public float pausaAposImpacto = 0.2f;

    private readonly List<RaycastHit2D> impactosDaInvestida = new List<RaycastHit2D>(16);
    private ContactFilter2D filtroInvestida;
    private float tempoPausaImpactoRestante;

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
        audioInimigo.TocarInvocacao(this);
        posicaoInicial = transform.position;
        rb = GetComponent<Rigidbody2D>();
        colisorBixel = GetComponent<Collider2D>();
        rb.freezeRotation = true;

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        filtroInvestida = new ContactFilter2D();
        filtroInvestida.useTriggers = false;
        filtroInvestida.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));

        if (pivoVisual == null && transform.childCount > 0)
            pivoVisual = transform.GetChild(0);

        if (armatureComponent == null)
            armatureComponent = GetComponentInChildren<UnityArmatureComponent>();

        LocalizarJogador();
        AtualizarEscala();
    }

    private void FixedUpdate()
    {
        audioInimigo.Atualizar(this);
        if (estaMorto) return;

        if (tempoRecargaRestante > 0f)
            tempoRecargaRestante -= Time.fixedDeltaTime;

        if (tempoPausaImpactoRestante > 0f)
        {
            tempoPausaImpactoRestante -= Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (tempoInvestidaRestante > 0f)
        {
            if (VerificarImpactoLateralPrevisto())
                return;

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

        if (objeto != null)
        {
            PlayerHealth vida = objeto.GetComponentInParent<PlayerHealth>();
            if (vida != null) objeto = vida.gameObject;
        }
        else
        {
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

        if (Mathf.Abs(jogador.position.x - transform.position.x) > alcanceDeteccao)
            return false;

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

        return Mathf.Abs(jogador.position.y - transform.position.y)
            <= Mathf.Max(margemAlturaDeteccao, alcanceDeteccao * 0.25f);
    }

    private void IniciarInvestida()
    {
        direcaoInvestida = jogador.position.x >= transform.position.x ? 1f : -1f;
        Virar(direcaoInvestida > 0f);
        tempoInvestidaRestante = duracaoInvestida;
        tempoRecargaRestante = recargaInvestida;
        audioInimigo.TocarAtaque(this);

        if (VerificarImpactoLateralPrevisto())
            return;

        rb.linearVelocity = new Vector2(direcaoInvestida * velocidadeInvestida, rb.linearVelocity.y);
    }

    private bool VerificarImpactoLateralPrevisto()
    {
        if (colisorBixel == null || !colisorBixel.enabled || velocidadeInvestida <= 0f)
            return false;

        Vector2 direcao = direcaoInvestida > 0f ? Vector2.right : Vector2.left;
        float distanciaPasso = velocidadeInvestida * Time.fixedDeltaTime + margemColisaoLateral;
        impactosDaInvestida.Clear();

        colisorBixel.Cast(direcao, filtroInvestida, impactosDaInvestida, distanciaPasso);

        foreach (RaycastHit2D impacto in impactosDaInvestida)
        {
            if (impacto.collider == null || impacto.collider.isTrigger)
                continue;

            if (impacto.normal.x * direcaoInvestida > -0.65f)
                continue;

            if (impacto.distance <= 0.001f &&
                impacto.collider.bounds.max.y <= colisorBixel.bounds.min.y + 0.5f)
                continue;

            PlayerHealth kayaAtingida = impacto.collider.GetComponentInParent<PlayerHealth>();
            if (kayaAtingida != null)
                AplicarDanoEEmpurrar(kayaAtingida.gameObject);

            PararInvestidaPorImpacto();
            return true;
        }

        return false;
    }

    private void PararInvestidaPorImpacto()
    {
        if (tempoInvestidaRestante <= 0f) return;

        tempoInvestidaRestante = 0f;
        tempoPausaImpactoRestante = pausaAposImpacto;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        Virar(direcaoInvestida < 0f);
    }

    private void VerificarContatoLateral(Collision2D colisao)
    {
        if (estaMorto || tempoInvestidaRestante <= 0f) return;

        for (int i = 0; i < colisao.contactCount; i++)
        {
            ContactPoint2D contato = colisao.GetContact(i);
            if (contato.normal.x * direcaoInvestida <= -0.65f)
            {
                PararInvestidaPorImpacto();
                return;
            }
        }
    }

    private void Update()
    {
        feedbackDano.Atualizar();
        if (estaMorto) return;
        AtualizarAnimacao();
    }

    private void OnDisable()
    {
        feedbackDano.Restaurar();
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
        if (estaMorto || dano <= 0) return;

        audioInimigo.TocarHitRecebido(this);
        vida -= dano;
        feedbackDano.ReceberHit(this, armatureComponent);

        if (vida <= 0) Morrer();
    }


    private void Morrer()
    {
        EstadoMundoCheckpoint.RegistrarMorte(gameObject);
        audioInimigo.TocarMorte(this);
        if (estaMorto) return;
        estaMorto = true;

        rb.linearVelocity = Vector2.zero;
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>(true))
            c.enabled = false;
        rb.simulated = false;

        Destroy(gameObject, feedbackDano.DuracaoVisivel);
    }

    private void OnCollisionEnter2D(Collision2D colisao)
    {
        ProcessarInteracao(colisao.gameObject);
        VerificarContatoLateral(colisao);
    }

    private void OnCollisionStay2D(Collision2D colisao)
    {
        ProcessarInteracao(colisao.gameObject);
        VerificarContatoLateral(colisao);
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
            float vidaAntes = playerHealth.VidaAtual;
            playerHealth.TomarDanoDeInimigo(danoNoPlayer);
            if (playerHealth.VidaAtual < vidaAntes) audioInimigo.TocarHitCausado(this);
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