using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cavaleira da Morte: State Pattern (vida alta, media e baixa).
/// Cada estado decide seus proprios comportamentos de forma ciclica,
/// independente da maquina visual do Animator.
/// </summary>
[DisallowMultipleComponent]
public class MorteBossControllerGameplay : MonoBehaviour, IAudioBoss
{
    [Header("Audio da Morte - independente da Kaya")]
    [SerializeField] private AudioInimigo audioInimigo = new AudioInimigo();
    [SerializeField] private AudioClip somFoice;
    [SerializeField, Range(0f, 1f)] private float volumeFoice = 0.75f;
    [SerializeField] private AudioClip somInvestida;
    [SerializeField, Range(0f, 1f)] private float volumeInvestida = 0.7f;
    public void AudioAoReceberHit() => audioInimigo.TocarHitRecebido(this);
    public void AudioAoMorrer() => audioInimigo.TocarMorte(this);
    public void AudioAoCausarHit() => audioInimigo.TocarHitCausado(this);

    [Header("MUSICA - RITUAL / INVULNERABILIDADE")]
    [Tooltip("Musica exclusiva enquanto a Morte fica preta e invulneravel ao trocar de estagio. Arraste a faixa aqui.")]
    [SerializeField] private AudioClip musicaTransicaoEstagio;
    [Tooltip("Opcional. Fonte propria para a musica do ritual; se vazia, criada automaticamente.")]
    [SerializeField] private AudioSource fonteMusicaTransicao;
    [SerializeField, Range(0f, 1f)] private float volumeMusicaTransicao = 0.7f;
    [Tooltip("Quanto das musicas normais continua audivel enquanto o ritual toca. Zero = silencio das trilhas.")]
    [SerializeField, Range(0f, 1f)] private float volumeTrilhasDuranteTransicao = 0.08f;
    [SerializeField, Min(0.05f)] private float fadeEntradaMusicaTransicao = 0.25f;
    [SerializeField, Min(0.05f)] private float fadeSaidaMusicaTransicao = 0.35f;

    [Header("GRITO FINAL - ANIMACAO MORTEMORRE")]
    [Tooltip("Grito da Morte ao iniciar a animacao mortemorre. Nao confundir com o som Morte em Audio Inimigo.")]
    [SerializeField] private AudioClip gritoFinal;
    [Tooltip("Musica especial durante mortemorre. Se vazia, reutiliza a musica do ritual.")]
    [SerializeField] private AudioClip musicaMorteFinal;
    [SerializeField, Range(0f, 1f)] private float volumeMusicaMorteFinal = 0.7f;
    [SerializeField, Range(0f, 1f)] private float volumeInicialGrito = 0.08f;
    [SerializeField, Range(0f, 1f)] private float volumeMaximoGrito = 0.95f;
    [SerializeField, Min(0.05f)] private float tempoCrescimentoGrito = 2.5f;
    [SerializeField, Min(0f)] private float atrasoInicioGrito = 0f;
    [Tooltip("Reduz as musicas de fundo durante o grito. Nao reduz o proprio grito.")]
    [SerializeField, Range(0f, 1f)] private float volumeTrilhasDuranteGrito = 0.08f;
    [SerializeField, Min(0.05f)] private float fadeMusicasParaGrito = 0.35f;

    [Header("Referencias")]
    [SerializeField] private Transform jogador;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform raizVisual;
    [SerializeField] private GameObject prefabCorvo;
    [SerializeField] private Transform pontoLancamentoCorvos;
    [Tooltip("Arraste o prefab REAL do Lamentus aqui. Nao ha esse prefab no repositorio atual.")]
    [SerializeField] private GameObject prefabLamentus;

    [Header("State Pattern - vida da Morte")]
    [Tooltip("Vida elevada para aproximar a luta de um minuto. Ajuste apos testar o dano real.")]
    [Min(1f)] [SerializeField] private float vidaMaximaMorte = 1800f;
    [Tooltip("Vida alta acima de 50%, media ate 20%, baixa abaixo de 20%.")]
    [Range(0.25f, 0.9f)] [SerializeField] private float limiteVidaAlta = 0.5f;
    [Range(0.05f, 0.4f)] [SerializeField] private float limiteVidaBaixa = 0.2f;
    [Header("Velocidade dos ataques - ajuste no Inspector")]
    [Tooltip("Segundos de descanso entre ataques de cada estágio.")]
    [Min(0.05f)] [SerializeField] private float intervaloVidaAlta = 1.35f;
    [Min(0.05f)] [SerializeField] private float intervaloVidaMedia = 1.05f;
    [Min(0.05f)] [SerializeField] private float intervaloVidaBaixa = 0.8f;
    [Tooltip("1 = velocidade original. Também acelera os tempos dos eventos para acompanhar a animação.")]
    [Range(0.6f, 3f)] [SerializeField] private float velocidadeAnimAlta = 1.15f;
    [Range(0.6f, 3f)] [SerializeField] private float velocidadeAnimMedia = 1.30f;
    [Range(0.6f, 3f)] [SerializeField] private float velocidadeAnimBaixa = 1.45f;

    [Header("Transição dos estágios")]
    [Tooltip("Durante este tempo a Morte para, fica preta e invulnerável.")]
    [Min(0.2f)] [SerializeField] private float duracaoTransicaoEstagio = 1.8f;
    [Min(0f)] [SerializeField] private float intensidadeTremorBarra = 12f;
    [Min(1f)] [SerializeField] private float frequenciaTremorBarra = 23f;

    [Header("Deteccao e aproximacao")]
    [Min(1f)] [SerializeField] private float distanciaDeteccao = 500f;
    [Min(1f)] [SerializeField] private float velocidadeInvestida = 900f;
    [Min(0f)] [SerializeField] private float distanciaParadaAntesDaKaya = 70f;
    [Min(0.1f)] [SerializeField] private float tempoMaximoAproximacao = 2.1f;

