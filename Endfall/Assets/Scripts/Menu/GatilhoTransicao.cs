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

        // A cortina global cuida do fade-out E fade-in da proxima cena.
        // Nao usa o antigo CanvasGroup para evitar fades duplicados.
        if (!string.IsNullOrEmpty(nomeProximaCena))
            ExplicacaoManager.ExibirAntesDaFase(nomeProximaCena);
        else
            Debug.LogError("[GatilhoTransicao] Preencha 'Nome Proxima Cena' no Inspector!");

        yield break;
    }
}