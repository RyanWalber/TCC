using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private string idUnico = System.Guid.NewGuid().ToString();

    void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.MoedaJaFoiColetada(idUnico))
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            SaveManager.Instance.ColetarMoeda(idUnico);
            gameObject.SetActive(false);
        }
    }

    [ContextMenu("Gerar Novo ID")]
    private void GerarID()
    {
        idUnico = System.Guid.NewGuid().ToString();
    }
}