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
    private bool aplicarPosicaoSalvaAoCarregar = false;
    private bool renascimentoPendente = false;

    private float vidaKayaAtual = 100f;
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

        moedasColetadasAtual = 0;
        dadosAtuais.moedasNoCheckpoint = 0;
        dadosAtuais.moedasAtuais = 0;
        dadosAtuais.temPosicaoSalvamento = false;
        aplicarPosicaoSalvaAoCarregar = false;

        ResetarStatusKaya();

        if (slot != 0)
        {
            fileDataHandler.Save(dadosAtuais, slot);
        }
        fileDataHandler.Save(dadosAtuais, 0);

        renascimentoPendente = true;
        ExplicacaoManager.ExibirIntroducaoNovoJogo();
    }

    public void CriarNovoJogo() => NovoJogo(0);
    public void CriarNovoJogo(int slot) => NovoJogo(slot);

    public void CarregarDados(SaveData dados)
    {
        if (dados == null) return;
        ExplicacaoManager.CancelarFragmentoPendente();
        dadosAtuais = dados;

        if (dadosAtuais.versaoDados < 2)
        {
            dadosAtuais.moedasAtuais = dadosAtuais.moedasNoCheckpoint;
            dadosAtuais.moedasColetadasIDsCheckpoint = dadosAtuais.moedasColetadasIDs != null
                ? new List<string>(dadosAtuais.moedasColetadasIDs) : new List<string>();
            dadosAtuais.elixiresVidaColetadosIDsCheckpoint = dadosAtuais.elixiresVidaColetadosIDs != null
                ? new List<string>(dadosAtuais.elixiresVidaColetadosIDs) : new List<string>();
            dadosAtuais.versaoDados = 2;
        }

        if (dadosAtuais.moedasColetadasIDs == null) dadosAtuais.moedasColetadasIDs = new List<string>();
        if (dadosAtuais.elixiresVidaColetadosIDs == null) dadosAtuais.elixiresVidaColetadosIDs = new List<string>();
        if (dadosAtuais.moedasColetadasIDsCheckpoint == null) dadosAtuais.moedasColetadasIDsCheckpoint = new List<string>();
        if (dadosAtuais.elixiresVidaColetadosIDsCheckpoint == null) dadosAtuais.elixiresVidaColetadosIDsCheckpoint = new List<string>();
        if (dadosAtuais.inimigosEliminadosIDs == null) dadosAtuais.inimigosEliminadosIDs = new List<string>();
        if (dadosAtuais.inimigosEliminadosIDsCheckpoint == null) dadosAtuais.inimigosEliminadosIDsCheckpoint = new List<string>();

        dadosAtuais = CriarSnapshotDoUltimoCheckpoint(dadosAtuais);
        renascimentoPendente = true;

        moedasColetadasAtual = dadosAtuais.moedasAtuais;
        vidaKayaAtual = dadosAtuais.vidaKaya;
        furiaKayaAtual = dadosAtuais.furiaKaya;
        aplicarPosicaoSalvaAoCarregar = dadosAtuais.temPosicaoSalvamento;

        fileDataHandler.Save(dadosAtuais, 0);
        if (!string.IsNullOrEmpty(dadosAtuais.nomeCena))
            CarregarCenaComFade(dadosAtuais.nomeCena);
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
        if (dadosAtuais == null) dadosAtuais = new SaveData();

        SaveData dadosCheckpoint = CriarSnapshotDoUltimoCheckpoint(dadosAtuais);
        fileDataHandler.Save(dadosCheckpoint, slot);
        fileDataHandler.Save(dadosCheckpoint, 0);
    }

    private static SaveData CriarSnapshotDoUltimoCheckpoint(SaveData origem)
    {
        if (origem == null) return new SaveData();

        List<string> moedasConfirmadas = origem.moedasColetadasIDsCheckpoint != null
            ? new List<string>(origem.moedasColetadasIDsCheckpoint)
            : new List<string>();
        List<string> elixiresConfirmados = origem.elixiresVidaColetadosIDsCheckpoint != null
            ? new List<string>(origem.elixiresVidaColetadosIDsCheckpoint)
            : new List<string>();

        List<string> inimigosConfirmados = origem.inimigosEliminadosIDsCheckpoint != null ? new List<string>(origem.inimigosEliminadosIDsCheckpoint) : new List<string>();
        bool checkpointAtivo = origem.passouCheckpoint;
        bool temStatusCheckpoint = origem.checkpointStatusRegistrado;

        SaveData copia = new SaveData
        {
            versaoDados = 2,
            faseAtual = origem.faseAtual,
            nomeCena = checkpointAtivo && !string.IsNullOrEmpty(origem.nomeCenaCheckpoint)
                ? origem.nomeCenaCheckpoint : origem.nomeCena,
            passouCheckpoint = checkpointAtivo,
            nomeCenaCheckpoint = origem.nomeCenaCheckpoint,
            posicaoCheckpoint = origem.posicaoCheckpoint,
            checkpointStatusRegistrado = temStatusCheckpoint,
            vidaKayaCheckpoint = origem.vidaKayaCheckpoint,
            furiaKayaCheckpoint = origem.furiaKayaCheckpoint,

            temPosicaoSalvamento = false,
            posicaoSalvamento = Vector3.zero,

            moedasNoCheckpoint = Mathf.Max(0, origem.moedasNoCheckpoint),
            moedasAtuais = Mathf.Max(0, origem.moedasNoCheckpoint),
            moedasColetadasIDsCheckpoint = moedasConfirmadas,
            moedasColetadasIDs = new List<string>(moedasConfirmadas),
            elixiresVidaColetadosIDsCheckpoint = elixiresConfirmados,
            elixiresVidaColetadosIDs = new List<string>(elixiresConfirmados),
            inimigosEliminadosIDs = new List<string>(inimigosConfirmados),
            inimigosEliminadosIDsCheckpoint = inimigosConfirmados,

            vidaKaya = temStatusCheckpoint || !checkpointAtivo
                ? origem.vidaKayaCheckpoint : origem.vidaKaya,
            furiaKaya = temStatusCheckpoint || !checkpointAtivo
                ? origem.furiaKayaCheckpoint : origem.furiaKaya
        };

        return copia;
    }

    public void RegistrarCheckpoint(Vector3 posicao)
    {
        if (dadosAtuais == null) dadosAtuais = new SaveData();
        if (LevelManager.Instance != null)
            LevelManager.Instance.RegistrarColetaveisPendentesNoSave();
        EstadoMundoCheckpoint.ConsolidarCheckpoint(dadosAtuais, posicao);
        SincronizarStatusKayaDoJogador();

        string cenaAtual = SceneManager.GetActiveScene().name;
        dadosAtuais.versaoDados = 2;
        dadosAtuais.passouCheckpoint = true;
        dadosAtuais.posicaoCheckpoint = posicao;
        dadosAtuais.posicaoSalvamento = posicao;
        dadosAtuais.temPosicaoSalvamento = true;
        dadosAtuais.moedasNoCheckpoint = moedasColetadasAtual;
        dadosAtuais.moedasAtuais = moedasColetadasAtual;
        dadosAtuais.vidaKaya = vidaKayaAtual;
        dadosAtuais.furiaKaya = furiaKayaAtual;
        dadosAtuais.nomeCena = cenaAtual;
        dadosAtuais.nomeCenaCheckpoint = cenaAtual;
        dadosAtuais.vidaKayaCheckpoint = vidaKayaAtual;
        dadosAtuais.furiaKayaCheckpoint = furiaKayaAtual;
        dadosAtuais.checkpointStatusRegistrado = true;
        dadosAtuais.moedasColetadasIDsCheckpoint = new List<string>(dadosAtuais.moedasColetadasIDs);
        dadosAtuais.elixiresVidaColetadosIDsCheckpoint = new List<string>(dadosAtuais.elixiresVidaColetadosIDs);
        dadosAtuais.inimigosEliminadosIDsCheckpoint = new List<string>(dadosAtuais.inimigosEliminadosIDs);
        aplicarPosicaoSalvaAoCarregar = false;
        fileDataHandler.Save(dadosAtuais, 0);
    }

    public void AtivarCheckpoint(Vector3 posicao) => RegistrarCheckpoint(posicao);

    public void ColetarMoeda(string idMoeda)
    {
        if (string.IsNullOrEmpty(idMoeda) || dadosAtuais == null) return;
        if (dadosAtuais.moedasColetadasIDs == null) dadosAtuais.moedasColetadasIDs = new List<string>();
        if (!dadosAtuais.moedasColetadasIDs.Contains(idMoeda))
        {
            dadosAtuais.moedasColetadasIDs.Add(idMoeda);
            moedasColetadasAtual++;
            dadosAtuais.moedasAtuais = moedasColetadasAtual;
        }
    }

    public bool MoedaJaFoiColetada(string idMoeda) =>
        dadosAtuais != null && dadosAtuais.moedasColetadasIDs != null &&
        dadosAtuais.moedasColetadasIDs.Contains(idMoeda);

    public bool ElixirVidaJaFoiColetado(string id) =>
        dadosAtuais != null && dadosAtuais.elixiresVidaColetadosIDs != null && dadosAtuais.elixiresVidaColetadosIDs.Contains(id);

    public void RegistrarElixirVidaColetado(string id)
    {
        if (string.IsNullOrEmpty(id) || dadosAtuais == null) return;
        if (dadosAtuais.elixiresVidaColetadosIDs == null)
            dadosAtuais.elixiresVidaColetadosIDs = new List<string>();
        if (!dadosAtuais.elixiresVidaColetadosIDs.Contains(id))
            dadosAtuais.elixiresVidaColetadosIDs.Add(id);

    }

    public int GetMoedasAtuais() => moedasColetadasAtual;

    public bool ConsumirPosicaoSalvaAoCarregar()
    {
        bool resultado = aplicarPosicaoSalvaAoCarregar &&
                         dadosAtuais != null && dadosAtuais.temPosicaoSalvamento;
        aplicarPosicaoSalvaAoCarregar = false;
        return resultado;
    }

    public void RegistrarTotalMoedasFase(int quantidade)
    {
        totalMoedasNaFase = quantidade;
    }

    public int GetTotalMoedasFase() => totalMoedasNaFase;

    public void AtualizarStatusKaya(float vida, float furia)
    {
        if (renascimentoPendente) return;

        vidaKayaAtual = vida;
        furiaKayaAtual = furia;
        if (dadosAtuais == null) dadosAtuais = new SaveData();
        dadosAtuais.vidaKaya = vida;
        dadosAtuais.furiaKaya = furia;
    }

    public void ConfirmarRenascimentoInicializado()
    {
        renascimentoPendente = false;
    }

    public void PrepararTransicaoDeFase(string nomeProximaCena, float vidaDepoisDaRecuperacao)
    {
        if (dadosAtuais == null) dadosAtuais = new SaveData();
        if (LevelManager.Instance != null)
            LevelManager.Instance.RegistrarColetaveisPendentesNoSave();

        renascimentoPendente = true;
        vidaKayaAtual = Mathf.Clamp(vidaDepoisDaRecuperacao, 1f, 100000f);
        dadosAtuais.vidaKaya = vidaKayaAtual;
        SistemaFuria furia = FindFirstObjectByType<SistemaFuria>();
        if (furia != null)
            furiaKayaAtual = furia.FuriaAtual;
        dadosAtuais.furiaKaya = furiaKayaAtual;

        dadosAtuais.versaoDados = 2;
        dadosAtuais.nomeCena = nomeProximaCena;
        switch (nomeProximaCena)
        {
            case "FaseFome": dadosAtuais.faseAtual = 1; break;
            case "FasePeste": dadosAtuais.faseAtual = 2; break;
            case "FaseGuerra": dadosAtuais.faseAtual = 3; break;
            case "FaseMorte": dadosAtuais.faseAtual = 4; break;
        }
        dadosAtuais.passouCheckpoint = false;
        dadosAtuais.posicaoCheckpoint = Vector3.zero;
        dadosAtuais.nomeCenaCheckpoint = "";
        dadosAtuais.checkpointStatusRegistrado = false;
        dadosAtuais.temPosicaoSalvamento = false;
        dadosAtuais.posicaoSalvamento = Vector3.zero;
        dadosAtuais.vidaKayaCheckpoint = vidaKayaAtual;
        dadosAtuais.furiaKayaCheckpoint = furiaKayaAtual;
        ReiniciarMoedasParaNovaFase();
        dadosAtuais.inimigosEliminadosIDs = new List<string>();
        dadosAtuais.inimigosEliminadosIDsCheckpoint = new List<string>();
        dadosAtuais.elixiresVidaColetadosIDsCheckpoint = new List<string>(dadosAtuais.elixiresVidaColetadosIDs);
        aplicarPosicaoSalvaAoCarregar = false;
        fileDataHandler.Save(dadosAtuais, 0);
    }

    private void ReiniciarMoedasParaNovaFase()
    {
        moedasColetadasAtual = 0;
        totalMoedasNaFase = 0;
        dadosAtuais.moedasAtuais = 0;
        dadosAtuais.moedasNoCheckpoint = 0;
        dadosAtuais.moedasColetadasIDs = new List<string>();
        dadosAtuais.moedasColetadasIDsCheckpoint = new List<string>();
    }

    public float GetVidaKaya() => vidaKayaAtual;

    public void RegistrarInimigoDerrotado(string id)
    {
        if (dadosAtuais == null || string.IsNullOrEmpty(id)) return;
        if (dadosAtuais.inimigosEliminadosIDs == null) dadosAtuais.inimigosEliminadosIDs = new List<string>();
        if (!dadosAtuais.inimigosEliminadosIDs.Contains(id)) dadosAtuais.inimigosEliminadosIDs.Add(id);
    }
    public float GetFuriaKaya() => furiaKayaAtual;

    public void DescartarCheckpointDeOutraCena(string cenaAtual)
    {
        if (dadosAtuais == null || !dadosAtuais.passouCheckpoint)
            return;

        string cenaCheckpoint = !string.IsNullOrEmpty(dadosAtuais.nomeCenaCheckpoint)
            ? dadosAtuais.nomeCenaCheckpoint
            : dadosAtuais.nomeCena;

        if (cenaCheckpoint == cenaAtual)
            return;

        dadosAtuais.passouCheckpoint = false;
        dadosAtuais.posicaoCheckpoint = Vector3.zero;
        dadosAtuais.nomeCenaCheckpoint = "";
        dadosAtuais.checkpointStatusRegistrado = false;
        dadosAtuais.nomeCena = cenaAtual;

        fileDataHandler.Save(dadosAtuais, 0);
    }

    public void RenascerNoCheckpoint()
    {
        renascimentoPendente = true;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (dadosAtuais == null) dadosAtuais = new SaveData();

        PlayerHealth hpAtual = Object.FindFirstObjectByType<PlayerHealth>();
        SistemaFuria furiaAtual = Object.FindFirstObjectByType<SistemaFuria>();
        float vidaMaximaReal = hpAtual != null ? hpAtual.VidaMaxima : 100f;
        vidaKayaAtual = vidaMaximaReal;
        furiaKayaAtual = furiaAtual != null ? furiaAtual.FuriaAtual : dadosAtuais.furiaKaya;

        string cenaDestino = SceneManager.GetActiveScene().name;
        if (dadosAtuais.passouCheckpoint)
        {
            if (!string.IsNullOrEmpty(dadosAtuais.nomeCenaCheckpoint))
                cenaDestino = dadosAtuais.nomeCenaCheckpoint;
            else if (!string.IsNullOrEmpty(dadosAtuais.nomeCena))
                cenaDestino = dadosAtuais.nomeCena;
        }

        moedasColetadasAtual = Mathf.Max(0, dadosAtuais.moedasNoCheckpoint);
        dadosAtuais.moedasAtuais = moedasColetadasAtual;
        if (dadosAtuais.moedasColetadasIDsCheckpoint != null)
            dadosAtuais.moedasColetadasIDs = new List<string>(dadosAtuais.moedasColetadasIDsCheckpoint);
        if (dadosAtuais.elixiresVidaColetadosIDsCheckpoint != null)
            dadosAtuais.elixiresVidaColetadosIDs = new List<string>(dadosAtuais.elixiresVidaColetadosIDsCheckpoint);
        dadosAtuais.inimigosEliminadosIDs = dadosAtuais.inimigosEliminadosIDsCheckpoint != null ? new List<string>(dadosAtuais.inimigosEliminadosIDsCheckpoint) : new List<string>();

        dadosAtuais.versaoDados = 2;
        dadosAtuais.vidaKaya = vidaKayaAtual;
        dadosAtuais.furiaKaya = furiaKayaAtual;
        dadosAtuais.nomeCena = cenaDestino;
        if (dadosAtuais.passouCheckpoint)
        {
            dadosAtuais.posicaoSalvamento = dadosAtuais.posicaoCheckpoint;
            dadosAtuais.temPosicaoSalvamento = true;
        }
        else
        {
            dadosAtuais.posicaoSalvamento = Vector3.zero;
            dadosAtuais.temPosicaoSalvamento = false;
        }

        aplicarPosicaoSalvaAoCarregar = false;
        fileDataHandler.Save(dadosAtuais, 0);
        CarregarCenaComFade(cenaDestino);
    }

    private void ResetarStatusKaya()
    {
        vidaKayaAtual = 100f;
        furiaKayaAtual = 0f;
        dadosAtuais.vidaKaya = vidaKayaAtual;
        dadosAtuais.furiaKaya = furiaKayaAtual;
    }

    public void AvancarFase()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.RegistrarColetaveisPendentesNoSave();

        dadosAtuais.faseAtual++;
        renascimentoPendente = true;
        dadosAtuais.passouCheckpoint = false;
        dadosAtuais.posicaoCheckpoint = Vector3.zero;
        dadosAtuais.nomeCenaCheckpoint = "";
        dadosAtuais.checkpointStatusRegistrado = false;
        dadosAtuais.temPosicaoSalvamento = false;
        dadosAtuais.posicaoSalvamento = Vector3.zero;
        dadosAtuais.vidaKayaCheckpoint = vidaKayaAtual;
        dadosAtuais.furiaKayaCheckpoint = furiaKayaAtual;

        ReiniciarMoedasParaNovaFase();
        dadosAtuais.inimigosEliminadosIDs = new List<string>();
        dadosAtuais.inimigosEliminadosIDsCheckpoint = new List<string>();
        dadosAtuais.elixiresVidaColetadosIDsCheckpoint = new List<string>(dadosAtuais.elixiresVidaColetadosIDs);


        dadosAtuais.vidaKaya = vidaKayaAtual;
        dadosAtuais.furiaKaya = furiaKayaAtual;

        switch (dadosAtuais.faseAtual)
        {
            case 2: dadosAtuais.nomeCena = "FasePeste"; break;
            case 3: dadosAtuais.nomeCena = "FaseGuerra"; break;
            case 4: dadosAtuais.nomeCena = "FaseMorte"; break;
            default: dadosAtuais.nomeCena = "Menu"; break;
        }

        fileDataHandler.Save(dadosAtuais, 0);

        if (Application.CanStreamedLevelBeLoaded(dadosAtuais.nomeCena))
        {
            ExplicacaoManager.ExibirAntesDaFase(dadosAtuais.nomeCena);
        }
        else
        {
            ExplicacaoManager.CancelarFragmentoPendente();
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
        LevelLoader.CarregarComFade(nomeCena);
    }
}