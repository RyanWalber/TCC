using UnityEngine;

public class MiraMouse : MonoBehaviour
{
    [Header("Configuracao do Corpo")]
    [SerializeField] private Transform aniKaya;
    [SerializeField] private float ajusteCentroX = 0f;

    [Header("Mecanica estilo Plazma Burst")]
    [SerializeField] private float tempoManterMira = 0.4f;
    [SerializeField] private float velocidadeTransicao = 12f;

    [Header("Estabilidade visual da mira")]
    [Tooltip("Corrige o retorno do braco para a pose da animacao, sem limitar a mira em 360 graus. Desmarque para usar o comportamento anterior.")]
    [SerializeField] private bool estabilizarPoseBraco = true;
    [Tooltip("Evita que a Kaya troque de lado repetidamente com o cursor perto do centro.")]
    [Min(0f)] [SerializeField] private float margemTrocaDirecao = 3f;

    [Header("Pose visual dos bracos")]
    [Tooltip("Ajuste estetico dos bracos nos angulos extremos. Nao altera o disparo. Desmarque para voltar ao visual anterior.")]
    [SerializeField] private bool corrigirPoseExtrema = true;
    [Tooltip("Zero desliga a correcao; um usa a intensidade maxima configurada.")]
    [Range(0f, 1f)] [SerializeField] private float intensidadePoseExtrema = 0.8f;
    [Range(20f, 80f)] [SerializeField] private float anguloInicioCorrecao = 48f;
    [Range(0f, 25f)] [SerializeField] private float correcaoBracoFrente = 16f;
    [Range(0f, 25f)] [SerializeField] private float correcaoBracoTras = 11f;

    private Camera cameraPrincipal;
    private PlayerHealth vidaKaya;
    private PlayerController controleKaya;
    private bool orientacaoCongeladaNaMorte;
    private Vector3 posicaoOriginalKaya;
    private Transform centroDoPersonagem;

    private float timerTiro = 0f;
    private float pesoMira = 0f;
    private float anguloMiraSuavizado;
    private bool estavaMirando;
    private bool olhandoEsquerdaAntesDoDialogo;

    private Quaternion rotacaoNeutraBraco;
    private Quaternion ultimaRotacaoAplicada;
    private bool rotacaoAplicadaNoFrameAnterior;

    // Hierarquias de bracos independentes do Ponto De Disparo.
    private EstadoBraco bracoFrente;
    private EstadoBraco bracoTras;

    private sealed class EstadoBraco
    {
        public Transform alvo;
        public Vector3 basePosicao;
        public Quaternion baseRotacao;
        public Vector3 ultimaPosicao;
        public Quaternion ultimaRotacao;
        public bool alterado;

        public EstadoBraco(Transform transformBraco)
        {
            alvo = transformBraco;
            basePosicao = alvo.localPosition;
            baseRotacao = alvo.localRotation;
            ultimaPosicao = basePosicao;
            ultimaRotacao = baseRotacao;
        }
    }

    void Start()
    {
        cameraPrincipal = Camera.main;
        vidaKaya = GetComponentInParent<PlayerHealth>();
        controleKaya = GetComponentInParent<PlayerController>();
        centroDoPersonagem = transform.root;
        rotacaoNeutraBraco = transform.localRotation;
        ultimaRotacaoAplicada = rotacaoNeutraBraco;

        // Caminhos confirmados no prefab usado pela Kaya.
        Transform frente = transform.Find("kayaseparada_4");
        Transform tras = transform.Find("kayaseparada_3");
        if (frente != null && tras != null)
        {
            bracoFrente = new EstadoBraco(frente);
            bracoTras = new EstadoBraco(tras);
        }
        else
        {
            // Prefabs alternativos nao recebem nenhuma modificacao.
            corrigirPoseExtrema = false;
        }

        if (aniKaya == null && transform.parent != null)
        {
            aniKaya = transform.parent;
        }

        if (aniKaya != null)
        {
            posicaoOriginalKaya = aniKaya.localPosition;
            olhandoEsquerdaAntesDoDialogo =
                Mathf.Abs(Mathf.DeltaAngle(aniKaya.localEulerAngles.y, 180f)) < 90f;
        }
    }

