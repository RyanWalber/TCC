using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Uma unica instancia atravessa Splash -> Menu -> Explicacao sem reiniciar a trilha.
// Tambem gerencia o som de clique de todos os botoes UI.
[DisallowMultipleComponent]
public class AudioManagerGlobal : MonoBehaviour
{
    public static AudioManagerGlobal Instance { get; private set; }

    private AudioSource fonteMusica;
    private AudioSource fonteInterface;
    private AudioClip musicaMenu;
    private AudioClip somClique;
    private float volumeMusicaMenu = 0.65f;
    private float volumeMusicaNarrativa = 0.18f;
    private float volumeClique = 0.65f;
    private float tempoEntrada = 2f;
    private float nivelTrilha;
    private Coroutine animacaoVolume;

    public static AudioManagerGlobal Garantir()
    {
        if (Instance != null) return Instance;
        GameObject objeto = new GameObject("AudioGlobalEndFall");
        return objeto.AddComponent<AudioManagerGlobal>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        fonteMusica = gameObject.AddComponent<AudioSource>();
        fonteMusica.playOnAwake = false;
        fonteMusica.loop = true;
        fonteMusica.spatialBlend = 0f;
        fonteMusica.volume = 0f;

        fonteInterface = gameObject.AddComponent<AudioSource>();
        fonteInterface.playOnAwake = false;
        fonteInterface.loop = false;
        fonteInterface.spatialBlend = 0f;
        fonteInterface.ignoreListenerPause = true;

        AudioListener.volume = ConfiguracoesJogo.VolumeGeral;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;
    }

    public void Configurar(AudioClip faixa, AudioClip clique, float volumeMenu,
        float volumeNarrativa, float volumeSfx, float fadeEntrada)
    {
        if (faixa != null && (musicaMenu == null || !fonteMusica.isPlaying))
        {
            musicaMenu = faixa;
            fonteMusica.clip = faixa;
        }

        if (clique != null) somClique = clique;
        volumeMusicaMenu = Mathf.Clamp01(volumeMenu);
        volumeMusicaNarrativa = Mathf.Clamp01(volumeNarrativa);
        volumeClique = Mathf.Clamp01(volumeSfx);
        tempoEntrada = Mathf.Max(0.1f, fadeEntrada);
        AtualizarVolumeMusica();
    }

    // Quando a cena Menu e executada diretamente no Editor, aceita clips
    // alternativos sem substituir a faixa que veio da Splash.
    public void PreencherClipsAusentes(AudioClip faixa, AudioClip clique)
    {
        if (musicaMenu == null && faixa != null)
        {
            musicaMenu = faixa;
            fonteMusica.clip = faixa;
        }
        if (somClique == null && clique != null) somClique = clique;
    }

    public void IniciarNaSplash()
    {
        IniciarTrilhaSeNecessario();
        FadePara(1f, tempoEntrada);
    }

    // Entrada direta no Menu pelo Editor, sem passar pela Splash.
    public void IniciarNoMenu()
    {
        IniciarTrilhaSeNecessario();
        FadePara(1f, tempoEntrada);
    }

    public void TocarClique()
    {
        if (somClique != null && fonteInterface != null)
            fonteInterface.PlayOneShot(somClique, volumeClique);
    }

    public void RegistrarBotoes()
    {
        Button[] botoes = FindObjectsByType<Button>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Button botao in botoes)
        {
            if (botao == null || botao.gameObject.scene != SceneManager.GetActiveScene()) continue;
            SomCliqueBotao som = botao.GetComponent<SomCliqueBotao>();
            if (som == null) som = botao.gameObject.AddComponent<SomCliqueBotao>();
            som.Registrar();
        }
    }

    // Chamado pelo LevelLoader no comeco do fade de saida.
    public void AoIniciarTransicao(string nomeCena, float duracaoFade)
    {
        if (nomeCena == "Explicação")
        {
            if (fonteMusica != null && fonteMusica.isPlaying)
                FadePara(VolumeRelativoNarrativa(), Mathf.Max(1f, duracaoFade));
        }
        else if (nomeCena != "Menu" && nomeCena != "Splash")
        {
            if (fonteMusica != null && fonteMusica.isPlaying)
                FadePara(0f, Mathf.Max(0.05f, duracaoFade));
        }
    }

    private void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        RegistrarBotoes();
        if (cena.name == "Menu")
        {
            IniciarTrilhaSeNecessario();
            FadePara(1f, tempoEntrada);
        }
        else if (cena.name == "Explicação")
        {
            // Sem inicio automatico: fragmentos posteriores nao repetem
            // a musica da abertura depois que o jogador iniciou a campanha.
            if (fonteMusica != null && fonteMusica.isPlaying)
                FadePara(VolumeRelativoNarrativa(), 0.5f);
        }
        else if (cena.name != "Splash")
        {
            // Ao entrar numa fase, a cortina global ja terminou o fade
            // de saida e a faixa do menu nao vaza para a jogabilidade.
            if (animacaoVolume != null)
            {
                StopCoroutine(animacaoVolume);
                animacaoVolume = null;
            }
            nivelTrilha = 0f;
            if (fonteMusica != null) fonteMusica.Stop();
            AtualizarVolumeMusica();
        }
    }

    private float VolumeRelativoNarrativa()
    {
        return volumeMusicaMenu <= 0.001f
            ? 0f : Mathf.Clamp01(volumeMusicaNarrativa / volumeMusicaMenu);
    }

    private void IniciarTrilhaSeNecessario()
    {
        if (musicaMenu == null || fonteMusica == null) return;
        if (fonteMusica.isPlaying) return;

        fonteMusica.clip = musicaMenu;
        nivelTrilha = 0f;
        fonteMusica.Play();
        AtualizarVolumeMusica();
    }

    private void FadePara(float destino, float duracao)
    {
        if (animacaoVolume != null) StopCoroutine(animacaoVolume);
        animacaoVolume = StartCoroutine(AnimarTrilha(destino, duracao));
    }

    private IEnumerator AnimarTrilha(float destino, float duracao)
    {
        float inicio = nivelTrilha;
        float tempo = 0f;
        duracao = Mathf.Max(0.05f, duracao);
        while (tempo < duracao)
        {
            tempo += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            nivelTrilha = Mathf.Lerp(inicio, destino, Mathf.Clamp01(tempo / duracao));
            AtualizarVolumeMusica();
            yield return null;
        }
        nivelTrilha = destino;
        AtualizarVolumeMusica();
        animacaoVolume = null;
    }

    private void Update()
    {
        // O toggle silencia somente a musica, sem parar o loop ou a narracao.
        AtualizarVolumeMusica();
    }

    private void AtualizarVolumeMusica()
    {
        if (fonteMusica == null) return;
        fonteMusica.volume = ConfiguracoesJogo.MusicaAtiva
            ? Mathf.Clamp01(volumeMusicaMenu * nivelTrilha) : 0f;
    }
}
