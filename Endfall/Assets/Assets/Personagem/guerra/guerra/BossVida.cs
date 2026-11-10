using System.Collections;
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
            Morrer();
            return;
        }

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

        // Desativa física e colisões
        if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;
        if (TryGetComponent<Rigidbody2D>(out var rb)) rb.simulated = false;

        // Desativa os scripts de comportamento/IA do Boss automaticamente
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this) script.enabled = false;
        }

        // Destrói o objeto pai da barra de vida completa
        if (conteinerBarraDeVida != null)
        {
            Destroy(conteinerBarraDeVida);
        }
        else if (barraDeVida != null)
        {
            Transform pai = barraDeVida.transform.parent;
            Destroy(pai != null ? pai.gameObject : barraDeVida.gameObject);
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