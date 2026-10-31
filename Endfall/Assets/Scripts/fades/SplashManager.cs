using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Abertura cinematografica: executada somente quando Splash e carregada.
public class SplashManager : MonoBehaviour
{
    [Header("Cena seguinte")]
    [SerializeField] private string nomeCenaMenu = "Menu";
    [Header("Titulo EndFall")]
    [SerializeField] private string titulo = "E N D F A L L";
    [SerializeField] private TMP_FontAsset fonteTitulo;
    [SerializeField] private float tamanhoFonte = 110f;
    [SerializeField] private float tempoFadeIn = 1f;
    [Min(0f)] [SerializeField] private float atrasoVozAposFadeIn = 0.35f;
    [SerializeField] private float tempoPermanencia = 1.5f;
    [SerializeField] private float tempoFadeOut = 0.8f;
    [Header("Trilha continua: Splash -> Menu -> Bem-vindo -> Fragmento I -> Fome")]
    [Tooltip("Uma faixa em loop: nao reinicia quando muda de cena.")]
    [SerializeField] private AudioClip musicaDeMenu;
    [Range(0f, 1f)] [SerializeField] private float volumeMusicaMenu = 0.65f;
    [Range(0f, 1f)] [SerializeField] private float volumeMusicaNarrativa = 0.18f;
    [Min(0.1f)] [SerializeField] private float fadeEntradaMusica = 2f;

    [Header("Som dos botoes (menu, slots e pausa)")]
    [SerializeField] private AudioClip somCliqueBotoes;
    [Range(0f, 1f)] [SerializeField] private float volumeCliqueBotoes = 0.65f;

    [Header("Narracao (adicione depois)")]
    [SerializeField] private AudioClip vozEndFall;
    [Range(0f, 1f)] [SerializeField] private float volumeVoz = 1f;

    private IEnumerator Start()
    {
        // O AudioSource global permanece entre as cenas, em loop continuo.
        AudioManagerGlobal audioGlobal = AudioManagerGlobal.Garantir();
        audioGlobal.Configurar(musicaDeMenu, somCliqueBotoes, volumeMusicaMenu,
            volumeMusicaNarrativa, volumeCliqueBotoes, fadeEntradaMusica);
        audioGlobal.IniciarNaSplash();

        // Nao ha dependencia de prefab nem de um TMP arrastado no Inspector.
        GameObject canvasObj = new GameObject("AberturaEndFall", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 500;
        GameObject textoObj = new GameObject("TituloEndFall", typeof(RectTransform), typeof(TextMeshProUGUI));
        textoObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rect = textoObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        TextMeshProUGUI texto = textoObj.GetComponent<TextMeshProUGUI>();
        texto.text = titulo;
        if (fonteTitulo != null) texto.font = fonteTitulo;
        texto.fontSize = tamanhoFonte;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 28f;
        texto.fontSizeMax = tamanhoFonte;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;
        Color baseCor = new Color(0.91f, 0.86f, 0.79f, 1f);
        texto.color = new Color(baseCor.r, baseCor.g, baseCor.b, 0f);

        AudioSource voz = gameObject.AddComponent<AudioSource>();
        voz.playOnAwake = false;
        voz.spatialBlend = 0f;
        voz.volume = volumeVoz;
        voz.clip = vozEndFall;

        // Espera o Fade In global da Splash para evitar titulo oculto.
        while (LevelLoader.Instance != null && LevelLoader.Instance.EstaEmTransicao)
            yield return null;

        yield return AnimarTitulo(texto, baseCor, 0f, 1f, tempoFadeIn);
        // A voz so comeca quando o titulo ja esta 100% visivel.
        // Pequena pausa opcional para deixar a abertura mais cinematografica.
        if (atrasoVozAposFadeIn > 0f)
            yield return new WaitForSecondsRealtime(atrasoVozAposFadeIn);
        if (vozEndFall != null) voz.Play();
        float espera = Mathf.Max(0f, tempoPermanencia);
        if (vozEndFall != null) espera = Mathf.Max(espera, vozEndFall.length);
        yield return new WaitForSecondsRealtime(espera);
        yield return AnimarTitulo(texto, baseCor, 1f, 0f, tempoFadeOut);
        LevelLoader.CarregarComFade(nomeCenaMenu);
    }

    private IEnumerator AnimarTitulo(TMP_Text texto, Color cor, float inicio, float fim, float duracao)
    {
        float t = 0f;
        duracao = Mathf.Max(0.05f, duracao);
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(inicio, fim, Mathf.Clamp01(t / duracao));
            texto.color = new Color(cor.r, cor.g, cor.b, a);
            yield return null;
        }
        texto.color = new Color(cor.r, cor.g, cor.b, fim);
    }
}
