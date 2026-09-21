using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public static SaveManager Instancia => Instance;

    [SerializeField] private bool usarCriptografia = true;

    private SaveData dadosAtuais;
    private FileDataHandler fileDataHandler;
    private int moedasColetadasAtual = 0;
    private int totalMoedasNaFase = 0;

    public SaveData DadosAtuais => dadosAtuais;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        fileDataHandler = new FileDataHandler(Application.persistentDataPath, usarCriptografia);
        dadosAtuais = new SaveData();
    }

    public SaveData GetDadosAtuais() => dadosAtuais;
    public bool TemAutosave() => fileDataHandler.SlotExists(0);
    public bool ExisteSaveNoSlot(int slot) => fileDataHandler.SlotExists(slot);

    public void NovoJogo()
    {
        dadosAtuais = new SaveData();
        dadosAtuais.nomeCena = "FaseFome";
        dadosAtuais.faseAtual = 1;
        moedasColetadasAtual = 0;
        fileDataHandler.Save(dadosAtuais, 0);
        SceneManager.LoadScene(dadosAtuais.nomeCena);
    }

    public void CriarNovoJogo() => NovoJogo();

    public void CarregarDados(SaveData dados)
    {
        if (dados != null)
        {
            dadosAtuais = dados;
            fileDataHandler.Save(dadosAtuais, 0);
            moedasColetadasAtual = dadosAtuais.moedasNoCheckpoint;

            if (!string.IsNullOrEmpty(dadosAtuais.nomeCena))
            {
                SceneManager.LoadScene(dadosAtuais.nomeCena);
            }
        }
    }

    public void CarregarSlot(int slot)
    {
        SaveData dadosCarregados = fileDataHandler.Load(slot);
        if (dadosCarregados != null)
        {
            CarregarDados(dadosCarregados);
        }
    }

    public void CarregarSlot(SaveData dados) => CarregarDados(dados);
    public void CarregarDados(int slot) => CarregarSlot(slot);
    public void CarregarDados() => CarregarSlot(0);

    public void SalvarNoSlot(int slot)
    {
        dadosAtuais.nomeCena = SceneManager.GetActiveScene().name;
        fileDataHandler.Save(dadosAtuais, slot);
        fileDataHandler.Save(dadosAtuais, 0);
    }

    public void RegistrarCheckpoint(Vector3 posicao)
    {
        dadosAtuais.passouCheckpoint = true;
        dadosAtuais.posicaoCheckpoint = posicao;
        dadosAtuais.moedasNoCheckpoint = moedasColetadasAtual;
        dadosAtuais.nomeCena = SceneManager.GetActiveScene().name;
        fileDataHandler.Save(dadosAtuais, 0);
    }

    public void AtivarCheckpoint(Vector3 posicao) => RegistrarCheckpoint(posicao);

    public void ColetarMoeda(string idMoeda)
    {
        if (!dadosAtuais.moedasColetadasIDs.Contains(idMoeda))
        {
            dadosAtuais.moedasColetadasIDs.Add(idMoeda);
            moedasColetadasAtual++;
        }
    }

    public bool MoedaJaFoiColetada(string idMoeda) => dadosAtuais.moedasColetadasIDs.Contains(idMoeda);
    public int GetMoedasAtuais() => moedasColetadasAtual;

    public void RegistrarTotalMoedasFase(int quantidade)
    {
        totalMoedasNaFase = quantidade;
    }

    public int GetTotalMoedasFase() => totalMoedasNaFase;

    public void AvancarFase()
    {
        dadosAtuais.faseAtual++;
        dadosAtuais.passouCheckpoint = false;
        dadosAtuais.moedasNoCheckpoint = 0;
        dadosAtuais.moedasColetadasIDs.Clear();
        moedasColetadasAtual = 0;

        if (dadosAtuais.faseAtual == 2)
        {
            dadosAtuais.nomeCena = "FasePeste";
        }
        else
        {
            dadosAtuais.nomeCena = "Menu";
        }

        fileDataHandler.Save(dadosAtuais, 0);

        if (Application.CanStreamedLevelBeLoaded(dadosAtuais.nomeCena))
        {
            SceneManager.LoadScene(dadosAtuais.nomeCena);
        }
        else
        {
            SceneManager.LoadScene("Menu");
        }
    }
}