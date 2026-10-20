using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSetup : MonoBehaviour
{
    [SerializeField] private Transform pontoInicial;
    [SerializeField] private GameObject jogador;

    [Tooltip("Marque verdadeiro apenas se quiser que os checkpoints persistam nesta cena")]
    [SerializeField] private bool usarCheckpointSalvo = true;

    void Start()
    {
        if (jogador == null)
        {
            // Fallback para builds/cenas em que a referência não foi atribuída.
            jogador = GameObject.FindGameObjectWithTag("Player");
        }

        if (jogador == null)
        {
            Debug.LogError("[LevelSetup] Não foi encontrado um objeto Player. Atribua a Kaya no Inspector e confira a Tag.");
            return;
        }

        SaveData dados = SaveManager.Instance != null ? SaveManager.Instance.GetDadosAtuais() : null;
        string cenaAtual = SceneManager.GetActiveScene().name;

        Vector3 posicaoDestino = pontoInicial != null ? pontoInicial.position : jogador.transform.position;

        if (usarCheckpointSalvo && dados != null && dados.passouCheckpoint)
        {
            // Nunca usa a coordenada de um checkpoint pertencente a outra fase.
            // Saves antigos não possuem nomeCenaCheckpoint, então usamos nomeCena como fallback.
            string cenaCheckpoint = !string.IsNullOrEmpty(dados.nomeCenaCheckpoint)
                ? dados.nomeCenaCheckpoint
                : dados.nomeCena;

            if (cenaCheckpoint == cenaAtual)
            {
                posicaoDestino = dados.posicaoCheckpoint;
            }
        }

        jogador.transform.position = posicaoDestino;

        Rigidbody2D rb = jogador.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void ResetarCheckpointDoSave()
    {
        if (SaveManager.Instance != null)
        {
            SaveData dados = SaveManager.Instance.GetDadosAtuais();
            if (dados != null)
            {
                dados.passouCheckpoint = false;
                dados.posicaoCheckpoint = Vector3.zero;
                dados.nomeCenaCheckpoint = "";
                dados.checkpointStatusRegistrado = false;
            }
        }
    }
}