using UnityEngine;

// Controla a transparencia da barra por gatilho, sem desativar a UI
// durante o fade ou destruir imediatamente a barra na derrota do boss.
[DisallowMultipleComponent]
public sealed class BossBarraFade : MonoBehaviour
{
    private CanvasGroup grupo;
    private bool preparado;
    private bool apareceu;
    private bool encerrando;
    private bool destruirAoTerminar;
    private float destino;
    private float duracao = 0.4f;
    private float duracaoSaida = 0.5f;
    private Transform boss;
    private BossVida vida;
    private VidaBossGuerra vidaGuerra;

    public static BossBarraFade Garantir(GameObject barra)
    {
        if (barra == null) return null;
        if (!barra.activeSelf) barra.SetActive(true);

        BossBarraFade componente = barra.GetComponent<BossBarraFade>();
        if (componente == null) componente = barra.AddComponent<BossBarraFade>();
        componente.Preparar();
        return componente;
    }

    private void Preparar()
    {
        if (preparado) return;
        preparado = true;
        grupo = GetComponent<CanvasGroup>();
        if (grupo == null) grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
        destino = 0f;
    }

    public void VincularChefe(Transform alvo, BossVida vidaBoss,
        VidaBossGuerra vidaDoBossGuerra, float fadeSaida)
    {
        Preparar();
        boss = alvo;
        vida = vidaBoss;
        vidaGuerra = vidaDoBossGuerra;
        duracaoSaida = Mathf.Max(0.05f, fadeSaida);
    }

    public void Mostrar(float fadeEntrada)
    {
        Preparar();
        if (encerrando) return;
        apareceu = true;
        destruirAoTerminar = false;
        duracao = Mathf.Max(0.05f, fadeEntrada);
        destino = 1f;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    public void Ocultar(float fadeSaida)
    {
        if (!preparado || encerrando) return;
        destino = 0f;
        duracao = Mathf.Max(0.05f, fadeSaida);
    }

    public void OcultarEDestruir(float fadeSaida)
    {
        if (encerrando) return;
        Preparar();
        encerrando = true;
        destruirAoTerminar = true;
        duracao = Mathf.Max(0.05f, fadeSaida);
        destino = 0f;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!preparado || grupo == null) return;

        // A UI segue viva enquanto o boss e destruido ou faz a animacao final.
        if (apareceu && !encerrando)
        {
            bool derrotado = (vida != null && vida.EstaMorto) ||
                (vidaGuerra != null && vidaGuerra.GetVidaAtual() <= 0f) ||
                (boss == null);
            if (derrotado) OcultarEDestruir(duracaoSaida);
        }

        grupo.alpha = Mathf.MoveTowards(grupo.alpha, destino,
            Time.unscaledDeltaTime / Mathf.Max(0.05f, duracao));

        if (destruirAoTerminar && grupo.alpha <= 0.001f)
            Destroy(gameObject);
    }

    // Mantem o Canvas vivo quando a UI e filha do proprio boss.
    public void LiberarDaHierarquiaDoBoss(Transform raizBoss)
    {
        if (raizBoss == null || !transform.IsChildOf(raizBoss)) return;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && !canvas.transform.IsChildOf(raizBoss))
            transform.SetParent(canvas.transform, true);
        else
            transform.SetParent(null, true);
    }
}
