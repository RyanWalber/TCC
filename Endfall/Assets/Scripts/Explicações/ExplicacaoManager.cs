using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Uma unica cena de narrativa para os quatro momentos da campanha.
// Novo Jogo passa pelo fragmento 1; Continuar, Carregar e respawn nao passam.
public class ExplicacaoManager : MonoBehaviour
{
    private const string CenaExplicacao = "Explicação";

    // Dados temporarios da transicao, NUNCA incluidos no SaveData.
    // Assim o save sempre aponta para a fase verdadeira, nao para a narrativa.
    private static int fragmentoPendente = -1;
    private static string proximaCenaPendente;
    private static bool apresentacaoPendente;

    [Header("Interface")]
    [SerializeField] private TMP_Text textoTela;
    [SerializeField] private Image cortinaTransicao;

    [Header("Visualizacao direta no Editor")]
    [Range(1, 4)] [SerializeField] private int fragmentoParaTeste = 1;
    [Min(0.005f)] [SerializeField] private float intervaloPorCaractere = 0.03f;
    [SerializeField] private bool mostrarApresentacaoAoTestar = true;
    [Min(0.1f)] [SerializeField] private float duracaoFadePagina = 0.65f;

    [Header("Audio da digitacao (apresentacao e todos os fragmentos)")]
    [Tooltip("Arraste aqui seu arquivo com o som de teclado.")]
    [SerializeField] private AudioClip somDigitacao;
    [Tooltip("Opcional: deixe vazio para criar um AudioSource 2D automaticamente.")]
    [SerializeField] private AudioSource fonteDigitacao;
    [Range(0f, 1f)] [SerializeField] private float volumeDigitacao = 0.35f;

    [Header("Boas-vindas: antes do Fragmento I")]
    [TextArea(5, 12)]
    [SerializeField] private string textoApresentacao =
        "<size=35><color=#E8C994>Bem-vindo a EndFall.</color></size>\n\n" +
        "Em um mundo marcado pela ruína, a jornada de Kaya está apenas começando.\n" +
        "Reinos feridos por calamidades escondem perigos, escolhas e segredos.\n" +
        "Siga em frente. Nem tudo é o que parece.\n\n" +
        "<size=18><color=#B8B6B1>ESPAÇO OU CLIQUE: revelar texto / continuar</color></size>";

    [Header("Textos - editaveis no Inspector")]
    [TextArea(8, 16)]
    [SerializeField] private string fragmento1 = "<size=35><color=#E8C994>FRAGMENTO I - O ÚLTIMO LEGADO</color></size>\n\nAntes que os reinos fossem consumidos pela Fome, Peste, Guerra e Morte, existiam aqueles que enfrentavam as sombras: os caçadores.\n\nKaya cresceu entre eles, aprendendo que coragem não é ausência de medo, mas a escolha de proteger.\n\nAos dezoito anos, ela ainda não sabia quanto custaria carregar esse legado.\n\n<size=18><color=#B8B6B1>ESPAÇO OU CLIQUE: revelar texto / continuar</color></size>";

    [TextArea(8, 16)]
    [SerializeField] private string fragmento2 = "<size=35><color=#E8C994>FRAGMENTO II - A NOITE DAS CINZAS</color></size>\n\nA família de Kaya não desapareceu por acaso. Em uma única noite, sua casa foi destruída e uma linhagem inteira de caçadores foi exterminada.\n\nSeus pais morreram tentando resistir. Kaya foi a única a sobreviver.\n\nRestou-lhe uma lembrança que não se apaga... e uma pergunta: quem levou a morte até sua porta?\n\n<size=18><color=#B8B6B1>ESPAÇO OU CLIQUE: revelar texto / continuar</color></size>";

    [TextArea(8, 16)]
    [SerializeField] private string fragmento3 = "<size=35><color=#E8C994>FRAGMENTO III - O RASTRO DA VERDADE</color></size>\n\nA cada domínio atravessado, Kaya encontrou sinais de que o massacre era parte de algo maior. A mesma força que espalhava a Fome e a Peste alimentava a Guerra.\n\nOs fragmentos da Colt, uma arma lendária, poderiam romper esse ciclo.\n\nEla já não procurava apenas vingança. Procurava uma verdade que o mundo parecia temer.\n\n<size=18><color=#B8B6B1>ESPAÇO OU CLIQUE: revelar texto / continuar</color></size>";