    [Header("Corvos")]
    [Tooltip("O State altera a quantidade automaticamente: 4, 6 ou 10.")]
    [Min(1)] [SerializeField] private int quantidadeCorvos = 6;
    [Min(0.05f)] [SerializeField] private float intervaloCorvos = 0.16f;
    [Min(1f)] [SerializeField] private float velocidadeCorvo = 520f;
    [Min(0)] [SerializeField] private int danoCorvo = 5;

    [Header("Lamentus - invocacao ao subir")]
    [SerializeField] private LayerMask camadasChao = 1 << 3;
    [Min(10f)] [SerializeField] private float distanciaLateralLamentus = 85f;
    [Min(1)] [SerializeField] private int maximoLamentusVivos = 2;
    [Range(0f, 1.75f)] [SerializeField] private float instanteInvocacao = 0.9f;

    [Header("Foice e golpe")]
    [Min(0f)] [SerializeField] private float danoFoice = 75f;
    [Min(0f)] [SerializeField] private float danoFoiceArremessada = 45f;
    [Min(1f)] [SerializeField] private float raioAcertoFoiceArremessada = 24f;
    [Range(0.2f, 1.7f)] [SerializeField] private float inicioColisaoFoice = 1.06f;
    [Range(1f, 2.15f)] [SerializeField] private float fimColisaoFoice = 1.75f;

    [Header("Alcance da foice arremessada")]
    [Tooltip("A foice vai ate uma distancia EXTRA depois do X onde Kaya estava quando foi arremessada. Mantem a ida e a volta da animacao.")]
    [SerializeField] private bool limitarFoiceAteKaya = true;
    [Tooltip("Distancia que a foice ultrapassa a posicao inicial da Kaya, na direcao do arremesso. Aumente para dar mais espaco de esquiva.")]
    [Min(0f)] [SerializeField] private float distanciaExtraDepoisDaKaya = 80f;
    [Tooltip("X local do inicio da fuga da foice no clip mortelancandofoice. Valor medido no clip original.")]
    [SerializeField] private float inicioOriginalFoiceLocalX = -67.4f;
    [Tooltip("Distancia horizontal total do arremesso original (605.7 no clip). Mantem a ida/volta suaves.")]
    [Min(1f)] [SerializeField] private float distanciaAnimadaFoice = 605.7f;

    private const float DuracaoCorvos = 1.95f;
    private const float DuracaoSubindo = 1.75f;
    private const float DuracaoFoiceArremessada = 2.1667f;
    private const float DuracaoAtaqueFoice = 1.15f;
    private const float InicioGolpe = 0.28f;
    private const float FimGolpe = 0.62f;

    private Rigidbody2D corpoFisico;
    private BossVida vidaBoss;
    private MorteFoiceHitbox hitboxFoice;
    private Transform foiceAnimada;
    private Collider2D colliderKaya;
    private bool arremessoFoiceEmAndamento;
    private bool arremessoFoiceAcertou;
    private bool foiceTemPosicaoAnterior;
    private float alvoXFoice;
    private float tempoAnimacaoFoice;
    private Vector2 foicePosicaoAnterior;
    private Coroutine rotinaAtaque;
    private readonly List<GameObject> lamentusInvocados = new List<GameObject>();
    private Vector3 escalaVisualOriginal = Vector3.one;
    private bool escalaCapturada;
    private bool atacando;
    private bool movimentoAtivo;
    private float destinoX;
    private float proximoAtaque;
    private int indiceAtaque;
    private EstadoVida estadoAtual;
    private Acao acaoEmExecucao;
    private float velocidadeAnimacaoAtual = 1f;
    private float multiplicadorVelocidadeMovimento = 1f;
    private bool emTransicaoDeEstagio;
    private Coroutine rotinaTransicao;
    private bool tocandoRitual;
    private KayaAudioManager musicaKaya;

    private enum Acao
    {
        CorvosDiretos,
        CorvosLeque,
        InvocarLamentus,
        LancarFoice,
        LancarFoiceAposAproximar,
        InvestidaComGolpe,
        InvestidaLongaComGolpe
    }

    // Padrao State propriamente dito: cada classe encapsula a politica
    // de selecao dos comportamentos para uma faixa de HP.
    private abstract class EstadoVida
    {
        public abstract string Nome { get; }
        public abstract int QuantidadeComportamentos { get; }
        public abstract Acao Selecionar(int indice);
    }

    private sealed class VidaAlta : EstadoVida
    {
        public override string Nome => "Vida Alta (>50%)";
        public override int QuantidadeComportamentos => 3;
        public override Acao Selecionar(int indice)
        {
            switch (indice % 3)
            {
                case 0: return Acao.CorvosDiretos;    // 4 corvos focados
                case 1: return Acao.InvocarLamentus;  // mortesubindo
                default: return Acao.CorvosLeque;     // 4 corvos espalhados
            }
        }
    }

    private sealed class VidaMedia : EstadoVida
    {
        public override string Nome => "Vida Media (20%-50%)";
        public override int QuantidadeComportamentos => 3;
        public override Acao Selecionar(int indice)
        {
            switch (indice % 3)
            {
                case 0: return Acao.CorvosDiretos;              // 6 corvos
                case 1: return Acao.LancarFoice;                // arremessa parada
                default: return Acao.LancarFoiceAposAproximar; // aproxima e arremessa
            }
        }
    }

    private sealed class VidaBaixa : EstadoVida
    {
        public override string Nome => "Vida Baixa (<=20%)";
        public override int QuantidadeComportamentos => 3;
        public override Acao Selecionar(int indice)
        {
            switch (indice % 3)
            {
                case 0: return Acao.CorvosDiretos;           // 10 corvos
                case 1: return Acao.InvestidaComGolpe;       // aproxima e golpeia
                default: return Acao.InvestidaLongaComGolpe; // aproxima mais perto e golpeia
            }
        }
    }

