using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Cria a janela no Canvas do menu ou da pausa sem exigir novos prefabs.
// Os valores sao persistidos em PlayerPrefs, independentemente do save.
public class PainelConfiguracoes : MonoBehaviour
{
    private Action aoFechar;
    private TextMeshProUGUI textoVolume;
    private TextMeshProUGUI textoMusica;
    private TextMeshProUGUI textoSensibilidade;
    private Slider sliderVolume;
    private Slider sliderSensibilidade;

    private static readonly Color CorFundo = new Color(0.035f, 0.035f, 0.05f, 0.94f);
    private static readonly Color CorPainel = new Color(0.09f, 0.09f, 0.12f, 1f);
    private static readonly Color CorTexto = new Color(0.92f, 0.88f, 0.83f, 1f);
    private static readonly Color CorDestaque = new Color(0.76f, 0.58f, 0.39f, 1f);

    public static PainelConfiguracoes Criar(Transform canvas, Action aoFechar)
    {
        if (canvas == null) return null;

        GameObject raiz = new GameObject("ConfiguracoesEndFall", typeof(RectTransform), typeof(Image));
        raiz.transform.SetParent(canvas, false);
        RectTransform retangulo = raiz.GetComponent<RectTransform>();
        retangulo.anchorMin = Vector2.zero;
        retangulo.anchorMax = Vector2.one;
        retangulo.offsetMin = Vector2.zero;
        retangulo.offsetMax = Vector2.zero;
        Image fundo = raiz.GetComponent<Image>();
        fundo.color = CorFundo;
        fundo.raycastTarget = true;
        raiz.transform.SetAsLastSibling();

        PainelConfiguracoes painel = raiz.AddComponent<PainelConfiguracoes>();
        painel.aoFechar = aoFechar;
        painel.Montar();
        raiz.SetActive(false);
        return painel;
    }

