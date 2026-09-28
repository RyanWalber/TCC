using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private string idUnico;

    public string IdUnico => idUnico;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(idUnico))
        {
            idUnico = System.Guid.NewGuid().ToString();
        }
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(idUnico))
        {
            idUnico = System.Guid.NewGuid().ToString();
        }

        if (SaveManager.Instance != null && SaveManager.Instance.MoedaJaFoiColetada(idUnico))
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.ColetarMoeda(idUnico);
            }

            gameObject.SetActive(false);
        }
    }

    [ContextMenu("Gerar Novo ID")]
    private void GerarID()
    {
        idUnico = System.Guid.NewGuid().ToString();
    }
}