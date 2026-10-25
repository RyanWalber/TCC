using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Kaya/Audio/Kaya Audio Manager")]
public class KayaAudioManager : MonoBehaviour
{
    [Header("Fontes de audio (opcionais: criadas automaticamente se vazias)")]
    [SerializeField] private AudioSource fonteEfeitos;
    [SerializeField] private AudioSource fontePassos;
    [SerializeField] private AudioSource fonteRespiracaoNormal;
    [SerializeField] private AudioSource fonteRespiracaoCansada;

    [Header("Movimentacao")]
    [SerializeField] private AudioClip passos;
    [SerializeField, Range(0f, 1f)] private float volumePassos = 0.35f;
    [SerializeField, Min(0.1f)] private float intervaloPassos = 0.32f;
    [SerializeField] private AudioClip pulo;
    [SerializeField, Range(0f, 1f)] private float volumePulo = 0.65f;
    [SerializeField] private AudioClip aterrissagem;
    [SerializeField, Range(0f, 1f)] private float volumeAterrissagem = 0.55f;
    [SerializeField] private AudioClip dash;
    [SerializeField, Range(0f, 1f)] private float volumeDash = 0.65f;

    [Header("Combate")]
    [SerializeField] private AudioClip tiro;
    [SerializeField, Range(0f, 1f)] private float volumeTiro = 0.7f;
    [SerializeField] private AudioClip receberDano;
    [SerializeField, Range(0f, 1f)] private float volumeDano = 0.75f;
    [SerializeField] private AudioClip morte;
    [SerializeField, Range(0f, 1f)] private float volumeMorte = 0.8f;

    [Header("Furia")]
    [SerializeField] private AudioClip ativarFuria;
    [SerializeField, Range(0f, 1f)] private float volumeAtivarFuria = 0.7f;
    [SerializeField] private AudioClip desativarFuria;
    [SerializeField, Range(0f, 1f)] private float volumeDesativarFuria = 0.6f;

    [Header("Respiracao (clips em loop)")]
    [SerializeField] private AudioClip respiracaoNormal;
    [SerializeField, Range(0f, 1f)] private float volumeRespiracaoNormal = 0.12f;
    [SerializeField] private AudioClip respiracaoCansada;
    [SerializeField, Range(0f, 1f)] private float volumeRespiracaoCansada = 0.55f;
    [SerializeField, Min(0f)] private float esperaAposCombate = 3f;
    [SerializeField, Min(0f)] private float duracaoFolegoPesado = 2f;
    [SerializeField, Min(0.1f)] private float tempoRecuperacao = 8f;
    [SerializeField, Min(0.1f)] private float suavidadeTransicao = 1f;

    [Header("Interacoes")]
    [SerializeField] private AudioClip coletarElixir;
    [SerializeField, Range(0f, 1f)] private float volumeElixir = 0.65f;
    [SerializeField] private AudioClip ativarCheckpoint;
    [SerializeField, Range(0f, 1f)] private float volumeCheckpoint = 0.7f;

    private PlayerController movimento;
    private float proximoPasso;
    private float ultimoEventoCombate = float.NegativeInfinity;
    private float misturaCansaco;
    private bool morreu;

    private void Awake()
    {
        movimento = GetComponentInParent<PlayerController>();

        // Aproveita os dois AudioSources que ja estavam configurados no TiroPlayer.
        TiroPlayer tiroAnterior = GetComponentInChildren<TiroPlayer>(true);
        if (tiroAnterior != null)
        {
            if (fonteEfeitos == null) fonteEfeitos = tiroAnterior.FonteAudioTiro;
            if (fonteRespiracaoNormal == null) fonteRespiracaoNormal = tiroAnterior.FonteAudioRespiracao;
        }

        if (fonteEfeitos == null) fonteEfeitos = CriarFonte("Audio - Efeitos");
        if (fontePassos == null || fontePassos == fonteEfeitos)
            fontePassos = CriarFonte("Audio - Passos");

        if (fonteRespiracaoNormal == null ||
            fonteRespiracaoNormal == fonteEfeitos || fonteRespiracaoNormal == fontePassos)
            fonteRespiracaoNormal = CriarFonte("Audio - Respiracao Normal");

        if (fonteRespiracaoCansada == null ||
            fonteRespiracaoCansada == fonteEfeitos ||
            fonteRespiracaoCansada == fontePassos ||
            fonteRespiracaoCansada == fonteRespiracaoNormal)
            fonteRespiracaoCansada = CriarFonte("Audio - Respiracao Cansada");

        // Compatibilidade com os clips que ja foram arrastados para os AudioSources.
        if (tiro == null && fonteEfeitos.clip != null) tiro = fonteEfeitos.clip;
        if (respiracaoNormal == null && fonteRespiracaoNormal.clip != null)
            respiracaoNormal = fonteRespiracaoNormal.clip;

        ConfigurarFonte(fonteEfeitos, false);
        ConfigurarFonte(fontePassos, false);
        ConfigurarFonte(fonteRespiracaoNormal, true);
        ConfigurarFonte(fonteRespiracaoCansada, true);

        // So as fontes de respiracao tocam continuamente.
        fonteEfeitos.Stop();
        fontePassos.Stop();
        IniciarRespiracao(fonteRespiracaoNormal, respiracaoNormal, volumeRespiracaoNormal);
        IniciarRespiracao(fonteRespiracaoCansada, respiracaoCansada, 0f);
    }

