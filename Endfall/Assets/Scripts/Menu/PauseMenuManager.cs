using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Painéis de UI")]
    [SerializeField] private GameObject painelPause;
    [SerializeField] private GameObject painelSlots;

    private bool estaPausado = false;
    private bool modoSalvar = false; // true = Salvando | false = Carregando

    void Update()
    {
        // Pressionar P ou ESC alterna o Pause
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (estaPausado)
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
        Time.timeScale = 0f; // Congela o jogo (física e animações)
        if (painelPause != null) painelPause.SetActive(true);
        if (painelSlots != null) painelSlots.SetActive(false);
    }

    public void ContinuarJogo()
    {
        estaPausado = false;
        Time.timeScale = 1f; // Volta o jogo ao normal
        if (painelPause != null) painelPause.SetActive(false);
        if (painelSlots != null) painelSlots.SetActive(false);
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
            ContinuarJogo(); // Fecha o pause após salvar
        }
        else
        {
            Time.timeScale = 1f; // Restaura o tempo antes de carregar a cena
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
        Time.timeScale = 1f; // Garante que o tempo descongela ao voltar ao menu
        SceneManager.LoadScene("Menu");
    }
}