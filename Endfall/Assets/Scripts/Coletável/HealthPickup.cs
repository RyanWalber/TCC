using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("Configuração do elixir")]
    [Min(1f)] [SerializeField] private float quantidadeVida = 25f;

    [Tooltip("ID único desta instância do elixir. Cada poção da fase deve ter um ID diferente.")]
    [SerializeField] private string idUnico;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(idUnico))
            GerarNovoId();

        // Corrige IDs repetidos entre instâncias colocadas na cena.
        // A instância com o ID de instância do Unity maior recebe um novo ID,
        // evitando que todas as poções sejam registradas como uma só.
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
            return;

        HealthPickup[] elixires = FindObjectsOfType<HealthPickup>(true);
        foreach (HealthPickup outro in elixires)
        {
            if (outro == this || outro.idUnico != idUnico)
                continue;

            if (GetInstanceID() > outro.GetInstanceID())
            {
                GerarNovoId();
            }

            break;
        }
    }

    private void GerarNovoId()
    {
        idUnico = System.Guid.NewGuid().ToString();
    }

    private void Start()
    {
        // Mantém um ID estável durante as execuções para o salvamento funcionar.
        if (string.IsNullOrWhiteSpace(idUnico))
            GerarNovoId();

        if (SaveManager.Instance != null && SaveManager.Instance.ElixirVidaJaFoiColetado(idUnico))
            gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth vida = other.GetComponent<PlayerHealth>();
        if (vida == null) vida = other.GetComponentInParent<PlayerHealth>();
        if (vida == null) return;

        if (vida.VidaAtual >= vida.VidaMaxima) return;

        vida.RecuperarVida(quantidadeVida);

        if (SaveManager.Instance != null)
            SaveManager.Instance.RegistrarElixirVidaColetado(idUnico);

        gameObject.SetActive(false);
    }
}
