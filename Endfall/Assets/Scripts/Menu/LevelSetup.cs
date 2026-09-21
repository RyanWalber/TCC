using UnityEngine;

public class LevelSetup : MonoBehaviour
{
    [SerializeField] private Transform pontoInicial;
    [SerializeField] private GameObject jogador;

    void Start()
    {
        if (jogador == null)
        {
            Debug.LogError("");
            return;
        }

        SaveData dados = SaveManager.Instance != null ? SaveManager.Instance.GetDadosAtuais() : null;

        Vector3 posicaoDestino = pontoInicial != null ? pontoInicial.position : jogador.transform.position;

        if (dados != null && dados.passouCheckpoint && dados.posicaoCheckpoint != Vector3.zero)
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
}