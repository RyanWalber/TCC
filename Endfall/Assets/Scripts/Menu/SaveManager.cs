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

    // --- Bônus: Controle de Vida e Fúria ---
    private float vidaKayaAtual = 100f; // Ajuste o valor inicial/máximo padrão
    private float furiaKayaAtual = 0f;

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

    // Lê os valores reais dos componentes antes de um salvamento importante.
    private void SincronizarStatusKayaDoJogador()
    {
        PlayerHealth saude = FindFirstObjectByType<PlayerHealth>();
        SistemaFuria furia = FindFirstObjectByType<SistemaFuria>();

        if (saude != null)
            vidaKayaAtual = saude.VidaAtual;

        if (furia != null)
            furiaKayaAtual = furia.FuriaAtual;

        if (dadosAtuais != null)
        {
            dadosAtuais.vidaKaya = vidaKayaAtual;
            dadosAtuais.furiaKaya = furiaKayaAtual;
        }
    }
    public bool ExisteSaveNoSlot(int slot) => fileDataHandler.SlotExists(slot);

    public void NovoJogo(int slot = 0)
    {
        dadosAtuais = new SaveData();
        dadosAtuais.nomeCena = "FaseFome";
        dadosAtuais.faseAtual = 1;

        // Reseta dados no Novo Jogo
        moedasColetadasAtual = 0;
        dadosAtuais.moedasNoCheckpoint = 0;

        // Reseta Vida e Fúria para os valores padrão no Novo Jogo
        ResetarStatusKaya();

        if (slot != 0)
        {
            fileDataHandler.Save(dadosAtuais, slot);
        }
        fileDataHandler.Save(dadosAtuais, 0);

        CarregarCenaComFade(dadosAtuais.nomeCena);
    }

    public void CriarNovoJogo() => NovoJogo(0);
    public void CriarNovoJogo(int slot) => NovoJogo(slot);

    public void CarregarDados(SaveData dados)
    {
        if (dados != null)
        {
            dadosAtuais = dados;
            fileDataHandler.Save(dadosAtuais, 0);

            // Sincroniza os atributos contidos no slot carregado.
            moedasColetadasAtual = dadosAtuais.moedasNoCheckpoint;
            vidaKayaAtual = dadosAtuais.vidaKaya;
            furiaKayaAtual = dadosAtuais.furiaKaya;

            if (!string.IsNullOrEmpty(dadosAtuais.nomeCena))
            {
                CarregarCenaComFade(dadosAtuais.nomeCena);
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
        SincronizarStatusKayaDoJogador();
        dadosAtuais.nomeCena = SceneManager.GetActiveScene().name;

        // Garante que o estado de vida, fúria e moedas fiquem salvos
        dadosAtuais.moedasNoCheckpoint = moedasColetadasAtual;
        dadosAtuais.vidaKaya = vidaKayaAtual;
        dadosAtuais.furiaKaya = furiaKayaAtual;

        fileDataHandler.Save(dadosAtuais, slot);
        fileDataHandler.Save(dadosAtuais, 0);
    }

    public void RegistrarCheckpoint(Vector3 posicao)
    {
        SincronizarStatusKayaDoJogador();

        string cenaAtual = SceneManager.GetActiveScene().name;
        dadosAtuais.passouCheckpoint = true;
        dadosAtuais.posicaoCheckpoint = posicao;
        dadosAtuais.moedasNoCheckpoint = moedasColetadasAtual;
        dadosAtuais.vidaKaya = vidaKayaAtual;
        dadosAtuais.furiaKaya = furiaKayaAtual;
        dadosAtuais.nomeCena = cenaAtual;

        // Snapshot separado: morrer retorna exatamente aos atributos deste checkpoint,
        // mesmo que um slot seja salvo depois com outros valores.
        dadosAtuais.nomeCenaCheckpoint = cenaAtual;
        dadosAtuais.vidaKayaCheckpoint = vidaKayaAtual;
        dadosAtuais.furiaKayaCheckpoint = furiaKayaAtual;
        dadosAtuais.checkpointStatusRegistrado = true;

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

    // --- MÉTODOS PARA VIDA E FÚRIA ---
    public void AtualizarStatusKaya(float vida, float furia)
    {
        vidaKayaAtual = vida;
        furiaKayaAtual = furia;
        dadosAtuais.vidaKaya = vida;
        dadosAtuais.furiaKaya = furia;
    }

    public float GetVidaKaya() => vidaKayaAtual;
    public float GetFuriaKaya() => furiaKayaAtual;

    // Usado quando a Kaya morre: restaura o snapshot do checkpoint e recarrega
    // a cena correspondente, sem abrir o painel de Game Over.
    public void RenascerNoCheckpoint()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (dadosAtuais == null)
            dadosAtuais = new SaveData();

        string cenaDestino = SceneManager.GetActiveScene().name;

        if (dadosAtuais.passouCheckpoint)
        {
            if (!string.IsNullOrEmpty(dadosAtuais.nomeCenaCheckpoint))
                cenaDestino = dadosAtuais.nomeCenaCheckpoint;
            else if (!string.IsNullOrEmpty(dadosAtuais.nomeCena))
                cenaDestino = dadosAtuais.nomeCena;

            if (dadosAtuais.checkpointStatusRegistrado)
            {
                vidaKayaAtual = dadosAtuais.vidaKayaCheckpoint;
                furiaKayaAtual = dadosAtuais.furiaKayaCheckpoint;
            }
            else
            {
                // Compatibilidade com saves antigos que não têm snapshot separado.
                vidaKayaAtual = dadosAtuais.vidaKaya;
                furiaKayaAtual = dadosAtuais.furiaKaya;
            }
        }
        else
        {
            // Sem checkpoint ativado, volta ao início da cena com os valores padrão.
            vidaKayaAtual = 100f;
            furiaKayaAtual = 0f;
        }

        dadosAtuais.vidaKaya = vidaKayaAtual;
        dadosAtuais.furiaKaya = furiaKayaAtual;
        dadosAtuais.nomeCena = cenaDestino;

        fileDataHandler.Save(dadosAtuais, 0);
        CarregarCenaComFade(cenaDestino);
    }

    private void ResetarStatusKaya()
    {
        vidaKayaAtual = 100f; // Altere se o HP máximo for diferente
        furiaKayaAtual = 0f;
        dadosAtuais.vidaKaya = vidaKayaAtual;
        dadosAtuais.furiaKaya = furiaKayaAtual;
    }

    public void AvancarFase()
    {
        dadosAtuais.faseAtual++;
        dadosAtuais.passouCheckpoint = false;
        dadosAtuais.posicaoCheckpoint = Vector3.zero;
        dadosAtuais.nomeCenaCheckpoint = "";
        dadosAtuais.checkpointStatusRegistrado = false;

        // CORREÇÃO DAS MOEDAS:
        // Mantemos 'moedasColetadasAtual' e registramos o valor acumulado no checkpoint
        dadosAtuais.moedasNoCheckpoint = moedasColetadasAtual;

        // Limpamos apenas a lista de IDs coletados da fase passada, para não dar conflito com IDs da Fase 2
        dadosAtuais.moedasColetadasIDs.Clear();

        // Salva o status de vida e fúria para a próxima fase
        dadosAtuais.vidaKaya = vidaKayaAtual;
        dadosAtuais.furiaKaya = furiaKayaAtual;

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
            CarregarCenaComFade(dadosAtuais.nomeCena);
        }
        else
        {
            CarregarCenaComFade("Menu");
        }
    }

    public void SairDoJogo()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void CarregarCenaComFade(string nomeCena)
    {
        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.CarregarCena(nomeCena);
        }
        else
        {
            SceneManager.LoadScene(nomeCena);
        }
    }
}