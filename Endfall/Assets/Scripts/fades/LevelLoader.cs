using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Cortina unica e persistente para entrada, saida, respawn e troca de cenas.
// Funciona tambem quando uma fase e iniciada diretamente no Editor.
public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }

    // A narrativa pode aguardar o fim do Fade In antes de digitar.
    public bool EstaEmTransicao => emTransicao || fadeInicial != null;

    [Header("UI de Transicao")]
    [SerializeField] private Image cortinaTransicao;
    [Min(0.05f)] [SerializeField] private float tempoTransicao = 0.6f;

    private Image cortinaGlobal;
    private Coroutine fadeInicial;
    private bool emTransicao;

    // Se a cena de abertura ja possui LevelLoader, reutiliza essa instancia.
    // Caso o usuario execute uma fase diretamente no Editor, cria uma unica.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InicializarAoAbrirCena()
    {
        GarantirInstancia();
    }

    public static LevelLoader GarantirInstancia()
    {
        if (Instance != null) return Instance;

        LevelLoader existente = FindFirstObjectByType<LevelLoader>();
        if (existente != null)
        {
            Instance = existente;
            return Instance;
        }

        GameObject controlador = new GameObject("LevelLoaderGlobal");
        return controlador.AddComponent<LevelLoader>();
    }

    public static void CarregarComFade(string nomeCena)
    {
        LevelLoader loader = GarantirInstancia();
        if (loader != null) loader.CarregarCena(nomeCena);
    }

    private void Awake()
    {
        // A Splash possui um LevelLoader no proprio Canvas. Persistir esse
        // objeto preservaria a interface inteira da Splash na proxima cena.
        // Mantemos apenas um controlador independente da UI de cada cena.
        if (cortinaTransicao != null)
        {
            cortinaTransicao.enabled = false;
            cortinaTransicao.raycastTarget = false;
        }

        if (Instance != null && Instance != this)
        {
            // Destroi so o componente, nunca o Canvas da cena atual.
            Destroy(this);
            return;
        }

        if (GetComponent<Canvas>() != null || transform.parent != null)
        {
            GameObject controlador = new GameObject("LevelLoaderGlobal");
            LevelLoader persistente = controlador.AddComponent<LevelLoader>();
            persistente.tempoTransicao = tempoTransicao;
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CriarCortinaGlobal();
        DefinirAlpha(1f);
        cortinaGlobal.raycastTarget = true;
    }

    private void Start()
    {
        if (!emTransicao)
            fadeInicial = StartCoroutine(AbrirCena());
    }

    private IEnumerator AbrirCena()
    {
        // Garante pelo menos um frame inteiramente preto na primeira cena.
        DefinirAlpha(1f);
        yield return null;
        if (emTransicao)
        {
            fadeInicial = null;
            yield break;
        }

        yield return AnimarFade(1f, 0f);
        if (!emTransicao && cortinaGlobal != null)
            cortinaGlobal.raycastTarget = false;
        fadeInicial = null;
    }

    private void CriarCortinaGlobal()
    {
        // Uma Canvas propria acima dos HUDs e menus de todas as cenas.
        GameObject canvasObj = new GameObject("CanvasTransicaoGlobal",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32000;

        GameObject fundoObj = new GameObject("FadePreto",
            typeof(RectTransform), typeof(Image));
        fundoObj.transform.SetParent(canvasObj.transform, false);
        RectTransform retangulo = fundoObj.GetComponent<RectTransform>();
        retangulo.anchorMin = Vector2.zero;
        retangulo.anchorMax = Vector2.one;
        retangulo.offsetMin = Vector2.zero;
        retangulo.offsetMax = Vector2.zero;

        cortinaGlobal = fundoObj.GetComponent<Image>();
        cortinaGlobal.color = Color.black;
        cortinaGlobal.raycastTarget = true;
    }

    public void CarregarCena(string nomeCena)
    {
        if (emTransicao) return;

        if (string.IsNullOrEmpty(nomeCena) ||
            !Application.CanStreamedLevelBeLoaded(nomeCena))
        {
            Debug.LogError("[LevelLoader] Cena invalida ou ausente do Build Settings: " + nomeCena, this);
            return;
        }

        // O audio acompanha o Fade Out global, sem interromper o loop
        // quando o destino for a tela de narrativa.
        if (AudioManagerGlobal.Instance != null)
            AudioManagerGlobal.Instance.AoIniciarTransicao(nomeCena, tempoTransicao);

        emTransicao = true;
        if (fadeInicial != null)
        {
            StopCoroutine(fadeInicial);
            fadeInicial = null;
        }
        StartCoroutine(TrocarCena(nomeCena));
    }

    private IEnumerator TrocarCena(string nomeCena)
    {
        if (cortinaGlobal == null)
            CriarCortinaGlobal();

        cortinaGlobal.raycastTarget = true;
        yield return AnimarFade(cortinaGlobal.color.a, 1f);

        // Desfaz pausa antes de carregar, mas o fade ja ocorreu em tempo real.
        Time.timeScale = 1f;
        AudioListener.pause = false;

        AsyncOperation operacao = SceneManager.LoadSceneAsync(nomeCena);
        if (operacao == null)
        {
            Debug.LogError("[LevelLoader] Nao foi possivel carregar: " + nomeCena, this);
            yield return AnimarFade(1f, 0f);
            cortinaGlobal.raycastTarget = false;
            emTransicao = false;
            yield break;
        }

        operacao.allowSceneActivation = false;
        while (operacao.progress < 0.9f)
            yield return null;

        operacao.allowSceneActivation = true;
        while (!operacao.isDone)
            yield return null;

        // Mantem a tela preta ate o Start/LevelSetup posicionar a Kaya.
        // Um frame adicional permite a primeira renderizacao da nova cena.
        DefinirAlpha(1f);
        yield return null;
        yield return null;
        yield return AnimarFade(1f, 0f);
        cortinaGlobal.raycastTarget = false;
        emTransicao = false;
    }

    private IEnumerator AnimarFade(float inicio, float fim)
    {
        float tempo = 0f;
        float duracao = Mathf.Max(0.05f, tempoTransicao);
        while (tempo < duracao)
        {
            // Carregar uma fase pode gerar um frame muito longo. Sem limitar
            // esse delta, o Fade In saltaria diretamente de preto para claro.
            tempo += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            DefinirAlpha(Mathf.Lerp(inicio, fim, Mathf.Clamp01(tempo / duracao)));
            yield return null;
        }
        DefinirAlpha(fim);
    }

    private void DefinirAlpha(float alpha)
    {
        if (cortinaGlobal == null) return;
        Color cor = cortinaGlobal.color;
        cor.a = Mathf.Clamp01(alpha);
        cortinaGlobal.color = cor;
    }
}
