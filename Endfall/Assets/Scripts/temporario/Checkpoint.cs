using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Efeitos Visuais")]
    [SerializeField] private ParticleSystem fogoParticulas; // Arraste o 'Efeitofogo' aqui

    [Header("Configurações do Checkpoint")]
    [Tooltip("Transform de onde o jogador vai renascer (se deixares vazio, usa a posição do Player)")]
    [SerializeField] private Transform pontoDeRenascer;

    private bool jaAtivado = false;

    private void Start()
    {
        // Garante que o fogo começa apagado ao iniciar a cena
        if (fogoParticulas != null)
        {
            fogoParticulas.Stop();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!jaAtivado && other.CompareTag("Player"))
        {
            jaAtivado = true;

            // 1. Acende o fogo da pira
            if (fogoParticulas != null)
            {
                fogoParticulas.Play();
            }

            // 2. Regista a posição no SaveManager
            Vector3 posicaoSalvar = pontoDeRenascer != null ? pontoDeRenascer.position : other.transform.position;

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.RegistrarCheckpoint(posicaoSalvar);
                Debug.Log(">>> Checkpoint ativado e salvo em: " + posicaoSalvar);
            }
            else
            {
                Debug.LogWarning("SaveManager.Instance não foi encontrado na cena!");
            }
        }
    }
}