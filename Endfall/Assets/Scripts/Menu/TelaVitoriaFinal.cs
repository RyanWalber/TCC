using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Encerramento cinematografico auto suficiente: carta -> titulo -> creditos -> menu.
// A interface e criada na propria cena, sem exigir Canvas montado manualmente.
public sealed class TelaVitoriaFinal : MonoBehaviour
{
    [Header("Identidade visual da abertura")]
    [SerializeField] private string titulo = "E N D F A L L";
    [SerializeField] private TMP_FontAsset fonteTitulo;
    [SerializeField] private TMP_FontAsset fonteTexto;
    [SerializeField] private AudioClip vozEndFall;
    [SerializeField] private AudioClip musicaFinal;
    [Range(0f, 1f)] [SerializeField] private float volumeMusica = 0.16f;
    [Range(0f, 1f)] [SerializeField] private float volumeVoz = 0.85f;

    [Header("Ultima carta de Cael")]
    [TextArea(9, 17)]
    [SerializeField] private string cartaFinal =
        "Kaya,\n\n" +
        "Então você conseguiu. Derrubou a própria Morte. Confesso que não esperava tanto de uma humana tão teimosa.\n\n" +
        "Seus pais não voltarão, e nenhuma vitória apagará essa ausência. Mas o caminho que começou com vingança termina com algo maior: a liberdade de escolher o próprio destino.\n\n" +
        "Agora vá viver. Afinal, seria um desperdício sobreviver a tudo isso só para continuar me ouvindo falar.\n\n" +
        "Com certo orgulho — não se acostume,\nCael.";

    [Header("Creditos finais")]
    [TextArea(14, 28)]
    [SerializeField] private string textoCreditos =
        "E N D F A L L\n\n" +
        "CARCARÁ PRODUÇÕES\n\n\n" +
        "UM JOGO DE\nRYAN WALBER SOUZA SILVA\n\n\n" +
        "DESENVOLVIMENTO E PROGRAMAÇÃO\nRyan Walber Souza Silva\n\n" +
        "CRIAÇÃO E NARRATIVA\nRyan Walber Souza Silva\n\n\n" +
        "PROJETO DE CONCLUSÃO DE CURSO\nJOGOS DIGITAIS — IFRN\n\n\n" +
        "AGRADECIMENTOS\n\n" +
        "Aos professores, pelos ensinamentos.\n" +
        "Aos amigos e colegas, pela companhia.\n" +
        "À minha família, pelo apoio.\n" +
        "E a você, por chegar até o fim desta jornada.\n\n\n" +
        "OBRIGADO POR JOGAR.";

    [Header("Tempos editaveis")]
    [Min(0.005f)] [SerializeField] private float segundosPorLetra = 0.022f;
    [Min(0.1f)] [SerializeField] private float duracaoFade = 1f;
    [Min(0.2f)] [SerializeField] private float tempoTitulo = 3f;
    [Min(20f)] [SerializeField] private float velocidadeCreditos = 125f;
    [SerializeField] private string nomeCenaMenu = "Menu";

    private enum Etapa { Preparando, Carta, Logo, Creditos, Encerramento, Saindo }
    private Etapa etapa;
    private CanvasGroup grupoCarta;
    private CanvasGroup grupoLogo;
    private CanvasGroup grupoCreditos;
    private CanvasGroup grupoEncerramento;
    private TextMeshProUGUI corpoCarta;
    private TextMeshProUGUI rodapeCarta;
    private RectTransform blocoCreditos;
    private TextMeshProUGUI textoCreditosUI;
    private AudioSource musica;
    private AudioSource voz;

