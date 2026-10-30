using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ExplicacaoManager : MonoBehaviour
{
    [Header("UI de Transição")]
    [SerializeField] private Image cortinaTransicao; 
    [SerializeField] private float tempoTransicao = 1.0f;

    [Header("Próxima Cena")]
    [SerializeField] private string nomeDaProximaCena = "FaseFome";

    private bool podeAvancar = false;
    private bool trocandoDeCena = false;

    private void Start()
    {
        // O LevelLoader faz o FadeIn ao entrar nesta cena.
        if (LevelLoader.Instance != null)
        {
            if (cortinaTransicao != null)
            {
                cortinaTransicao.enabled = false;
                cortinaTransicao.raycastTarget = false;
            }
            podeAvancar = true;
            return;
        }

        if (cortinaTransicao != null)
        {
            cortinaTransicao.fillAmount = 1f;
            StartCoroutine(ExecutarFadeIn());
        }
        else
        {
            podeAvancar = true;
        }
    }

    private void Update()
    {
        if (podeAvancar && !trocandoDeCena)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                StartCoroutine(ExecutarFadeOutECarregar());
            }
        }
    }

    private IEnumerator ExecutarFadeIn()
    {
        podeAvancar = false;
        yield return StartCoroutine(AnimarCortina(1f, 0f));
        podeAvancar = true;
    }

    private IEnumerator ExecutarFadeOutECarregar()
    {
        trocandoDeCena = true;
        podeAvancar = false;

        // A mesma cortina global fecha a cena e abre a seguinte.
        if (LevelLoader.Instance != null)
        {
            LevelLoader.CarregarComFade(nomeDaProximaCena);
            yield break;
        }

        if (cortinaTransicao != null)
            yield return StartCoroutine(AnimarCortina(0f, 1f));

        LevelLoader.CarregarComFade(nomeDaProximaCena);
    }

    private IEnumerator AnimarCortina(float inicio, float fim)
    {
        float tempo = 0f;
        while (tempo < tempoTransicao)
        {
            tempo += Time.deltaTime;
            cortinaTransicao.fillAmount = Mathf.Lerp(inicio, fim, tempo / tempoTransicao);
            yield return null;
        }
        cortinaTransicao.fillAmount = fim;
    }
}