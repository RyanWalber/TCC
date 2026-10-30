using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float duracaoFade = 1.2f;

    private void Start()
    {
        // A cortina global ja executa o FadeIn; evita uma segunda cortina.
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void MudarDeCena(string nomeCena)
    {
        LevelLoader.CarregarComFade(nomeCena);
    }

    private IEnumerator FadeIn()
    {
        canvasGroup.blocksRaycasts = true;
        float tempo = duracaoFade;

        while (tempo > 0)
        {
            tempo -= Time.deltaTime;
            canvasGroup.alpha = tempo / duracaoFade;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    private IEnumerator FadeOutECarregar(string nomeCena)
    {
        canvasGroup.blocksRaycasts = true;
        float tempo = 0f;

        while (tempo < duracaoFade)
        {
            tempo += Time.deltaTime;
            canvasGroup.alpha = tempo / duracaoFade;
            yield return null;
        }

        canvasGroup.alpha = 1f;

        AsyncOperation carregamento = SceneManager.LoadSceneAsync(nomeCena);
        while (!carregamento.isDone)
        {
            yield return null;
        }
    }
}