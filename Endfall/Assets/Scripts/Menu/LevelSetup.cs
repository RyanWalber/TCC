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

        // Posição padrão sempre será o Ponto Inicial da cena
        Vector3 posicaoDestino = pontoInicial != null ? pontoInicial.position : jogador.transform.position;

        // Só aplica o checkpoint se a opção estiver ativa E o checkpoint existir
        if (usarCheckpointSalvo && dados != null && dados.passouCheckpoint && dados.posicaoCheckpoint != Vector3.zero)
        {
            posicaoDestino = dados.posicaoCheckpoint;
        }

        jogador.transform.position = posicaoDestino;

        // Zera o movimento da física para não carregar inércia da cena anterior
        Rigidbody2D rb = jogador.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    // Método auxiliar caso queira resetar o checkpoint no SaveManager ao passar de fase
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