    private readonly EstadoVida alta = new VidaAlta();
    private readonly EstadoVida media = new VidaMedia();
    private readonly EstadoVida baixa = new VidaBaixa();

    private void Awake()
    {
        corpoFisico = GetComponent<Rigidbody2D>();
        vidaBoss = GetComponent<BossVida>();
        if (vidaBoss == null) vidaBoss = GetComponentInParent<BossVida>();
        if (vidaBoss != null)
            vidaBoss.VidaAlterada += AoAlterarVida;

        if (corpoFisico != null)
        {
            corpoFisico.bodyType = RigidbodyType2D.Kinematic;
            corpoFisico.gravityScale = 0f;
            corpoFisico.freezeRotation = true;
            corpoFisico.useFullKinematicContacts = true;
            corpoFisico.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        EncontrarJogador();
        if (jogador != null)
            musicaKaya = jogador.GetComponentInParent<KayaAudioManager>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (raizVisual == null && animator != null) raizVisual = animator.transform;
        if (raizVisual != null)
        {
            escalaVisualOriginal = raizVisual.localScale;
            escalaCapturada = true;
        }

        if (animator != null)
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foiceAnimada = animator.transform.Find("foice_0");
        }

        PrepararHitboxFoice();
    }

    private void Start()
    {
        if (vidaBoss != null)
            vidaBoss.ConfigurarVidaMaxima(vidaMaximaMorte);
        else
            Debug.LogError("[Morte] BossVida nao encontrado.", this);

        if (animator == null)
            Debug.LogError("[Morte] Animator nao encontrado.", this);
        if (prefabCorvo == null)
            Debug.LogWarning("[Morte] Prefab Corvo nao configurado.", this);
        if (prefabLamentus == null)
            Debug.LogWarning("[Morte] Lamentus: arraste o prefab no Inspector. As outras habilidades continuam funcionando.", this);

        AtualizarEstadoVida();
    }

    private void AoAlterarVida(float percentual)
    {
        // O evento vem do BossVida no mesmo frame do dano, impedindo
        // que um segundo projétil acerte antes da invulnerabilidade.
        if (isActiveAndEnabled && vidaBoss != null && !vidaBoss.EstaMorto)
            AtualizarEstadoVida();
    }

    private float VelocidadeDaFase(EstadoVida fase)
    {
        return Mathf.Max(0.1f, fase == alta ? velocidadeAnimAlta :
            fase == media ? velocidadeAnimMedia : velocidadeAnimBaixa);
    }

    private float IntervaloDaFase(EstadoVida fase)
    {
        return Mathf.Max(0.05f, fase == alta ? intervaloVidaAlta :
            fase == media ? intervaloVidaMedia : intervaloVidaBaixa);
    }

    private float TempoDeAnimacao(float segundosDoClipe)
    {
        return Mathf.Max(0.001f, segundosDoClipe / velocidadeAnimacaoAtual);
    }

    private void EncontrarJogador()
    {
        if (jogador != null) return;
        GameObject objeto = GameObject.FindGameObjectWithTag("Player");
        if (objeto != null) jogador = objeto.transform;
    }

    private void Update()
    {
        audioInimigo.Atualizar(this);
        AtualizarVolumeRitual();
        if (vidaBoss != null && vidaBoss.EstaMorto) return;

        EncontrarJogador();
        if (jogador == null) return;

        AtualizarEstadoVida();
        if (emTransicaoDeEstagio) return;

        AtualizarOrientacao();
        if (atacando || Time.time < proximoAtaque) return;
        if (Vector2.Distance(transform.position, jogador.position) > distanciaDeteccao)
            return;

        PlayerHealth vidaKaya = jogador.GetComponentInParent<PlayerHealth>();
        if (vidaKaya == null)
            vidaKaya = jogador.GetComponentInChildren<PlayerHealth>(true);
        if (vidaKaya != null && vidaKaya.VidaAtual <= 0f)
            return;

        if (estadoAtual == null) return;
        Acao acao = estadoAtual.Selecionar(indiceAtaque);
        indiceAtaque = (indiceAtaque + 1) % estadoAtual.QuantidadeComportamentos;
        atacando = true;
        // Congela a escolha de fase durante a habilidade, mesmo que
        // o jogador reduza a vida enquanto a horda esta sendo lancada.
        EstadoVida faseDoAtaque = estadoAtual;
        acaoEmExecucao = acao;
        velocidadeAnimacaoAtual = VelocidadeDaFase(faseDoAtaque);
        if (animator != null) animator.speed = velocidadeAnimacaoAtual;
        rotinaAtaque = StartCoroutine(ExecutarAcao(acao, faseDoAtaque));
    }

    private void AtualizarEstadoVida()
    {
        float percentual = vidaBoss != null ? vidaBoss.ObterPercentualVida() : 1f;
        EstadoVida novo = percentual > limiteVidaAlta ? alta :
            percentual > limiteVidaBaixa ? media : baixa;

        if (novo == estadoAtual) return;

        EstadoVida anterior = estadoAtual;
        estadoAtual = novo;
        indiceAtaque = 0;
        if (animator != null)
            animator.SetInteger("FaseVida",
                estadoAtual == alta ? 1 : estadoAtual == media ? 2 : 3);

        // Inicialização não deve provocar transição visual.
        if (anterior != null && !emTransicaoDeEstagio)
        {
            IniciarTransicaoDeEstagio();
        }
        Debug.Log("[Morte / State] Entrou em " + estadoAtual.Nome, this);
    }

