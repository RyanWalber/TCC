using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Efeitos Visuais")]
    [SerializeField] private ParticleSystem fogoParticulas; // Arraste o 'Efeitofogo' aqui

    [Header("Audio deste checkpoint")]
    [SerializeField] private AudioClip somAtivarCheckpoint;
    [SerializeField, Range(0f, 1f)] private float volumeAtivarCheckpoint = 0.8f;

    [Header("Audio das chamas (objeto das particulas)")]
    [SerializeField] private AudioChamas audioChamas;
    [Tooltip("Opcional: usado somente se o AudioChamas nao possuir um clipe.")]
    [SerializeField] private AudioClip somChamasPadrao;

    [Header("Configurações do Checkpoint")]
    [Tooltip("Transform de onde o jogador vai renascer (se deixares vazio, usa a posição do Player)")]
    [SerializeField] private Transform pontoDeRenascer;

    private bool jaAtivado = false;

    private void Awake()
    {
        if (fogoParticulas == null) return;
        if (audioChamas == null) audioChamas = fogoParticulas.GetComponent<AudioChamas>();
        if (audioChamas == null) audioChamas = fogoParticulas.gameObject.AddComponent<AudioChamas>();
        audioChamas.Configurar(fogoParticulas, somChamasPadrao);
    }

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
            AudioObjeto2D.Tocar(somAtivarCheckpoint, volumeAtivarCheckpoint,
                transform.position, "Checkpoint");

            // 1. Acende o fogo da pira
            if (fogoParticulas != null)
            {
                fogoParticulas.Play();
                if (audioChamas != null) audioChamas.Acender();
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