    [TextArea(8, 16)]
    [SerializeField] private string fragmento4 = "<size=35><color=#E8C994>FRAGMENTO IV - DIANTE DA MORTE</color></size>\n\nDepois da Guerra, resta um último domínio. Nele espera a sombra por trás do massacre que levou os pais de Kaya: a Morte.\n\nAgora ela conhece o inimigo que precisa enfrentar. Mas sua última batalha será maior que a vingança.\n\nSe vencer, talvez consiga libertar os reinos do ciclo de destruição — e dar ao legado de sua família um destino além da dor.\n\n<size=18><color=#B8B6B1>ESPAÇO OU CLIQUE: revelar texto / continuar</color></size>";

    [Header("Anuncio dos dominios - arquivos de voz opcionais")]
    [SerializeField] private AudioClip vozDominioFome;
    [SerializeField] private AudioClip vozDominioPeste;
    [SerializeField] private AudioClip vozDominioGuerra;
    [SerializeField] private AudioClip vozDominioMorte;
    [Range(0f, 1f)] [SerializeField] private float volumeVozDominio = 1f;
    [SerializeField] private float duracaoFadeTitulo = 0.7f;
    [SerializeField] private float permanenciaTitulo = 1.4f;
    [SerializeField] private float tamanhoTitulo = 82f;

    [Header("Destino padrao quando a cena e executada diretamente")]
    [SerializeField] private string nomeDaProximaCena = "FaseFome";

    private Coroutine digitacao;
    private Image cortinaEntrePaginas;
    private int indiceAtual;
    private bool apresentacaoAtual;
    private bool trocandoPagina;
    private bool terminouTexto;
    private bool trocandoCena;
    private bool exibindoDominio;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void LimparAoIniciarPlay()
    {
        CancelarFragmentoPendente();
    }

    // Novo Jogo: Bem-vindo -> Fragmento I -> FaseFome.
    public static void ExibirIntroducaoNovoJogo()
    {
        ExibirAntesDaFaseInterno("FaseFome", true);
    }

    // Entre as fases: somente o fragmento correspondente.
    public static void ExibirAntesDaFase(string nomeCenaDestino)
    {
        ExibirAntesDaFaseInterno(nomeCenaDestino, false);
    }

    private static void ExibirAntesDaFaseInterno(string nomeCenaDestino, bool incluirApresentacao)
    {
        int indice = IndicePorCena(nomeCenaDestino);
        if (indice < 0)
        {
            CancelarFragmentoPendente();
            LevelLoader.CarregarComFade(nomeCenaDestino);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(CenaExplicacao))
        {
            Debug.LogWarning("[ExplicacaoManager] Adicione a cena 'Explicação' ao Build Profiles > Scene List. Abrindo a fase diretamente.");
            CancelarFragmentoPendente();
            LevelLoader.CarregarComFade(nomeCenaDestino);
            return;
        }

        fragmentoPendente = indice;
        proximaCenaPendente = nomeCenaDestino;
        apresentacaoPendente = incluirApresentacao && indice == 0;
        LevelLoader.CarregarComFade(CenaExplicacao);
    }

    public static void CancelarFragmentoPendente()
    {
        fragmentoPendente = -1;
        proximaCenaPendente = null;
        apresentacaoPendente = false;
    }

    private static int IndicePorCena(string cena)
    {
        switch (cena)
        {
            case "FaseFome": return 0;
            case "FasePeste": return 1;
            case "FaseGuerra": return 2;
            case "FaseMorte": return 3;
            default: return -1;
        }
    }

    private static string DestinoPorIndice(int i)
    {
        switch (i)
        {
            case 0: return "FaseFome";
            case 1: return "FasePeste";
            case 2: return "FaseGuerra";
            default: return "FaseMorte";
        }
    }

    private string FragmentoAtual()
    {
        switch (indiceAtual)
        {
            case 0: return fragmento1;
            case 1: return fragmento2;
            case 2: return fragmento3;
            default: return fragmento4;
        }
    }

