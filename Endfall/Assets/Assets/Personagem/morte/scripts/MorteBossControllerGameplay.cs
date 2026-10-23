using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Controla a boss Morte: detecção da Kaya, horda de corvos, investida curta/longa,
/// hitbox da foice e vida. Não depende de Animation Events; os tempos são configuráveis.
/// </summary>
[DisallowMultipleComponent]
public class MorteBossControllerGameplay : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform jogador;
    [SerializeField] private Animator animator;
    [Tooltip("Raiz visual animada (normalmente o filho chamado Morte). Se vazio, usa o Animator.")]
    [SerializeField] private Transform raizVisual;
    [SerializeField] private GameObject prefabCorvo;
    [Tooltip("Ponto de lançamento opcional. Se vazio, usa o deslocamento configurado abaixo.")]
    [SerializeField] private Transform pontoLancamentoCorvos;
    [SerializeField] private Slider barraVida;
    [SerializeField] private UnityEvent aoSerDerrotada = new UnityEvent();

    [Header("Vida da Boss")]
    [Min(1f)] [SerializeField] private float vidaMaxima = 100f;
    [SerializeField] private float tempoPiscarDano = 0.08f;

    [Header("Detecção e ritmo")]
    [Min(1f)] [SerializeField] private float distanciaDeteccao = 1200f;
    [Min(0f)] [SerializeField] private float diferencaVerticalMaxima = 300f;
    [Min(0.1f)] [SerializeField] private float tempoEntreAtaques = 2.4f;
    [Range(0f, 1f)] [SerializeField] private float chanceHordaCorvos = 0.45f;
    [Tooltip("Distâncias maiores que este valor selecionam a versão longa da investida.")]
    [Min(1f)] [SerializeField] private float distanciaInvestidaLonga = 300f;
    [Tooltip("Distância que a Morte tenta manter entre ela e a Kaya no fim da investida.")]
    [Min(0f)] [SerializeField] private float distanciaParadaAntesDaKaya = 55f;
    [Tooltip("Arte da Morte nas animações originais está virada para a esquerda.")]
    [SerializeField] private bool visualOriginalOlhaEsquerda = true;

    [Header("Ataque de corvos")]
    [Min(1)] [SerializeField] private int quantidadeCorvos = 5;
    [Min(0f)] [SerializeField] private float atrasoPrimeiroCorvo = 1.35f;
    [Min(0.01f)] [SerializeField] private float intervaloEntreCorvos = 0.13f;
    [Min(1f)] [SerializeField] private float velocidadeCorvo = 520f;
    [Min(1)] [SerializeField] private int danoCorvo = 12;
    [Min(0.1f)] [SerializeField] private float tempoVidaCorvo = 4f;
    [Min(0f)] [SerializeField] private float recuperacaoHordaCorvos = 0.3f;
    [SerializeField] private Vector2 offsetLancamentoCorvos = new Vector2(25f, 80f);
    [Range(0f, 35f)] [SerializeField] private float dispersaoCorvosGraus = 8f;
    [Tooltip("O sprite original do prefab Corvo aponta para a esquerda.")]
    [SerializeField] private bool corvoOriginalOlhaEsquerda = true;

    [Header("Investida e foice")]
    [Min(0.01f)] [SerializeField] private float tempoPreparacaoInvestida = 0.22f;
    [Min(1f)] [SerializeField] private float velocidadeInvestidaCurta = 700f;
    [Min(1f)] [SerializeField] private float velocidadeInvestidaLonga = 900f;
    [Min(1f)] [SerializeField] private float distanciaMaximaInvestidaCurta = 240f;
    [Min(1f)] [SerializeField] private float distanciaMaximaInvestidaLonga = 1100f;
    [Min(0.1f)] [SerializeField] private float tempoMaximoInvestidaCurta = 0.55f;
    [Min(0.1f)] [SerializeField] private float tempoMaximoInvestidaLonga = 1.4f;
    [Range(0.01f, 0.95f)] [SerializeField] private float inicioHitboxFoiceNormalizado = 0.30f;
    [Range(0.05f, 1f)] [SerializeField] private float fimHitboxFoiceNormalizado = 0.62f;
    [Min(0.1f)] [SerializeField] private float danoFoice = 25f;
    [Min(0f)] [SerializeField] private float recuperacaoDepoisDoGolpe = 0.35f;
    [Min(0.2f)] [SerializeField] private float duracaoMaximaAnimacaoGolpe = 1.5f;

    private Rigidbody2D corpoFisico;
    private MorteFoiceHitbox hitboxFoice;
    private SpriteRenderer[] renderizadores;
    private Color[] coresOriginais;
    private Coroutine rotinaAtaqueAtual;
    private Coroutine rotinaPiscar;
    private Vector3 escalaVisualOriginal = Vector3.one;
    private float vidaAtual;
    private float proximoAtaque;
    private float destinoInvestidaX;
    private float velocidadeMovimentoAtual;
    private bool movimentoInvestidaAtivo;
    private bool emAtaque;
    private bool derrotada;
    private bool visualEscalaCapturada;

    private const string EstadoInvestidaLonga = "Base Layer.morteindolonge";
    private const string EstadoAtaqueFoice = "Base Layer.morteindoataque";
    private static readonly int TriggerCorvos = Animator.StringToHash("AtaqueCorvos");
    private static readonly int TriggerInvestida = Animator.StringToHash("Investida");
    private static readonly int HashEstadoLongo = Animator.StringToHash(EstadoInvestidaLonga);
    private static readonly int HashEstadoFoice = Animator.StringToHash(EstadoAtaqueFoice);

    public float VidaAtual => vidaAtual;
    public float VidaMaxima => vidaMaxima;
    public bool EstaDerrotada => derrotada;

    private void Awake()
    {
        // O collider principal da boss recebe a tag Inimigo para que os tiros da Kaya
        // reconheçam o acerto mesmo quando atingem o collider do pai BossMorte.
        if (CompareTag("Untagged"))
            gameObject.tag = "Inimigo";

        corpoFisico = GetComponent<Rigidbody2D>();
        if (corpoFisico == null && GetComponent<Collider2D>() != null)
        {
            // Um Rigidbody2D deve ficar no pai BossMorte, junto do collider principal.
            // Se estiver ausente, criamos um em runtime sem alterar ossos/animator.
            corpoFisico = gameObject.AddComponent<Rigidbody2D>();
        }

        renderizadores = GetComponentsInChildren<SpriteRenderer>(true);
        coresOriginais = new Color[renderizadores.Length];

        for (int i = 0; i < renderizadores.Length; i++)
        {
            if (renderizadores[i] != null)
                coresOriginais[i] = renderizadores[i].color;
        }
    }

    private void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (raizVisual == null && animator != null)
            raizVisual = animator.transform;

        if (raizVisual != null)
        {
            escalaVisualOriginal = raizVisual.localScale;
            visualEscalaCapturada = true;
        }

        if (corpoFisico != null)
        {
            // A boss é movida pelo controller, não pela gravidade. Um único Rigidbody2D
            // deve ficar no objeto pai que contém o CapsuleCollider2D principal.
            corpoFisico.bodyType = RigidbodyType2D.Kinematic;
            corpoFisico.gravityScale = 0f;
            corpoFisico.freezeRotation = true;
            corpoFisico.useFullKinematicContacts = true;
            corpoFisico.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        Rigidbody2D[] corposNaHierarquia = GetComponentsInChildren<Rigidbody2D>(true);
        foreach (Rigidbody2D corpoFilho in corposNaHierarquia)
        {
            if (corpoFilho != null && corpoFilho != corpoFisico)
            {
                // Mantém os colliders animados da foice ativos; o corpo filho fica cinemático,
                // sem gravidade, e acompanha o deslocamento da raiz BossMorte.
                corpoFilho.linearVelocity = Vector2.zero;
                corpoFilho.bodyType = RigidbodyType2D.Kinematic;
                corpoFilho.gravityScale = 0f;
                corpoFilho.freezeRotation = true;
                corpoFilho.simulated = true;
            }
        }

        vidaAtual = Mathf.Max(1f, vidaMaxima);
        LocalizarJogador();
        ConfigurarBarraVida();
        PrepararHitboxFoice();

        if (animator == null)
            Debug.LogError("[MorteBossController] Não encontrei o Animator da Morte nos filhos. Arraste o Animator para o campo Animator.", this);

        if (prefabCorvo == null)
            Debug.LogWarning("[MorteBossController] O campo Prefab Corvo está vazio. Arraste Assets/Assets/Personagem/morte/Corvo.prefab no Inspector para habilitar a horda.", this);
    }

    private void Update()
    {
        if (derrotada) return;

        if (jogador == null)
        {
            LocalizarJogador();
            if (jogador == null) return;
        }

        AtualizarOrientacao();

        if (emAtaque || Time.time < proximoAtaque) return;

        float distanciaX = Mathf.Abs(jogador.position.x - transform.position.x);
        float distanciaY = Mathf.Abs(jogador.position.y - transform.position.y);

        if (distanciaX > distanciaDeteccao || distanciaY > diferencaVerticalMaxima)
            return;

        PlayerHealth saudeJogador = jogador.GetComponentInParent<PlayerHealth>();
        if (saudeJogador != null && saudeJogador.VidaAtual <= 0f)
            return;

        emAtaque = true;
        bool usarCorvos = prefabCorvo != null && Random.value < chanceHordaCorvos;

        if (usarCorvos)
            rotinaAtaqueAtual = StartCoroutine(AtaqueCorvos());
        else
            rotinaAtaqueAtual = StartCoroutine(AtaqueInvestida(distanciaX >= distanciaInvestidaLonga));
    }

    private void FixedUpdate()
    {
        if (!movimentoInvestidaAtivo || derrotada) return;

        float xAtual = corpoFisico != null ? corpoFisico.position.x : transform.position.x;
        float novoX = Mathf.MoveTowards(xAtual, destinoInvestidaX, velocidadeMovimentoAtual * Time.fixedDeltaTime);

        if (corpoFisico != null)
            corpoFisico.MovePosition(new Vector2(novoX, corpoFisico.position.y));
        else
            transform.position = new Vector3(novoX, transform.position.y, transform.position.z);

        if (Mathf.Abs(novoX - destinoInvestidaX) <= 0.1f)
            movimentoInvestidaAtivo = false;
    }

    private void LocalizarJogador()
    {
        GameObject objetoJogador = GameObject.FindGameObjectWithTag("Player");
        if (objetoJogador != null)
            jogador = objetoJogador.transform;
    }

    private void AtualizarOrientacao()
    {
        if (jogador == null || raizVisual == null || !visualEscalaCapturada) return;

        bool jogadorAEsquerda = jogador.position.x < transform.position.x;
        bool inverterEscala = jogadorAEsquerda != visualOriginalOlhaEsquerda;
        float sinalX = inverterEscala ? -1f : 1f;
        raizVisual.localScale = new Vector3(Mathf.Abs(escalaVisualOriginal.x) * sinalX,
                                            escalaVisualOriginal.y,
                                            escalaVisualOriginal.z);
    }

    private void ConfigurarBarraVida()
    {
        if (barraVida == null)
            barraVida = FindFirstObjectByType<Slider>(FindObjectsInactive.Include);

        if (barraVida == null)
        {
            GameObject objetoBarra = GameObject.Find("BarraVidaBoss");
            if (objetoBarra != null)
                barraVida = objetoBarra.GetComponentInChildren<Slider>(true);
        }

        if (barraVida != null)
        {
            barraVida.minValue = 0f;
            barraVida.maxValue = vidaMaxima;
            barraVida.value = vidaAtual;
        }
    }

    private void PrepararHitboxFoice()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        Collider2D colliderFoice = null;

        foreach (Collider2D col in colliders)
        {
            if (col != null && col.gameObject.name.ToLowerInvariant().Contains("foice"))
            {
                colliderFoice = col;
                break;
            }
        }

        if (colliderFoice == null)
        {
            Debug.LogWarning("[MorteBossController] Não encontrei um Collider2D num objeto cujo nome contém 'foice'. O golpe ainda anima, mas não causará dano até a hitbox da foice estar configurada.", this);
            return;
        }

        hitboxFoice = colliderFoice.GetComponent<MorteFoiceHitbox>();
        if (hitboxFoice == null)
            hitboxFoice = colliderFoice.gameObject.AddComponent<MorteFoiceHitbox>();

        hitboxFoice.ConfigurarDano(danoFoice);
        hitboxFoice.DefinirAtiva(false);
    }

    private IEnumerator AtaqueCorvos()
    {
        if (animator != null)
        {
            animator.ResetTrigger(TriggerInvestida);
            animator.SetTrigger(TriggerCorvos);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, atrasoPrimeiroCorvo));

        for (int i = 0; i < quantidadeCorvos && !derrotada; i++)
        {
            if (jogador != null)
                LancarCorvo();

            if (i < quantidadeCorvos - 1)
                yield return new WaitForSeconds(intervaloEntreCorvos);
        }

        if (recuperacaoHordaCorvos > 0f)
            yield return new WaitForSeconds(recuperacaoHordaCorvos);

        FinalizarAtaque();
    }

    private void LancarCorvo()
    {
        if (prefabCorvo == null || jogador == null) return;

        float direcaoX = jogador.position.x >= transform.position.x ? 1f : -1f;
        Vector3 posicaoSpawn = pontoLancamentoCorvos != null
            ? pontoLancamentoCorvos.position
            : transform.position + new Vector3(offsetLancamentoCorvos.x * direcaoX,
                                                offsetLancamentoCorvos.y, 0f);

        Vector2 direcao = ((Vector2)jogador.position - (Vector2)posicaoSpawn).normalized;
        if (direcao.sqrMagnitude < 0.001f)
            direcao = new Vector2(direcaoX, 0f);

        float variacao = Random.Range(-dispersaoCorvosGraus, dispersaoCorvosGraus);
        direcao = (Vector2)(Quaternion.Euler(0f, 0f, variacao) * direcao);

        GameObject novoCorvo = Instantiate(prefabCorvo, posicaoSpawn, Quaternion.identity);
        CorvoBossProjectile projetil = novoCorvo.GetComponent<CorvoBossProjectile>();
        if (projetil == null)
            projetil = novoCorvo.AddComponent<CorvoBossProjectile>();

        projetil.Configurar(direcao, velocidadeCorvo, danoCorvo, tempoVidaCorvo,
                            GetComponentsInChildren<Collider2D>(true), corvoOriginalOlhaEsquerda);
    }

    private IEnumerator AtaqueInvestida(bool longa)
    {
        if (animator != null)
        {
            animator.ResetTrigger(TriggerCorvos);
            animator.SetTrigger(TriggerInvestida);
        }

        yield return new WaitForSeconds(tempoPreparacaoInvestida);

        if (longa && animator != null && animator.HasState(0, HashEstadoLongo))
        {
            // O clip curto prepara o movimento; a versão longa mantém a animação enquanto a
            // boss percorre a distância. Se o estado longo não existir, seguimos com o curto.
            animator.Play(HashEstadoLongo, 0, 0f);
        }

        IniciarMovimentoInvestida(longa);

        float limiteTempo = longa ? tempoMaximoInvestidaLonga : tempoMaximoInvestidaCurta;
        float tempo = 0f;
        while (movimentoInvestidaAtivo && tempo < limiteTempo && !derrotada)
        {
            tempo += Time.deltaTime;
            yield return null;
        }

        movimentoInvestidaAtivo = false;

        if (animator != null && animator.HasState(0, HashEstadoFoice))
            animator.Play(HashEstadoFoice, 0, 0f);

        yield return StartCoroutine(ExecutarJanelaFoice());

        if (recuperacaoDepoisDoGolpe > 0f)
            yield return new WaitForSeconds(recuperacaoDepoisDoGolpe);

        FinalizarAtaque();
    }

    private void IniciarMovimentoInvestida(bool longa)
    {
        if (jogador == null) return;

        float direcaoX = jogador.position.x >= transform.position.x ? 1f : -1f;
        float distanciaAtual = Mathf.Abs(jogador.position.x - transform.position.x);
        float distanciaMovimento = Mathf.Max(0f, distanciaAtual - distanciaParadaAntesDaKaya);
        float distanciaMaxima = longa ? distanciaMaximaInvestidaLonga : distanciaMaximaInvestidaCurta;

        distanciaMovimento = Mathf.Min(distanciaMovimento, distanciaMaxima);
        destinoInvestidaX = transform.position.x + direcaoX * distanciaMovimento;
        velocidadeMovimentoAtual = longa ? velocidadeInvestidaLonga : velocidadeInvestidaCurta;
        movimentoInvestidaAtivo = distanciaMovimento > 0.5f;

        if (corpoFisico != null)
            corpoFisico.linearVelocity = Vector2.zero;
    }

    private IEnumerator ExecutarJanelaFoice()
    {
        if (hitboxFoice == null)
        {
            if (duracaoMaximaAnimacaoGolpe > 0f)
                yield return new WaitForSeconds(duracaoMaximaAnimacaoGolpe);
            yield break;
        }

        hitboxFoice.DefinirAtiva(false);

        float tempoDecorrido = 0f;
        float limite = duracaoMaximaAnimacaoGolpe;
        bool usarTempoAnimacao = animator != null && animator.HasState(0, HashEstadoFoice);

        // O estado de ataque é iniciado imediatamente antes desta coroutine. A janela da
        // hitbox usa normalizedTime; se o estado não estiver disponível, há fallback por tempo.
        while (tempoDecorrido < limite && !derrotada)
        {
            float progresso;
            if (usarTempoAnimacao)
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName(EstadoAtaqueFoice))
                    progresso = info.normalizedTime;
                else
                    progresso = tempoDecorrido / Mathf.Max(0.01f, limite);
            }
            else
            {
                progresso = tempoDecorrido / Mathf.Max(0.01f, limite);
            }

            bool dentroDaJanela = progresso >= inicioHitboxFoiceNormalizado &&
                                  progresso <= fimHitboxFoiceNormalizado;
            hitboxFoice.DefinirAtiva(dentroDaJanela);

            if (usarTempoAnimacao && animator.GetCurrentAnimatorStateInfo(0).IsName(EstadoAtaqueFoice) &&
                animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.98f)
                break;

            tempoDecorrido += Time.deltaTime;
            yield return null;
        }

        hitboxFoice.DefinirAtiva(false);
    }

    private void FinalizarAtaque()
    {
        if (hitboxFoice != null)
            hitboxFoice.DefinirAtiva(false);

        movimentoInvestidaAtivo = false;
        emAtaque = false;
        proximoAtaque = Time.time + Mathf.Max(0.1f, tempoEntreAtaques);
        rotinaAtaqueAtual = null;
    }

    /// <summary>Chamado por SendMessageUpwards a partir do projétil da Kaya.</summary>
    public void TomarDano(int quantidadeDano)
    {
        if (derrotada || quantidadeDano <= 0) return;

        vidaAtual = Mathf.Clamp(vidaAtual - quantidadeDano, 0f, vidaMaxima);
        if (barraVida != null)
            barraVida.value = vidaAtual;

        if (rotinaPiscar != null)
            StopCoroutine(rotinaPiscar);
        rotinaPiscar = StartCoroutine(PiscarDano());

        if (vidaAtual <= 0f)
            Derrotar();
    }

    public void ReceberDano(int quantidadeDano) => TomarDano(quantidadeDano);

    private IEnumerator PiscarDano()
    {
        for (int i = 0; i < renderizadores.Length; i++)
        {
            if (renderizadores[i] != null)
                renderizadores[i].color = Color.red;
        }

        yield return new WaitForSeconds(tempoPiscarDano);

        for (int i = 0; i < renderizadores.Length; i++)
        {
            if (renderizadores[i] != null)
                renderizadores[i].color = coresOriginais[i];
        }

        rotinaPiscar = null;
    }

    private void Derrotar()
    {
        if (derrotada) return;

        derrotada = true;
        emAtaque = false;
        movimentoInvestidaAtivo = false;

        if (rotinaAtaqueAtual != null)
            StopCoroutine(rotinaAtaqueAtual);

        if (hitboxFoice != null)
            hitboxFoice.DefinirAtiva(false);

        if (corpoFisico != null)
        {
            corpoFisico.linearVelocity = Vector2.zero;
            corpoFisico.simulated = false;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D col in colliders)
        {
            if (col != null) col.enabled = false;
        }

        if (barraVida != null)
            barraVida.value = 0f;

        // Ainda não existe um clip de morte; congelamos a pose atual para não voltar a animar.
        if (animator != null)
            animator.speed = 0f;

        // Não há um clip de morte configurado ainda; deixamos o sprite visível como cadáver.
        // O UnityEvent permite ligar uma saída/plataforma ou o encerramento da fase no Inspector.
        aoSerDerrotada?.Invoke();
        enabled = false;
    }

    private void OnDisable()
    {
        movimentoInvestidaAtivo = false;
        if (hitboxFoice != null)
            hitboxFoice.DefinirAtiva(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccao);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaInvestidaLonga);
    }
}
