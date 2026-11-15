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
    [SerializeField] private Image fillVida;
    [SerializeField] private Image fillVidaFantasma;

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
    [Header("Fixacao dos pes na morte em solo")]
    [Tooltip("Fixa os pes pelo esqueleto, sem transladar a Kaya inteira.")]
    [SerializeField] private bool ancorarPesPelosOssos = true;
    [Range(1, 10)] [SerializeField] private int iteracoesAncoragemPes = 5;
    [Range(2f, 50f)] [SerializeField] private float maximoGrausPorArticulacao = 24f;
    [Min(0.005f)] [SerializeField] private float toleranciaAncoragemPes = 0.04f;

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
    private Vector3 alvoMortePe1;
    private Vector3 alvoMortePe2;
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

        if (SaveManager.Instance != null)
            SaveManager.Instance.ConfirmarRenascimentoInicializado();

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
        if (estaMorta) return;

        if (fillVidaFantasma != null && fillVida != null)
        {
            if (fillVidaFantasma.fillAmount > fillVida.fillAmount)
            {
                fillVidaFantasma.fillAmount -= velocidadeFantasma * Time.deltaTime;
            }
        }

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

        if (travarPosicaoHorizontalNaMorte && posicaoMorteCapturada)
            visualKaya.localPosition = posicaoLocalVisualNaMorte;

        if (respeitarDirecaoAoMorrer)
        {
            Vector3 angulos = visualKaya.localEulerAngles;
            angulos.y = olhandoParaEsquerdaAoMorrer ? 180f : 0f;
            visualKaya.localEulerAngles = angulos;
        }

        if (!morreuApoiadaNoChao || !fixarPesDuranteMorte ||
            !ancorarPesPelosOssos || !referenciaDosPesCapturada)
            return;
        if (peMorte1 != null) FixarPeComIK2D(peMorte1, alvoMortePe1);
        if (peMorte2 != null) FixarPeComIK2D(peMorte2, alvoMortePe2);
    }

    private void FixarPeComIK2D(Transform pe, Vector3 destino)
    {
        Vector2 alvo = destino;
        if (!fixarAlturaDosPes) alvo.y = pe.position.y;
        int iteracoes = Mathf.Clamp(iteracoesAncoragemPes, 1, 10);
        float anguloMaximo = Mathf.Clamp(maximoGrausPorArticulacao, 2f, 50f);
        float tolerancia = Mathf.Max(0.005f, toleranciaAncoragemPes);
        for (int i = 0; i < iteracoes; i++)
        {
            if (Vector2.Distance(pe.position, alvo) <= tolerancia) break;
            Transform articulacao = pe.parent;
            for (int j = 0; j < 3 && articulacao != null;
                 j++, articulacao = articulacao.parent)
            {
                Vector2 atual = (Vector2)pe.position - (Vector2)articulacao.position;
                Vector2 destinoRelativo = alvo - (Vector2)articulacao.position;
                if (atual.sqrMagnitude < 0.000001f ||
                    destinoRelativo.sqrMagnitude < 0.000001f)
                    continue;

                float diferenca = Vector2.SignedAngle(atual, destinoRelativo);
                articulacao.Rotate(Vector3.forward,
                    Mathf.Clamp(diferenca, -anguloMaximo, anguloMaximo),
                    Space.World);
                if (Vector2.Distance(pe.position, alvo) <= tolerancia) break;
            }
        }
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

        PlayerController controle = GetComponent<PlayerController>();
        morreuApoiadaNoChao = DetectarApoioNoChao(controle);

        StopAllCoroutines();
        AplicarCorKaya(Color.white, true);

        if (visualKaya != null)
        {
            olhandoParaEsquerdaAoMorrer =
                Mathf.Abs(Mathf.DeltaAngle(visualKaya.localEulerAngles.y, 180f)) < 90f;
            posicaoLocalVisualNaMorte = visualKaya.localPosition;
            posicaoMorteCapturada = true;
        }

        if (morreuApoiadaNoChao && fixarPesDuranteMorte &&
            (peMorte1 != null || peMorte2 != null))
        {
            posicaoReferenciaDosPes = ObterCentroPesNoMundo();
            if (peMorte1 != null) alvoMortePe1 = peMorte1.position;
            if (peMorte2 != null) alvoMortePe2 = peMorte2.position;
            referenciaDosPesCapturada = true;
        }

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
                corpo.constraints = RigidbodyConstraints2D.FreezeAll;
            }
            else
            {
                corpo.constraints =
                    (restricoesRigidbodyAntesMorte &
                     ~RigidbodyConstraints2D.FreezePositionY) |
                    RigidbodyConstraints2D.FreezePositionX;
                corpo.WakeUp();
            }
        }

        if (audioKaya != null)
            audioKaya.TocarMorte(usarMorteCinematografica);

        if (usarMorteCinematografica)
        {
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
            RenascerDepoisDaMorte();
        }
    }

    private bool DetectarApoioNoChao(PlayerController controle)
    {
        if (controle != null && controle.EstaNoChao)
            return true;

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
            tempoRealDecorrido += Mathf.Min(Time.unscaledDeltaTime, 0.1f);

            float progressoLento = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(
                tempoRealDecorrido / duracaoDesaceleracao));
            Time.timeScale = Mathf.Lerp(
                escalaTempoAntesMorte, Mathf.Clamp(velocidadeMundoNaMorte, 0.05f, 1f),
                progressoLento);
            Time.fixedDeltaTime = passoFisicaAntesMorte * Mathf.Max(0.01f,
                Time.timeScale / Mathf.Max(0.01f, escalaTempoAntesMorte));

            AtualizarCameraLentaNosAudios(
                Time.timeScale / Mathf.Max(0.01f, escalaTempoAntesMorte));

            float progressoFade = Mathf.Clamp01(
                (tempoRealDecorrido - atraso) / duracaoFade);
            if (cortina != null)
                cortina.DefinirEscurecimentoDeMorte(
                    Mathf.SmoothStep(0f, 1f, progressoFade));

            yield return null;
        }

        if (cortina != null) cortina.DefinirEscurecimentoDeMorte(1f);
        RestaurarTempoDaMorte();
        cinematicaMorteEmAndamento = false;
        RenascerDepoisDaMorte();
    }

    private void RestaurarTempoDaMorte()
    {
        RestaurarPitchDosAudios();
        Time.timeScale = Mathf.Max(0.01f, escalaTempoAntesMorte);
        Time.fixedDeltaTime = passoFisicaAntesMorte;
    }

    private void OnDisable()
    {
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