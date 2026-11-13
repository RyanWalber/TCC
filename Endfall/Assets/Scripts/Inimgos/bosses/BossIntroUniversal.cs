using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BossIntroUniversal : MonoBehaviour
{
    [Header("ALVO DO BOSS")]
    [Tooltip("Arraste o objeto do Boss aqui pelo Inspector")]
    public Transform bossTransform;

    [Header("CONFIGURAÇÃO DA INTRO")]
    [Tooltip("Tempo em segundos que a câmera fica parada focada no Boss")]
    public float tempoFocoNoBoss = 2f;

    [Header("COMBATE POR GATILHO")]
    [Tooltip("Quanto tempo depois de pisar no gatilho o boss pode atacar. A introducao da camera tem prioridade, para evitar ataques durante o foco.")]
    [Min(0f)] public float atrasoParaAtacar = 3f;

    [Header("BARRA DE VIDA DO BOSS")]
    [Tooltip("Opcional. Arraste o painel INTEIRO da barra de vida. Se vazio, procura a UI associada a BossVida ou BossHealthBarController.")]
    [SerializeField] private GameObject painelBarraVida;
    [Min(0.05f)] [SerializeField] private float fadeEntradaBarra = 0.4f;
    [Min(0.05f)] [SerializeField] private float fadeSaidaBarra = 0.5f;

    [Header("ÁREA DO GATILHO (GIZMO)")]
    public Vector2 tamanhoGatilho = new Vector2(4f, 6f);
    public Color corGizmo = Color.cyan;

    private BoxCollider2D col;
    private Transform playerTransform;
    private Component cinemachineCam;
    private bool jaDisparou = false;
    private BossBarraFade fadeBarra;
    private BossHealthBarController controleBarra;

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = tamanhoGatilho;
    }

    private void Start()
    {
        PrepararBarraBoss();

        // 1. Localiza a Kaya pela Tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        // 2. Localiza o objeto CinemachineCamera na Hierarchy
        GameObject cineObj = GameObject.Find("CinemachineCamera") ?? GameObject.Find("CM vcam1");
        if (cineObj == null)
        {
            var todosObjetos = FindObjectsOfType<GameObject>();
            foreach (var obj in todosObjetos)
            {
                if (obj.name.Contains("Cinemachine") || obj.name.Contains("CM"))
                {
                    cineObj = obj;
                    break;
                }
            }
        }

        if (cineObj != null)
        {
            foreach (var comp in cineObj.GetComponents<Component>())
            {
                if (comp != null && comp.GetType().Name.Contains("Cinemachine"))
                {
                    cinemachineCam = comp;
                    break;
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (jaDisparou) return;

        if (collision.CompareTag("Player"))
        {
            jaDisparou = true;

            // Inicia a trilha da batalha no gatilho da introducao, sem
            // depender da distancia da Kaya ate o boss.
            KayaAudioManager musica = collision.GetComponentInParent<KayaAudioManager>();
            if (musica == null && playerTransform != null)
                musica = playerTransform.GetComponentInParent<KayaAudioManager>();
            if (musica != null && bossTransform != null)
                musica.IniciarCombateBoss(bossTransform);

            // O estado de combate fica no boss: este gatilho pode ser
            // desativado ao fim da introducao sem cancelar os ataques.
            AtivarChefeComAtraso();
            if (fadeBarra == null) PrepararBarraBoss();
            if (fadeBarra != null) fadeBarra.Mostrar(fadeEntradaBarra);
            if (controleBarra != null)
                controleBarra.AtivarPorGatilho(fadeEntradaBarra, fadeSaidaBarra);

            StartCoroutine(RotinaApresentacaoBoss());
        }
    }


    private static T EncontrarNoChefe<T>(Transform alvo) where T : Component
    {
        if (alvo == null) return null;
        T componente = alvo.GetComponent<T>();
        if (componente == null) componente = alvo.GetComponentInParent<T>();
        if (componente == null) componente = alvo.GetComponentInChildren<T>(true);
        return componente;
    }

    private void AtivarChefeComAtraso()
    {
        if (bossTransform == null) return;
        // Quando a camera ainda estiver focada no boss, nao permitir
        // dano antecipado. O atraso configurado comeca no gatilho.
        float espera = Mathf.Max(Mathf.Max(0f, atrasoParaAtacar),
            Mathf.Max(0f, tempoFocoNoBoss) + 1.2f);

        BossFomeController fome = EncontrarNoChefe<BossFomeController>(bossTransform);
        if (fome != null) fome.AtivarPeloGatilho(espera);

        BossPeste peste = EncontrarNoChefe<BossPeste>(bossTransform);
        if (peste != null) peste.AtivarPeloGatilho(espera);

        BossGuerraController guerra = EncontrarNoChefe<BossGuerraController>(bossTransform);
        if (guerra != null) guerra.AtivarPeloGatilho(espera);

        MorteBossControllerGameplay morte = EncontrarNoChefe<MorteBossControllerGameplay>(bossTransform);
        if (morte != null) morte.AtivarPeloGatilho(espera);
    }

    private void PrepararBarraBoss()
    {
        if (bossTransform == null) return;

        BossVida vida = EncontrarNoChefe<BossVida>(bossTransform);
        VidaBossGuerra vidaGuerra = EncontrarNoChefe<VidaBossGuerra>(bossTransform);
        controleBarra = EncontrarNoChefe<BossHealthBarController>(bossTransform);
        if (painelBarraVida == null && vida != null)
            painelBarraVida = vida.ObterPainelBarra();
        if (painelBarraVida == null && controleBarra != null)
            painelBarraVida = controleBarra.barraVidaUI;

        if (painelBarraVida == null)
        {
            Debug.LogWarning("[INTRO BOSS] Barra nao localizada automaticamente. " +
                "Arraste o painel inteiro em 'Painel Barra Vida' no Inspector do gatilho.", this);
            return;
        }

        fadeBarra = BossBarraFade.Garantir(painelBarraVida);
        if (fadeBarra != null)
            fadeBarra.VincularChefe(bossTransform, vida, vidaGuerra, fadeSaidaBarra);
    }

    private IEnumerator RotinaApresentacaoBoss()
    {
        if (cinemachineCam == null || playerTransform == null || bossTransform == null)
        {
            Debug.LogError("[INTRO BOSS] Certifique-se de que arrastou o Boss para o Inspector do gatilho!");
            yield break;
        }

        // Para o movimento da Kaya
        Rigidbody2D rbPlayer = playerTransform.GetComponent<Rigidbody2D>();
        if (rbPlayer != null) rbPlayer.linearVelocity = Vector2.zero;

        // 1. MUTA O ALVO DA CINEMACHINE PARA O BOSS
        AlterarAlvoCinemachine(bossTransform);

        // 2. TEMPO DE ESPERA (Viagem da câmara + Foco no Boss)
        yield return new WaitForSeconds(tempoFocoNoBoss + 1.2f);

        // 3. VOLTA O ALVO DA CINEMACHINE PARA A KAYA
        AlterarAlvoCinemachine(playerTransform);

        // Desativa este gatilho
        gameObject.SetActive(false);
    }

    private void AlterarAlvoCinemachine(Transform novoAlvo)
    {
        if (cinemachineCam == null) return;

        var tipo = cinemachineCam.GetType();

        // Cinemachine v2 (m_Follow / m_LookAt)
        var fieldFollow = tipo.GetField("m_Follow");
        if (fieldFollow != null) fieldFollow.SetValue(cinemachineCam, novoAlvo);

        var fieldLookAt = tipo.GetField("m_LookAt");
        if (fieldLookAt != null) fieldLookAt.SetValue(cinemachineCam, novoAlvo);

        // Propriedade Follow
        var propFollow = tipo.GetProperty("Follow");
        if (propFollow != null && propFollow.CanWrite) propFollow.SetValue(cinemachineCam, novoAlvo);

        // Unity 6 Cinemachine 3.x (Target.TrackingTarget)
        var propTarget = tipo.GetProperty("Target");
        if (propTarget != null)
        {
            var targetObj = propTarget.GetValue(cinemachineCam);
            if (targetObj != null)
            {
                var trackingProp = targetObj.GetType().GetProperty("TrackingTarget");
                if (trackingProp != null && trackingProp.CanWrite)
                {
                    trackingProp.SetValue(targetObj, novoAlvo);
                }
            }
        }
    }

    private void OnValidate()
    {
        if (col == null) col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            col.size = tamanhoGatilho;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = corGizmo;
        Gizmos.DrawWireCube(transform.position, (Vector3)tamanhoGatilho);
    }
}