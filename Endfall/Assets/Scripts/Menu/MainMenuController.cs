using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject botaoContinuar;
    [SerializeField] private GameObject painelSlots;
    [SerializeField] private string nomePrimeiraFase = "FaseFome";

    private bool modoCarregar = false;

    private void Start()
    {
        if (botaoContinuar != null)
        {
            botaoContinuar.SetActive(SaveSystem.SlotExiste(0));
        }

        if (painelSlots != null)
        {
            painelSlots.SetActive(false);
        }
    }

    public void BotaoNovoJogo()
    {
        SaveManager.Instancia.CriarNovoJogo();
        SceneManager.LoadScene(nomePrimeiraFase);
    }

    public void BotaoContinuar()
    {
        SaveData dados = SaveSystem.Carregar(0);
        if (dados != null)
        {
            SaveManager.Instancia.CarregarDados(dados);
            SceneManager.LoadScene(dados.nomeCena);
        }
    }

    public void BotaoAbrirCarregar()
    {
        modoCarregar = true;
        painelSlots.SetActive(true);
    }

    public void BotaoSelecionarSlot(int slot)
    {
        if (modoCarregar)
        {
            SaveData dados = SaveSystem.Carregar(slot);
            if (dados != null)
            {
                SaveManager.Instancia.CarregarDados(dados);
                SceneManager.LoadScene(dados.nomeCena);
            }
        }
    }

    public void BotaoFecharSlots()
    {
        painelSlots.SetActive(false);
    }

    public void BotaoSair()
    {
        Application.Quit();
    }
}