using UnityEngine;

/// <summary>
/// Adicione a uma area com Collider2D marcado como Is Trigger.
/// Quando a Kaya entrar, abre o PainelDialogo escolhido.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GatilhoDialogo : MonoBehaviour
{
    [Header("Painel que sera aberto")]
    [SerializeField] private PainelDialogo painelDialogo;

    [Header("Falas deste local (vazio = usa as falas do painel)")]
    [SerializeField] private FalaDialogo[] falas;

    [Header("Ativacao")]
    [SerializeField] private bool apenasUmaVez = true;

    private bool jaAtivado;

    private void Reset()
    {
        Collider2D colisor = GetComponent<Collider2D>();
        if (colisor != null)
            colisor.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (jaAtivado && apenasUmaVez)
            return;

        // A Tag Player pode estar num filho; o PlayerHealth fica na Kaya.
        bool ehKaya = outro.CompareTag("Player") ||
                      outro.GetComponentInParent<PlayerHealth>() != null;
        if (!ehKaya)
            return;

        if (painelDialogo == null)
        {
            Debug.LogWarning("[GatilhoDialogo] Arraste o PainelDialogo no Inspector.", this);
            return;
        }

        if (painelDialogo.EstaAberto)
            return;

        painelDialogo.IniciarDialogo(falas);
        if (painelDialogo.EstaAberto)
            jaAtivado = true;
    }

    // Opcional: pode ser chamado por outro evento/script.
    public void ReativarGatilho()
    {
        jaAtivado = false;
    }
}
