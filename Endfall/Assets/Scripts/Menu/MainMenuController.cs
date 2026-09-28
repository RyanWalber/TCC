using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Painéis de UI")]
    [SerializeField] private GameObject painelPrincipal;
    [SerializeField] private GameObject painelSlots;

    [Header("Botões e Configurações")]
    [SerializeField] private GameObject botaoContinuar;
    [SerializeField] private string nomePrimeiraFase = "FaseFome";

    private bool modoCarregar = false;

    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        MostrarPainelPrincipal();

        if (botaoContinuar != null)
        {
            bool temAutosave = SaveManager.Instancia != null && SaveManager.Instancia.TemAutosave();
            botaoContinuar.SetActive(temAutosave);
        }
    }

    public void BotaoNovoJogo()
    {
        modoCarregar = false;
        MostrarPainelSlots();
    }

    public void BotaoContinuar()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (SaveManager.Instancia != null)
        {
            SaveManager.Instancia.CarregarDados();
        }
    }

    public void BotaoAbrirCarregar()
    {
        modoCarregar = true;
        MostrarPainelSlots();
    }

    public void BotaoSelecionarSlot(int slot)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (modoCarregar)
        {
            if (SaveManager.Instancia != null)
            {
                SaveManager.Instancia.CarregarSlot(slot);
            }
        }
        else
        {
            if (SaveManager.Instancia != null)
            {
                SaveManager.Instancia.NovoJogo(slot);
            }
            else
            {
                SceneManager.LoadScene(nomePrimeiraFase);
            }
        }
    }

    public void BotaoFecharSlots()
    {
        MostrarPainelPrincipal();
    }

    private void MostrarPainelPrincipal()
    {
        if (painelPrincipal != null) painelPrincipal.SetActive(true);
        if (painelSlots != null) painelSlots.SetActive(false);
    }

    private void MostrarPainelSlots()
    {
        if (painelPrincipal != null) painelPrincipal.SetActive(false);
        if (painelSlots != null) painelSlots.SetActive(true);
    }

    public void BotaoSair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}