using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Painéis de UI")]
    [SerializeField] private GameObject painelPrincipal;
    [SerializeField] private GameObject painelSlots;

    [Header("Botões")]
    [SerializeField] private Button botaoContinuar;

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        if (painelPrincipal != null) painelPrincipal.SetActive(true);
        if (painelSlots != null) painelSlots.SetActive(false);

        AtualizarBotaoContinuar();
    }

    public void AtualizarBotaoContinuar()
    {
        if (botaoContinuar != null)
        {
            bool temAutosave = SaveManager.Instance != null && SaveManager.Instance.TemAutosave();
            botaoContinuar.interactable = temAutosave;
        }
    }

    public void OnClickNovoJogo()
    {
        Time.timeScale = 1f;
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.NovoJogo();
        }
    }

    public void OnClickContinuar()
    {
        Time.timeScale = 1f;
        if (SaveManager.Instance != null && SaveManager.Instance.TemAutosave())
        {
            SaveManager.Instance.CarregarDados();
        }
    }

    public void OnClickAbrirSlots()
    {
        if (painelPrincipal != null) painelPrincipal.SetActive(false);
        if (painelSlots != null) painelSlots.SetActive(true);
    }

    public void OnClickVoltarPrincipal()
    {
        if (painelSlots != null) painelSlots.SetActive(false);
        if (painelPrincipal != null) painelPrincipal.SetActive(true);
    }

    public void OnClickCarregarSlot(int slot)
    {
        Time.timeScale = 1f;
        if (SaveManager.Instance != null && SaveManager.Instance.ExisteSaveNoSlot(slot))
        {
            SaveManager.Instance.CarregarSlot(slot);
        }
    }

    public void OnClickSair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}