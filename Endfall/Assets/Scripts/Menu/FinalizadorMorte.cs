using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

// Cinematica final exclusiva da FaseMorte.
// Mantem a boss e sua animacao vivas ate o fim da fuga da foice.
public sealed class FinalizadorMorte : MonoBehaviour
{
    private const string CenaFinal = "VitoriaFinal";
    private const string NomeEstado = "Base Layer.mortemorre";
    private const string NomeClip = "mortemorre";
    private const string NomeFoice = "foice_0";

    [Header("Camera da cena final")]
    [Min(0.05f)] [SerializeField] private float suavidadeCamera = 0.65f;
    [Min(0f)] [SerializeField] private float velocidadeMaximaCamera = 2400f;
    [Range(0.5f, 1.2f)] [SerializeField] private float zoomDuranteMorte = 0.85f;
    [Range(0.5f, 1.2f)] [SerializeField] private float zoomAoSeguirFoice = 0.95f;

    [Header("Sincronia com a animacao")]
    // mortemorre.anim tem 10s. A foice_0 comeca sua fuga por volta de 4.5s.
    [Min(0f)] [SerializeField] private float iniciarSeguimentoFoice = 4.7f;
    [Min(0.05f)] [SerializeField] private float transicaoParaFoice = 0.9f;
    [Min(0.2f)] [SerializeField] private float iniciarFadeAntesDoFim = 1.1f;

    private static bool agendado;
    public static bool EmAndamento => agendado;

    private Transform boss;
    private Animator animatorMorte;
    private Transform foice;
    private Camera cameraReal;
    private CinemachineBrain cerebro;
    private bool cerebroEstavaAtivo;
    private float zoomInicial;
    private float velocidadeZoom;
    private Vector3 velocidadeCamera;
    private Vector3 centroMorte;
    private float tempoDecorrido;
    private float duracaoAnimacao;
    private bool fadeIniciado;
    private PlayerController controlesKaya;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarAoAbrirPlay()
    {
        agendado = false;
    }

    public static void Agendar(BossVida vidaFinal)
    {
        if (agendado || vidaFinal == null ||
            SceneManager.GetActiveScene().name != "FaseMorte")
            return;

        if (!Application.CanStreamedLevelBeLoaded(CenaFinal))
        {
            Debug.LogError("[EndFall] Adicione VitoriaFinal ao Build Settings.");
            return;
        }

        agendado = true;
        GameObject controlador = new GameObject("CinematicaFinalDaMorte");
        FinalizadorMorte final = controlador.AddComponent<FinalizadorMorte>();
        final.Configurar(vidaFinal.transform);
    }

    private void Configurar(Transform raizBoss)
    {
        boss = raizBoss;
        animatorMorte = boss.GetComponentInChildren<Animator>(true);
        if (animatorMorte != null)
            foice = animatorMorte.transform.Find(NomeFoice);

        // Evita que projeteis ainda vivos causem dano durante o encerramento.
        foreach (CorvoBossProjectile corvo in
                 FindObjectsByType<CorvoBossProjectile>(FindObjectsSortMode.None))
        {
            if (corvo != null) Destroy(corvo.gameObject);
        }

        GameObject kaya = GameObject.FindGameObjectWithTag("Player");
        if (kaya != null)
        {
            controlesKaya = kaya.GetComponent<PlayerController>();
            if (controlesKaya == null)
                controlesKaya = kaya.GetComponentInChildren<PlayerController>();
            if (controlesKaya != null)
                controlesKaya.DefinirBloqueioDialogo(true);
        }

        // Nao congela Time.timeScale: a animacao e a fisica continuam.
        Time.timeScale = 1f;
        AudioListener.pause = false;

        centroMorte = CentroVisualMorte(raizBoss);
        duracaoAnimacao = ObterDuracaoDaAnimacao();

        cameraReal = Camera.main;
        if (cameraReal == null)
            cameraReal = FindFirstObjectByType<Camera>();
        if (cameraReal != null)
        {
            cerebro = cameraReal.GetComponent<CinemachineBrain>();
            if (cerebro != null)
            {
                cerebroEstavaAtivo = cerebro.enabled;
                // Controle temporario exclusivo durante o encerramento.
                // Nenhum prefab ou configuracao permanente de Cinemachine muda.
                cerebro.enabled = false;
            }
            zoomInicial = cameraReal.orthographicSize;
        }

        if (animatorMorte != null)
        {
            animatorMorte.enabled = true;
            animatorMorte.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animatorMorte.speed = 1f;
            animatorMorte.ResetTrigger("AtaqueCorvos");
            animatorMorte.ResetTrigger("InvocarLamentus");
            animatorMorte.ResetTrigger("LancarFoice");
            animatorMorte.ResetTrigger("Aproximar");
            animatorMorte.ResetTrigger("Investida");
            animatorMorte.ResetTrigger("CancelarAtaque");
            animatorMorte.ResetTrigger("Morte");
            int estado = Animator.StringToHash(NomeEstado);
            if (animatorMorte.HasState(0, estado))
                animatorMorte.SetTrigger("Morte");
            else
                Debug.LogWarning("[EndFall] Estado mortemorre nao encontrado no Animator da Morte.", this);
        }
        else
        {
            Debug.LogWarning("[EndFall] Animator da Morte nao encontrado. A transicao final continua.", this);
        }

        if (foice == null)
            Debug.LogWarning("[EndFall] Foice_0 nao encontrada. A camera permanecera focada na Morte.", this);

        StartCoroutine(Encerramento());
    }

