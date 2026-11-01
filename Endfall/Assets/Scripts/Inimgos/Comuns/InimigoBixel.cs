using System.Collections;
using System.Collections.Generic;
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
        posicaoInicial = transform.position;
        rb = GetComponent<Rigidbody2D>();
        colisorBixel = GetComponent<Collider2D>();
        rb.freezeRotation = true;

        // Ajuda a evitar atravessamentos com velocidades altas.
        // O Cast mais abaixo garante que a investida PARE ao ver uma parede lateral.
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
        if (estaMorto) return;

        if (tempoRecargaRestante > 0f)
            tempoRecargaRestante -= Time.fixedDeltaTime;

        // O Bixel para um instante apos bater de lado e depois patrulha
        // em sentido contrario ao do obstaculo.
        if (tempoPausaImpactoRestante > 0f)
        {
            tempoPausaImpactoRestante -= Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (tempoInvestidaRestante > 0f)
        {
            // Testa a distancia que seria percorrida NESTE passo de fisica.
            // Isso evita atravessar paredes finas com velocidadeInvestida alta.
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

        // Detecta uma parede mesmo no PRIMEIRO passo da investida.
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

        // Faz uma varredura com o formato real do CapsuleCollider2D.
        // Colisores Trigger sao ignorados (moedas, checkpoints, zonas etc.).
        colisorBixel.Cast(direcao, filtroInvestida, impactosDaInvestida, distanciaPasso);

        foreach (RaycastHit2D impacto in impactosDaInvestida)
        {
            if (impacto.collider == null || impacto.collider.isTrigger)
                continue;

            // Somente superficies VERTICAIS que se opoem ao movimento.
            // Chao e teto possuem normal predominantemente em Y.
            if (impacto.normal.x * direcaoInvestida > -0.65f)
                continue;

            // Quando um collider do piso sobrepoe ligeiramente os pes do
            // inimigo, nao o interpreta como uma parede lateral.
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

        // Retoma a patrulha para o lado OPOSTO ao da colisao.
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