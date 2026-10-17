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
        // 1. RESETA O CHECKPOINT NO SAVE MANAGER
        // Garante que o jogador nasça no 'PontoInicial' da nova fase e não no checkpoint antigo
        if (SaveManager.Instance != null)
        {
            SaveData dados = SaveManager.Instance.GetDadosAtuais();
            if (dados != null)
            {
                dados.passouCheckpoint = false;
                dados.posicaoCheckpoint = Vector3.zero;
            }
        }

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