    private void Start()
    {
        ConfigurarSomDigitacao();

        if (cortinaTransicao != null)
        {
            cortinaTransicao.enabled = false;
            cortinaTransicao.raycastTarget = false;
        }

        bool chamadoPelaCampanha = fragmentoPendente >= 0;
        indiceAtual = chamadoPelaCampanha
            ? fragmentoPendente : Mathf.Clamp(fragmentoParaTeste - 1, 0, 3);
        apresentacaoAtual = apresentacaoPendente ||
            (!chamadoPelaCampanha && mostrarApresentacaoAoTestar && indiceAtual == 0);

        if (!string.IsNullOrEmpty(proximaCenaPendente))
            nomeDaProximaCena = proximaCenaPendente;
        else if (!chamadoPelaCampanha)
            nomeDaProximaCena = DestinoPorIndice(indiceAtual);

        CancelarFragmentoPendente();
        if (textoTela == null)
            textoTela = FindFirstObjectByType<TextMeshProUGUI>();
        if (textoTela == null)
        {
            Debug.LogError("[ExplicacaoManager] Text (TMP) nao encontrado.", this);
            terminouTexto = true;
            return;
        }

        PrepararTexto(apresentacaoAtual ? textoApresentacao : FragmentoAtual());
        if (apresentacaoAtual) CriarCortinaPagina();
        digitacao = StartCoroutine(RevelarTexto());
    }

    // Um AudioSource proprio impede que o som de teclado interfira
    // em musicas e outros efeitos do projeto.
    private void ConfigurarSomDigitacao()
    {
        if (somDigitacao == null) return;

        if (fonteDigitacao == null)
        {
            GameObject objetoAudio = new GameObject("SomDeTeclado");
            objetoAudio.transform.SetParent(transform, false);
            fonteDigitacao = objetoAudio.AddComponent<AudioSource>();
        }

        fonteDigitacao.playOnAwake = false;
        fonteDigitacao.spatialBlend = 0f;
        fonteDigitacao.loop = true;
        fonteDigitacao.volume = Mathf.Clamp01(volumeDigitacao);
        fonteDigitacao.clip = somDigitacao;
    }

    private void IniciarSomDigitacao()
    {
        if (fonteDigitacao == null || somDigitacao == null) return;
        if (fonteDigitacao.isPlaying) return;

        fonteDigitacao.clip = somDigitacao;
        fonteDigitacao.loop = true;
        fonteDigitacao.volume = Mathf.Clamp01(volumeDigitacao);
        fonteDigitacao.Play();
    }

    private void PararSomDigitacao()
    {
        if (fonteDigitacao != null && fonteDigitacao.isPlaying)
            fonteDigitacao.Stop();
    }

    private void OnDisable()
    {
        // Nada de audio tocando depois que a cena termina.
        PararSomDigitacao();
    }

    private void PrepararTexto(string conteudo)
    {
        PararSomDigitacao();
        textoTela.text = conteudo;
        textoTela.maxVisibleCharacters = 0;
        textoTela.ForceMeshUpdate();
        terminouTexto = false;
    }

    private void CriarCortinaPagina()
    {
        if (cortinaEntrePaginas != null || textoTela == null) return;
        Canvas canvas = textoTela.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[ExplicacaoManager] Canvas ausente; paginas sem fade local.", this);
            return;
        }

        GameObject objeto = new GameObject("FadeApresentacaoFragmento",
            typeof(RectTransform), typeof(Image));
        RectTransform retangulo = objeto.GetComponent<RectTransform>();
        retangulo.SetParent(canvas.transform, false);
        retangulo.anchorMin = Vector2.zero;
        retangulo.anchorMax = Vector2.one;
        retangulo.offsetMin = Vector2.zero;
        retangulo.offsetMax = Vector2.zero;
        retangulo.SetAsLastSibling();

