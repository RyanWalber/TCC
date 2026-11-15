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
        if (plataformaMovel != null)
        {
            plataformaMovel.SetActive(false);
        }
    }

    private void Update()
    {
        if (bossAlvo == null && plataformaMovel != null)
        {
            plataformaMovel.SetActive(true);

            Debug.Log("<color=green>[ARENA] Boss derrotado! Plataforma móvel ativada!</color>");

            Destroy(this);
        }
    }
}