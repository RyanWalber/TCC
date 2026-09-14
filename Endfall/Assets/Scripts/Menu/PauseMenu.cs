using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject painelPause;
    [SerializeField] private GameObject painelSlots;

    private bool estaPausado = false;
    private bool modoSalvar = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            AlternarPause(!estaPausado);
        }
    }

    public void AlternarPause(bool pausar)
    {
        estaPausado = pausar;
        Time.timeScale = pausar ? 0f : 1f;
        painelPause.SetActive(pausar);

        if (!pausar && painelSlots != null)
        {
            painelSlots.SetActive(false);
        }
    }

    public void BotaoAbrirSalvar()
    {
        modoSalvar = true;
        painelSlots.SetActive(true);
    }

    public void BotaoAbrirCarregar()
    {
        modoSalvar = false;
        painelSlots.SetActive(true);
    }

    public void BotaoSelecionarSlot(int slot)
    {
        if (modoSalvar)
        {
            SaveManager.Instancia.SalvarNoSlot(slot);
            painelSlots.SetActive(false);
        }
        else
        {
            SaveData dados = SaveSystem.Carregar(slot);
            if (dados != null)
            {
                Time.timeScale = 1f;
                SaveManager.Instancia.CarregarDados(dados);
                SceneManager.LoadScene(dados.nomeCena);
            }
        }
    }

    public void BotaoVoltarAoMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }
}