    void LateUpdate()
    {
        // Nao sobrescreve a rotacao do objeto Ani Kaya quando a animacao
        // de morte o coloca na horizontal. A mira continua normal em vida.
        if (vidaKaya != null && vidaKaya.VidaAtual <= 0f)
        {
            if (!orientacaoCongeladaNaMorte)
            {
                // Recoloca a pose da arma sob controle exclusivo da animacao.
                timerTiro = 0f;
                pesoMira = 0f;
                estavaMirando = false;
                orientacaoCongeladaNaMorte = true;
                rotacaoAplicadaNoFrameAnterior = false;
            }
            RestaurarBracos();
            return;
        }

        if (controleKaya != null && controleKaya.ControlesBloqueadosPorDialogo)
        {
            // O Animator pode alterar a POSICAO do objeto Ani Kaya durante
            // a conversa. Antes, o retorno antecipado pulava a correcao
            // visual feita normalmente abaixo; por isso o desenho subia
            // ou se deslocava e voltava ao lugar no final do dialogo.
            // Corrige so o visual, sem mover o Rigidbody2D nem o player.
            if (aniKaya != null)
            {
                aniKaya.localRotation = Quaternion.Euler(
                    0f, olhandoEsquerdaAntesDoDialogo ? 180f : 0f, 0f);
                aniKaya.localPosition = olhandoEsquerdaAntesDoDialogo
                    ? new Vector3(posicaoOriginalKaya.x + ajusteCentroX,
                        posicaoOriginalKaya.y, posicaoOriginalKaya.z)
                    : posicaoOriginalKaya;
            }

            // Continua bloqueando mira/tiro e evita manter uma pose
            // aplicada pela mira quando a conversa comeca.
            timerTiro = 0f;
            pesoMira = 0f;
            estavaMirando = false;
            if (estabilizarPoseBraco && rotacaoAplicadaNoFrameAnterior &&
                Quaternion.Angle(transform.localRotation, ultimaRotacaoAplicada) < 0.1f)
                transform.localRotation = rotacaoNeutraBraco;
            rotacaoAplicadaNoFrameAnterior = false;
            CorrigirBracos(0f, 0f);
            return;
        }

        if (cameraPrincipal == null || aniKaya == null) return;

        Quaternion rotacaoAnimacao = transform.localRotation;

        if (estabilizarPoseBraco)
        {
            // No frame anterior a mira pode ter escrito no mesmo Transform.
            // Se o Animator nao o modificou, nao reutiliza esse angulo
            // artificial como a pose base do proximo frame.
            bool aindaEhRotacaoDaMira = rotacaoAplicadaNoFrameAnterior &&
                Quaternion.Angle(rotacaoAnimacao, ultimaRotacaoAplicada) < 0.1f;

            if (aindaEhRotacaoDaMira)
                rotacaoAnimacao = rotacaoNeutraBraco;
            else
                rotacaoNeutraBraco = rotacaoAnimacao;
        }

        Vector3 posicaoMouseTela = Input.mousePosition;
        Vector3 posicaoMouseMundo = cameraPrincipal.ScreenToWorldPoint(new Vector3(
            posicaoMouseTela.x,
            posicaoMouseTela.y,
            -cameraPrincipal.transform.position.z
        ));

        bool olhandoEsquerda = posicaoMouseMundo.x < centroDoPersonagem.position.x;
        if (estabilizarPoseBraco)
        {
            // Pequena histerese apenas na VIRADA do corpo. A direcao
            // de mira e de disparo continua livre nos 360 graus.
            float distanciaHorizontal = posicaoMouseMundo.x - centroDoPersonagem.position.x;
            if (Mathf.Abs(distanciaHorizontal) <= margemTrocaDirecao)
                olhandoEsquerda = olhandoEsquerdaAntesDoDialogo;
        }
        olhandoEsquerdaAntesDoDialogo = olhandoEsquerda;

        if (olhandoEsquerda)
        {
            aniKaya.localRotation = Quaternion.Euler(0f, 180f, 0f);
            aniKaya.localPosition = new Vector3(posicaoOriginalKaya.x + ajusteCentroX, posicaoOriginalKaya.y, posicaoOriginalKaya.z);
        }
        else
        {
            aniKaya.localRotation = Quaternion.Euler(0f, 0f, 0f);
            aniKaya.localPosition = posicaoOriginalKaya;
        }

        bool apertandoAtirar = Input.GetMouseButton(0);

        if (apertandoAtirar)
        {
            timerTiro = tempoManterMira;
        }
        else if (timerTiro > 0)
        {
            timerTiro -= Time.deltaTime;
        }

        bool miraAtiva = apertandoAtirar || timerTiro > 0;
        float pesoAlvo = miraAtiva ? 1f : 0f;
        float sensibilidade = ConfiguracoesJogo.SensibilidadeMira;
        pesoMira = Mathf.MoveTowards(pesoMira, pesoAlvo,
            Time.deltaTime * velocidadeTransicao * sensibilidade);

        if (pesoMira <= 0f)
        {
            estavaMirando = false;
            transform.localRotation = rotacaoAnimacao;
            ultimaRotacaoAplicada = transform.localRotation;
            rotacaoAplicadaNoFrameAnterior = estabilizarPoseBraco;
            CorrigirBracos(0f, 0f);
            return;
        }

        Vector2 direcaoMundo = posicaoMouseMundo - transform.position;
        float anguloMundo = Mathf.Atan2(direcaoMundo.y, direcaoMundo.x) * Mathf.Rad2Deg;
        float anguloMira = olhandoEsquerda ? (180f - anguloMundo) : anguloMundo;

        // A sensibilidade regula a velocidade de resposta angular da mira 2D.
        // A posicao do cursor e o virar da Kaya continuam normais.
        if (!estavaMirando)
            anguloMiraSuavizado = anguloMira;
        else
            anguloMiraSuavizado = Mathf.MoveTowardsAngle(anguloMiraSuavizado,
                anguloMira, Mathf.Max(120f, velocidadeTransicao * 60f) *
                sensibilidade * Time.deltaTime);
        estavaMirando = true;
        Quaternion rotacaoMira = Quaternion.Euler(0f, 0f, anguloMiraSuavizado);

        transform.localRotation = Quaternion.Slerp(rotacaoAnimacao, rotacaoMira, pesoMira);
        ultimaRotacaoAplicada = transform.localRotation;
        rotacaoAplicadaNoFrameAnterior = estabilizarPoseBraco;
        CorrigirBracos(anguloMiraSuavizado, pesoMira);
    }

