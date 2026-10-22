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
            StartCoroutine(TrocarDeCena(collision));
        }
    }

    private IEnumerator TrocarDeCena(Collider2D jogadorCollider)
    {
        // Usa o collider que realmente entrou no gatilho; collision não existe dentro da coroutine.
        PlayerHealth hp = jogadorCollider != null
            ? jogadorCollider.GetComponentInParent<PlayerHealth>()
            : null;
        if (hp == null) hp = FindFirstObjectByType<PlayerHealth>();

        // Ao avançar de fase, recupera 25% da vida atual e guarda exatamente esse resultado.

        float vidaDepoisDaRecuperacao = hp != null ? hp.VidaAtual : 100f;
        if (hp != null && hp.VidaAtual > 0f && hp.VidaAtual < hp.VidaMaxima)
        {
            float vidaAntes = hp.VidaAtual;
            hp.RecuperarVida(vidaAntes * 0.25f);
            vidaDepoisDaRecuperacao = hp.VidaAtual;
        }

        // Salva o valor calculado antes do fade para que a nova cena não restaure vida cheia.
        if (SaveManager.Instance != null && !string.IsNullOrEmpty(nomeProximaCena))
            SaveManager.Instance.PrepararTransicaoDeFase(nomeProximaCena, vidaDepoisDaRecuperacao);

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