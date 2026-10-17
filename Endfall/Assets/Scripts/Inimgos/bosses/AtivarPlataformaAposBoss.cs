using UnityEngine;

public class AtivarPlataformaAposBoss : MonoBehaviour
{
    [Header("OBJETOS DA ARENA")]
    [Tooltip("Arraste o objeto do Boss da fase aqui")]
    public GameObject bossAlvo;

    [Tooltip("Arraste o objeto da sua Plataforma Móvel aqui")]
    public GameObject plataformaMovel;

    private void Start()
    {
        // Desativa a plataforma móvel e paralisa tudo nela no início da fase
        if (plataformaMovel != null)
        {
            plataformaMovel.SetActive(false);
        }
    }

    private void Update()
    {
        // Quando o Boss morre e é destruído pela Unity, a variável 'bossAlvo' fica nula
        if (bossAlvo == null && plataformaMovel != null)
        {
            // Liga a plataforma móvel e faz ela começar a andar/funcionar
            plataformaMovel.SetActive(true);

            Debug.Log("<color=green>[ARENA] Boss derrotado! Plataforma móvel ativada!</color>");

            // Destrói apenas este script gerenciador para poupar memória do jogo
            Destroy(this);
        }
    }
}