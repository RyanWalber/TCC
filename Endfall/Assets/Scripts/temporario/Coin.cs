using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Identificador Único")]
    [Tooltip("Dê um nome único para cada moeda na cena, ex: moeda_fase1_01")]
    public string coinID;

    private void Start()
    {
        if (string.IsNullOrEmpty(coinID))
        {
            coinID = gameObject.name + "_" + transform.position.ToString();
        }

        if (SaveManager.Instance != null && SaveManager.Instance.dadosAtuais.moedasColetadasIDs.Contains(coinID))
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
                LevelManager.Instance.ColetarMoeda(coinID);
            }
            gameObject.SetActive(false);
        }
    }
}