    private void IniciarTransicaoDeEstagio()
    {
        emTransicaoDeEstagio = true;

        // Interrompe qualquer habilidade anterior (até a horda em
        // andamento). Corvos já lançados podem continuar voando.
        if (rotinaAtaque != null)
        {
            StopCoroutine(rotinaAtaque);
            rotinaAtaque = null;
        }
        atacando = false;
        movimentoAtivo = false;
        arremessoFoiceEmAndamento = false;
        foiceTemPosicaoAnterior = false;
        multiplicadorVelocidadeMovimento = 1f;
        if (corpoFisico != null) corpoFisico.linearVelocity = Vector2.zero;
        if (hitboxFoice != null) hitboxFoice.DefinirAtiva(false);

        // Invulnerabilidade aplicada IMEDIATAMENTE, antes de aguardar
        // um frame; BossVida também colore a personagem de preto.
        if (vidaBoss != null)
            vidaBoss.DefinirInvulnerabilidadeDeFase(true);

        IniciarAudioRitual();

        if (animator != null)
        {
            string[] limpar = { "AtaqueCorvos", "InvocarLamentus",
                "LancarFoice", "Aproximar", "Investida", "CancelarAtaque" };
            foreach (string gatilho in limpar) animator.ResetTrigger(gatilho);
            animator.SetTrigger("CancelarAtaque");
            animator.speed = 0f; // Congela a pose; nao altera timeScale global
        }

        rotinaTransicao = StartCoroutine(TransicaoDeEstagio());
    }

    private IEnumerator TransicaoDeEstagio()
    {
        float duracao = Mathf.Max(0.2f, duracaoTransicaoEstagio);
        if (vidaBoss != null)
            yield return vidaBoss.TremerBarraVida(duracao,
                intensidadeTremorBarra, frequenciaTremorBarra);
        else
            yield return new WaitForSecondsRealtime(duracao);

        if (vidaBoss == null || vidaBoss.EstaMorto)
        {
            EncerrarAudioRitual(true);
            yield break;
        }

        EncerrarAudioRitual(false);
        if (animator != null) animator.speed = 1f;
        vidaBoss.DefinirInvulnerabilidadeDeFase(false);
        emTransicaoDeEstagio = false;
        rotinaTransicao = null;
        // Dá tempo ao Animator para retornar suavemente à pose ociosa.
        proximoAtaque = Time.time + 0.3f;
    }

    private IEnumerator ExecutarAcao(Acao acao, EstadoVida faseDoAtaque)
    {
        // A aproximação comum é usada apenas antes da invocação
        // no estágio 1; as outras fases têm variações próprias.
        if (acao == Acao.InvocarLamentus)
            yield return AproximarDaKaya();

        switch (acao)
        {
            case Acao.CorvosDiretos:
                yield return AtaqueCorvos(false, faseDoAtaque);
                break;
            case Acao.CorvosLeque:
                yield return AtaqueCorvos(true, faseDoAtaque);
                break;
            case Acao.InvocarLamentus:
                yield return AtaqueInvocacao();
                break;
            case Acao.LancarFoice:
                yield return AtaqueFoiceArremessada();
                break;
            case Acao.LancarFoiceAposAproximar:
                yield return AproximarDaKaya();
                yield return AtaqueFoiceArremessada();
                break;
            case Acao.InvestidaComGolpe:
                yield return AproximarDaKaya();
                yield return AtaqueInvestidaFinal();
                break;
            case Acao.InvestidaLongaComGolpe:
                yield return AproximarDaKaya(
                    Mathf.Max(15f, distanciaParadaAntesDaKaya * 0.55f),
                    1.2f, true);
                yield return AtaqueInvestidaFinal();
                break;
        }

        movimentoAtivo = false;
        multiplicadorVelocidadeMovimento = 1f;
        if (hitboxFoice != null) hitboxFoice.DefinirAtiva(false);
        if (animator != null) animator.speed = 1f;
        // O retorno para morteociosa acontece pelas setas do Animator.
        atacando = false;
        rotinaAtaque = null;
        proximoAtaque = Time.time + IntervaloDaFase(estadoAtual);
    }

    private void FixedUpdate()
    {
        if (!movimentoAtivo || (vidaBoss != null && vidaBoss.EstaMorto)) return;

        float atual = corpoFisico != null ? corpoFisico.position.x : transform.position.x;
        float x = Mathf.MoveTowards(atual, destinoX,
            velocidadeInvestida * multiplicadorVelocidadeMovimento * Time.fixedDeltaTime);
        if (corpoFisico != null)
            corpoFisico.MovePosition(new Vector2(x, corpoFisico.position.y));
        else
            transform.position = new Vector3(x, transform.position.y, transform.position.z);

        if (Mathf.Abs(x - destinoX) <= 0.25f)
            movimentoAtivo = false;
    }

    private IEnumerator AproximarDaKaya(
        float distanciaParada = -1f, float fatorVelocidade = 1f,
        bool animacaoLonga = false)
    {
        EncontrarJogador();
        if (jogador == null) yield break;

        float distancia = jogador.position.x - transform.position.x;
        float parada = distanciaParada >= 0f ?
            distanciaParada : distanciaParadaAntesDaKaya;
        float avancar = Mathf.Max(0f, Mathf.Abs(distancia) - parada);
        if (avancar <= 1f) yield break;

        destinoX = transform.position.x + Mathf.Sign(distancia) * avancar;
        multiplicadorVelocidadeMovimento = Mathf.Max(0.2f, fatorVelocidade);
        movimentoAtivo = true;
        if (animator != null)
        {
            animator.SetBool("AproximacaoLonga",
                animacaoLonga || Mathf.Abs(distancia) > distanciaDeteccao * 0.45f);
            DispararTrigger("Aproximar");
        }

        float espera = 0f;
        float limite = TempoDeAnimacao(Mathf.Max(0.2f, tempoMaximoAproximacao));
        while (movimentoAtivo && espera < limite)
        {
            espera += Time.deltaTime;
            yield return null;
        }
        movimentoAtivo = false;
        multiplicadorVelocidadeMovimento = 1f;
        if (corpoFisico != null) corpoFisico.linearVelocity = Vector2.zero;
    }

