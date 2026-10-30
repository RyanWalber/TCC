using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Cortina unica e persistente para entrada, saida, respawn e troca de cenas.
// Funciona tambem quando uma fase e iniciada diretamente no Editor.
public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }

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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Evita que a cortina antiga da Splash e a global se sobreponham.
        if (cortinaTransicao != null)
            cortinaTransicao.enabled = false;

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

        // Um frame para o LevelSetup posicionar a Kaya no checkpoint.
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
            tempo += Time.unscaledDeltaTime;
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
