using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configurações de Vida")]
    [SerializeField] private float vidaMaxima = 100f;
    private float vidaAtual;
    public float VidaAtual => vidaAtual;
    public float VidaMaxima => vidaMaxima;

    [Header("Imagens da UI")]
    [SerializeField] private Image fillVida;          // Barra Vermelha (vida)
    [SerializeField] private Image fillVidaFantasma;  // Barra Amarela/Branca (vidaFantasma)

    [Header("Velocidade da Barra Fantasma")]
    [SerializeField] private float velocidadeFantasma = 1.5f;

    [Header("Fúria e Efeitos")]
    [SerializeField] private float furiaPorDanoTomado = 15f;
    private SistemaFuria sistemaFuria;
    private HUDJuice hudJuice;

    [Header("Game Over e Feedback de Dano")]
    [SerializeField] private GameObject painelGameOver;
    [SerializeField] private Color corDano = Color.red;
    [SerializeField] private float tempoInvencivel = 0.8f;
    [Header("Piscada da Kaya quando toma dano")]
    [SerializeField, Min(0.03f)] private float tempoFlashVermelho = 0.12f;
    [SerializeField, Range(1, 5)] private int quantidadePiscadas = 2;

    // Lista de todos os Sprites do corpo fatiado da Kaya
    private SpriteRenderer[] todosSprites;
    private Color[] coresOriginais;

    private bool estaInvencivel = false;
    private bool estaMorta = false;
    private KayaAudioManager audioKaya;

    [Header("Animacao de Morte")]
    [SerializeField] private Animator animatorKaya;
    [SerializeField, Min(0.1f)] private float duracaoMorteFallback = 2.5f;

    [Header("Morte cinematografica - queda em camera lenta")]
    [Tooltip("Ao morrer, Kaya inicia a animacao de queda em camera lenta e a tela escurece ANTES de chegar ao chao.")]
    [SerializeField] private bool usarMorteCinematografica = true;
    [Tooltip("Velocidade do mundo no final da desaceleracao. 0.22 = 22% da velocidade normal.")]
    [Range(0.05f, 1f)] [SerializeField] private float velocidadeMundoNaMorte = 0.22f;
    [Tooltip("Tempo REAL para passar da velocidade normal para camera lenta.")]
    [Min(0.05f)] [SerializeField] private float tempoEntradaCameraLenta = 0.3f;
    [Tooltip("Desacelera tambem a musica e os efeitos sonoros pelo pitch, sem reiniciar os AudioSources.")]
    [SerializeField] private bool desacelerarMusicasESons = true;
    [Tooltip("Quanto a velocidade de audio acompanha a camera lenta. 1 = segue exatamente a lentidao do mundo; 0 = nao altera audio.")]
    [Range(0f, 1f)] [SerializeField] private float intensidadeCameraLentaAudio = 1f;
    [Tooltip("Velocidade da animacao kayamorrendo em tempo REAL. 0.75 = 75% da velocidade original; nao espera a animacao terminar.")]
    [Range(0.1f, 1.5f)] [SerializeField] private float velocidadeAnimacaoQueda = 0.75f;
    [Tooltip("Tempo REAL de queda visivel antes de o fade preto comecar.")]
    [Min(0f)] [SerializeField] private float atrasoFadeNaMorte = 0.25f;
    [Tooltip("Duracao REAL do fade ate a tela ficar preta e renascer. Diminua para cortar a queda mais cedo.")]
    [Min(0.1f)] [SerializeField] private float duracaoFadeNaMorte = 1.8f;

    private bool cinematicaMorteEmAndamento;
    private float escalaTempoAntesMorte = 1f;
    private float passoFisicaAntesMorte = 0.02f;
    private AnimatorUpdateMode modoAnimatorAntesMorte;
    private float velocidadeAnimatorAntesMorte = 1f;

    // A posicao dos pes so e travada quando a morte INICIA no chao.
    // No ar, a queda real permanece sob o controle do Rigidbody2D.
    private bool morreuApoiadaNoChao;
    private RigidbodyConstraints2D restricoesRigidbodyAntesMorte;
    private readonly Dictionary<AudioSource, float> pitchesOriginaisAudio =
        new Dictionary<AudioSource, float>();
    private float proximaBuscaAudioMorte;

    [Header("Estabilizacao visual durante a morte")]
    [Tooltip("Objeto visual que recebe a animacao. Se vazio, usa o objeto do Animator.")]
    [SerializeField] private Transform visualKaya;
    [Tooltip("Trava a posicao visual X/Y/Z onde Kaya recebeu o golpe fatal, sem alterar a queda fisica se morreu no ar.")]
    [SerializeField] private bool travarPosicaoHorizontalNaMorte = true;
    [Tooltip("Quanto a compensacao VERTICAL dos pes pode deslocar o visual do lugar da morte. Zero = corpo absolutamente fixo.")]
    [Min(0f)] [SerializeField] private float limiteCorrecaoVerticalPes = 0f;

    [Header("Pes fixos durante a queda")]
    [Tooltip("Mantem a posicao dos pes no mundo enquanto o corpo executa kayamorrendo.")]
    [SerializeField] private bool fixarPesDuranteMorte = true;
    [Tooltip("Ponta do pe da primeira perna. Se vazio, tenta localizar automaticamente no rig.")]
    [SerializeField] private Transform peMorte1;
    [Tooltip("Ponta do pe da segunda perna. Se vazio, tenta localizar automaticamente no rig.")]
    [SerializeField] private Transform peMorte2;
    [Tooltip("Mantem tambem os pes na mesma altura do chao.")]
    [SerializeField] private bool fixarAlturaDosPes = true;

    [Header("Direcao da queda")]
    [Tooltip("Mantem a morte espelhada conforme a direcao em que Kaya estava olhando no momento do golpe fatal.")]
    [SerializeField] private bool respeitarDirecaoAoMorrer = true;
    private bool olhandoParaEsquerdaAoMorrer;

    private Vector3 posicaoReferenciaDosPes;
    private bool referenciaDosPesCapturada;
    private static readonly int EstadoMorrendo = Animator.StringToHash("kayamorrendo");

    private Vector3 posicaoLocalVisualNaMorte;
    private bool posicaoMorteCapturada;
    private static readonly int TriggerMorrer = Animator.StringToHash("Morrer");

    private void Start()
    {
        audioKaya = GetComponent<KayaAudioManager>();
        if (animatorKaya == null)
        {
            PlayerAnimation animacao = GetComponentInChildren<PlayerAnimation>(true);
            if (animacao != null) animatorKaya = animacao.GetComponent<Animator>();
            if (animatorKaya == null) animatorKaya = GetComponentInChildren<Animator>(true);
        }
        if (visualKaya == null && animatorKaya != null)
            visualKaya = animatorKaya.transform;

        // Os dois caminhos pertencem ao rig original de Ani Kaya.
        // Se alguma cena usar outro rig, os campos continuam editaveis no Inspector.
        if (visualKaya != null)
        {
            if (peMorte1 == null)
                peMorte1 = visualKaya.Find("kayaseparada_9/bone_1/bone_2/bone_3/bone_4");
            if (peMorte2 == null)
                peMorte2 = visualKaya.Find("kayaseparada_10/bone_1/bone_2/bone_3/bone_4");
        }

        sistemaFuria = GetComponent<SistemaFuria>();
        hudJuice = FindFirstObjectByType<HUDJuice>();

        float vidaSalva = SaveManager.Instance != null
            ? SaveManager.Instance.GetVidaKaya()
            : vidaMaxima;
        vidaAtual = Mathf.Clamp(vidaSalva, 1f, vidaMaxima);

        // A nova Kaya já leu a vida salva (cheia após a morte). Só agora liberamos
        // a sincronização de vida/Fúria para os próximos Updates.
        if (SaveManager.Instance != null)
            SaveManager.Instance.ConfirmarRenascimentoInicializado();

        // Pega todos os sprites do corpo da Kaya
        todosSprites = GetComponentsInChildren<SpriteRenderer>(true);
        if (todosSprites != null && todosSprites.Length > 0)
        {
            coresOriginais = new Color[todosSprites.Length];
            for (int i = 0; i < todosSprites.Length; i++)
            {
                coresOriginais[i] = todosSprites[i].color;
            }
        }

        if (painelGameOver != null)
        {
            painelGameOver.SetActive(false);
        }

        AtualizarUIImediato();
    }

    private void Update()
    {
        // Depois de morrer, não continue gravando vida 0 durante o carregamento da cena.
        if (estaMorta) return;

        if (fillVidaFantasma != null && fillVida != null)
        {
            if (fillVidaFantasma.fillAmount > fillVida.fillAmount)
            {
                fillVidaFantasma.fillAmount -= velocidadeFantasma * Time.deltaTime;
            }
        }

        // Mantém o estado atual pronto para salvamentos manuais/autosaves.
        if (SaveManager.Instance != null)
        {
            float furiaAtual = sistemaFuria != null
                ? sistemaFuria.FuriaAtual
                : SaveManager.Instance.GetFuriaKaya();
            SaveManager.Instance.AtualizarStatusKaya(vidaAtual, furiaAtual);
        }
    }

    private void LateUpdate()
    {
        if (!estaMorta || visualKaya == null) return;

        // A animação kayamorrendo original gravava a POSICAO do rig:
        // X -27.9 -> 47.8 e Y 7.4 -> -8.4. Mantemos a raiz do
        // personagem no mesmo ponto em que morreu, em ambos os lados.
        // Nao mexer no Transform do PLAYER: se morreu no ar, ele ainda
        // pode cair normalmente pelo Rigidbody2D.
        if (travarPosicaoHorizontalNaMorte && posicaoMorteCapturada)
            visualKaya.localPosition = posicaoLocalVisualNaMorte;

        // Captura o Y da mira no golpe fatal e nunca usa o mouse de novo.
        // Preserva o giro Z de queda, que e espelhado automaticamente.
        if (respeitarDirecaoAoMorrer)
        {
            Vector3 angulos = visualKaya.localEulerAngles;
            angulos.y = olhandoParaEsquerdaAoMorrer ? 180f : 0f;
            visualKaya.localEulerAngles = angulos;
        }

        // Na morte em solo, nao mover a Kaya horizontalmente para
        // perseguir o centro dos pes: esse era o tranco que fazia o
        // corpo deslizar para os lados enquanto o rig mudava de pose.
        // So um ajuste vertical MINIMO mantem o contato com o chao.
        // Limite 0 => posicao do visual absolutamente fixa.
        if (!morreuApoiadaNoChao || !fixarPesDuranteMorte ||
            !fixarAlturaDosPes || limiteCorrecaoVerticalPes <= 0f ||
            !referenciaDosPesCapturada ||
            (peMorte1 == null && peMorte2 == null))
            return;

        if (animatorKaya == null || !animatorKaya.isActiveAndEnabled)
            return;
        AnimatorStateInfo estado = animatorKaya.GetCurrentAnimatorStateInfo(0);
        if (estado.shortNameHash != EstadoMorrendo) return;

        float deltaY = posicaoReferenciaDosPes.y - ObterCentroPesNoMundo().y;
        float correcaoY = Mathf.Clamp(deltaY,
            -limiteCorrecaoVerticalPes, limiteCorrecaoVerticalPes);
        visualKaya.position += Vector3.up * correcaoY;
    }

    private Vector3 ObterCentroPesNoMundo()
    {
        if (peMorte1 != null && peMorte2 != null)
            return (peMorte1.position + peMorte2.position) * 0.5f;

        return peMorte1 != null ? peMorte1.position : peMorte2.position;
    }

    public void TomarDano(float quantidadeDano)
    {
        if (estaMorta || estaInvencivel || quantidadeDano <= 0f) return;

        if (audioKaya != null) audioKaya.TocarDano();
        vidaAtual -= quantidadeDano;
        vidaAtual = Mathf.Clamp(vidaAtual, 0f, vidaMaxima);

        if (fillVida != null)
        {
            fillVida.fillAmount = vidaAtual / vidaMaxima;
        }

        if (hudJuice != null)
        {
            hudJuice.TremerHUD();
            hudJuice.AtualizarRetrato(vidaAtual / vidaMaxima);
        }

        if (vidaAtual <= 0f)
        {
            Morrer();
        }
        else
        {
            StartCoroutine(RotinaInvencibilidade());
        }
    }

    public void TomarDanoDeInimigo(float quantidadeDano)
    {
        if (estaMorta || estaInvencivel || quantidadeDano <= 0f) return;

        // Soma a Fúria antes do dano, assim até um golpe fatal conta como dano recebido.
        if (sistemaFuria != null)
            sistemaFuria.AdicionarFuria(furiaPorDanoTomado);

        TomarDano(quantidadeDano);
    }

    private void AplicarCorKaya(Color cor, bool restaurarOriginal)
    {
        if (todosSprites == null || coresOriginais == null) return;
        for (int i = 0; i < todosSprites.Length; i++)
        {
            if (todosSprites[i] != null)
                todosSprites[i].color = restaurarOriginal ? coresOriginais[i] : cor;
        }
    }

    private IEnumerator RotinaInvencibilidade()
    {
        estaInvencivel = true;
        int piscadas = Mathf.Max(1, quantidadePiscadas);
        float duracaoTotal = Mathf.Max(0.05f, tempoInvencivel);
        float vermelho = Mathf.Min(Mathf.Max(0.03f, tempoFlashVermelho),
            duracaoTotal / (2f * piscadas));
        float normal = Mathf.Max(0f, (duracaoTotal - vermelho * piscadas) / piscadas);

        for (int i = 0; i < piscadas; i++)
        {
            AplicarCorKaya(corDano, false);
            yield return new WaitForSeconds(vermelho);
            AplicarCorKaya(Color.white, true);
            if (normal > 0f) yield return new WaitForSeconds(normal);
        }
        estaInvencivel = false;
    }

    private void Morrer()
    {
        if (estaMorta) return;
        estaMorta = true;
        referenciaDosPesCapturada = false;

        // Decide uma vez, no golpe fatal. Nenhum frame da animacao
        // pode transformar uma morte no ar em morte fixa no chao.
        PlayerController controle = GetComponent<PlayerController>();
        morreuApoiadaNoChao = DetectarApoioNoChao(controle);

        // Impede que a rotina de invencibilidade altere as cores durante a morte.
        StopAllCoroutines();
        AplicarCorKaya(Color.white, true);

        if (visualKaya != null)
        {
            // A orientacao da MiraMouse e Y=180 para a esquerda e Y=0
            // para a direita. Captura o lado ANTES de desativar a mira.
            // O MiraMouse usa Y=180 para a esquerda; o Dot com o
            // vetor right variava conforme a rotacao Z da mira.
            olhandoParaEsquerdaAoMorrer =
                Mathf.Abs(Mathf.DeltaAngle(visualKaya.localEulerAngles.y, 180f)) < 90f;
            posicaoLocalVisualNaMorte = visualKaya.localPosition;
            posicaoMorteCapturada = true;
        }

        // Captura os pes ANTES da primeira pose de kayamorrendo para
        // evitar que a mudanca inicial da animacao provoque um salto.
        if (morreuApoiadaNoChao && fixarPesDuranteMorte &&
            (peMorte1 != null || peMorte2 != null))
        {
            posicaoReferenciaDosPes = ObterCentroPesNoMundo();
            referenciaDosPesCapturada = true;
        }

        // Desativa toda a mira para nao disputar a rotacao com a animacao de morte.
        foreach (MiraMouse mira in GetComponentsInChildren<MiraMouse>(true))
            mira.enabled = false;

        if (controle != null) controle.enabled = false;

        TiroPlayer[] armas = GetComponentsInChildren<TiroPlayer>(true);
        foreach (TiroPlayer arma in armas)
            arma.enabled = false;

        Rigidbody2D corpo = GetComponent<Rigidbody2D>();
        if (corpo != null)
        {
            restricoesRigidbodyAntesMorte = corpo.constraints;
            float velocidadeVertical = morreuApoiadaNoChao ? 0f :
                Mathf.Min(0f, corpo.linearVelocity.y);
            corpo.linearVelocity = new Vector2(0f, velocidadeVertical);
            corpo.angularVelocity = 0f;

            if (morreuApoiadaNoChao)
            {
                // No chao, trava raiz FISICA; o visual ainda pode mover
                // ossos, com seus pes compensados no LateUpdate.
                corpo.constraints = RigidbodyConstraints2D.FreezeAll;
            }
            else
            {
                // No ar, preserva a gravidade e as colisoes. Congela
                // apenas o X fisico para Kaya nao ser arremessada.
                corpo.constraints =
                    (restricoesRigidbodyAntesMorte &
                     ~RigidbodyConstraints2D.FreezePositionY) |
                    RigidbodyConstraints2D.FreezePositionX;
                corpo.WakeUp();
            }
        }

        // Nao parar ambiente nem trilha de combate durante o slow motion.
        if (audioKaya != null)
            audioKaya.TocarMorte(usarMorteCinematografica);

        if (usarMorteCinematografica)
        {
            // O mundo inteiro fica lento via Time.timeScale. A animacao da
            // Kaya usa tempo real, mas com velocidade reduzida, para que a
            // queda seja VISIVEL antes do fade, sem chegar ao quadro final.
            if (animatorKaya != null && animatorKaya.isActiveAndEnabled)
            {
                modoAnimatorAntesMorte = animatorKaya.updateMode;
                velocidadeAnimatorAntesMorte = animatorKaya.speed;
                animatorKaya.updateMode = AnimatorUpdateMode.UnscaledTime;
                animatorKaya.speed = Mathf.Clamp(velocidadeAnimacaoQueda, 0.1f, 1.5f);
                animatorKaya.ResetTrigger(TriggerMorrer);
                animatorKaya.SetTrigger(TriggerMorrer);
            }

            StartCoroutine(ExecutarMorteCinematografica());
        }
        else if (animatorKaya != null && animatorKaya.isActiveAndEnabled)
        {
            animatorKaya.ResetTrigger(TriggerMorrer);
            animatorKaya.SetTrigger(TriggerMorrer);
            StartCoroutine(AguardarAnimacaoMorte());
        }
        else
        {
            // Modo classico sem Animator: respawn direto.
            RenascerDepoisDaMorte();
        }
    }

    private bool DetectarApoioNoChao(PlayerController controle)
    {
        if (controle != null && controle.EstaNoChao)
            return true;

        // Verificacao extra para morte no exato frame de aterrissagem
        // ou cenas em que o PlayerController foi desabilitado antes.
        Collider2D corpo = GetComponent<Collider2D>();
        if (corpo == null) return false;
        Bounds limites = corpo.bounds;
        float alturaTeste = Mathf.Max(0.15f, limites.size.y * 0.08f);
        float alcance = alturaTeste + 0.22f;

        for (int i = -1; i <= 1; i++)
        {
            float x = limites.center.x + i * limites.extents.x * 0.6f;
            Vector2 inicio = new Vector2(x, limites.min.y + alturaTeste);
            RaycastHit2D[] acertos = Physics2D.RaycastAll(
                inicio, Vector2.down, alcance);
            foreach (RaycastHit2D acerto in acertos)
            {
                if (acerto.collider == null || acerto.collider.isTrigger ||
                    acerto.collider.transform.IsChildOf(transform) ||
                    acerto.normal.y < 0.7f)
                    continue;
                return true;
            }
        }
        return false;
    }

    private void AtualizarCameraLentaNosAudios(float velocidadeRelativa)
    {
        if (!desacelerarMusicasESons) return;

        // AudioSource nao obedece a Time.timeScale. Alterar o pitch do
        // proprio source desacelera tambem as musicas que JA estavam
        // tocando, sem Stop, Play, reinicio ou descontinuidade de volume.
        // Captura tambem tiros/corvos/SFX criados durante a cinematica.
        if (Time.unscaledTime >= proximaBuscaAudioMorte)
        {
            AudioSource[] fontes = FindObjectsByType<AudioSource>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (AudioSource fonte in fontes)
            {
                if (fonte != null && !pitchesOriginaisAudio.ContainsKey(fonte))
                    pitchesOriginaisAudio.Add(fonte, fonte.pitch);
            }
            proximaBuscaAudioMorte = Time.unscaledTime + 0.07f;
        }

        float fator = Mathf.Lerp(1f,
            Mathf.Clamp(velocidadeRelativa, 0.05f, 1f),
            Mathf.Clamp01(intensidadeCameraLentaAudio));
        foreach (KeyValuePair<AudioSource, float> par in pitchesOriginaisAudio)
        {
            if (par.Key != null)
                par.Key.pitch = Mathf.Clamp(par.Value * fator, -3f, 3f);
        }
    }

    private void RestaurarPitchDosAudios()
    {
        foreach (KeyValuePair<AudioSource, float> par in pitchesOriginaisAudio)
        {
            if (par.Key != null)
                par.Key.pitch = par.Value;
        }
        pitchesOriginaisAudio.Clear();
        proximaBuscaAudioMorte = 0f;
    }

    private IEnumerator ExecutarMorteCinematografica()
    {
        cinematicaMorteEmAndamento = true;
        escalaTempoAntesMorte = Time.timeScale;
        passoFisicaAntesMorte = Time.fixedDeltaTime;

        LevelLoader cortina = LevelLoader.GarantirInstancia();
        float atraso = Mathf.Max(0f, atrasoFadeNaMorte);
        float duracaoFade = Mathf.Max(0.1f, duracaoFadeNaMorte);
        float duracaoDesaceleracao = Mathf.Max(0.05f, tempoEntradaCameraLenta);
        float duracaoTotal = atraso + duracaoFade;
        float tempoRealDecorrido = 0f;

        while (tempoRealDecorrido < duracaoTotal)
        {
            // UnscaledDeltaTime: o fade nao congela quando o mundo desacelera.
            tempoRealDecorrido += Mathf.Min(Time.unscaledDeltaTime, 0.1f);

            float progressoLento = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(
                tempoRealDecorrido / duracaoDesaceleracao));
            Time.timeScale = Mathf.Lerp(
                escalaTempoAntesMorte, Mathf.Clamp(velocidadeMundoNaMorte, 0.05f, 1f),
                progressoLento);
            Time.fixedDeltaTime = passoFisicaAntesMorte * Mathf.Max(0.01f,
                Time.timeScale / Mathf.Max(0.01f, escalaTempoAntesMorte));

            // Musicas e tiros seguem a mesma curva de camera lenta.
            AtualizarCameraLentaNosAudios(
                Time.timeScale / Mathf.Max(0.01f, escalaTempoAntesMorte));

            float progressoFade = Mathf.Clamp01(
                (tempoRealDecorrido - atraso) / duracaoFade);
            if (cortina != null)
                cortina.DefinirEscurecimentoDeMorte(
                    Mathf.SmoothStep(0f, 1f, progressoFade));

            yield return null;
        }

        // O ultimo quadro da animacao fica coberto pela tela preta.
        // Nao aguarda os 3 segundos do clipe kayamorrendo.
        if (cortina != null) cortina.DefinirEscurecimentoDeMorte(1f);
        RestaurarTempoDaMorte();
        cinematicaMorteEmAndamento = false;
        RenascerDepoisDaMorte();
    }

    private void RestaurarTempoDaMorte()
    {
        // Restaurar os pitchs originais ANTES de trocar a cena.
        RestaurarPitchDosAudios();
        Time.timeScale = Mathf.Max(0.01f, escalaTempoAntesMorte);
        Time.fixedDeltaTime = passoFisicaAntesMorte;
    }

    private void OnDisable()
    {
        // Se a cena ou Kaya for desativada durante a cinematica,
        // o proximo Play/respawn nao pode permanecer em camera lenta.
        if (!cinematicaMorteEmAndamento) return;
        RestaurarTempoDaMorte();
        cinematicaMorteEmAndamento = false;

        if (animatorKaya != null)
        {
            animatorKaya.updateMode = modoAnimatorAntesMorte;
            animatorKaya.speed = velocidadeAnimatorAntesMorte;
        }

        LevelLoader cortina = LevelLoader.Instance;
        if (cortina != null) cortina.CancelarEscurecimentoDeMorte();
    }

    private IEnumerator AguardarAnimacaoMorte()
    {
        // O Animator pode processar o Trigger apenas no proximo frame.
        float tempoEsperaEstado = 0f;
        while (tempoEsperaEstado < 1f)
        {
            if (animatorKaya == null) break;
            AnimatorStateInfo estado = animatorKaya.GetCurrentAnimatorStateInfo(0);
            if (estado.IsName("kayamorrendo")) break;
            tempoEsperaEstado += Time.deltaTime;
            yield return null;
        }

        if (animatorKaya != null &&
            animatorKaya.GetCurrentAnimatorStateInfo(0).IsName("kayamorrendo"))
        {
            // Aguarda o fim do estado real, mesmo que a duracao da animacao mude.
            float seguranca = 0f;
            float limite = Mathf.Max(duracaoMorteFallback, 0.5f) + 30f;
            while (seguranca < limite)
            {
                AnimatorStateInfo estado = animatorKaya.GetCurrentAnimatorStateInfo(0);
                if (!estado.IsName("kayamorrendo") || estado.normalizedTime >= 1f)
                    break;

                seguranca += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] Estado 'kayamorrendo' nao encontrado no Animator. Confira o Trigger 'Morrer'.", this);
            yield return new WaitForSeconds(duracaoMorteFallback);
        }

        RenascerDepoisDaMorte();
    }

    private void RenascerDepoisDaMorte()
    {
        // Mantem o save e o checkpoint existentes, apenas adiando a chamada.
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RenascerNoCheckpoint();
            return;
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;
        LevelLoader.CarregarComFade(SceneManager.GetActiveScene().name);
    }

    public void RecuperarVida(float quantidade)
    {
        if (estaMorta || quantidade <= 0f) return;

        vidaAtual = Mathf.Clamp(vidaAtual + quantidade, 0f, vidaMaxima);
        AtualizarUIImediato();

        if (hudJuice != null)
        {
            hudJuice.AtualizarRetrato(vidaAtual / vidaMaxima);
        }

        if (SaveManager.Instance != null)
        {
            float furiaAtual = sistemaFuria != null ? sistemaFuria.FuriaAtual : SaveManager.Instance.GetFuriaKaya();
            SaveManager.Instance.AtualizarStatusKaya(vidaAtual, furiaAtual);
        }
    }

    private void AtualizarUIImediato()
    {
        float pct = vidaAtual / vidaMaxima;
        if (fillVida != null) fillVida.fillAmount = pct;
        if (fillVidaFantasma != null) fillVidaFantasma.fillAmount = pct;
    }
}