    private IEnumerator AtaqueCorvos(bool emLeque, EstadoVida faseDoAtaque)
    {
        DispararTrigger("AtaqueCorvos");
        yield return new WaitForSeconds(TempoDeAnimacao(0.65f));

        int quantidade = faseDoAtaque == alta ? 4 :
            faseDoAtaque == media ? 6 : 10;
        // A segunda horda da fase alta espalha os mesmos 4 corvos
        // em angulos distintos: terceira habilidade sem nova animacao.
        // Garante que ate a horda de 10 saia durante o clipe
        // mortelancando (1.95s), sem tiros depois do fim da animacao.
        float intervaloReal = Mathf.Min(Mathf.Max(0.05f, intervaloCorvos),
            (DuracaoCorvos - 0.75f) / Mathf.Max(1, quantidade - 1));
        for (int i = 0; i < quantidade; i++)
        {
            LancarCorvo(i, quantidade, emLeque);
            if (i + 1 < quantidade)
                yield return new WaitForSeconds(TempoDeAnimacao(intervaloReal));
        }

        float usado = 0.65f + Mathf.Max(0f, quantidade - 1) * intervaloReal;
        if (DuracaoCorvos > usado)
            yield return new WaitForSeconds(TempoDeAnimacao(DuracaoCorvos - usado));
    }

    private void LancarCorvo(int indice, int total, bool emLeque)
    {
        if (prefabCorvo == null || jogador == null) return;

        float lado = jogador.position.x >= transform.position.x ? 1f : -1f;
        Vector3 origemBase = pontoLancamentoCorvos != null ?
            pontoLancamentoCorvos.position :
            transform.position + new Vector3(lado * 25f, 80f, 0f);

        float centro = (total - 1) * 0.5f;
        float altura = (indice - centro) * 35f;
        float afastamento = (indice % 2 == 0 ? -1f : 1f) * (8f + indice / 2 * 12f);
        Vector3 origem = origemBase + new Vector3(afastamento, altura, 0f);

        Vector2 direcao = (Vector2)(jogador.position - origem);
        if (direcao.sqrMagnitude < 0.001f) direcao = Vector2.right * lado;
        direcao.Normalize();
        if (emLeque)
        {
            float angulo = Mathf.Lerp(-24f, 24f, indice / Mathf.Max(1f, total - 1f));
            direcao = Quaternion.Euler(0f, 0f, angulo) * direcao;
        }

        GameObject corvo = Instantiate(prefabCorvo, origem, Quaternion.identity);
        CorvoBossProjectile projetil = corvo.GetComponent<CorvoBossProjectile>();
        if (projetil == null) projetil = corvo.AddComponent<CorvoBossProjectile>();
        projetil.Configurar(direcao, velocidadeCorvo, Mathf.Max(1, danoCorvo), 4f,
            GetComponentsInChildren<Collider2D>(true), true);
    }

    private IEnumerator AtaqueInvocacao()
    {
        DispararTrigger("InvocarLamentus");
        float tempoSpawn = Mathf.Clamp(instanteInvocacao, 0f, DuracaoSubindo);
        if (tempoSpawn > 0f)
            yield return new WaitForSeconds(TempoDeAnimacao(tempoSpawn));
        InvocarLamentus();
        if (DuracaoSubindo > tempoSpawn)
            yield return new WaitForSeconds(TempoDeAnimacao(DuracaoSubindo - tempoSpawn));
    }

    private void InvocarLamentus()
    {
        if (prefabLamentus == null || jogador == null) return;

        lamentusInvocados.RemoveAll(item => item == null);
        if (lamentusInvocados.Count >= maximoLamentusVivos)
            return;

        Collider2D colKaya = jogador.GetComponent<Collider2D>();
        float pesKaya = colKaya != null ? colKaya.bounds.min.y : jogador.position.y;
        float lado = jogador.position.x >= transform.position.x ? 1f : -1f;
        bool achou = false;
        Vector2 superficie = Vector2.zero;
        // Se nao houver plataforma de um lado, tenta o outro antes de desistir.
        for (int tentativa = 0; tentativa < 2 && !achou; tentativa++)
        {
            float sinal = tentativa == 0 ? lado : -lado;
            float x = jogador.position.x + sinal * distanciaLateralLamentus;
            Vector2 origem = new Vector2(x, pesKaya + 65f);
            RaycastHit2D[] impactos = Physics2D.RaycastAll(origem, Vector2.down,
                650f, camadasChao);
            foreach (RaycastHit2D impacto in impactos)
            {
                if (impacto.collider == null || impacto.collider.isTrigger ||
                    impacto.normal.y < 0.55f) continue;
                if (impacto.point.y > pesKaya + 10f) continue;
                superficie = impacto.point;
                achou = true;
                break;
            }
        }

        if (!achou)
        {
            Debug.LogWarning("[Morte] Nao achei o piso para invocar Lamentus perto da Kaya.", this);
            return;
        }

        InimigoParado modeloParado = prefabLamentus.GetComponent<InimigoParado>();
        Vector3 inicial = new Vector3(superficie.x,
            superficie.y + 1f, prefabLamentus.transform.position.z);
        if (modeloParado != null)
            inicial.y = superficie.y - Mathf.Max(0f, modeloParado.alturaEmergir);

        GameObject invocado = Instantiate(prefabLamentus, inicial, Quaternion.identity);
        if (invocado == null) return;

        if (modeloParado != null)
        {
            InimigoParado parado = invocado.GetComponent<InimigoParado>();
            if (parado != null)
            {
                parado.player = jogador;
                parado.raioDeteccao = Mathf.Max(parado.raioDeteccao,
                    Vector2.Distance(jogador.position, inicial) + 15f);
                Vector3 po = parado.offsetPoeira;
                po.y = Mathf.Max(0f, modeloParado.alturaEmergir) + 0.5f;
                parado.offsetPoeira = po;
            }
        }
        else
        {
            Collider2D colisao = invocado.GetComponentInChildren<Collider2D>();
            if (colisao != null)
            {
                float diferenca = superficie.y + 1f - colisao.bounds.min.y;
                invocado.transform.position += Vector3.up * diferenca;
            }
        }

        lamentusInvocados.Add(invocado);
        // O audio de invocacao e configurado no proprio prefab Lamentus.
        // InimigoParado toca o seu som ao surgir; outros inimigos comuns
        // tocam o som de aparicao no proprio Start.
        InimigoParado audioDoInvocado = invocado.GetComponent<InimigoParado>();
        if (audioDoInvocado != null) audioDoInvocado.TocarAoSerInvocado();
    }

