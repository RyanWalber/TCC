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

        if (dados != null && SaveManager.Instance != null && SaveManager.Instance.ConsumirPosicaoSalvaAoCarregar())
        {
            posicaoDestino = dados.posicaoSalvamento;
        }
        else if (dados != null && dados.passouCheckpoint)
        {
            string cenaCheckpoint = !string.IsNullOrEmpty(dados.nomeCenaCheckpoint)
                ? dados.nomeCenaCheckpoint
                : dados.nomeCena;

            if (cenaCheckpoint == cenaAtual)
            {
                if (usarCheckpointSalvo)
                {
                    posicaoDestino = dados.posicaoCheckpoint;
                }
            }
            else if (SaveManager.Instance != null)
            {
                SaveManager.Instance.DescartarCheckpointDeOutraCena(cenaAtual);
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