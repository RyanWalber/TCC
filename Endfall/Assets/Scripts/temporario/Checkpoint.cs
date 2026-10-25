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
            KayaAudioManager audioKaya = other.GetComponentInParent<KayaAudioManager>();
            if (audioKaya != null) audioKaya.TocarCheckpoint();

            // 1. Acende o fogo da pira
            if (fogoParticulas != null)
            {
                fogoParticulas.Play();
            }

            // 2. Regista a posição no SaveManager
            Vector3 posicaoSalvar = pontoDeRenascer != null ? pontoDeRenascer.position : other.transform.position;

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.ProcessarCheckpoint(posicaoSalvar);
                Debug.Log(">>> Checkpoint ativado e salvo em: " + posicaoSalvar);
            }
            else if (SaveManager.Instance != null)
            {
                // Fallback para cenas que ainda não têm LevelManager.
                SaveManager.Instance.RegistrarCheckpoint(posicaoSalvar);
                Debug.LogWarning("Checkpoint salvo sem sincronizar moedas: LevelManager não foi encontrado.");
            }
            else
            {
                Debug.LogWarning("SaveManager.Instance não foi encontrado na cena!");
            }
        }
    }
}