    private void CorrigirBracos(float angulo, float peso)
    {
        if (bracoFrente == null || bracoTras == null)
            return;

        float intensidade = 0f;
        if (corrigirPoseExtrema && intensidadePoseExtrema > 0f)
        {
            float inclinacao = Mathf.Abs(Mathf.DeltaAngle(0f, angulo));
            float t = Mathf.InverseLerp(anguloInicioCorrecao, 90f, inclinacao);
            t = t * t * (3f - 2f * t); // Smoothstep sem animacoes novas.
            intensidade = t * Mathf.Clamp01(peso) * intensidadePoseExtrema;
        }

        // Counter-rotation moderada do braco nos angulos acentuados.
        // O Ponto De Disparo e irmao dos bracos; sua rotacao nao muda.
        float sinal = Mathf.Sign(Mathf.DeltaAngle(0f, angulo));
        AplicarPose(bracoFrente, -sinal * correcaoBracoFrente * intensidade,
            new Vector3(1.2f * intensidade, 0.2f * intensidade, 0f));
        AplicarPose(bracoTras, -sinal * correcaoBracoTras * intensidade,
            new Vector3(0.65f * intensidade, 0.1f * intensidade, 0f));
    }

    private static void AplicarPose(EstadoBraco braco, float graus, Vector3 deslocamento)
    {
        if (braco == null || braco.alvo == null) return;

        // Se o Animator nao escreveu uma nova pose no frame, removemos
        // implicitamente nossa contribuicao anterior para nao acumular giro.
        bool aindaEhNossoValor = braco.alterado &&
            (braco.alvo.localPosition - braco.ultimaPosicao).sqrMagnitude < 0.0001f &&
            Quaternion.Angle(braco.alvo.localRotation, braco.ultimaRotacao) < 0.1f;

        if (!aindaEhNossoValor)
        {
            braco.basePosicao = braco.alvo.localPosition;
            braco.baseRotacao = braco.alvo.localRotation;
        }

        braco.alvo.localPosition = braco.basePosicao + deslocamento;
        braco.alvo.localRotation = braco.baseRotacao * Quaternion.Euler(0f, 0f, graus);
        braco.ultimaPosicao = braco.alvo.localPosition;
        braco.ultimaRotacao = braco.alvo.localRotation;
        braco.alterado = true;
    }

    private void RestaurarBracos()
    {
        RestaurarBraco(bracoFrente);
        RestaurarBraco(bracoTras);
    }

    private static void RestaurarBraco(EstadoBraco braco)
    {
        if (braco == null || braco.alvo == null || !braco.alterado)
            return;

        // Se a animacao de morte mudou o braco, nao sobrescreve a nova pose.
        bool aindaEhNossoValor =
            (braco.alvo.localPosition - braco.ultimaPosicao).sqrMagnitude < 0.0001f &&
            Quaternion.Angle(braco.alvo.localRotation, braco.ultimaRotacao) < 0.1f;
        if (aindaEhNossoValor)
        {
            braco.alvo.localPosition = braco.basePosicao;
            braco.alvo.localRotation = braco.baseRotacao;
        }
        braco.alterado = false;
    }

    private void OnDisable()
    {
        RestaurarBracos();
    }
}