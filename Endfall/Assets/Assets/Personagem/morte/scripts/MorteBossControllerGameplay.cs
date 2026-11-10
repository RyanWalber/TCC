using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cavaleira da Morte: State Pattern (vida alta, media e baixa).
/// Cada estado decide seus proprios comportamentos de forma ciclica,
/// independente da maquina visual do Animator.
/// </summary>
[DisallowMultipleComponent]
public class MorteBossControllerGameplay : MonoBehaviour
{
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
    [Min(0.1f)] [SerializeField] private float intervaloAtaques = 2.4f;

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

    private enum Acao
    {
        CorvosDiretos,
        CorvosLeque,
        InvocarLamentus,
        LancarFoice,
        InvestidaComGolpe
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
                case 0: return Acao.CorvosDiretos;    // 6 corvos
                case 1: return Acao.InvocarLamentus;
                default: return Acao.LancarFoice;
            }
        }
    }

    private sealed class VidaBaixa : EstadoVida
    {
        public override string Nome => "Vida Baixa (<=20%)";
        public override int QuantidadeComportamentos => 4;
        public override Acao Selecionar(int indice)
        {
            switch (indice % 4)
            {
                case 0: return Acao.CorvosDiretos;    // 10 corvos
                case 1: return Acao.InvocarLamentus;
                case 2: return Acao.LancarFoice;
                default: return Acao.InvestidaComGolpe;
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

        if (corpoFisico != null)
        {
            corpoFisico.bodyType = RigidbodyType2D.Kinematic;
            corpoFisico.gravityScale = 0f;
            corpoFisico.freezeRotation = true;
            corpoFisico.useFullKinematicContacts = true;
            corpoFisico.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        EncontrarJogador();
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

    private void EncontrarJogador()
    {
        if (jogador != null) return;
        GameObject objeto = GameObject.FindGameObjectWithTag("Player");
        if (objeto != null) jogador = objeto.transform;
    }

    private void Update()
    {
        if (vidaBoss != null && vidaBoss.EstaMorto) return;

        EncontrarJogador();
        if (jogador == null) return;

        AtualizarOrientacao();
        AtualizarEstadoVida();

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
        rotinaAtaque = StartCoroutine(ExecutarAcao(acao, faseDoAtaque));
    }

    private void AtualizarEstadoVida()
    {
        float percentual = vidaBoss != null ? vidaBoss.ObterPercentualVida() : 1f;
        EstadoVida novo = percentual > limiteVidaAlta ? alta :
            percentual > limiteVidaBaixa ? media : baixa;

        if (novo == estadoAtual) return;
        estadoAtual = novo;
        indiceAtaque = 0;
        // O Animator reflete e pode filtrar ataques por esta fase.
        if (animator != null)
            animator.SetInteger("FaseVida",
                estadoAtual == alta ? 1 : estadoAtual == media ? 2 : 3);
        Debug.Log("[Morte / State] Entrou em " + estadoAtual.Nome, this);
    }

    private IEnumerator ExecutarAcao(Acao acao, EstadoVida faseDoAtaque)
    {
        // Aproximacao serve SOMENTE para posicionar antes de invocar/lançar.
        // O ataque real da investida pertence exclusivamente a Vida Baixa.
        if (acao == Acao.InvocarLamentus || acao == Acao.LancarFoice)
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
            case Acao.InvestidaComGolpe:
                yield return AproximarDaKaya();
                yield return AtaqueInvestidaFinal();
                break;
        }

        movimentoAtivo = false;
        if (hitboxFoice != null) hitboxFoice.DefinirAtiva(false);
        // O retorno para morteociosa acontece por Has Exit Time,
        // configurado VISUALMENTE no Animator; nao forca Animator.Play().
        atacando = false;
        rotinaAtaque = null;
        proximoAtaque = Time.time + Mathf.Max(0.1f, intervaloAtaques);
    }

    private void FixedUpdate()
    {
        if (!movimentoAtivo || (vidaBoss != null && vidaBoss.EstaMorto)) return;

        float atual = corpoFisico != null ? corpoFisico.position.x : transform.position.x;
        float x = Mathf.MoveTowards(atual, destinoX, velocidadeInvestida * Time.fixedDeltaTime);
        if (corpoFisico != null)
            corpoFisico.MovePosition(new Vector2(x, corpoFisico.position.y));
        else
            transform.position = new Vector3(x, transform.position.y, transform.position.z);

        if (Mathf.Abs(x - destinoX) <= 0.25f)
            movimentoAtivo = false;
    }

    private IEnumerator AproximarDaKaya()
    {
        EncontrarJogador();
        if (jogador == null) yield break;

        float distancia = jogador.position.x - transform.position.x;
        float avancar = Mathf.Max(0f, Mathf.Abs(distancia) - distanciaParadaAntesDaKaya);
        if (avancar <= 1f) yield break;

        destinoX = transform.position.x + Mathf.Sign(distancia) * avancar;
        movimentoAtivo = true;
        if (animator != null)
        {
            animator.SetBool("AproximacaoLonga",
                Mathf.Abs(distancia) > distanciaDeteccao * 0.45f);
            DispararTrigger("Aproximar");
        }

        float espera = 0f;
        float limite = Mathf.Max(0.2f, tempoMaximoAproximacao);
        while (movimentoAtivo && espera < limite)
        {
            espera += Time.deltaTime;
            yield return null;
        }
        movimentoAtivo = false;
        if (corpoFisico != null) corpoFisico.linearVelocity = Vector2.zero;
    }

    private IEnumerator AtaqueCorvos(bool emLeque, EstadoVida faseDoAtaque)
    {
        DispararTrigger("AtaqueCorvos");
        yield return new WaitForSeconds(0.65f);

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
                yield return new WaitForSeconds(intervaloReal);
        }

        float usado = 0.65f + Mathf.Max(0f, quantidade - 1) * intervaloReal;
        if (DuracaoCorvos > usado)
            yield return new WaitForSeconds(DuracaoCorvos - usado);
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
            yield return new WaitForSeconds(tempoSpawn);
        InvocarLamentus();
        if (DuracaoSubindo > tempoSpawn)
            yield return new WaitForSeconds(DuracaoSubindo - tempoSpawn);
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
    }

    private IEnumerator AtaqueFoiceArremessada()
    {
        DispararTrigger("LancarFoice");
        // O proprio clipe ja anima foice_0 indo e voltando.
        // Nao duplicamos o desenho nem alteramos os keyframes.
        bool acertou = false;
        float decorrido = 0f;
        Vector2 anterior = foiceAnimada != null ?
            (Vector2)foiceAnimada.position : (Vector2)transform.position;

        while (decorrido < DuracaoFoiceArremessada)
        {
            decorrido += Time.deltaTime;
            if (foiceAnimada != null)
            {
                Vector2 atual = foiceAnimada.position;
                if (!acertou && decorrido >= inicioColisaoFoice &&
                    decorrido <= fimColisaoFoice)
                    acertou = VerificarHitFoiceArremessada(anterior, atual);
                anterior = atual;
            }
            yield return null;
        }
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
            vidaKaya.TomarDanoDeInimigo(danoFoiceArremessada);
        return true;
    }

    private IEnumerator AtaqueInvestidaFinal()
    {
        // O movimento "indo" ou "indolonge" so aproxima. Somente aqui
        // a animacao indoataque habilita a hitbox com dano.
        DispararTrigger("Investida");
        float tempo = 0f;
        while (tempo < DuracaoAtaqueFoice)
        {
            tempo += Time.deltaTime;
            float progresso = tempo / DuracaoAtaqueFoice;
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
            "Aproximar", "Investida"
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 1f);
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccao);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        atacando = false;
        movimentoAtivo = false;
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