    private IEnumerator AtaqueFoiceArremessada()
    {
        // Mira somente uma vez: Kaya pode fugir para qualquer lado apos
        // o lancamento. A foice cruza o X inicial da Kaya e so entao retorna.
        // Mantem o tempo e a animacao original de 2.1667 segundos.
        EncontrarJogador();
        if (jogador != null)
        {
            float direcao = jogador.position.x < transform.position.x ? -1f : 1f;
            alvoXFoice = jogador.position.x + direcao *
                Mathf.Max(0f, distanciaExtraDepoisDaKaya);
        }
        else
        {
            alvoXFoice = transform.position.x;
        }
        arremessoFoiceAcertou = false;
        foiceTemPosicaoAnterior = false;
        tempoAnimacaoFoice = 0f;
        arremessoFoiceEmAndamento = foiceAnimada != null;
        audioInimigo.TocarEspecial(this, somFoice, volumeFoice);
        DispararTrigger("LancarFoice");

        float decorrido = 0f;
        while (decorrido < TempoDeAnimacao(DuracaoFoiceArremessada))
        {
            decorrido += Time.deltaTime;
            tempoAnimacaoFoice = decorrido * velocidadeAnimacaoAtual;
            // O reposicionamento VISUAL e o acerto sao feitos no LateUpdate,
            // depois que o Animator escreve os keyframes da foice.
            yield return null;
        }

        arremessoFoiceEmAndamento = false;
        foiceTemPosicaoAnterior = false;
    }

