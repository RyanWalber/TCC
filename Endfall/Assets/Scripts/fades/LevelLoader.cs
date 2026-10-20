using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }

    [Header("UI de Transição")]
    [SerializeField] private Image cortinaTransicao; 
    [SerializeField] private float tempoTransicao = 0.6f; 

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
        cortinaTransicao.raycastTarget = true; 

        yield return StartCoroutine(AnimarFade(0f, 1f));

        Time.timeScale = 1f;
        AudioListener.pause = false;

        AsyncOperation operacao = SceneManager.LoadSceneAsync(nomeCena);

        operacao.allowSceneActivation = false;

        while (operacao.progress < 0.9f)
        {
            yield return null;
        }

        SetAlpha(1f);

        operacao.allowSceneActivation = true;

        while (!operacao.isDone)
        {
            yield return null;
        }

        yield return null;

        yield return StartCoroutine(AnimarFade(1f, 0f));

        cortinaTransicao.raycastTarget = false; 
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