    private AudioSource CriarFonte(string nome)
    {
        GameObject filho = new GameObject(nome);
        filho.transform.SetParent(transform, false);
        return filho.AddComponent<AudioSource>();
    }

    private static void ConfigurarFonte(AudioSource fonte, bool loop)
    {
        fonte.playOnAwake = false;
        fonte.loop = loop;
        fonte.spatialBlend = 0f;
    }

    private static void IniciarRespiracao(AudioSource fonte, AudioClip clip, float volume)
    {
        fonte.Stop();
        fonte.clip = clip;
        fonte.volume = volume;

        if (clip != null)
            fonte.Play();
    }

    private void Update()
    {
        if (morreu) return;

        AtualizarPassos();
        AtualizarRespiracao();
    }

    private void AtualizarPassos()
    {
        if (passos == null || movimento == null || !movimento.EstaMovendoNoChao)
            return;

        if (Time.time < proximoPasso) return;

        fontePassos.PlayOneShot(passos, volumePassos);
        proximoPasso = Time.time + intervaloPassos;
    }

    private void AtualizarRespiracao()
    {
        float tempoDesdeCombate = Time.time - ultimoEventoCombate;
        float alvo = 0f;

        if (tempoDesdeCombate >= esperaAposCombate)
        {
            float tempoCansaco = tempoDesdeCombate - esperaAposCombate;
            if (tempoCansaco < duracaoFolegoPesado)
                alvo = 1f;
            else
                alvo = 1f - Mathf.Clamp01((tempoCansaco - duracaoFolegoPesado) / tempoRecuperacao);
        }

        misturaCansaco = Mathf.MoveTowards(misturaCansaco, alvo,
            Time.deltaTime / suavidadeTransicao);

        if (respiracaoCansada != null)
        {
            fonteRespiracaoNormal.volume = volumeRespiracaoNormal * (1f - misturaCansaco);
            fonteRespiracaoCansada.volume = volumeRespiracaoCansada * misturaCansaco;
        }
        else
        {
            // Se nao houver clip de respiracao cansada, intensifica a gravacao normal.
            fonteRespiracaoNormal.volume = Mathf.Lerp(
                volumeRespiracaoNormal, volumeRespiracaoCansada, misturaCansaco);
        }
    }

    private void TocarEfeito(AudioClip clip, float volume)
    {
        if (morreu || clip == null || fonteEfeitos == null) return;
        fonteEfeitos.PlayOneShot(clip, volume);
    }

    public void RegistrarCombate()
    {
        if (!morreu)
            ultimoEventoCombate = Time.time;
    }

    public void TocarPulo() => TocarEfeito(pulo, volumePulo);
    public void TocarAterrissagem() => TocarEfeito(aterrissagem, volumeAterrissagem);
    public void TocarDash() => TocarEfeito(dash, volumeDash);

    public void TocarTiro()
    {
        TocarEfeito(tiro, volumeTiro);
        RegistrarCombate();
    }

    public void TocarDano()
    {
        TocarEfeito(receberDano, volumeDano);
        RegistrarCombate();
    }

    public void TocarAtivarFuria() => TocarEfeito(ativarFuria, volumeAtivarFuria);
    public void TocarDesativarFuria() => TocarEfeito(desativarFuria, volumeDesativarFuria);
    public void TocarElixir() => TocarEfeito(coletarElixir, volumeElixir);
    public void TocarCheckpoint() => TocarEfeito(ativarCheckpoint, volumeCheckpoint);

    public void TocarMorte()
    {
        if (morreu) return;
        morreu = true;

        fonteRespiracaoNormal.Stop();
        fonteRespiracaoCansada.Stop();
        fontePassos.Stop();

        // A cena pode recarregar imediatamente depois da morte.
        // Um objeto temporario conserva o som durante o renascimento.
        if (morte == null) return;

        GameObject objeto = new GameObject("Audio - Morte Kaya");
        DontDestroyOnLoad(objeto);
        AudioSource fonte = objeto.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f;
        fonte.PlayOneShot(morte, volumeMorte);
        Destroy(objeto, morte.length + 0.2f);
    }
}
