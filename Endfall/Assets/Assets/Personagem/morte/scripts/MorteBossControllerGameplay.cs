using System.Collections;
using UnityEngine;

/// <summary>
/// IA da Morte. A vida, a barra e a derrota ficam no sistema universal BossVida.
/// Este script cuida apenas dos dois ataques e das animações.
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

    [Header("Combate")]
    [Min(1f)] [SerializeField] private float distanciaDeteccao = 800f;
    [Min(0.1f)] [SerializeField] private float intervaloAtaques = 2.4f;
    [Min(1)] [SerializeField] private int quantidadeCorvos = 5;
    [Min(0.01f)] [SerializeField] private float intervaloCorvos = 0.13f;
    [Min(1f)] [SerializeField] private float velocidadeCorvo = 520f;
    [Min(0.1f)] [SerializeField] private float vidaCorvo = 4f;
    [Min(0f)] [SerializeField] private float danoCorvo = 5f;
    [Min(0f)] [SerializeField] private float danoFoice = 11f;
    [Min(1f)] [SerializeField] private float velocidadeInvestida = 900f;
    [Min(0.1f)] [SerializeField] private float distanciaMaximaInvestida = 1100f;

    private Rigidbody2D corpoFisico;
    private MorteFoiceHitbox hitboxFoice;
    private Vector3 escalaVisualOriginal = Vector3.one;
    private float proximoAtaque;
    private float destinoInvestidaX;
    private bool movimentoAtivo;
    private bool atacando;
    private bool escalaCapturada;

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

        if (Mathf.Abs(jogador.position.x - transform.position.x) > distanciaDeteccao)
            return;

        atacando = true;

        // A horda é usada quando a Kaya está mais distante; perto, a Morte investe.
        if (Mathf.Abs(jogador.position.x - transform.position.x) > distanciaDeteccao * 0.45f && prefabCorvo != null)
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
        // O sprite original da Morte já está virado para a esquerda.
        // Portanto, escala positiva olha para a Kaya quando ela está à esquerda;
        // invertemos apenas quando o jogador está à direita.
        float sinal = jogador.position.x < transform.position.x ? 1f : -1f;
        raizVisual.localScale = new Vector3(Mathf.Abs(escalaVisualOriginal.x) * sinal,
                                            escalaVisualOriginal.y, escalaVisualOriginal.z);
    }

    private void PrepararHitboxFoice()
    {
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true))
        {
            if (col == null || !col.gameObject.name.ToLowerInvariant().Contains("foice")) continue;

            hitboxFoice = col.GetComponent<MorteFoiceHitbox>();
            if (hitboxFoice == null) hitboxFoice = col.gameObject.AddComponent<MorteFoiceHitbox>();
            hitboxFoice.ConfigurarDano(danoFoice);
            hitboxFoice.DefinirAtiva(false);
            return;
        }

        Debug.LogWarning("[Morte] Não encontrei Collider2D em um objeto com 'foice' no nome.", this);
    }

    private IEnumerator AtaqueCorvos()
    {
        if (animator != null)
        {
            animator.ResetTrigger(TriggerInvestida);
            animator.SetTrigger(TriggerCorvos);
        }

        // Dá tempo para a animação de lançamento começar.
        yield return new WaitForSeconds(0.65f);

        for (int i = 0; i < quantidadeCorvos; i++)
        {
            if (jogador != null) LancarCorvo(i);
            if (i < quantidadeCorvos - 1)
                yield return new WaitForSeconds(intervaloCorvos);
        }

        FinalizarAtaque();
    }

    private void LancarCorvo(int indice)
    {
        if (prefabCorvo == null || jogador == null) return;

        float lado = jogador.position.x >= transform.position.x ? 1f : -1f;
        Vector3 origemBase = pontoLancamentoCorvos != null
            ? pontoLancamentoCorvos.position
            : transform.position + new Vector3(lado * 25f, 80f, 0f);

        // Espalha as origens verticalmente e um pouco na horizontal para os corvos
        // não aparecerem empilhados no mesmo ponto.
        float altura = (indice - (quantidadeCorvos - 1) * 0.5f) * 13f;
        float deslocamentoX = (indice % 2 == 0 ? -1f : 1f) * (5f + indice * 2f);
        Vector3 origem = origemBase + new Vector3(deslocamentoX, altura, 0f);

        Vector2 direcao = ((Vector2)jogador.position - (Vector2)origem).normalized;
        if (direcao.sqrMagnitude < 0.001f) direcao = new Vector2(lado, 0f);

        GameObject corvo = Instantiate(prefabCorvo, origem, Quaternion.identity);
        CorvoBossProjectile projetil = corvo.GetComponent<CorvoBossProjectile>();
        if (projetil == null) projetil = corvo.AddComponent<CorvoBossProjectile>();

        Collider2D[] collidersDono = GetComponentsInChildren<Collider2D>(true);
        projetil.Configurar(direcao, velocidadeCorvo, Mathf.RoundToInt(danoCorvo), vidaCorvo,
                            collidersDono, true);
    }

    private IEnumerator AtaqueInvestida()
    {
        if (animator != null)
        {
            animator.ResetTrigger(TriggerCorvos);
            animator.SetTrigger(TriggerInvestida);
        }

        yield return new WaitForSeconds(0.22f);
        if (jogador == null) { FinalizarAtaque(); yield break; }

        // Captura a posição uma única vez: a Kaya pode desviar depois que a investida começa.
        float xJogadorNoInicio = jogador.position.x;
        float direcao = Mathf.Sign(xJogadorNoInicio - transform.position.x);
        if (Mathf.Approximately(direcao, 0f)) direcao = 1f;
        float distancia = Mathf.Min(Mathf.Abs(xJogadorNoInicio - transform.position.x), distanciaMaximaInvestida);
        destinoInvestidaX = transform.position.x + direcao * distancia;
        movimentoAtivo = distancia > 0.5f;

        if (corpoFisico != null) corpoFisico.linearVelocity = Vector2.zero;

        if (distancia > distanciaDeteccao * 0.45f && animator != null && animator.HasState(0, EstadoInvestidaLonga))
            animator.Play(EstadoInvestidaLonga, 0, 0f);

        float tempo = 0f;
        while (movimentoAtivo && tempo < 1.4f)
        {
            tempo += Time.deltaTime;
            yield return null;
        }
        movimentoAtivo = false;

        if (animator != null && animator.HasState(0, EstadoAtaqueFoice))
            animator.Play(EstadoAtaqueFoice, 0, 0f);

        yield return StartCoroutine(JanelaFoice());
        FinalizarAtaque();
    }

    private IEnumerator JanelaFoice()
    {
        if (hitboxFoice == null)
        {
            yield return new WaitForSeconds(0.6f);
            yield break;
        }

        float tempo = 0f;
        const float duracao = 0.8f;
        while (tempo < duracao)
        {
            float progresso = tempo / duracao;
            hitboxFoice.DefinirAtiva(progresso >= 0.30f && progresso <= 0.62f);
            tempo += Time.deltaTime;
            yield return null;
        }
        hitboxFoice.DefinirAtiva(false);
    }

    private void FinalizarAtaque()
    {
        if (hitboxFoice != null) hitboxFoice.DefinirAtiva(false);
        movimentoAtivo = false;
        atacando = false;
        proximoAtaque = Time.time + intervaloAtaques;
    }

    // Mantém compatibilidade com os projéteis que enviam dano por SendMessageUpwards.
    // A vida real continua sendo controlada pelo componente universal BossVida.
    public void TomarDano(int quantidadeDano)
    {
        if (quantidadeDano <= 0) return;

        BossVida vidaUniversal = GetComponent<BossVida>();
        if (vidaUniversal == null)
            vidaUniversal = GetComponentInChildren<BossVida>(true);

        if (vidaUniversal != null)
            vidaUniversal.TomarDano(quantidadeDano);
        else
            Debug.LogWarning("[Morte] Não encontrei BossVida no pai ou nos filhos. Adicione o componente universal de vida da boss.", this);
    }

    public void ReceberDano(int quantidadeDano) => TomarDano(quantidadeDano);

    private void OnDrawGizmosSelected()
    {
        // A IA mede distância apenas no eixo X; os gizmos mostram essa faixa horizontal.
        Vector3 centro = transform.position;
        float alturaGizmo = 35f;

        // Alcance máximo para a boss perceber a Kaya.
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 1f);
        DesenharFaixaHorizontal(centro, Mathf.Max(0f, distanciaDeteccao), alturaGizmo);

        // Dentro desta faixa, a Morte prefere a investida; fora dela, usa os corvos.
        Gizmos.color = new Color(1f, 0.25f, 0.25f, 1f);
        DesenharFaixaHorizontal(centro, Mathf.Max(0f, distanciaDeteccao * 0.45f), alturaGizmo * 0.65f);

        // Alcance máximo permitido para a investida.
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 1f);
        DesenharFaixaHorizontal(centro, Mathf.Max(0f, distanciaMaximaInvestida), alturaGizmo * 1.3f);

        if (jogador != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(centro, new Vector3(jogador.position.x, centro.y, centro.z));
            Gizmos.DrawSphere(new Vector3(jogador.position.x, centro.y, centro.z), 5f);
        }
    }

    private static void DesenharFaixaHorizontal(Vector3 centro, float distancia, float meiaAltura)
    {
        Vector3 esquerda = new Vector3(centro.x - distancia, centro.y, centro.z);
        Vector3 direita = new Vector3(centro.x + distancia, centro.y, centro.z);
        Gizmos.DrawLine(esquerda, direita);
        Gizmos.DrawLine(esquerda + Vector3.up * meiaAltura, esquerda - Vector3.up * meiaAltura);
        Gizmos.DrawLine(direita + Vector3.up * meiaAltura, direita - Vector3.up * meiaAltura);
        Gizmos.DrawWireSphere(esquerda, 4f);
        Gizmos.DrawWireSphere(direita, 4f);
    }

    private void OnDisable()
    {
        movimentoAtivo = false;
        if (hitboxFoice != null) hitboxFoice.DefinirAtiva(false);
    }
}