        cortinaEntrePaginas = objeto.GetComponent<Image>();
        cortinaEntrePaginas.color = new Color(0f, 0f, 0f, 0f);
        cortinaEntrePaginas.raycastTarget = false;
    }

    private void AplicarAlpha(float alpha)
    {
        if (cortinaEntrePaginas == null) return;
        Color cor = cortinaEntrePaginas.color;
        cor.a = Mathf.Clamp01(alpha);
        cortinaEntrePaginas.color = cor;
    }

    private IEnumerator AnimarPagina(float inicial, float final)
    {
        PararSomDigitacao(); // Silencio durante os fades.
        if (cortinaEntrePaginas == null) yield break;
        cortinaEntrePaginas.raycastTarget = true;
        float tempo = 0f;
        float duracao = Mathf.Max(0.1f, duracaoFadePagina);
        AplicarAlpha(inicial);
        while (tempo < duracao)
        {
            tempo += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            AplicarAlpha(Mathf.Lerp(inicial, final, Mathf.Clamp01(tempo / duracao)));
            yield return null;
        }
        AplicarAlpha(final);
    }

    private IEnumerator ApresentacaoParaFragmento()
    {
        trocandoPagina = true;
        if (cortinaEntrePaginas == null) CriarCortinaPagina();

        // Fade Out da apresentacao: tela toda preta antes de trocar o texto.
        yield return AnimarPagina(0f, 1f);
        apresentacaoAtual = false;
        PrepararTexto(FragmentoAtual());
        yield return null;

        // Fade In do Fragmento I; texto comeca a surgir depois da abertura.
        yield return AnimarPagina(1f, 0f);
        if (cortinaEntrePaginas != null)
            cortinaEntrePaginas.raycastTarget = false;
        trocandoPagina = false;
        digitacao = StartCoroutine(RevelarTexto());
    }

    private IEnumerator RevelarTexto()
    {
        yield return null;
        // Nao perde caracteres enquanto o LevelLoader abre a cena.
        while (LevelLoader.Instance != null && LevelLoader.Instance.EstaEmTransicao)
            yield return null;

        if (textoTela == null) yield break;
        textoTela.ForceMeshUpdate();
        int total = textoTela.textInfo.characterCount;

        // Inicia somente quando as letras vao comecar a aparecer.
        if (total > 0) IniciarSomDigitacao();

        for (int i = 1; i <= total; i++)
        {
            textoTela.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(Mathf.Max(0.005f, intervaloPorCaractere));
        }

        PararSomDigitacao();
        terminouTexto = true;
        digitacao = null;
    }

    private void Update()
    {
        if (trocandoCena || trocandoPagina || exibindoDominio) return;
        if (!Input.GetKeyDown(KeyCode.Space) &&
            !Input.GetKeyDown(KeyCode.Return) &&
            !Input.GetMouseButtonDown(0)) return;

        if (!terminouTexto)
        {
            // Primeiro clique revela tudo na pagina atual.
            if (digitacao != null) StopCoroutine(digitacao);
            digitacao = null;
            PararSomDigitacao();
            if (textoTela != null)
                textoTela.maxVisibleCharacters = int.MaxValue;
            terminouTexto = true;
        }
        else if (apresentacaoAtual)
        {
            // Segundo clique: Bem-vindo -> Fade Out -> Fragmento I -> Fade In.
            StartCoroutine(ApresentacaoParaFragmento());
        }
        else
        {
            // Depois do fragmento: Fade Out -> fase -> Fade In global.
            trocandoCena = true;
            PararSomDigitacao();
            StartCoroutine(ExibirTituloDominio());
        }
    }
    private AudioClip VozDoDominio()
    {
        switch (indiceAtual)
        {
            case 0: return vozDominioFome;
            case 1: return vozDominioPeste;
            case 2: return vozDominioGuerra;
            default: return vozDominioMorte;
        }
    }

    private string NomeDoDominio()
    {
        switch (indiceAtual)
        {
            case 0: return "DOMÍNIO DA FOME";
            case 1: return "DOMÍNIO DA PESTE";
            case 2: return "DOMÍNIO DA GUERRA";
            default: return "DOMÍNIO DA MORTE";
        }
    }

    private IEnumerator ExibirTituloDominio()
    {
        exibindoDominio = true;
        PararSomDigitacao();
        if (cortinaEntrePaginas == null) CriarCortinaPagina();

        // Fecha o fragmento antes de mostrar o anuncio.
        yield return AnimarPagina(0f, 1f);
        if (textoTela == null)
        {
            LevelLoader.CarregarComFade(nomeDaProximaCena);
            yield break;
        }

        string textoAnterior = textoTela.text;
        float tamanhoAnterior = textoTela.fontSize;
        TextAlignmentOptions alinhamentoAnterior = textoTela.alignment;
        textoTela.text = NomeDoDominio();
        textoTela.fontSize = tamanhoTitulo;
        textoTela.alignment = TextAlignmentOptions.Center;
        textoTela.maxVisibleCharacters = int.MaxValue;
        textoTela.ForceMeshUpdate();

        yield return AnimarPagina(1f, 0f);

        AudioClip clipe = VozDoDominio();
        AudioSource fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f;
        fonte.volume = volumeVozDominio;
        if (clipe != null) fonte.PlayOneShot(clipe);

        yield return new WaitForSecondsRealtime(
            Mathf.Max(Mathf.Max(0f, permanenciaTitulo), clipe != null ? clipe.length : 0f));

        yield return AnimarPagina(0f, 1f);
        // O LevelLoader faz a passagem preta para a fase e o Fade In.
        textoTela.text = textoAnterior;
        textoTela.fontSize = tamanhoAnterior;
        textoTela.alignment = alinhamentoAnterior;
        LevelLoader.CarregarComFade(nomeDaProximaCena);
    }

}
