using UnityEngine;

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
            Debug.LogError("[LevelSetup] Objeto do Jogador não atribuído no Inspector!");
            return;
        }

        SaveData dados = SaveManager.Instance != null ? SaveManager.Instance.GetDadosAtuais() : null;

        Vector3 posicaoDestino = pontoInicial != null ? pontoInicial.position : jogador.transform.position;

        if (usarCheckpointSalvo && dados != null && dados.passouCheckpoint && dados.posicaoCheckpoint != Vector3.zero)
        {
            posicaoDestino = dados.posicaoCheckpoint;
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
            }
        }
    }
}