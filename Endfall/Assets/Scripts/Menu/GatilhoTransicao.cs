using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GatilhoTransicao : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private string nomeProximaCena;
    [SerializeField] private float duracaoFade = 1.2f;

    private bool trocando = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !trocando)
        {
            trocando = true;
            StartCoroutine(TrocarDeCena());
        }
    }

    private IEnumerator TrocarDeCena()
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
        SceneManager.LoadScene(nomeProximaCena);
    }
}