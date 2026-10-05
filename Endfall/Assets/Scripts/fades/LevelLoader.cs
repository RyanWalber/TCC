using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }

    [Header("UI de Transição")]
    [SerializeField] private Image cortinaTransicao; // Imagem preta da transição
    [SerializeField] private float tempoTransicao = 0.5f;

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
            // Começa 100% cobrindo a tela
            SetAlpha(1f);
            cortinaTransicao.raycastTarget = true;

            // Fade-In: Revela a cena inicial
            StartCoroutine(AnimarFade(1f, 0f));
        }
    }

    public void CarregarCena(string nomeCena)
    {
        StartCoroutine(RotinaTrocaDeCena(nomeCena));
    }

    private IEnumerator RotinaTrocaDeCena(string nomeCena)
    {
        // Bloqueia cliques durante o escurecimento para evitar cliques duplos
        cortinaTransicao.raycastTarget = true;

        // 1. FADE-OUT: A tela fica preta cobrindo a cena atual
        yield return StartCoroutine(AnimarFade(0f, 1f));

        Time.timeScale = 1f;
        AudioListener.pause = false;

        // 2. Carrega a nova cena enquanto a tela está 100% PRETA
        AsyncOperation operacao = SceneManager.LoadSceneAsync(nomeCena);
        while (!operacao.isDone)
        {
            yield return null;
        }

        // 3. FADE-IN: A tela clareia revelando a nova cena
        yield return StartCoroutine(AnimarFade(1f, 0f));

        // Desativa o bloqueio de cliques para você poder jogar/clicar nos botões
        cortinaTransicao.raycastTarget = false;
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

        // Se a tela ficou transparente, libera os cliques imediatamente
        if (alphaFinal <= 0f)
        {
            cortinaTransicao.raycastTarget = false;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (cortinaTransicao == null) return;
        Color cor = cortinaTransicao.color;
        cor.a = alpha;
        cortinaTransicao.color = cor;
    }
}