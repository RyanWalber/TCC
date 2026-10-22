using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("Configuração do elixir")]
    [Min(1f)] [SerializeField] private float quantidadeVida = 25f;
    [Tooltip("ID único para este elixir. Não reutilize o mesmo ID em outros elixires.")]
    [SerializeField] private string idUnico;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(idUnico))
            idUnico = System.Guid.NewGuid().ToString();
    }

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(idUnico))
            idUnico = System.Guid.NewGuid().ToString();

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
