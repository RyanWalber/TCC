using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VictoryZone : MonoBehaviour
{
    [SerializeField] private GameObject painelVitoria;
    [SerializeField] private TextMeshProUGUI textoMoedas;
    private bool aguardandoEntrada = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Time.timeScale = 0f;
            painelVitoria.SetActive(true);

            int coletadas = SaveManager.Instance.GetMoedasAtuais();
            int total = SaveManager.Instance.GetTotalMoedasFase();

            if (textoMoedas != null)
                textoMoedas.text = $"Moedas: {coletadas} / {total}";

            aguardandoEntrada = true;
        }
    }

    void Update()
    {
        if (aguardandoEntrada && Input.anyKeyDown)
        {
            Time.timeScale = 1f;
            SaveManager.Instance.AvancarFase();
        }
    }
}