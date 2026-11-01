using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Abertura cinematografica: executada somente quando Splash e carregada.
public class SplashManager : MonoBehaviour
{
    [Header("Cena seguinte")]
    [SerializeField] private string nomeCenaMenu = "Menu";
    [Header("Primeira tela - Carcara Producoes")]
    [Tooltip("Opcional: se vazio, carrega Resources/CarcaraProducoes.png automaticamente.")]
    [SerializeField] private Texture2D imagemCarcara;
    [Min(0.05f)] [SerializeField] private float fadeEntradaCarcara = 0.9f;
    [Min(0f)] [SerializeField] private float permanenciaCarcara = 2f;
    [Min(0.05f)] [SerializeField] private float fadeSaidaCarcara = 0.8f;

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

    [Header("Cursores do jogo (opcionais se usar Resources/Cursores)")]
    [Tooltip("PNG transparente para Splash, Menu, Pausa e Explicacao.")]
    [SerializeField] private Texture2D cursorMenu;
    [Tooltip("PNG transparente de mira para FaseFome, FasePeste, FaseGuerra e FaseMorte.")]
    [SerializeField] private Texture2D cursorMira;
    [Tooltip("Posicao de clique em pixels, a partir do canto superior esquerdo da seta.")]
    [SerializeField] private Vector2 pontoCliqueSeta = Vector2.zero;
    [Tooltip("Offset da mira em pixels em relacao ao centro: X para a direita, Y para baixo, no hotspot.")]
    [SerializeField] private Vector2 offsetHotspotMira = Vector2.zero;

    [Header("Narracao (adicione depois)")]
    [SerializeField] private AudioClip vozEndFall;
    [Range(0f, 1f)] [SerializeField] private float volumeVoz = 1f;

    private IEnumerator Start()
    {
        // Um so cursor persiste em todas as cenas; no jogo vira mira.
        CursorManagerGlobal.Garantir().Configurar(cursorMenu, cursorMira, pontoCliqueSeta, offsetHotspotMira);

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
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Fundo preto e logo no mesmo Canvas, antes de mostrar o titulo EndFall.
        GameObject fundoObj = new GameObject("FundoPretoAbertura", typeof(RectTransform), typeof(Image));
        fundoObj.transform.SetParent(canvasObj.transform, false);
        RectTransform fundoRect = fundoObj.GetComponent<RectTransform>();
        fundoRect.anchorMin = Vector2.zero;
        fundoRect.anchorMax = Vector2.one;
        fundoRect.offsetMin = fundoRect.offsetMax = Vector2.zero;
        Image fundo = fundoObj.GetComponent<Image>();
        fundo.color = Color.black;
        fundo.raycastTarget = false;

        Texture2D logo = imagemCarcara != null
            ? imagemCarcara
            : Resources.Load<Texture2D>("CarcaraProducoes");

        GameObject painelCarcara = new GameObject("PrimeiraTelaCarcara", typeof(RectTransform), typeof(CanvasGroup));
        painelCarcara.transform.SetParent(canvasObj.transform, false);
        RectTransform painelRect = painelCarcara.GetComponent<RectTransform>();
        painelRect.anchorMin = painelRect.anchorMax = new Vector2(0.5f, 0.5f);
        painelRect.anchoredPosition = Vector2.zero;
        painelRect.sizeDelta = new Vector2(1160f, 840f);
        CanvasGroup grupoCarcara = painelCarcara.GetComponent<CanvasGroup>();
        grupoCarcara.alpha = 0f;
        grupoCarcara.interactable = false;
        grupoCarcara.blocksRaycasts = false;

        if (logo != null)
        {
            GameObject imagemObj = new GameObject("ImagemCarcaraProducoes", typeof(RectTransform), typeof(RawImage));
            imagemObj.transform.SetParent(painelCarcara.transform, false);
            RectTransform imagemRect = imagemObj.GetComponent<RectTransform>();
            imagemRect.anchorMin = imagemRect.anchorMax = new Vector2(0.5f, 0.5f);
            imagemRect.anchoredPosition = Vector2.zero;
            float proporcao = (float)logo.width / logo.height;
            float largura = Mathf.Min(1160f, 840f * proporcao);
            imagemRect.sizeDelta = new Vector2(largura, largura / proporcao);
            RawImage imagem = imagemObj.GetComponent<RawImage>();
            imagem.texture = logo;
            imagem.color = Color.white;
            imagem.raycastTarget = false;
        }
        else
        {
            Debug.LogWarning("[Splash] Imagem ausente: coloque CarcaraProducoes.png em Assets/Resources/.");
            GameObject fallback = new GameObject("TextoCarcaraProvisorio", typeof(RectTransform), typeof(TextMeshProUGUI));
            fallback.transform.SetParent(painelCarcara.transform, false);
            RectTransform fallbackRect = fallback.GetComponent<RectTransform>();
            fallbackRect.anchorMin = Vector2.zero;
            fallbackRect.anchorMax = Vector2.one;
            fallbackRect.offsetMin = fallbackRect.offsetMax = Vector2.zero;
            TextMeshProUGUI textoCarcara = fallback.GetComponent<TextMeshProUGUI>();
            textoCarcara.text = "CARCARÁ\nPRODUÇÕES";
            textoCarcara.fontSize = 100;
            textoCarcara.enableAutoSizing = true;
            textoCarcara.fontSizeMin = 32;
            textoCarcara.fontSizeMax = 100;
            textoCarcara.alignment = TextAlignmentOptions.Center;
            textoCarcara.color = new Color(1f, 0.44f, 0.07f, 1f);
            textoCarcara.raycastTarget = false;
        }

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

        // Primeira tela obrigatoria: estúdio antes do nome do jogo.
        yield return AnimarOpacidade(grupoCarcara, 0f, 1f, fadeEntradaCarcara);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, permanenciaCarcara));
        yield return AnimarOpacidade(grupoCarcara, 1f, 0f, fadeSaidaCarcara);
        painelCarcara.SetActive(false);

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

    private IEnumerator AnimarOpacidade(CanvasGroup grupo, float inicio, float fim, float duracao)
    {
        float t = 0f;
        duracao = Mathf.Max(0.05f, duracao);
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(inicio, fim, Mathf.Clamp01(t / duracao));
            yield return null;
        }
        grupo.alpha = fim;
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
