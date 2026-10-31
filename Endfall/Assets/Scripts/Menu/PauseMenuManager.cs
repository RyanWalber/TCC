using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Painéis de UI")]
    [SerializeField] private GameObject painelPause;
    [SerializeField] private GameObject painelSlots;

    private bool estaPausado = false;
    private bool modoSalvar = false;
    private PainelConfiguracoes painelConfiguracoes;

    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        estaPausado = false;
    }

    private void Start()
    {
        if (painelPause == null) return;
        Canvas canvas = painelPause.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        painelConfiguracoes = PainelConfiguracoes.Criar(canvas.transform, () =>
        {
            if (painelPause != null) painelPause.SetActive(true);
        });
        PainelConfiguracoes.CriarBotaoAcesso(painelPause.transform,
            AbrirConfiguracoesNaPausa);

        AudioManagerGlobal.Garantir().RegistrarBotoes();
    }

    private void AbrirConfiguracoesNaPausa()
    {
        if (painelConfiguracoes == null) return;
        if (painelPause != null) painelPause.SetActive(false);
        if (painelSlots != null) painelSlots.SetActive(false);
        painelConfiguracoes.Abrir();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (painelConfiguracoes != null && painelConfiguracoes.gameObject.activeSelf)
            {
                painelConfiguracoes.Fechar();
            }
            else if (painelSlots != null && painelSlots.activeSelf)
            {
                OnClickVoltarAoPause();
            }
            else if (estaPausado)
            {
                ContinuarJogo();
            }
            else
            {
                PausarJogo();
            }
        }
    }

    public void PausarJogo()
    {
        estaPausado = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        if (painelPause != null) painelPause.SetActive(true);
        if (painelSlots != null) painelSlots.SetActive(false);
    }

    public void ContinuarJogo()
    {
        estaPausado = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (painelPause != null) painelPause.SetActive(false);
        if (painelSlots != null) painelSlots.SetActive(false);
        if (painelConfiguracoes != null) painelConfiguracoes.Esconder();
    }

    public void OnClickSalvarJogo()
    {
        modoSalvar = true;
        if (painelPause != null) painelPause.SetActive(false);
        if (painelSlots != null) painelSlots.SetActive(true);
    }

    public void OnClickCarregarJogo()
    {
        modoSalvar = false;
        if (painelPause != null) painelPause.SetActive(false);
        if (painelSlots != null) painelSlots.SetActive(true);
    }

    public void OnClickSlot(int slot)
    {
        if (SaveManager.Instance == null) return;

        if (modoSalvar)
        {
            SaveManager.Instance.SalvarNoSlot(slot);
            ContinuarJogo();
        }
        else
        {
            estaPausado = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SaveManager.Instance.CarregarSlot(slot);
        }
    }

    public void OnClickVoltarAoPause()
    {
        if (painelSlots != null) painelSlots.SetActive(false);
        if (painelPause != null) painelPause.SetActive(true);
    }

    public void OnClickVoltarAoMenu()
    {
        estaPausado = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (painelConfiguracoes != null) painelConfiguracoes.Esconder();
        LevelLoader.CarregarComFade("Menu");
    }
}