    public static Button CriarBotaoAcesso(Transform pai, Action abrir)
    {
        if (pai == null) return null;

        Button botao = CriarBotao("AbrirConfiguracoes", pai, "CONFIGURAÇÕES",
            new Vector2(278f, 61f), Vector2.zero, 27f);
        RectTransform rect = botao.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-45f, -36f);
        botao.onClick.AddListener(() => abrir?.Invoke());
        return botao;
    }

    private void Montar()
    {
        RectTransform corpo = NovoRect("CorpoConfiguracoes", transform,
            new Vector2(890f, 650f), Vector2.zero);
        Image painelImagem = corpo.gameObject.AddComponent<Image>();
        painelImagem.color = CorPainel;
        painelImagem.raycastTarget = true;
        Outline borda = corpo.gameObject.AddComponent<Outline>();
        borda.effectColor = new Color(0.38f, 0.27f, 0.18f, 0.8f);
        borda.effectDistance = new Vector2(2f, -2f);

        CriarTexto("Titulo", corpo, "CONFIGURAÇÕES", 48f,
            new Vector2(750f, 70f), new Vector2(0f, 243f), CorTexto);
        CriarTexto("Subtitulo", corpo, "ENDFALL  •  ÁUDIO E CONTROLES", 19f,
            new Vector2(700f, 32f), new Vector2(0f, 192f), CorDestaque);

        textoVolume = CriarTexto("RotuloVolume", corpo, "", 26f,
            new Vector2(660f, 42f), new Vector2(0f, 128f), CorTexto);
        sliderVolume = CriarSlider("VolumeGeral", corpo, 0f, 1f, new Vector2(0f, 85f));
        sliderVolume.onValueChanged.AddListener(valor =>
        {
            ConfiguracoesJogo.VolumeGeral = valor;
            AtualizarRotulos();
        });

        Button musica = CriarBotao("BotaoMusica", corpo, "",
            new Vector2(560f, 60f), new Vector2(0f, -4f), 27f);
        textoMusica = musica.GetComponentInChildren<TextMeshProUGUI>();
        musica.onClick.AddListener(() =>
        {
            ConfiguracoesJogo.MusicaAtiva = !ConfiguracoesJogo.MusicaAtiva;
            AtualizarRotulos();
        });

        textoSensibilidade = CriarTexto("RotuloSensibilidade", corpo, "", 26f,
            new Vector2(660f, 42f), new Vector2(0f, -91f), CorTexto);
        sliderSensibilidade = CriarSlider("SensibilidadeDaMira", corpo,
            0.5f, 2f, new Vector2(0f, -133f));
        sliderSensibilidade.onValueChanged.AddListener(valor =>
        {
            ConfiguracoesJogo.SensibilidadeMira = valor;
            AtualizarRotulos();
        });

        CriarTexto("ExplicacaoMira", corpo,
            "Sensibilidade ajusta a resposta da mira 2D, não a velocidade do cursor.",
            18f, new Vector2(780f, 40f), new Vector2(0f, -185f),
            new Color(0.74f, 0.72f, 0.70f, 1f));

        Button voltar = CriarBotao("VoltarConfiguracoes", corpo, "VOLTAR",
            new Vector2(300f, 58f), new Vector2(0f, -257f), 28f);
        voltar.onClick.AddListener(Fechar);
    }

    public void Abrir()
    {
        gameObject.SetActive(true);
        if (sliderVolume != null) sliderVolume.SetValueWithoutNotify(ConfiguracoesJogo.VolumeGeral);
        if (sliderSensibilidade != null)
            sliderSensibilidade.SetValueWithoutNotify(ConfiguracoesJogo.SensibilidadeMira);
        AtualizarRotulos();
        transform.SetAsLastSibling();
        if (AudioManagerGlobal.Instance != null)
            AudioManagerGlobal.Instance.RegistrarBotoes();
    }

    public void Fechar()
    {
        gameObject.SetActive(false);
        aoFechar?.Invoke();
    }

    public void Esconder()
    {
        gameObject.SetActive(false);
    }

    private void AtualizarRotulos()
    {
        if (textoVolume != null)
            textoVolume.text = "VOLUME GERAL: " +
                Mathf.RoundToInt(ConfiguracoesJogo.VolumeGeral * 100f) + "%";
        if (textoMusica != null)
            textoMusica.text = ConfiguracoesJogo.MusicaAtiva
                ? "MÚSICA: LIGADA" : "MÚSICA: DESLIGADA";
        if (textoSensibilidade != null)
            textoSensibilidade.text = "SENSIBILIDADE DA MIRA: " +
                ConfiguracoesJogo.SensibilidadeMira.ToString("0.00") + "x";
    }

    private static RectTransform NovoRect(string nome, Transform pai,
        Vector2 tamanho, Vector2 posicao)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(pai, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = tamanho;
        rect.anchoredPosition = posicao;
        return rect;
    }

    private static TextMeshProUGUI CriarTexto(string nome, Transform pai, string conteudo,
        float tamanhoFonte, Vector2 tamanho, Vector2 posicao, Color cor)
    {
        RectTransform rect = NovoRect(nome, pai, tamanho, posicao);
        TextMeshProUGUI texto = rect.gameObject.AddComponent<TextMeshProUGUI>();
        texto.text = conteudo;
        texto.fontSize = tamanhoFonte;
        texto.color = cor;
        texto.alignment = TextAlignmentOptions.Center;
        texto.enableWordWrapping = true;
        texto.raycastTarget = false;
        return texto;
    }

    private static Button CriarBotao(string nome, Transform pai, string legenda,
        Vector2 tamanho, Vector2 posicao, float tamanhoFonte)
    {
        RectTransform rect = NovoRect(nome, pai, tamanho, posicao);
        Image fundo = rect.gameObject.AddComponent<Image>();
        fundo.color = new Color(0.22f, 0.19f, 0.18f, 1f);
        Button botao = rect.gameObject.AddComponent<Button>();
        botao.targetGraphic = fundo;
        ColorBlock cores = botao.colors;
        cores.highlightedColor = new Color(1f, 0.88f, 0.75f, 1f);
        cores.pressedColor = new Color(0.68f, 0.55f, 0.45f, 1f);
        botao.colors = cores;

        TextMeshProUGUI texto = CriarTexto("Texto", rect, legenda, tamanhoFonte,
            Vector2.zero, Vector2.zero, CorTexto);
        RectTransform textoRect = texto.rectTransform;
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = new Vector2(8f, 3f);
        textoRect.offsetMax = new Vector2(-8f, -3f);
        return botao;
    }

    private static Slider CriarSlider(string nome, Transform pai, float minimo,
        float maximo, Vector2 posicao)
    {
        RectTransform raiz = NovoRect(nome, pai, new Vector2(620f, 44f), posicao);
        Slider controle = raiz.gameObject.AddComponent<Slider>();
        controle.direction = Slider.Direction.LeftToRight;

        RectTransform trilho = NovoRect("Trilho", raiz, new Vector2(620f, 14f), Vector2.zero);
        Image imagemTrilho = trilho.gameObject.AddComponent<Image>();
        imagemTrilho.color = new Color(0.28f, 0.27f, 0.29f, 1f);

        RectTransform areaPreenchida = NovoRect("AreaPreenchida", raiz, Vector2.zero, Vector2.zero);
        areaPreenchida.anchorMin = Vector2.zero;
        areaPreenchida.anchorMax = Vector2.one;
        areaPreenchida.offsetMin = new Vector2(12f, 0f);
        areaPreenchida.offsetMax = new Vector2(-12f, 0f);
        RectTransform preenchimento = NovoRect("Preenchimento", areaPreenchida, Vector2.zero, Vector2.zero);
        preenchimento.anchorMin = Vector2.zero;
        preenchimento.anchorMax = Vector2.one;
        preenchimento.offsetMin = Vector2.zero;
        preenchimento.offsetMax = Vector2.zero;
        Image imagemPreenchimento = preenchimento.gameObject.AddComponent<Image>();
        imagemPreenchimento.color = CorDestaque;
        imagemPreenchimento.raycastTarget = false;

        RectTransform areaAlca = NovoRect("AreaAlca", raiz, Vector2.zero, Vector2.zero);
        areaAlca.anchorMin = Vector2.zero;
        areaAlca.anchorMax = Vector2.one;
        areaAlca.offsetMin = new Vector2(12f, 0f);
        areaAlca.offsetMax = new Vector2(-12f, 0f);
        RectTransform alca = NovoRect("Alca", areaAlca, new Vector2(28f, 28f), Vector2.zero);
        alca.anchorMin = new Vector2(0f, 0.5f);
        alca.anchorMax = new Vector2(0f, 0.5f);
        Image imagemAlca = alca.gameObject.AddComponent<Image>();
        imagemAlca.color = CorTexto;

        controle.fillRect = preenchimento;
        controle.handleRect = alca;
        controle.targetGraphic = imagemAlca;
        controle.minValue = minimo;
        controle.maxValue = maximo;
        return controle;
    }
}
