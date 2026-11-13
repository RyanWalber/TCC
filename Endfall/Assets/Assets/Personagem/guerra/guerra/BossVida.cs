using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossVida : MonoBehaviour
{
    [Header("Configurações de Vida")]
    [SerializeField] private float vidaMaxima = 100f;
    private float vidaAtual;

    [Header("UI da Barra de Vida")]
    [SerializeField] private Slider barraDeVida;

    [Tooltip("Arraste aqui o objeto PAI de toda a UI da barra (ex: BarraVidaBoss)")]
    [SerializeField] private GameObject conteinerBarraDeVida;

    [Header("Feedback de Dano (Piscar Vermelho)")]
    [SerializeField] private Color corDano = Color.red;
    [SerializeField] private float duracaoPiscar = 0.15f;

    [Header("Elixires nas arenas dos chefes - opcional")]
    [Tooltip("Preencha com o prefab 'Vida' (HealthPickup). Vazio = nao gera nada.")]
    [SerializeField] private HealthPickup prefabElixirArena;
    [Tooltip("A Kaya precisa estar a esta distancia do chefe para o gerador iniciar.")]
    [Min(1f)] [SerializeField] private float distanciaAtivacaoElixires = 350f;
    [Tooltip("Tempo ate o primeiro elixir depois que Kaya entra na arena.")]
    [Min(0f)] [SerializeField] private float atrasoPrimeiroElixir = 7f;
    [Tooltip("Intervalo entre os elixires durante a luta.")]
    [Min(0.5f)] [SerializeField] private float intervaloSpawnElixires = 18f;
    [Tooltip("Maximo de elixires presentes ao mesmo tempo.")]
    [Min(1)] [SerializeField] private int maximoElixiresPresentes = 2;
    [Tooltip("Total permitido durante toda a luta. Use 0 para quantidade ilimitada.")]
    [Min(0)] [SerializeField] private int quantidadeTotalElixires = 6;
    [Tooltip("Quantidade de vida recuperada por elixir de arena.")]
    [Min(1f)] [SerializeField] private float curaElixirArena = 25f;
    [Tooltip("Opcional: arraste pontos definidos na Hierarchy. Quando preenchidos, o gerador escolhe dentre eles.")]
    [SerializeField] private Transform[] pontosElixires;
    [Header("Elixires - surgimento automatico sem pontos")]
    [Tooltip("Tenta encontrar plataformas embaixo desta area horizontal ao redor do chefe.")]
    [Min(1f)] [SerializeField] private float raioSpawnElixires = 200f;
    [SerializeField] private LayerMask chaoParaElixires = 1 << 3;
    [Min(0f)] [SerializeField] private float alturaAcimaDoChaoElixires = 12f;
    [Tooltip("Remove elixires temporarios gerados pela luta depois da derrota do chefe.")]
    [SerializeField] private bool removerElixiresQuandoBossMorrer = true;

    [Header("Morte")]
    [SerializeField] private float tempoParaDestruir = 0.5f;

    private SpriteRenderer[] todosSprites;
    private Color[] coresOriginais;
    private Coroutine corrotinaPiscar;
    private bool estaMorto = false;
    private bool invulneravelNaTransicao;
    private bool vidaConfigurada;
    private RectTransform barraTremendo;
    private Vector2 posicaoOriginalDaBarra;
    private readonly List<HealthPickup> elixiresArenaAtivos = new List<HealthPickup>();
    private int quantidadeGeradaArena;
    private Transform kayaNaArena;
    private Coroutine rotinaElixiresArena;

    public bool EstaMorto => estaMorto;
    public bool InvulneravelNaTransicao => invulneravelNaTransicao;
    // Usado pela Morte final para iniciar a reação no mesmo frame do dano.
    public event System.Action<float> VidaAlterada;

    // Usado pela Morte final sem alterar a vida de outros chefes.
    // Chamado em Start pelo controlador e refletido na barra de HP.
    public void ConfigurarVidaMaxima(float valor)
    {
        if (estaMorto) return;
        vidaMaxima = Mathf.Max(1f, valor);
        vidaAtual = vidaMaxima;
        vidaConfigurada = true;
        AtualizarBarraUI();
    }

    public GameObject ObterPainelBarra()
    {
        if (conteinerBarraDeVida != null) return conteinerBarraDeVida;
        if (barraDeVida != null)
        {
            Transform pai = barraDeVida.transform.parent;
            return pai != null ? pai.gameObject : barraDeVida.gameObject;
        }
        return null;
    }

    private void Start()
    {
        // Se o controlador ja definiu HP em Start, nao sobrescreve.
        if (!vidaConfigurada)
            vidaAtual = vidaMaxima;

        // Pega todos os SpriteRenderers do Boss e dos filhos
        todosSprites = GetComponentsInChildren<SpriteRenderer>();

        if (todosSprites != null && todosSprites.Length > 0)
        {
            coresOriginais = new Color[todosSprites.Length];
            for (int i = 0; i < todosSprites.Length; i++)
            {
                coresOriginais[i] = todosSprites[i].color;
            }
        }

        AtualizarBarraUI();

        // Barra invisivel ate o Gatilho Intro Boss; continua ativa para
        // manter o Slider e a configuracao original.
        GameObject painel = ObterPainelBarra();
        if (painel != null) BossBarraFade.Garantir(painel);

        // Cada boss possui seu proprio gerador (Fome, Peste, Guerra, Morte).
        // Nao inicia nada se o prefab opcional estiver vazio.
        if (prefabElixirArena != null)
            rotinaElixiresArena = StartCoroutine(GerarElixiresDuranteBatalha());
    }

    private IEnumerator GerarElixiresDuranteBatalha()
    {
        bool iniciou = false;
        float cronometro = 0f;
        float proximo = Mathf.Max(0f, atrasoPrimeiroElixir);
        while (!estaMorto)
        {
            if (kayaNaArena == null)
            {
                GameObject kaya = GameObject.FindGameObjectWithTag("Player");
                if (kaya != null) kayaNaArena = kaya.transform;
            }

            bool perto = kayaNaArena != null &&
                kayaNaArena.gameObject.activeInHierarchy &&
                Vector2.Distance(transform.position, kayaNaArena.position)
                <= distanciaAtivacaoElixires;

            if (perto && !iniciou) iniciou = true;
            if (perto && iniciou)
            {
                cronometro += Time.deltaTime;
                if (cronometro >= proximo)
                {
                    elixiresArenaAtivos.RemoveAll(x => x == null ||
                        !x.gameObject.activeInHierarchy);

                    bool podeGerar = elixiresArenaAtivos.Count <
                        Mathf.Max(1, maximoElixiresPresentes);
                    bool naoAtingiuTotal = quantidadeTotalElixires == 0 ||
                        quantidadeGeradaArena < quantidadeTotalElixires;

                    if (podeGerar && naoAtingiuTotal && TentarEscolherLocalElixir(out Vector3 posicao))
                    {
                        HealthPickup novo = Instantiate(prefabElixirArena, posicao,
                            Quaternion.identity);
                        novo.ConfigurarComoElixirDeBoss(curaElixirArena);
                        elixiresArenaAtivos.Add(novo);
                        quantidadeGeradaArena++;
                    }

                    cronometro = 0f;
                    proximo = Mathf.Max(0.5f, intervaloSpawnElixires);
                }
            }
            yield return null;
        }
    }

    private bool TentarEscolherLocalElixir(out Vector3 posicao)
    {
        posicao = transform.position;

        if (pontosElixires != null && pontosElixires.Length > 0)
        {
            int inicio = Random.Range(0, pontosElixires.Length);
            for (int i = 0; i < pontosElixires.Length; i++)
            {
                Transform ponto = pontosElixires[(inicio + i) % pontosElixires.Length];
                if (ponto == null) continue;
                posicao = ponto.position;
                return true;
            }
        }

        // Se nao houver pontos, tenta criar sobre o chao real, e nunca
        // no ar ou atravessando uma parede.
        for (int tentativa = 0; tentativa < 12; tentativa++)
        {
            float x = transform.position.x +
                Random.Range(-raioSpawnElixires, raioSpawnElixires);
            Vector2 origem = new Vector2(x, transform.position.y + 300f);
            RaycastHit2D[] impactos = Physics2D.RaycastAll(
                origem, Vector2.down, 900f, chaoParaElixires);
            foreach (RaycastHit2D impacto in impactos)
            {
                if (impacto.collider == null || impacto.collider.isTrigger ||
                    impacto.normal.y < 0.55f) continue;
                posicao = new Vector3(impacto.point.x,
                    impacto.point.y + alturaAcimaDoChaoElixires,
                    transform.position.z);
                return true;
            }
        }

        return false;
    }

    private void EncerrarElixiresDaArena()
    {
        if (rotinaElixiresArena != null)
        {
            StopCoroutine(rotinaElixiresArena);
            rotinaElixiresArena = null;
        }

        if (!removerElixiresQuandoBossMorrer) return;
        foreach (HealthPickup elixir in elixiresArenaAtivos)
            if (elixir != null) Destroy(elixir.gameObject);
        elixiresArenaAtivos.Clear();
    }

    public void ReceberDano(float dano) => TomarDano(dano);

    public void TomarDano(float dano)
    {
        if (estaMorto || invulneravelNaTransicao || dano <= 0f) return;

        vidaAtual = Mathf.Clamp(vidaAtual - dano, 0f, vidaMaxima);
        AtualizarBarraUI();

        // Morte definitiva tem prioridade sobre qualquer troca de estágio.
        if (vidaAtual <= 0f)
        {
            AudioInimigo.NotificarVidaBoss(this, false);
            Morrer();
            return;
        }

        AudioInimigo.NotificarVidaBoss(this, false);

        // O evento pode ativar a proteção de fase IMEDIATAMENTE.
        VidaAlterada?.Invoke(ObterPercentualVida());

        if (!invulneravelNaTransicao && todosSprites != null &&
            todosSprites.Length > 0)
        {
            if (corrotinaPiscar != null) StopCoroutine(corrotinaPiscar);
            corrotinaPiscar = StartCoroutine(RotinaPiscarVermelho());
        }
    }

    // Deixa a boss totalmente preta durante o ritual de troca de fase.
    // Os outros chefes nunca usam esse modo; seu comportamento permanece igual.
    public void DefinirInvulnerabilidadeDeFase(bool ativar)
    {
        if (estaMorto || invulneravelNaTransicao == ativar) return;
        invulneravelNaTransicao = ativar;

        // Evita o vermelho da reação ao dano competir com a cor preta.
        if (corrotinaPiscar != null)
        {
            StopCoroutine(corrotinaPiscar);
            corrotinaPiscar = null;
        }
        AtualizarCorTransicao();
    }

    private void AtualizarCorTransicao()
    {
        if (todosSprites == null) return;
        for (int i = 0; i < todosSprites.Length; i++)
        {
            SpriteRenderer sprite = todosSprites[i];
            if (sprite == null) continue;
            if (invulneravelNaTransicao)
            {
                // Preserva o alfa da arte original, mas zera RGB.
                float alfa = coresOriginais != null && i < coresOriginais.Length ?
                    coresOriginais[i].a : sprite.color.a;
                sprite.color = new Color(0f, 0f, 0f, alfa);
            }
            else if (coresOriginais != null && i < coresOriginais.Length)
                sprite.color = coresOriginais[i];
        }
    }

    private void LateUpdate()
    {
        // Animator pode escrever cores nos sprites a cada frame.
        // Aplicar depois dele garante preto total ate o tremor terminar.
        if (invulneravelNaTransicao)
            AtualizarCorTransicao();
    }

    // Faz somente a barra da boss oscilar, sem deslocar Camera nem jogador.
    public IEnumerator TremerBarraVida(float duracao, float amplitude, float frequencia)
    {
        RectTransform alvo = conteinerBarraDeVida != null ?
            conteinerBarraDeVida.GetComponent<RectTransform>() : null;
        if (alvo == null && barraDeVida != null)
            alvo = barraDeVida.GetComponent<RectTransform>();

        duracao = Mathf.Max(0.05f, duracao);
        if (alvo == null)
        {
            yield return new WaitForSecondsRealtime(duracao);
            yield break;
        }

        Vector2 original = alvo.anchoredPosition;
        barraTremendo = alvo;
        posicaoOriginalDaBarra = original;
        float decorrido = 0f;
        try
        {
            while (decorrido < duracao && alvo != null)
            {
                decorrido += Time.unscaledDeltaTime;
                float tempo = decorrido * Mathf.Max(1f, frequencia);
                float peso = Mathf.Sin(Mathf.PI * Mathf.Clamp01(decorrido / duracao));
                Vector2 deslocamento = new Vector2(
                    Mathf.Sin(tempo * 2.1f), Mathf.Cos(tempo * 2.8f)) *
                    Mathf.Max(0f, amplitude) * peso;
                alvo.anchoredPosition = original + deslocamento;
                yield return null;
            }
        }
        finally
        {
            if (alvo != null) alvo.anchoredPosition = original;
            if (barraTremendo == alvo) barraTremendo = null;
        }
    }

    private void OnDisable()
    {
        EncerrarElixiresDaArena();
        if (barraTremendo != null)
        {
            barraTremendo.anchoredPosition = posicaoOriginalDaBarra;
            barraTremendo = null;
        }
    }

    // Retorna um valor entre 0.0 (0%) e 1.0 (100%)
    public float ObterPercentualVida()
    {
        if (vidaMaxima <= 0f) return 0f;
        return vidaAtual / vidaMaxima;
    }

    private IEnumerator RotinaPiscarVermelho()
    {
        for (int i = 0; i < todosSprites.Length; i++)
        {
            if (todosSprites[i] != null)
                todosSprites[i].color = corDano;
        }

        yield return new WaitForSeconds(duracaoPiscar);

        for (int i = 0; i < todosSprites.Length; i++)
        {
            if (todosSprites[i] != null)
                todosSprites[i].color = coresOriginais[i];
        }
    }

    private void AtualizarBarraUI()
    {
        if (barraDeVida != null)
        {
            barraDeVida.maxValue = vidaMaxima;
            barraDeVida.value = vidaAtual;
        }
    }

    private void Morrer()
    {
        if (estaMorto) return;
        invulneravelNaTransicao = false;
        if (corrotinaPiscar != null)
        {
            StopCoroutine(corrotinaPiscar);
            corrotinaPiscar = null;
        }
        AtualizarCorTransicao();
        estaMorto = true;
        AudioInimigo.NotificarVidaBoss(this, true);
        EncerrarElixiresDaArena();

        // Desativa física e colisões
        if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;
        if (TryGetComponent<Rigidbody2D>(out var rb)) rb.simulated = false;

        // Desativa os scripts de comportamento/IA do Boss automaticamente
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this) script.enabled = false;
        }

        // Fade de saida em vez de apagar a barra instantaneamente.
        // O componente de UI sobrevive ate o alpha chegar a zero.
        GameObject painel = ObterPainelBarra();
        if (painel != null)
        {
            BossBarraFade transicao = BossBarraFade.Garantir(painel);
            if (transicao != null)
            {
                transicao.LiberarDaHierarquiaDoBoss(transform);
                transicao.OcultarEDestruir(0.5f);
            }
        }

        // Somente a Morte final permanece viva ate o ultimo frame
        // de mortemorre.anim. Os demais BossVida continuam iguais.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "FaseMorte" &&
            (GetComponentInChildren<MorteBossControllerGameplay>(true) != null ||
             GetComponentInParent<MorteBossControllerGameplay>() != null))
        {
            // Colisores da foice e dos filhos tambem devem parar de causar dano.
            foreach (Collider2D colliderFinal in GetComponentsInChildren<Collider2D>(true))
                colliderFinal.enabled = false;
            foreach (Rigidbody2D corpoFinal in GetComponentsInChildren<Rigidbody2D>(true))
                corpoFinal.simulated = false;

            FinalizadorMorte.Agendar(this);
            return;
        }

        Destroy(gameObject, tempoParaDestruir);
    }
}