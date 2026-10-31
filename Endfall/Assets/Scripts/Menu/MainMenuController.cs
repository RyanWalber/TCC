using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Painéis de UI")]
    [SerializeField] private GameObject painelPrincipal;
    [SerializeField] private GameObject painelSlots;
    [Tooltip("Seu painel de configuracoes criado na Unity.")]
    [SerializeField] private GameObject painelConfiguracoes;

    [Header("Botões e Configurações")]
    [SerializeField] private GameObject botaoContinuar;
    [SerializeField] private string nomePrimeiraFase = "FaseFome";

    [Header("Audio opcional ao testar Menu diretamente (sem Splash)")]
    [SerializeField] private AudioClip musicaMenuTeste;
    [SerializeField] private AudioClip somCliqueTeste;

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

        // A faixa ja esta tocando se viemos da Splash. Nunca reiniciar o loop.
        bool audioJaExistia = AudioManagerGlobal.Instance != null;
        AudioManagerGlobal audioGlobal = AudioManagerGlobal.Garantir();
        audioGlobal.PreencherClipsAusentes(musicaMenuTeste, somCliqueTeste);
        if (!audioJaExistia) audioGlobal.IniciarNoMenu();

        if (painelConfiguracoes != null) painelConfiguracoes.SetActive(false);
        audioGlobal.RegistrarBotoes();

        if (botaoContinuar != null)
        {
            bool temAutosave = SaveManager.Instancia != null && SaveManager.Instancia.TemAutosave();
            botaoContinuar.SetActive(temAutosave);
        }
    }

    public void BotaoAbrirConfiguracoes()
    {
        if (painelConfiguracoes == null)
        {
            Debug.LogWarning("[Menu] Arraste seu painel de configuracoes para o MainMenuController.");
            return;
        }
        if (painelPrincipal != null) painelPrincipal.SetActive(false);
        if (painelSlots != null) painelSlots.SetActive(false);
        painelConfiguracoes.SetActive(true);
        PainelConfiguracoes controle = painelConfiguracoes.GetComponent<PainelConfiguracoes>();
        if (controle != null) controle.SincronizarInterface();
        AudioManagerGlobal.Garantir().RegistrarBotoes();
    }

    public void BotaoFecharConfiguracoes()
    {
        if (painelConfiguracoes != null)
        {
            PainelConfiguracoes controle = painelConfiguracoes.GetComponent<PainelConfiguracoes>();
            if (controle != null) controle.SalvarConfiguracoes();
            painelConfiguracoes.SetActive(false);
        }
        MostrarPainelPrincipal();
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
            else
            {
                CarregarCenaComFade(nomePrimeiraFase);
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
                ExplicacaoManager.ExibirIntroducaoNovoJogo();
            }
        }
    }

    public void BotaoFecharSlots()
    {
        MostrarPainelPrincipal();
    }

    private void MostrarPainelPrincipal()
    {
        if (painelConfiguracoes != null) painelConfiguracoes.SetActive(false);
        if (painelPrincipal != null) painelPrincipal.SetActive(true);
        if (painelSlots != null) painelSlots.SetActive(false);
    }

    private void MostrarPainelSlots()
    {
        if (painelConfiguracoes != null) painelConfiguracoes.SetActive(false);
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

    public void CarregarCenaComFade(string nomeCena)
    {
        LevelLoader.CarregarComFade(nomeCena);
    }
}