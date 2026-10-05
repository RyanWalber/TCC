using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }

    [Header("UI de Transição")]
    [SerializeField] private Image cortinaTransicao; // Arraste a CortinaGlobal aqui
    [SerializeField] private float tempoTransicao = 0.6f; // Tempo do Fade

    private bool emTransicao = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (cortinaTransicao != null)
        {
            // Ao ligar o jogo, garante que começa 100% coberto e abre a tela
            SetAlpha(1f);
            cortinaTransicao.raycastTarget = true;
            StartCoroutine(AnimarFade(1f, 0f));
        }
    }

    public void CarregarCena(string nomeCena)
    {
        if (!emTransicao)
        {
            StartCoroutine(RotinaTrocaDeCena(nomeCena));
        }
    }

    private IEnumerator RotinaTrocaDeCena(string nomeCena)
    {
        emTransicao = true;
        cortinaTransicao.raycastTarget = true; // Bloqueia cliques durante o fade

        // 1. SAÍDA (Fade-Out): A cor preta vai surgindo devagar até cobrir o jogo (0 -> 1)
        yield return StartCoroutine(AnimarFade(0f, 1f));

        Time.timeScale = 1f;
        AudioListener.pause = false;

        // 2. TROCA DE CENA EM SEGUNDO PLANO (Com a tela 100% preta)
        AsyncOperation operacao = SceneManager.LoadSceneAsync(nomeCena);

        // IMPEDE a Unity de mostrar a nova cena até autorizarmos!
        operacao.allowSceneActivation = false;

        // Aguarda o carregamento em background (0.9 significa pronto na Unity)
        while (operacao.progress < 0.9f)
        {
            yield return null;
        }

        // Garante que a tela continua totalmente preta antes de trocar
        SetAlpha(1f);

        // Agora sim: autoriza a Unity a renderizar a nova cena
        operacao.allowSceneActivation = true;

        // Aguarda a conclusão total do carregamento da cena
        while (!operacao.isDone)
        {
            yield return null;
        }

        // Aguarda 1 frame extra para os objetos da nova cena inicializarem atrás do escuro
        yield return null;

        // 3. ENTRADA (Fade-In): A cor preta vai sumindo devagar revelando a nova cena (1 -> 0)
        yield return StartCoroutine(AnimarFade(1f, 0f));

        cortinaTransicao.raycastTarget = false; // Desbloqueia os cliques para o jogador
        emTransicao = false;
    }

    private IEnumerator AnimarFade(float alphaInicial, float alphaFinal)
    {
        float tempo = 0f;
        while (tempo < tempoTransicao)
        {
            tempo += Time.unscaledDeltaTime;
            float novoAlpha = Mathf.Lerp(alphaInicial, alphaFinal, tempo / tempoTransicao);
            SetAlpha(novoAlpha);
            yield return null;
        }

        SetAlpha(alphaFinal);
    }

    private void SetAlpha(float alpha)
    {
        if (cortinaTransicao == null) return;
        Color cor = cortinaTransicao.color;
        cor.a = alpha;
        cortinaTransicao.color = cor;
    }
}