    private bool cartaRevelada;
    private bool revelarCarta;
    private bool proximaParte;
    private bool pularTitulo;
    private bool pularCreditos;

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        MontarInterface();
        PrepararAudio();
        StartCoroutine(SequenciaFinal());
    }

    private void Update()
    {
        bool avancar = Input.GetMouseButtonDown(0) ||
                       Input.GetKeyDown(KeyCode.Space) ||
                       Input.GetKeyDown(KeyCode.Return) ||
                       Input.GetKeyDown(KeyCode.KeypadEnter);

        if (etapa == Etapa.Carta && avancar)
        {
            if (!cartaRevelada) revelarCarta = true;
            else proximaParte = true;
        }
        else if (etapa == Etapa.Logo && avancar)
            pularTitulo = true;
        else if (etapa == Etapa.Creditos &&
                 (avancar || Input.GetKeyDown(KeyCode.Escape)))
            pularCreditos = true;
        else if (etapa == Etapa.Encerramento && avancar)
            VoltarAoMenu();
    }

    private IEnumerator SequenciaFinal()
    {
        etapa = Etapa.Preparando;
        while (LevelLoader.Instance != null && LevelLoader.Instance.EstaEmTransicao)
            yield return null;

        if (musica != null && musica.clip != null) musica.Play();

        yield return AnimarAlpha(grupoCarta, 0f, 1f, duracaoFade);
        etapa = Etapa.Carta;
        corpoCarta.maxVisibleCharacters = 0;
        corpoCarta.ForceMeshUpdate();

        int total = corpoCarta.textInfo.characterCount;
        rodapeCarta.text = "CLIQUE OU ESPAÇO  •  REVELAR CARTA";
        for (int i = 1; i <= total; i++)
        {
            if (revelarCarta) break;
            corpoCarta.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(segundosPorLetra);
        }

        corpoCarta.maxVisibleCharacters = int.MaxValue;
        cartaRevelada = true;
        rodapeCarta.text = "CLIQUE OU ESPAÇO  •  CONTINUAR";
        while (!proximaParte) yield return null;

        etapa = Etapa.Preparando;
        yield return AnimarAlpha(grupoCarta, 1f, 0f, duracaoFade * 0.7f);

        etapa = Etapa.Logo;
        yield return AnimarAlpha(grupoLogo, 0f, 1f, 2f);
        if (voz != null && voz.clip != null)
            voz.Play();
        float duracaoLogo = Mathf.Max(tempoTitulo,
            voz != null && voz.clip != null ? voz.clip.length : 0f);
        float decorrido = 0f;
        while (decorrido < duracaoLogo && !pularTitulo)
        {
            decorrido += Time.unscaledDeltaTime;
            yield return null;
        }
        if (voz != null && voz.isPlaying) voz.Stop();
        etapa = Etapa.Preparando;
        yield return AnimarAlpha(grupoLogo, 1f, 0f, duracaoFade);

        etapa = Etapa.Creditos;
        yield return AnimarAlpha(grupoCreditos, 0f, 1f, 0.65f);
        // O texto entra POR CIMA e DESCE, como solicitado.
        float altura = blocoCreditos.sizeDelta.y;
        float inicio = altura * 0.5f + 620f;
        float fim = -altura * 0.5f - 620f;
        float posicao = inicio;
        while (posicao > fim && !pularCreditos)
        {
            posicao -= velocidadeCreditos * Time.unscaledDeltaTime;
            blocoCreditos.anchoredPosition = new Vector2(0f, posicao);
            yield return null;
        }

        etapa = Etapa.Preparando;
        yield return AnimarAlpha(grupoCreditos, 1f, 0f, 0.7f);
        yield return AnimarAlpha(grupoEncerramento, 0f, 1f, duracaoFade);
        etapa = Etapa.Encerramento;
    }

    private void MontarInterface()
    {
        GameObject objetoCanvas = new GameObject("CanvasFinalEndFall",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = objetoCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 800;
        CanvasScaler scaler = objetoCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform fundo = Retangulo("FundoEscuro", objetoCanvas.transform,
            new Vector2(1920f, 1080f), Vector2.zero);
        Image imagemFundo = fundo.gameObject.AddComponent<Image>();
        imagemFundo.color = new Color(0.027f, 0.024f, 0.025f, 1f);
        imagemFundo.raycastTarget = false;

        grupoCarta = Grupo("CartaFinalDeCael", objetoCanvas.transform);
        RectTransform papel = Retangulo("Carta", grupoCarta.transform,
            new Vector2(1480f, 855f), Vector2.zero);
        Image fundoCarta = papel.gameObject.AddComponent<Image>();
        fundoCarta.color = new Color(0.09f, 0.079f, 0.072f, 1f);
        fundoCarta.raycastTarget = false;

        CriarFaixa(papel, 1470f, 3f, 405f, new Color(0.63f, 0.46f, 0.25f, 1f));
        CriarFaixa(papel, 1470f, 3f, -403f, new Color(0.63f, 0.46f, 0.25f, 1f));
        Texto(papel, "Cabecalho", "UMA ÚLTIMA CARTA",
            49f, new Color(0.88f, 0.74f, 0.53f), new Vector2(1250f, 70f),
            new Vector2(0f, 325f), TextAlignmentOptions.Center, true);
        corpoCarta = Texto(papel, "TextoCarta", cartaFinal,
            37f, new Color(0.94f, 0.92f, 0.88f), new Vector2(1250f, 630f),
            new Vector2(0f, -25f), TextAlignmentOptions.TopLeft, false);
        corpoCarta.enableAutoSizing = true;
        corpoCarta.fontSizeMin = 25f;
        corpoCarta.fontSizeMax = 37f;
        rodapeCarta = Texto(papel, "InstrucaoCarta", "",
            22f, new Color(0.65f, 0.61f, 0.57f), new Vector2(1200f, 45f),
            new Vector2(0f, -355f), TextAlignmentOptions.Center, false);

        grupoLogo = Grupo("TituloIgualSplash", objetoCanvas.transform);
        Texto(grupoLogo.transform, "END FALL", titulo,
            110f, new Color(0.91f, 0.86f, 0.79f), new Vector2(1770f, 250f),
            Vector2.zero, TextAlignmentOptions.Center, true);
        Texto(grupoLogo.transform, "Legenda", "O FIM DE UMA JORNADA",
            29f, new Color(0.64f, 0.54f, 0.44f), new Vector2(1300f, 65f),
            new Vector2(0f, -170f), TextAlignmentOptions.Center, false);

        grupoCreditos = Grupo("CreditosDescendo", objetoCanvas.transform);
        string[] linhas = textoCreditos.Split(new[] { '\n' }, StringSplitOptions.None);
        // Ao descer, a ultima linha entra primeiro no topo. Invertemos
        // a ordem fisica para o publico ler os creditos na ordem correta.
        Array.Reverse(linhas);
        string invertido = string.Join("\n", linhas);
        textoCreditosUI = Texto(grupoCreditos.transform, "TextoRolante",
            invertido, 45f, new Color(0.94f, 0.90f, 0.84f),
            new Vector2(1450f, 2000f), Vector2.zero,
            TextAlignmentOptions.Center, false);
        float alturaTexto = Mathf.Max(1750f,
            textoCreditosUI.GetPreferredValues(invertido, 1450f, 0f).y + 150f);
        blocoCreditos = textoCreditosUI.rectTransform;
        blocoCreditos.sizeDelta = new Vector2(1450f, alturaTexto);
        blocoCreditos.anchoredPosition = new Vector2(0f, alturaTexto * 0.5f + 620f);
        Texto(grupoCreditos.transform, "DicaPularCreditos",
            "CLIQUE OU ESPAÇO PARA AVANÇAR",
            19f, new Color(0.52f, 0.49f, 0.47f),
            new Vector2(1000f, 48f), new Vector2(0f, -480f),
            TextAlignmentOptions.Center, false);

        grupoEncerramento = Grupo("ObrigadoPorJogar", objetoCanvas.transform);
        Texto(grupoEncerramento.transform, "Obrigado", "OBRIGADO POR JOGAR",
            66f, new Color(0.91f, 0.86f, 0.79f),
            new Vector2(1400f, 140f), new Vector2(0f, 85f),
            TextAlignmentOptions.Center, true);
        Texto(grupoEncerramento.transform, "Voltar",
            "CLIQUE OU ESPAÇO PARA VOLTAR AO MENU",
            25f, new Color(0.63f, 0.55f, 0.45f),
            new Vector2(1400f, 75f), new Vector2(0f, -125f),
            TextAlignmentOptions.Center, false);
    }

    private CanvasGroup Grupo(string nome, Transform pai)
    {
        RectTransform ret = Retangulo(nome, pai, new Vector2(1920f, 1080f), Vector2.zero);
        CanvasGroup grupo = ret.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
        return grupo;
    }

    private void CriarFaixa(Transform pai, float largura, float altura, float y, Color cor)
    {
        RectTransform ret = Retangulo("LinhaDourada", pai,
            new Vector2(largura, altura), new Vector2(0f, y));
        Image image = ret.gameObject.AddComponent<Image>();
        image.color = cor;
        image.raycastTarget = false;
    }

    private TextMeshProUGUI Texto(Transform pai, string nome, string conteudo,
        float tamanho, Color cor, Vector2 dimensoes, Vector2 posicao,
        TextAlignmentOptions alinhamento, bool usarFonteTitulo)
    {
        RectTransform ret = Retangulo(nome, pai, dimensoes, posicao);
        TextMeshProUGUI tmp = ret.gameObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset fonte = usarFonteTitulo ? fonteTitulo : fonteTexto;
        if (fonte == null) fonte = TMP_Settings.defaultFontAsset;
        if (fonte != null) tmp.font = fonte;
        tmp.text = conteudo;
        tmp.fontSize = tamanho;
        tmp.color = cor;
        tmp.alignment = alinhamento;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    private static RectTransform Retangulo(string nome, Transform pai, Vector2 tamanho, Vector2 posicao)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform));
        RectTransform ret = objeto.GetComponent<RectTransform>();
        ret.SetParent(pai, false);
        ret.anchorMin = ret.anchorMax = new Vector2(0.5f, 0.5f);
        ret.anchoredPosition = posicao;
        ret.sizeDelta = tamanho;
        return ret;
    }

    private void PrepararAudio()
    {
        if (musicaFinal != null)
        {
            musica = gameObject.AddComponent<AudioSource>();
            musica.playOnAwake = false;
            musica.loop = true;
            musica.spatialBlend = 0f;
            musica.clip = musicaFinal;
            musica.volume = volumeMusica;
        }
        if (vozEndFall != null)
        {
            voz = gameObject.AddComponent<AudioSource>();
            voz.playOnAwake = false;
            voz.loop = false;
            voz.spatialBlend = 0f;
            voz.clip = vozEndFall;
            voz.volume = volumeVoz;
        }
    }

    private static IEnumerator AnimarAlpha(CanvasGroup grupo, float inicio, float fim, float duracao)
    {
        float elapsed = 0f;
        duracao = Mathf.Max(0.05f, duracao);
        while (elapsed < duracao)
        {
            elapsed += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(inicio, fim, Mathf.Clamp01(elapsed / duracao));
            yield return null;
        }
        grupo.alpha = fim;
    }

    private void VoltarAoMenu()
    {
        if (etapa == Etapa.Saindo) return;
        etapa = Etapa.Saindo;
        if (musica != null) musica.Stop();
        if (voz != null) voz.Stop();
        LevelLoader.CarregarComFade(nomeCenaMenu);
    }
}
