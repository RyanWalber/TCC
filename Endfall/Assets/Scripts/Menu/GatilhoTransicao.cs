using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GatilhoTransicao : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private string nomeProximaCena;
    [SerializeField] private float duracaoFade = 1.2f;

    private bool trocando = false;

    private void Start()
    {
        // Encontra o CanvasGroup automaticamente se o campo ficar vazio no Inspector
        if (canvasGroup == null)
        {
            canvasGroup = FindFirstObjectByType<CanvasGroup>();
        }
    }

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
        // Ao avançar de fase, recupera 25% da vida ATUAL (não da vida máxima).
        PlayerHealth hp = FindFirstObjectByType<PlayerHealth>();
        if (hp != null && hp.VidaAtual > 0f && hp.VidaAtual < hp.VidaMaxima)
            hp.RecuperarVida(hp.VidaAtual * 0.25f);

        // Define a cena de destino e estabelece o estado-base da nova fase antes do fade.
        if (SaveManager.Instance != null && !string.IsNullOrEmpty(nomeProximaCena))
            SaveManager.Instance.PrepararTransicaoDeFase(nomeProximaCena);

        // 2. EXECUTA O FADE DE TELA (se houver CanvasGroup na cena)
        if (canvasGroup != null)
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
        }

        // 3. CARREGA A NOVA CENA
        if (!string.IsNullOrEmpty(nomeProximaCena))
        {
            SceneManager.LoadScene(nomeProximaCena);
        }
        else
        {
            Debug.LogError("[GatilhoTransicao] Preencha o campo 'Nome Proxima Cena' no Inspector!");
        }
    }
}