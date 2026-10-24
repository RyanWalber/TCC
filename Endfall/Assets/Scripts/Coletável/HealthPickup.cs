using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

#if UNITY_EDITOR
        // No Editor, detecta IDs repetidos entre objetos da cena sem usar
        // GetInstanceID(), que está obsoleto nas versões recentes do Unity.
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
            return;

        HealthPickup[] elixires = FindObjectsOfType<HealthPickup>(true);
        foreach (HealthPickup outro in elixires)
        {
            if (outro == this || outro.idUnico != idUnico)
                continue;

            string meuIdGlobal = GlobalObjectId.GetGlobalObjectIdSlow(this).ToString();
            string outroIdGlobal = GlobalObjectId.GetGlobalObjectIdSlow(outro).ToString();

            // Mantém o ID no objeto cujo identificador global vem primeiro.
            // O outro recebe um novo ID quando o Unity validar esse objeto.
            if (string.CompareOrdinal(meuIdGlobal, outroIdGlobal) > 0)
                GerarNovoId();

            break;
        }
#endif
    }

    private void GerarNovoId()
    {
        idUnico = System.Guid.NewGuid().ToString();
    }

    private void Start()
    {
        // Não gerar um ID novo a cada inicialização: ele precisa continuar
        // igual para que o sistema de salvamento reconheça a poção coletada.
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