    private void LateUpdate()
    {
        if (!arremessoFoiceEmAndamento || foiceAnimada == null) return;

        // Nao modifica animacoes de aproximação, ataque ou morte.
        if (animator != null &&
            !animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.mortelancandofoice"))
        {
            foiceTemPosicaoAnterior = false;
            return;
        }

        // O inicio da ida acontece em 1.0167s no clip; antes disso
        // os bracos e a foice continuam com a preparacao original.
        if (limitarFoiceAteKaya && tempoAnimacaoFoice >= 1.0167f &&
            foiceAnimada.parent != null && jogador != null)
        {
            Transform pai = foiceAnimada.parent;
            Vector3 local = foiceAnimada.localPosition;

            // A animacao vai para X local NEGATIVO (-67.4 ate -673.1).
            // A orientacao visual da Morte ja inverte o X para a direita
            // quando Kaya esta daquele lado: nao precisamos duplicar clips.
            Vector3 alvoMundo = new Vector3(alvoXFoice,
                foiceAnimada.position.y, foiceAnimada.position.z);
            float destinoLocalX = pai.InverseTransformPoint(alvoMundo).x;
            float distanciaDisponivel = Mathf.Max(0f,
                inicioOriginalFoiceLocalX - destinoLocalX);
            // Nao usar Clamp01: quando Kaya esta mais distante, a foice
            // precisa poder percorrer ALEM do deslocamento original do clip
            // para atingir o ponto extra, sem parar antes da posicao dela.
            float proporcao = Mathf.Max(0f,
                distanciaDisponivel / Mathf.Max(1f, distanciaAnimadaFoice));

            local.x = inicioOriginalFoiceLocalX +
                (local.x - inicioOriginalFoiceLocalX) * proporcao;
            foiceAnimada.localPosition = local;
        }

        // Verifica o dano pela MESMA posicao visivel, inclusive ao ultrapassar
        // Kaya. Nao cria hit invisivel nem rastreia Kaya depois do lancamento.
        Vector2 atual = foiceAnimada.position;
        if (foiceTemPosicaoAnterior && !arremessoFoiceAcertou &&
            tempoAnimacaoFoice >= inicioColisaoFoice &&
            tempoAnimacaoFoice <= fimColisaoFoice)
            arremessoFoiceAcertou =
                VerificarHitFoiceArremessada(foicePosicaoAnterior, atual);

        foicePosicaoAnterior = atual;
        foiceTemPosicaoAnterior = true;
    }

    private bool VerificarHitFoiceArremessada(Vector2 anterior, Vector2 atual)
    {
        if (jogador == null) return false;
        if (colliderKaya == null)
        {
            colliderKaya = jogador.GetComponent<Collider2D>();
            if (colliderKaya == null)
                colliderKaya = jogador.GetComponentInChildren<Collider2D>();
        }
        if (colliderKaya == null || !colliderKaya.enabled) return false;

        Vector2 centro = colliderKaya.bounds.center;
        Vector2 percurso = atual - anterior;
        float t = percurso.sqrMagnitude > 0.001f ?
            Mathf.Clamp01(Vector2.Dot(centro - anterior, percurso) / percurso.sqrMagnitude) : 0f;
        Vector2 pontoProximo = anterior + percurso * t;
        Vector2 pontoNoJogador = colliderKaya.ClosestPoint(pontoProximo);

        if (Vector2.Distance(pontoNoJogador, pontoProximo) >
            raioAcertoFoiceArremessada)
            return false;

        PlayerHealth vidaKaya = jogador.GetComponentInParent<PlayerHealth>();
        if (vidaKaya != null)
        {
            float antes = vidaKaya.VidaAtual;
            vidaKaya.TomarDanoDeInimigo(danoFoiceArremessada);
            if (vidaKaya.VidaAtual < antes) audioInimigo.TocarHitCausado(this);
        }
        return true;
    }

    private IEnumerator AtaqueInvestidaFinal()
    {
        // O movimento "indo" ou "indolonge" so aproxima. Somente aqui
        // a animacao indoataque habilita a hitbox com dano.
        audioInimigo.TocarEspecial(this, somInvestida, volumeInvestida);
        DispararTrigger("Investida");
        float tempo = 0f;
        float duracaoReal = TempoDeAnimacao(DuracaoAtaqueFoice);
        while (tempo < duracaoReal)
        {
            tempo += Time.deltaTime;
            float progresso = tempo / duracaoReal;
            if (hitboxFoice != null)
                hitboxFoice.DefinirAtiva(
                    progresso >= InicioGolpe && progresso <= FimGolpe);
            yield return null;
        }
        if (hitboxFoice != null)
            hitboxFoice.DefinirAtiva(false);
    }

    private void DispararTrigger(string nome)
    {
        if (animator == null) return;

        // Impede triggers antigos de ficarem pendentes e ativarem outro
        // estado apos a fase mudar. A animacao e decidida nas SETAS
        // do Morte 1.controller, nunca pelo Animator.Play aqui.
        string[] gatilhos = {
            "AtaqueCorvos", "InvocarLamentus", "LancarFoice",
            "Aproximar", "Investida", "CancelarAtaque"
        };
        foreach (string gatilho in gatilhos)
            if (gatilho != nome) animator.ResetTrigger(gatilho);

        animator.SetTrigger(nome);
    }

    private void AtualizarOrientacao()
    {
        if (jogador == null || raizVisual == null || !escalaCapturada) return;
        bool kayaEsquerda = jogador.position.x < transform.position.x;
        float sinal = kayaEsquerda ? 1f : -1f;
        raizVisual.localScale = new Vector3(
            Mathf.Abs(escalaVisualOriginal.x) * sinal,
            escalaVisualOriginal.y, escalaVisualOriginal.z);
    }

    private void PrepararHitboxFoice()
    {
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true))
        {
            if (col == null || !col.gameObject.name.ToLowerInvariant().Contains("foice"))
                continue;

            hitboxFoice = col.GetComponent<MorteFoiceHitbox>();
            if (hitboxFoice == null)
                hitboxFoice = col.gameObject.AddComponent<MorteFoiceHitbox>();
            hitboxFoice.ConfigurarDano(danoFoice);
            hitboxFoice.DefinirAtiva(false);
            break;
        }
        if (hitboxFoice == null)
            Debug.LogWarning("[Morte] Nenhum Collider2D de foice encontrado para o golpe da investida.", this);
    }

    public void TomarDano(int quantidadeDano)
    {
        if (quantidadeDano <= 0) return;
        if (vidaBoss == null) vidaBoss = GetComponent<BossVida>();
        if (vidaBoss != null)
            vidaBoss.TomarDano(quantidadeDano);
    }
    public void ReceberDano(int quantidadeDano) => TomarDano(quantidadeDano);


    private KayaAudioManager LocalizarMusicaKaya()
    {
        if (musicaKaya != null) return musicaKaya;
        if (jogador != null)
            musicaKaya = jogador.GetComponentInParent<KayaAudioManager>();
        if (musicaKaya == null)
        {
            GameObject kaya = GameObject.FindGameObjectWithTag("Player");
            if (kaya != null)
                musicaKaya = kaya.GetComponentInParent<KayaAudioManager>();
        }
        return musicaKaya;
    }

    private void IniciarAudioRitual()
    {
        if (musicaTransicaoEstagio == null) return;

        if (fonteMusicaTransicao == null)
        {
            GameObject emissor = new GameObject("Audio - Ritual da Morte");
            emissor.transform.SetParent(transform, false);
            fonteMusicaTransicao = emissor.AddComponent<AudioSource>();
        }

        fonteMusicaTransicao.playOnAwake = false;
        fonteMusicaTransicao.loop = true;
        fonteMusicaTransicao.spatialBlend = 0f;
        fonteMusicaTransicao.Stop();
        fonteMusicaTransicao.clip = musicaTransicaoEstagio;
        fonteMusicaTransicao.volume = 0f;
        fonteMusicaTransicao.Play();
        tocandoRitual = true;

        KayaAudioManager trilha = LocalizarMusicaKaya();
        if (trilha != null)
            trilha.AbafarMusicas(volumeTrilhasDuranteTransicao,
                fadeEntradaMusicaTransicao);
    }

    private void EncerrarAudioRitual(bool imediato)
    {
        tocandoRitual = false;
        if (imediato && fonteMusicaTransicao != null)
        {
            fonteMusicaTransicao.Stop();
            fonteMusicaTransicao.volume = 0f;
        }

        KayaAudioManager trilha = LocalizarMusicaKaya();
        if (trilha != null)
            trilha.RestaurarMusicas(imediato ? 0.05f : fadeSaidaMusicaTransicao);
    }

    private void AtualizarVolumeRitual()
    {
        if (fonteMusicaTransicao == null) return;

        float destino = tocandoRitual ? volumeMusicaTransicao : 0f;
        float duracao = tocandoRitual ? fadeEntradaMusicaTransicao : fadeSaidaMusicaTransicao;
        float passo = Time.unscaledDeltaTime / Mathf.Max(0.05f, duracao);
        fonteMusicaTransicao.volume = Mathf.MoveTowards(
            fonteMusicaTransicao.volume, destino, passo);

        if (!tocandoRitual && fonteMusicaTransicao.isPlaying &&
            fonteMusicaTransicao.volume <= 0.001f)
            fonteMusicaTransicao.Stop();
    }

    // Musica especial da animacao mortemorre, independente deste componente:
    // BossVida desativa a IA no golpe fatal e o som deve continuar na cinematica.
    public void IniciarMusicaMorteFinal(MonoBehaviour controladorCinematica)
    {
        AudioClip faixa = musicaMorteFinal != null ? musicaMorteFinal : musicaTransicaoEstagio;
        if (faixa == null || controladorCinematica == null) return;

        GameObject emissor = new GameObject("Audio - Musica Final da Morte");
        emissor.transform.SetParent(controladorCinematica.transform, false);
        AudioSource fonte = emissor.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.loop = true;
        fonte.spatialBlend = 0f;
        fonte.clip = faixa;
        fonte.volume = 0f;
        fonte.Play();

        KayaAudioManager trilha = LocalizarMusicaKaya();
        if (trilha != null)
            trilha.AbafarMusicas(volumeTrilhasDuranteTransicao,
                fadeEntradaMusicaTransicao);

        controladorCinematica.StartCoroutine(FadeMusicaMorteFinal(
            fonte, Mathf.Clamp01(volumeMusicaMorteFinal),
            Mathf.Max(0.05f, fadeEntradaMusicaTransicao)));
    }

    private static IEnumerator FadeMusicaMorteFinal(
        AudioSource fonte, float volumeAlvo, float duracao)
    {
        float decorrido = 0f;
        while (fonte != null && decorrido < duracao)
        {
            decorrido += Time.unscaledDeltaTime;
            fonte.volume = Mathf.Lerp(0f, volumeAlvo,
                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(decorrido / duracao)));
            yield return null;
        }
        if (fonte != null) fonte.volume = volumeAlvo;
    }

    // Chamado pelo FinalizadorMorte quando a animacao 'mortemorre' comeca.
    // O som roda no controlador da CINEMATICA, pois BossVida desativa este
    // script no golpe final e cancelaria coroutines executadas aqui.
    public void IniciarGritoFinal(MonoBehaviour controladorCinematica)
    {
        if (gritoFinal == null || controladorCinematica == null) return;

        GameObject emissor = new GameObject("Audio - Grito Final da Morte");
        emissor.transform.SetParent(controladorCinematica.transform, false);
        AudioSource fonte = emissor.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.loop = false;
        fonte.spatialBlend = 0f;
        fonte.volume = Mathf.Clamp01(volumeInicialGrito);
        fonte.clip = gritoFinal;

        KayaAudioManager trilha = LocalizarMusicaKaya();
        controladorCinematica.StartCoroutine(RotinaGritoFinal(
            fonte, Mathf.Clamp01(volumeInicialGrito), Mathf.Clamp01(volumeMaximoGrito),
            Mathf.Max(0.05f, tempoCrescimentoGrito),
            Mathf.Max(0f, atrasoInicioGrito),
            trilha, volumeTrilhasDuranteGrito, fadeMusicasParaGrito,
            musicaMorteFinal != null || musicaTransicaoEstagio != null));
    }

    private static IEnumerator RotinaGritoFinal(
        AudioSource fonte, float volumeInicial, float volumeMaximo,
        float duracaoCrescimento, float atraso,
        KayaAudioManager trilha, float volumeDuranteGrito, float tempoFadeMusica,
        bool manterTrilhasAbafadas)
    {
        if (atraso > 0f) yield return new WaitForSecondsRealtime(atraso);
        if (fonte == null || fonte.clip == null) yield break;

        if (trilha != null)
            trilha.AbafarMusicas(volumeDuranteGrito, tempoFadeMusica);

        fonte.volume = volumeInicial;
        fonte.Play();

        float decorrido = 0f;
        while (fonte != null && fonte.isPlaying)
        {
            decorrido += Time.unscaledDeltaTime;
            float progresso = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(decorrido / duracaoCrescimento));
            fonte.volume = Mathf.Lerp(volumeInicial, volumeMaximo, progresso);
            yield return null;
        }

        // Durante a cinematica final a faixa especial ainda esta tocando.
        // Restaurar a musica de combate aqui criaria um salto indesejado.
        if (trilha != null && !manterTrilhasAbafadas)
            trilha.RestaurarMusicas(tempoFadeMusica);
        if (fonte != null) Destroy(fonte.gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 1f);
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccao);
    }

    private void OnDisable()
    {
        EncerrarAudioRitual(true);
        if (vidaBoss != null)
        {
            vidaBoss.VidaAlterada -= AoAlterarVida;
            vidaBoss.DefinirInvulnerabilidadeDeFase(false);
        }
        StopAllCoroutines();
        atacando = false;
        movimentoAtivo = false;
        emTransicaoDeEstagio = false;
        rotinaTransicao = null;
        arremessoFoiceEmAndamento = false;
        foiceTemPosicaoAnterior = false;
        multiplicadorVelocidadeMovimento = 1f;
        if (animator != null) animator.speed = 1f;
        if (hitboxFoice != null) hitboxFoice.DefinirAtiva(false);

        // Nao deixa Lamentus vivos atacando Kaya na cinematica de morte.
        if (vidaBoss != null && vidaBoss.EstaMorto)
        {
            foreach (GameObject invocado in lamentusInvocados)
                if (invocado != null) Destroy(invocado);
            lamentusInvocados.Clear();
        }
    }
}
