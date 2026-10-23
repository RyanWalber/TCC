using System.Collections;
using UnityEngine;

/// <summary>
/// Controla somente os ataques da Morte. Vida e barra são gerenciadas pelos componentes
/// universais BossVida e BossHealthBarController.
/// </summary>
[DisallowMultipleComponent]
public class MorteBossControllerGameplay : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform jogador;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform raizVisual;
    [SerializeField] private GameObject prefabCorvo;
    [SerializeField] private Transform pontoLancamentoCorvos;

    [Header("Detecção")]
    [Min(1f)][SerializeField] private float distanciaDeteccao = 500f;
    [Min(0.1f)][SerializeField] private float intervaloAtaques = 2.4f;

    [Header("Horda de corvos")]
    [Min(1)][SerializeField] private int quantidadeCorvos = 6;
    [Min(0.05f)][SerializeField] private float intervaloCorvos = 0.16f;
    [Tooltip("Velocidade de voo dos corvos. Pode ser ajustada durante o balanceamento.")]
    [Min(1f)][SerializeField] private float velocidadeCorvo = 520f;

    [Header("Investida")]
    [Min(1f)][SerializeField] private float velocidadeInvestida = 900f;
    [Tooltip("Distância que a Morte mantém da Kaya ao terminar a investida. Ajuste no Inspector.")]
    [Min(0f)][SerializeField] private float distanciaParadaAntesDaKaya = 70f;
    [Tooltip("Dano causado por um acerto da foice. Com 150 de vida, 75 de dano equivale a dois golpes.")]
    [Min(0f)][SerializeField] private float danoFoice = 75f;

    private const int DanoCorvo = 5;
    private const float TempoVidaCorvo = 4f;
    private const float TempoPreparacaoInvestida = 0.22f;
    private const float AtrasoLancamentoCorvos = 0.65f;
    private const float EspacamentoVerticalCorvos = 35f;
    private const float DuracaoJanelaFoice = 0.8f;
    private const float InicioJanelaFoice = 0.30f;
    private const float FimJanelaFoice = 0.62f;

    private Rigidbody2D corpoFisico;
    private MorteFoiceHitbox hitboxFoice;
    private Vector3 escalaVisualOriginal = Vector3.one;
    private float proximoAtaque;
    private float destinoInvestidaX;
    private bool movimentoAtivo;
    private bool atacando;
    private bool escalaCapturada;
    private bool proximoAtaqueSeraCorvos = true;

    private static readonly int TriggerCorvos = Animator.StringToHash("AtaqueCorvos");
    private static readonly int TriggerInvestida = Animator.StringToHash("Investida");
    private static readonly int EstadoInvestidaLonga = Animator.StringToHash("Base Layer.morteindolonge");
    private static readonly int EstadoAtaqueFoice = Animator.StringToHash("Base Layer.morteindoataque");

    private void Awake()
    {
        corpoFisico = GetComponent<Rigidbody2D>();
        if (corpoFisico != null)
        {
            corpoFisico.bodyType = RigidbodyType2D.Kinematic;
            corpoFisico.gravityScale = 0f;
            corpoFisico.freezeRotation = true;
            corpoFisico.useFullKinematicContacts = true;
            corpoFisico.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        if (jogador == null)
        {
            GameObject objetoJogador = GameObject.FindGameObjectWithTag("Player");
            if (objetoJogador != null) jogador = objetoJogador.transform;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (raizVisual == null && animator != null)
            raizVisual = animator.transform;

        if (raizVisual != null)
        {
            escalaVisualOriginal = raizVisual.localScale;
            escalaCapturada = true;
        }

        PrepararHitboxFoice();
    }

    private void Start()
    {
        if (animator == null)
            Debug.LogError("[Morte] Não encontrei o Animator nos filhos de BossMorte.", this);
        if (prefabCorvo == null)
            Debug.LogWarning("[Morte] Arraste o prefab Corvo para o campo Prefab Corvo.", this);
    }

    private void Update()
    {
        if (jogador == null)
        {
            GameObject objetoJogador = GameObject.FindGameObjectWithTag("Player");
            if (objetoJogador == null) return;
            jogador = objetoJogador.transform;
        }

        AtualizarOrientacao();

        if (atacando || Time.time < proximoAtaque) return;

        // O círculo do Gizmo corresponde a esta mesma detecção por distância.
        if (Vector2.Distance(transform.position, jogador.position) > distanciaDeteccao)
            return;

        PlayerHealth vidaJogador = jogador.GetComponentInParent<PlayerHealth>();
        if (vidaJogador == null)
            vidaJogador = jogador.GetComponentInChildren<PlayerHealth>(true);
        if (vidaJogador != null && vidaJogador.VidaAtual <= 0f) return;

        atacando = true;

        // Padrão fixo: horda de corvos -> investida -> horda -> investida.
        if (proximoAtaqueSeraCorvos && prefabCorvo != null)
            StartCoroutine(AtaqueCorvos());
        else
            StartCoroutine(AtaqueInvestida());
    }

    private void FixedUpdate()
    {
        if (!movimentoAtivo) return;

        float xAtual = corpoFisico != null ? corpoFisico.position.x : transform.position.x;
        float novoX = Mathf.MoveTowards(xAtual, destinoInvestidaX, velocidadeInvestida * Time.fixedDeltaTime);

        if (corpoFisico != null)
            corpoFisico.MovePosition(new Vector2(novoX, corpoFisico.position.y));
        else
            transform.position = new Vector3(novoX, transform.position.y, transform.position.z);

        if (Mathf.Abs(novoX - destinoInvestidaX) <= 0.1f)
            movimentoAtivo = false;
    }

    private void AtualizarOrientacao()
    {
        if (jogador == null || raizVisual == null || !escalaCapturada) return;

        // A arte original da Morte olha para a esquerda.
        // Se Kaya estiver à esquerda, mantém a escala original; se estiver à direita, espelha.
        const bool ArteOriginalOlhaParaEsquerda = true;
        bool jogadorAEsquerda = jogador.position.x < transform.position.x;
        bool precisaEspelhar = ArteOriginalOlhaParaEsquerda ? !jogadorAEsquerda : jogadorAEsquerda;
        float sinal = precisaEspelhar ? -1f : 1f;
        raizVisual.localScale = new Vector3(
            Mathf.Abs(escalaVisualOriginal.x) * sinal,
            escalaVisualOriginal.y,
            escalaVisualOriginal.z);
    }

    private void PrepararHitboxFoice()
    {
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true))
        {
            if (col == null || !col.gameObject.name.ToLowerInvariant().Contains("foice")) continue;

            hitboxFoice = col.GetComponent<MorteFoiceHitbox>();
            if (hitboxFoice == null)
                hitboxFoice = col.gameObject.AddComponent<MorteFoiceHitbox>();

            hitboxFoice.ConfigurarDano(danoFoice);
            hitboxFoice.DefinirAtiva(false);
            return;
        }

        Debug.LogWarning("[Morte] Não encontrei Collider2D num objeto cujo nome contém 'foice'.", this);
    }

    private IEnumerator AtaqueCorvos()
    {
        if (animator != null)
        {
            animator.ResetTrigger(TriggerInvestida);
            animator.SetTrigger(TriggerCorvos);
        }

        yield return new WaitForSeconds(AtrasoLancamentoCorvos);

        for (int i = 0; i < quantidadeCorvos; i++)
        {
            if (jogador != null) LancarCorvo(i);
            if (i < quantidadeCorvos - 1)
                yield return new WaitForSeconds(intervaloCorvos);
        }

        FinalizarAtaque(foiHordaCorvos: true);
    }

    private void LancarCorvo(int indice)
    {
        if (prefabCorvo == null || jogador == null) return;

        float lado = jogador.position.x >= transform.position.x ? 1f : -1f;
        Vector3 origemBase = pontoLancamentoCorvos != null
            ? pontoLancamentoCorvos.position
            : transform.position + new Vector3(lado * 25f, 80f, 0f);

        // Cada corvo sai de uma altura e deslocamento horizontal diferentes.
        // A fileira se espalha para cima e para baixo, em vez de nascer empilhada.
        float centro = (quantidadeCorvos - 1) * 0.5f;
        float altura = (indice - centro) * EspacamentoVerticalCorvos;
        float deslocamentoX = (indice % 2 == 0 ? -1f : 1f) * (8f + (indice / 2) * 12f);
        Vector3 origem = origemBase + new Vector3(deslocamentoX, altura, 0f);

        Vector2 direcao = ((Vector2)jogador.position - (Vector2)origem).normalized;
        if (direcao.sqrMagnitude < 0.001f)
            direcao = new Vector2(lado, 0f);

        GameObject corvo = Instantiate(prefabCorvo, origem, Quaternion.identity);
        CorvoBossProjectile projetil = corvo.GetComponent<CorvoBossProjectile>();
        if (projetil == null)
            projetil = corvo.AddComponent<CorvoBossProjectile>();

        projetil.Configurar(
            direcao,
            velocidadeCorvo,
            DanoCorvo,
            TempoVidaCorvo,
            GetComponentsInChildren<Collider2D>(true),
            arteOriginalOlhaEsquerda: true);
    }

    private IEnumerator AtaqueInvestida()
    {
        if (animator != null)
        {
            animator.ResetTrigger(TriggerCorvos);
            animator.SetTrigger(TriggerInvestida);
        }

        // Preparação da animação; o destino é capturado quando a investida começa de fato.
        yield return new WaitForSeconds(TempoPreparacaoInvestida);
        if (jogador == null)
        {
            FinalizarAtaque(foiHordaCorvos: false);
            yield break;
        }

        float xJogadorNoInicio = jogador.position.x;
        float diferencaX = xJogadorNoInicio - transform.position.x;
        float distanciaInicial = Mathf.Abs(diferencaX);
        bool longa = distanciaInicial > distanciaDeteccao * 0.45f;

        // Destino fixo antes da Kaya, mantendo a distância definida no Inspector.
        // Se a boss já estiver dentro dessa distância, ela não avança mais em direção ao jogador.
        float direcao = Mathf.Sign(diferencaX);
        float distanciaDeAvanco = Mathf.Max(0f, distanciaInicial - distanciaParadaAntesDaKaya);
        destinoInvestidaX = transform.position.x + direcao * distanciaDeAvanco;
        movimentoAtivo = distanciaDeAvanco > 0.5f;

        if (corpoFisico != null) corpoFisico.linearVelocity = Vector2.zero;

        if (longa && animator != null && animator.HasState(0, EstadoInvestidaLonga))
            animator.Play(EstadoInvestidaLonga, 0, 0f);

        float tempo = 0f;
        float distanciaPercorrer = Mathf.Abs(destinoInvestidaX - transform.position.x);
        float limiteTempo = Mathf.Max(1.4f, distanciaPercorrer / Mathf.Max(1f, velocidadeInvestida) + 0.5f);

        while (movimentoAtivo && tempo < limiteTempo)
        {
            tempo += Time.deltaTime;
            yield return null;
        }

        movimentoAtivo = false;

        if (animator != null && animator.HasState(0, EstadoAtaqueFoice))
            animator.Play(EstadoAtaqueFoice, 0, 0f);

        yield return null;
        yield return StartCoroutine(JanelaFoice());
        FinalizarAtaque(foiHordaCorvos: false);
    }

    private IEnumerator JanelaFoice()
    {
        if (hitboxFoice == null)
        {
            yield return new WaitForSeconds(DuracaoJanelaFoice);
            yield break;
        }

        float tempo = 0f;
        while (tempo < DuracaoJanelaFoice)
        {
            float progresso = tempo / DuracaoJanelaFoice;
            hitboxFoice.DefinirAtiva(
                progresso >= InicioJanelaFoice && progresso <= FimJanelaFoice);
            tempo += Time.deltaTime;
            yield return null;
        }

        hitboxFoice.DefinirAtiva(false);
    }

    private void FinalizarAtaque(bool foiHordaCorvos)
    {
        if (hitboxFoice != null) hitboxFoice.DefinirAtiva(false);

        movimentoAtivo = false;
        atacando = false;
        proximoAtaque = Time.time + intervaloAtaques;
        proximoAtaqueSeraCorvos = !foiHordaCorvos;
    }

    // O projétil da Kaya envia dano inteiro pelo SendMessageUpwards.
    // A vida e a barra continuam centralizadas no componente universal BossVida.
    public void TomarDano(int quantidadeDano)
    {
        if (quantidadeDano <= 0) return;

        BossVida vidaUniversal = GetComponent<BossVida>();
        if (vidaUniversal == null)
            vidaUniversal = GetComponentInChildren<BossVida>(true);

        if (vidaUniversal != null)
            vidaUniversal.TomarDano(quantidadeDano);
    }

    public void ReceberDano(int quantidadeDano) => TomarDano(quantidadeDano);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 1f);
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccao);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        movimentoAtivo = false;
        if (hitboxFoice != null) hitboxFoice.DefinirAtiva(false);
    }
}
