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
    // Bloqueia atualizações da cena antiga durante o recarregamento pós-morte.
    private bool renascimentoPendente = false;

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
        dadosAtuais.moedasAtuais = 0;
        dadosAtuais.temPosicaoSalvamento = false;
        aplicarPosicaoSalvaAoCarregar = false;

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
        if (dados == null) return;
        dadosAtuais = dados;

        // Compatibilidade com salvamentos antigos que não guardavam posição exata.
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
        if (LevelManager.Instance != null)
            LevelManager.Instance.RegistrarColetaveisPendentesNoSave();

        SincronizarStatusKayaDoJogador();
        dadosAtuais.nomeCena = SceneManager.GetActiveScene().name;
        GameObject jogador = GameObject.FindGameObjectWithTag("Player");
        if (jogador != null)
        {
            dadosAtuais.posicaoSalvamento = jogador.transform.position;
            dadosAtuais.temPosicaoSalvamento = true;
        }

        // Não modifica moedasNoCheckpoint: esse valor pertence ao snapshot de renascimento.
        dadosAtuais.versaoDados = 2;
        dadosAtuais.moedasAtuais = moedasColetadasAtual;
        dadosAtuais.vidaKaya = vidaKayaAtual;
        dadosAtuais.furiaKaya = furiaKayaAtual;
        fileDataHandler.Save(dadosAtuais, slot);
        fileDataHandler.Save(dadosAtuais, 0);
    }

    public void RegistrarCheckpoint(Vector3 posicao)
    {
        if (dadosAtuais == null) dadosAtuais = new SaveData();
        if (LevelManager.Instance != null)
            LevelManager.Instance.RegistrarColetaveisPendentesNoSave();
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
        if (LevelManager.Instance != null)
            LevelManager.Instance.RegistrarColetaveisPendentesNoSave();
        if (dadosAtuais.elixiresVidaColetadosIDs == null)
            dadosAtuais.elixiresVidaColetadosIDs = new List<string>();
        if (!dadosAtuais.elixiresVidaColetadosIDs.Contains(id))
            dadosAtuais.elixiresVidaColetadosIDs.Add(id);

        SincronizarStatusKayaDoJogador();
        GameObject jogador = GameObject.FindGameObjectWithTag("Player");
        if (jogador != null)
        {
            dadosAtuais.posicaoSalvamento = jogador.transform.position;
            dadosAtuais.temPosicaoSalvamento = true;
            dadosAtuais.nomeCena = SceneManager.GetActiveScene().name;
        }
        dadosAtuais.moedasAtuais = moedasColetadasAtual;
        fileDataHandler.Save(dadosAtuais, 0);
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

    // --- MÉTODOS PARA VIDA E FÚRIA ---
    public void AtualizarStatusKaya(float vida, float furia)
    {
        // A cena antiga ainda pode executar Update durante o fade/reload.
        // Nunca permita que ela sobrescreva a vida cheia já preparada para o respawn.
        if (renascimentoPendente) return;

        vidaKayaAtual = vida;
        furiaKayaAtual = furia;
        if (dadosAtuais == null) dadosAtuais = new SaveData();
        dadosAtuais.vidaKaya = vida;
        dadosAtuais.furiaKaya = furia;
    }

    // A nova instância do jogador chama este método depois de ler a vida salva em Start.
    public void ConfirmarRenascimentoInicializado()
    {
        renascimentoPendente = false;
    }

    public void PrepararTransicaoDeFase(string nomeProximaCena, float vidaDepoisDaRecuperacao)
    {
        if (dadosAtuais == null) dadosAtuais = new SaveData();
        if (LevelManager.Instance != null)
            LevelManager.Instance.RegistrarColetaveisPendentesNoSave();

        // Usa o valor explícito calculado antes da troca, sem depender da ordem dos Updates.
        vidaKayaAtual = Mathf.Clamp(vidaDepoisDaRecuperacao, 1f, 100000f);
        dadosAtuais.vidaKaya = vidaKayaAtual;
        SistemaFuria furia = FindFirstObjectByType<SistemaFuria>();
        if (furia != null)
            furiaKayaAtual = furia.FuriaAtual;
        dadosAtuais.furiaKaya = furiaKayaAtual;

        dadosAtuais.versaoDados = 2;
        dadosAtuais.nomeCena = nomeProximaCena;
        dadosAtuais.passouCheckpoint = false;
        dadosAtuais.posicaoCheckpoint = Vector3.zero;
        dadosAtuais.nomeCenaCheckpoint = "";
        dadosAtuais.checkpointStatusRegistrado = false;
        dadosAtuais.temPosicaoSalvamento = false;
        dadosAtuais.posicaoSalvamento = Vector3.zero;
        dadosAtuais.moedasNoCheckpoint = moedasColetadasAtual;
        dadosAtuais.moedasAtuais = moedasColetadasAtual;
        dadosAtuais.moedasColetadasIDsCheckpoint = new List<string>(dadosAtuais.moedasColetadasIDs);
        dadosAtuais.elixiresVidaColetadosIDsCheckpoint = new List<string>(dadosAtuais.elixiresVidaColetadosIDs);
        aplicarPosicaoSalvaAoCarregar = false;
        fileDataHandler.Save(dadosAtuais, 0);
    }

    public float GetVidaKaya() => vidaKayaAtual;
    public float GetFuriaKaya() => furiaKayaAtual;

    // Usado quando a Kaya morre: restaura o snapshot do checkpoint e recarrega
    // a cena correspondente, sem abrir o painel de Game Over.
    // Se uma nova fase for carregada diretamente, descarta o checkpoint da fase anterior.
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
        // Vale tanto para morte por inimigo quanto para queda na ZonaMorte.
        renascimentoPendente = true;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (dadosAtuais == null) dadosAtuais = new SaveData();

        // Morte: restaura a vida máxima real configurada na Kaya e preserva a Fúria.
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

        // Reverte coletáveis e moedas para o snapshot do checkpoint/estado-base da fase.
        moedasColetadasAtual = Mathf.Max(0, dadosAtuais.moedasNoCheckpoint);
        dadosAtuais.moedasAtuais = moedasColetadasAtual;
        if (dadosAtuais.moedasColetadasIDsCheckpoint != null)
            dadosAtuais.moedasColetadasIDs = new List<string>(dadosAtuais.moedasColetadasIDsCheckpoint);
        if (dadosAtuais.elixiresVidaColetadosIDsCheckpoint != null)
            dadosAtuais.elixiresVidaColetadosIDs = new List<string>(dadosAtuais.elixiresVidaColetadosIDsCheckpoint);

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

        // Os IDs dos coletáveis possuem identificadores únicos e são preservados entre fases.

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