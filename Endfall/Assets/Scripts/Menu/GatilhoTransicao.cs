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
        PlayerHealth hp = jogadorCollider != null
            ? jogadorCollider.GetComponentInParent<PlayerHealth>()
            : null;
        if (hp == null) hp = FindFirstObjectByType<PlayerHealth>();


        float vidaDepoisDaRecuperacao = hp != null ? hp.VidaAtual : 100f;
        if (hp != null && hp.VidaAtual > 0f && hp.VidaAtual < hp.VidaMaxima)
        {
            float vidaAntes = hp.VidaAtual;
            hp.RecuperarVida(vidaAntes * 0.25f);
            vidaDepoisDaRecuperacao = hp.VidaAtual;
        }

        if (SaveManager.Instance != null && !string.IsNullOrEmpty(nomeProximaCena))
            SaveManager.Instance.PrepararTransicaoDeFase(nomeProximaCena, vidaDepoisDaRecuperacao);

        if (!string.IsNullOrEmpty(nomeProximaCena))
            ExplicacaoManager.ExibirAntesDaFase(nomeProximaCena);
        else
            Debug.LogError("[GatilhoTransicao] Preencha 'Nome Proxima Cena' no Inspector!");

        yield break;
    }
}