    private float ObterDuracaoDaAnimacao()
    {
        if (animatorMorte != null && animatorMorte.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in animatorMorte.runtimeAnimatorController.animationClips)
            {
                if (clip != null && clip.name == NomeClip)
                    return Mathf.Max(0.5f, clip.length);
            }
        }

        // Duracao verificada em Assets/Personagem/morte/mortemorre.anim.
        return 10f;
    }

    private static Vector3 CentroVisualMorte(Transform raiz)
    {
        // Exclui a foice: ela se desloca muito longe do corpo na animacao.
        bool encontrou = false;
        Bounds limites = new Bounds();
        foreach (SpriteRenderer sprite in raiz.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sprite == null || sprite.name.ToLowerInvariant().Contains("foice"))
                continue;

            if (!encontrou)
            {
                limites = sprite.bounds;
                encontrou = true;
            }
            else
                limites.Encapsulate(sprite.bounds);
        }

        return encontrou ? limites.center : raiz.position;
    }

    private Vector3 PosicaoFoice()
    {
        if (foice == null)
            return centroMorte;

        SpriteRenderer desenho = foice.GetComponent<SpriteRenderer>();
        return desenho != null && desenho.enabled ? desenho.bounds.center : foice.position;
    }

    private void LateUpdate()
    {
        if (cameraReal == null || !agendado ||
            SceneManager.GetActiveScene().name != "FaseMorte")
            return;

        float tempo = Time.unscaledDeltaTime;
        float aproximacao = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(iniciarSeguimentoFoice,
                iniciarSeguimentoFoice + transicaoParaFoice, tempoDecorrido));

        Vector3 objetivo = Vector3.Lerp(centroMorte, PosicaoFoice(),
            foice != null ? aproximacao : 0f);
        objetivo.z = cameraReal.transform.position.z;

        cameraReal.transform.position = Vector3.SmoothDamp(
            cameraReal.transform.position, objetivo, ref velocidadeCamera,
            Mathf.Max(0.05f, suavidadeCamera),
            Mathf.Max(1f, velocidadeMaximaCamera), Mathf.Max(0.0001f, tempo));

        if (cameraReal.orthographic)
        {
            float multiplicador = Mathf.Lerp(zoomDuranteMorte,
                zoomAoSeguirFoice, aproximacao);
            cameraReal.orthographicSize = Mathf.SmoothDamp(
                cameraReal.orthographicSize, zoomInicial * multiplicador,
                ref velocidadeZoom, 0.9f, Mathf.Infinity,
                Mathf.Max(0.0001f, tempo));
        }
    }

    private IEnumerator Encerramento()
    {
        tempoDecorrido = 0f;
        fadeIniciado = false;
        while (SceneManager.GetActiveScene().name == "FaseMorte" &&
               tempoDecorrido < duracaoAnimacao + 0.1f)
        {
            tempoDecorrido += Time.unscaledDeltaTime;

            // A animacao original esta marcada como loop. Pausamos no
            // ultimo quadro para evitar voltar ao primeiro durante o fade.
            if (animatorMorte != null &&
                tempoDecorrido >= duracaoAnimacao - 0.06f)
                animatorMorte.speed = 0f;

            if (!fadeIniciado &&
                tempoDecorrido >= Mathf.Max(0f,
                    duracaoAnimacao - iniciarFadeAntesDoFim))
            {
                fadeIniciado = true;
                LevelLoader.CarregarComFade(CenaFinal);
            }

            yield return null;
        }

        if (!fadeIniciado && SceneManager.GetActiveScene().name == "FaseMorte")
            LevelLoader.CarregarComFade(CenaFinal);
    }

    private void OnDestroy()
    {
        if (cerebro != null && SceneManager.GetActiveScene().name == "FaseMorte")
            cerebro.enabled = cerebroEstavaAtivo;

        agendado = false;
    }
}
