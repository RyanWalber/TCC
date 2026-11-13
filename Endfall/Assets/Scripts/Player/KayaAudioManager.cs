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
    [SerializeField] private AudioSource fonteAmbiente;
    [SerializeField] private AudioSource fonteMusicaCombate;

    [Header("Ambiente da fase (loop 2D)")]
    [Tooltip("Vento ou som de fundo da fase. Deixe vazio para explorar em silencio.")]
    [SerializeField] private AudioClip somAmbiente;
    [SerializeField, Range(0f, 1f)] private float volumeAmbiente = 0.25f;

    [Header("Musica de combate (crossfade com ambiente)")]
    [Tooltip("Arraste aqui a faixa de rock de combate (2.wav).")]
    [SerializeField] private AudioClip musicaCombate;
    [SerializeField, Range(0f, 1f)] private float volumeMusicaCombate = 0.1f;
    [Tooltip("Inicia combate quando inimigo vivo chega a esta distancia da Kaya.")]
    [SerializeField, Min(1f)] private float raioDeteccaoCombate = 220f;
    [SerializeField, Min(0.1f)] private float intervaloVerificacaoCombate = 0.35f;
    [Tooltip("Tempo, em segundos, para troca suave nos dois sentidos.")]
    [SerializeField, Min(0.1f)] private float duracaoTransicaoMusicas = 2f;
    [Tooltip("Quanto do ambiente fica tocando durante o combate (0 = apenas rock).")]
    [SerializeField, Range(0f, 1f)] private float proporcaoAmbienteNoCombate = 0f;
    [Tooltip("Evita alternancia rapida entre trilhas ao passar na borda da distancia.")]
    [SerializeField, Min(0f)] private float atrasoRetornoAmbiente = 1f;

    [Header("Movimentacao")]
    [Tooltip("Passo unico (usado quando nao ha variacoes preenchidas).")]
    [SerializeField] private AudioClip passos;
    [Tooltip("Opcional: arraste aqui as quatro pisadas para alternar automaticamente.")]
    [SerializeField] private AudioClip[] variacoesPassos;
    [SerializeField, Range(0f, 1f)] private float volumePassos = 0.35f;

    [Header("Sincronia dos passos com a animacao")]
    [Tooltip("Nome do estado de caminhada no Animator da Kaya.")]
    [SerializeField] private string estadoAnimacaoAndar = "kayandando";
    [Tooltip("Instante da primeira pisada dentro do ciclo da animacao (0 a 1).")]
    [SerializeField, Range(0f, 1f)] private float fasePrimeiraPisada = 0.15f;
    [Tooltip("Instante da segunda pisada dentro do ciclo da animacao (0 a 1).")]
    [SerializeField, Range(0f, 1f)] private float faseSegundaPisada = 0.65f;
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

    private PlayerController movimento;
    private Animator animatorPassos;
    private int hashEstadoAndar;
    private float ultimoTempoDaAnimacao;
    private bool passosSincronizados;
    private int indiceVariacaoPasso;
    private float ultimoEventoCombate = float.NegativeInfinity;
    private float misturaCansaco;
    private float misturaCombate;
    private float proximaBuscaDeInimigos;
    private float ultimoInstanteInimigoPerto = float.NegativeInfinity;

    // BossIntroUniversal inicia e mantem a trilha ate a derrota do chefe.
    private bool combateBossAtivo;
    private Transform bossDoGatilho;
    private BossVida vidaBossDoGatilho;
    private VidaBossGuerra vidaGuerraDoGatilho;

    private bool morreu;

    private void Awake()
    {
        movimento = GetComponentInParent<PlayerController>();
        animatorPassos = GetComponentInChildren<Animator>(true);
        hashEstadoAndar = Animator.StringToHash(estadoAnimacaoAndar);

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

        // O ambiente precisa de uma fonte propria para nao interromper os efeitos da Kaya.
        if (fonteAmbiente == null || fonteAmbiente == fonteEfeitos ||
            fonteAmbiente == fontePassos || fonteAmbiente == fonteRespiracaoNormal ||
            fonteAmbiente == fonteRespiracaoCansada)
            fonteAmbiente = CriarFonte("Audio - Ambiente da Fase");

        // Fontes independentes para as duas musicas. Nenhuma reutiliza tiro,
        // passos, respiracao ou efeitos.
        if (fonteMusicaCombate == null ||
            fonteMusicaCombate == fonteEfeitos ||
            fonteMusicaCombate == fontePassos ||
            fonteMusicaCombate == fonteRespiracaoNormal ||
            fonteMusicaCombate == fonteRespiracaoCansada ||
            fonteMusicaCombate == fonteAmbiente)
            fonteMusicaCombate = CriarFonte("Audio - Musica de Combate");

        // Compatibilidade com os clips que ja foram arrastados para os AudioSources.
        if (tiro == null && fonteEfeitos.clip != null) tiro = fonteEfeitos.clip;
        if (respiracaoNormal == null && fonteRespiracaoNormal.clip != null)
            respiracaoNormal = fonteRespiracaoNormal.clip;

        ConfigurarFonte(fonteEfeitos, false);
        ConfigurarFonte(fontePassos, false);
        ConfigurarFonte(fonteRespiracaoNormal, true);
        ConfigurarFonte(fonteRespiracaoCansada, true);
        ConfigurarFonte(fonteAmbiente, true);
        ConfigurarFonte(fonteMusicaCombate, true);

        // So as fontes de respiracao tocam continuamente.
        fonteEfeitos.Stop();
        fontePassos.Stop();
        IniciarRespiracao(fonteRespiracaoNormal, respiracaoNormal, volumeRespiracaoNormal);
        IniciarRespiracao(fonteRespiracaoCansada, respiracaoCansada, 0f);
        // Ambos os loops iniciam juntos no relogio DSP, para manter o mesmo
        // ponto musical mesmo enquanto um deles estiver com volume zero.
        // Essa sincronia e especialmente util se 1.wav e 2.wav tem a mesma duracao.
        fonteAmbiente.Stop();
        fonteMusicaCombate.Stop();
        fonteAmbiente.clip = somAmbiente;
        fonteAmbiente.volume = volumeAmbiente;
        fonteMusicaCombate.clip = musicaCombate;
        fonteMusicaCombate.volume = 0f;
        if (somAmbiente != null && musicaCombate != null)
        {
            double inicioSincronizado = AudioSettings.dspTime + 0.1d;
            fonteAmbiente.PlayScheduled(inicioSincronizado);
            fonteMusicaCombate.PlayScheduled(inicioSincronizado);
        }
        else
        {
            if (somAmbiente != null) fonteAmbiente.Play();
            if (musicaCombate != null) fonteMusicaCombate.Play();
        }
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

        AtualizarRespiracao();
        AtualizarTransicaoMusicas();
    }

    // O Animator atualiza sua pose antes do LateUpdate.
    // Assim, as pisadas seguem o ciclo REAL da animacao, mesmo se sua velocidade mudar.
    private void LateUpdate()
    {
        if (!morreu)
            AtualizarPassos();
    }

    private void AtualizarPassos()
    {
        if (movimento == null || !movimento.EstaMovendoNoChao ||
            animatorPassos == null || !animatorPassos.isActiveAndEnabled ||
            animatorPassos.IsInTransition(0))
        {
            passosSincronizados = false;
            return;
        }

        AnimatorStateInfo estado = animatorPassos.GetCurrentAnimatorStateInfo(0);
        if (estado.shortNameHash != hashEstadoAndar)
        {
            passosSincronizados = false;
            return;
        }

        float tempoAtual = estado.normalizedTime;
        if (!passosSincronizados || tempoAtual < ultimoTempoDaAnimacao)
        {
            ultimoTempoDaAnimacao = tempoAtual;
            passosSincronizados = true;
            return;
        }

        // normalizedTime avanca por ciclos: 0..1, 1..2, 2..3...
        // Detectar a passagem por cada marca impede que o som derive do movimento.
        if (Mathf.FloorToInt(tempoAtual - fasePrimeiraPisada) >
            Mathf.FloorToInt(ultimoTempoDaAnimacao - fasePrimeiraPisada))
            TocarUmaPisada();

        if (Mathf.FloorToInt(tempoAtual - faseSegundaPisada) >
            Mathf.FloorToInt(ultimoTempoDaAnimacao - faseSegundaPisada))
            TocarUmaPisada();

        ultimoTempoDaAnimacao = tempoAtual;
    }

    private void TocarUmaPisada()
    {
        if (fontePassos == null) return;

        AudioClip clip = passos;
        if (variacoesPassos != null && variacoesPassos.Length > 0)
        {
            // Busca a proxima variacao valida, sem exigir todos os campos preenchidos.
            for (int i = 0; i < variacoesPassos.Length; i++)
            {
                int posicao = (indiceVariacaoPasso + i) % variacoesPassos.Length;
                if (variacoesPassos[posicao] == null) continue;

                clip = variacoesPassos[posicao];
                indiceVariacaoPasso = (posicao + 1) % variacoesPassos.Length;
                break;
            }
        }

        if (clip != null)
            fontePassos.PlayOneShot(clip, volumePassos);
    }

    public void IniciarCombateBoss(Transform boss)
    {
        if (morreu || boss == null) return;

        BossVida vida = boss.GetComponent<BossVida>();
        if (vida == null) vida = boss.GetComponentInParent<BossVida>();
        if (vida == null) vida = boss.GetComponentInChildren<BossVida>(true);

        VidaBossGuerra vidaGuerra = boss.GetComponent<VidaBossGuerra>();
        if (vidaGuerra == null) vidaGuerra = boss.GetComponentInParent<VidaBossGuerra>();
        if (vidaGuerra == null) vidaGuerra = boss.GetComponentInChildren<VidaBossGuerra>(true);

        if (vida != null && vida.EstaMorto) return;
        if (vidaGuerra != null && vidaGuerra.GetVidaAtual() <= 0f) return;

        bossDoGatilho = boss;
        vidaBossDoGatilho = vida;
        vidaGuerraDoGatilho = vidaGuerra;
        combateBossAtivo = true;

        if (vida == null && vidaGuerra == null)
            Debug.LogWarning("[AudioKaya] Boss do gatilho sem BossVida ou VidaBossGuerra. " +
                "A musica encerrara quando esse boss desaparecer.", boss);
    }

    private void ConferirFimDoCombateBoss()
    {
        if (!combateBossAtivo) return;

        bool terminou = bossDoGatilho == null ||
            (vidaBossDoGatilho != null && vidaBossDoGatilho.EstaMorto) ||
            (vidaGuerraDoGatilho != null && vidaGuerraDoGatilho.GetVidaAtual() <= 0f);

        if (!terminou) return;

        combateBossAtivo = false;
        bossDoGatilho = null;
        vidaBossDoGatilho = null;
        vidaGuerraDoGatilho = null;
        ultimoInstanteInimigoPerto = float.NegativeInfinity;
        proximaBuscaDeInimigos = Time.time + Mathf.Max(0.1f, intervaloVerificacaoCombate);
    }

    private bool ExisteAmeacaProxima()
    {
        // Collider do proprio inimigo; nao confunde paredes e plataformas
        // com inimigos. A musica nao dispara por tiros sem inimigos.
        Collider2D[] proximos = Physics2D.OverlapCircleAll(
            transform.position, Mathf.Max(1f, raioDeteccaoCombate));
        foreach (Collider2D colisao in proximos)
        {
            if (colisao == null || !colisao.gameObject.activeInHierarchy ||
                colisao.transform.root == transform.root) continue;

            BossVida vidaBoss = colisao.GetComponentInParent<BossVida>();
            if (vidaBoss != null && vidaBoss.EstaMorto) continue;
            VidaBossGuerra vidaGuerra = colisao.GetComponentInParent<VidaBossGuerra>();
            if (vidaGuerra != null && vidaGuerra.GetVidaAtual() <= 0f) continue;

            InimigoBixel bixel = colisao.GetComponentInParent<InimigoBixel>();
            if (bixel != null && bixel.isActiveAndEnabled) return true;

            InimigoBixinho bixinho = colisao.GetComponentInParent<InimigoBixinho>();
            if (bixinho != null && bixinho.isActiveAndEnabled) return true;

            InimigoBixao bixao = colisao.GetComponentInParent<InimigoBixao>();
            if (bixao != null && bixao.isActiveAndEnabled) return true;

            InimigoParado parado = colisao.GetComponentInParent<InimigoParado>();
            if (parado != null && parado.isActiveAndEnabled) return true;

            // Bosses somente com gatilho; corvos fazem parte da batalha
            // do boss e nao devem manter a trilha apos a morte do chefe.
        }
        return false;
    }

    private void AtualizarTransicaoMusicas()
    {
        if (fonteAmbiente == null || fonteMusicaCombate == null) return;

        ConferirFimDoCombateBoss();

        // A proximidade so importa fora da luta iniciada pelo gatilho.
        if (!combateBossAtivo && Time.time >= proximaBuscaDeInimigos)
        {
            proximaBuscaDeInimigos = Time.time +
                Mathf.Max(0.1f, intervaloVerificacaoCombate);
            if (musicaCombate != null && ExisteAmeacaProxima())
                ultimoInstanteInimigoPerto = Time.time;
        }

        bool combateComum = Time.time - ultimoInstanteInimigoPerto <= atrasoRetornoAmbiente;
        bool emCombate = musicaCombate != null && (combateBossAtivo || combateComum);

        misturaCombate = Mathf.MoveTowards(misturaCombate,
            emCombate ? 1f : 0f,
            Time.deltaTime / Mathf.Max(0.1f, duracaoTransicaoMusicas));
        float crossfade = Mathf.SmoothStep(0f, 1f, misturaCombate);

        // Quando um sobe, o outro desce. Na volta acontece o inverso.
        fonteAmbiente.volume = volumeAmbiente *
            Mathf.Lerp(1f, proporcaoAmbienteNoCombate, crossfade);
        fonteMusicaCombate.volume = volumeMusicaCombate * crossfade;
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

    public void TocarMorte()
    {
        if (morreu) return;
        morreu = true;

        fonteRespiracaoNormal.Stop();
        fonteRespiracaoCansada.Stop();
        fontePassos.Stop();
        if (fonteMusicaCombate != null) fonteMusicaCombate.Stop();

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
