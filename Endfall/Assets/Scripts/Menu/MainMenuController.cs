using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI do Menu")]
    public GameObject botaoContinuar;
    public GameObject painelSlots;

    [Header("Configurações da Cena Inicial")]
    public string nomePrimeiraFase = "Tutorial";

    private void Start()
    {
        VerificarAutosave();

        if (painelSlots != null)
            painelSlots.SetActive(false);
    }

    private void VerificarAutosave()
    {
        if (SaveManager.Instance != null && botaoContinuar != null)
        {
            string caminhoAutosave = SaveManager.Instance.ObterCaminho(0);
            botaoContinuar.SetActive(File.Exists(caminhoAutosave));
        }
    }

    public void BotaoContinuar()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CarregarEIniciarJogo(0);
        }
    }

    public void BotaoNovoJogo()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.NovoJogo();
            SaveManager.Instance.dadosAtuais.nomeCena = nomePrimeiraFase;
        }
        SceneManager.LoadScene(nomePrimeiraFase);
    }

    public void BotaoAbrirCarregarSlots()
    {
        if (painelSlots != null)
            painelSlots.SetActive(true);
    }

    public void BotaoFecharSlots()
    {
        if (painelSlots != null)
            painelSlots.SetActive(false);
    }

    public void BotaoSelecionarSlot(int numeroSlot)
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CarregarEIniciarJogo(numeroSlot);
        }
    }

    public void BotaoSair()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}