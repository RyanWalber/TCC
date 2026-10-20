using UnityEngine;
using TMPro;

public class VictoryZone : MonoBehaviour
{
    [Header("UI da Vitória")]
    [SerializeField] private GameObject painelVitoria;
    [SerializeField] private TextMeshProUGUI textoMoedas;

    private bool aguardandoEntrada = false;
    private float timerSeguranca = 0f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !aguardandoEntrada)
        {
            aguardandoEntrada = true;
            Time.timeScale = 0f; 

            if (painelVitoria != null)
            {
                painelVitoria.SetActive(true);
            }

            int coletadas = LevelManager.Instance != null
                ? LevelManager.Instance.moedasFaseAtual
                : (SaveManager.Instance != null ? SaveManager.Instance.GetMoedasAtuais() : 0);
            int total = SaveManager.Instance != null ? SaveManager.Instance.GetTotalMoedasFase() : 0;

            if (textoMoedas != null)
            {
                textoMoedas.text = $"Moedas: {coletadas} / {total}";
            }

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SalvarNoSlot(0);
                Debug.Log("<color=yellow>[AUTOSAVE] Progresso salvo no Slot 0 na Zona de Vitória!</color>");
            }
        }
    }

    private void Update()
    {
        if (aguardandoEntrada)
        {
            timerSeguranca += Time.unscaledDeltaTime;

            if (timerSeguranca >= 0.4f && Input.anyKeyDown)
            {
                Time.timeScale = 1f; 
                SaveManager.Instance.AvancarFase();
            }
        }
    }
}