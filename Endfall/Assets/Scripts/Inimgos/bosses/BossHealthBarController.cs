using UnityEngine;

public class BossHealthBarController : MonoBehaviour
{
    [Header("Configuracoes da UI")]
    public GameObject barraVidaUI;
    public Transform jogador;
    [Tooltip("Opcional. Para fases antigas sem Gatilho Intro Boss.")]
    public bool ativarPorDistanciaSemGatilho = false;
    public float distanciaAtivacao = 10f;
    [Min(0.05f)] public float fadeEntrada = 0.4f;
    [Min(0.05f)] public float fadeSaida = 0.5f;

    private BossBarraFade fade;
    private bool entrouNoGatilho;
    private BossVida vida;
    private VidaBossGuerra vidaGuerra;

    private void Start()
    {
        BuscarJogador();
        vida = GetComponent<BossVida>();
        if (vida == null) vida = GetComponentInParent<BossVida>();
        vidaGuerra = GetComponent<VidaBossGuerra>();
        if (vidaGuerra == null) vidaGuerra = GetComponentInParent<VidaBossGuerra>();

        fade = BossBarraFade.Garantir(barraVidaUI);
        if (fade != null)
            fade.VincularChefe(transform, vida, vidaGuerra, fadeSaida);
    }

    public void AtivarPorGatilho(float duracaoEntrada, float duracaoSaida)
    {
        entrouNoGatilho = true;
        fadeEntrada = Mathf.Max(0.05f, duracaoEntrada);
        fadeSaida = Mathf.Max(0.05f, duracaoSaida);
        if (fade == null)
            fade = BossBarraFade.Garantir(barraVidaUI);
        if (fade != null)
        {
            fade.VincularChefe(transform, vida, vidaGuerra, fadeSaida);
            fade.Mostrar(fadeEntrada);
        }
    }

    private void Update()
    {
        if (fade == null) return;
        if ((vida != null && vida.EstaMorto) ||
            (vidaGuerra != null && vidaGuerra.GetVidaAtual() <= 0f))
        {
            fade.OcultarEDestruir(fadeSaida);
            return;
        }

        if (entrouNoGatilho || !ativarPorDistanciaSemGatilho) return;
        if (jogador == null) BuscarJogador();
        bool mostrar = jogador != null && jogador.gameObject.activeInHierarchy &&
            Vector2.Distance(transform.position, jogador.position) <= distanciaAtivacao;
        if (mostrar) fade.Mostrar(fadeEntrada);
        else fade.Ocultar(fadeSaida);
    }

    private void BuscarJogador()
    {
        if (jogador == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) jogador = p.transform;
        }
    }
}
