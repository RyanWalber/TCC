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

    [Header("Interface")]
    [SerializeField] private TMP_Text textoTela;
    [SerializeField] private Image cortinaTransicao;

    [Header("Visualizacao direta no Editor")]
    [Range(1, 4)] [SerializeField] private int fragmentoParaTeste = 1;
    [Min(0.005f)] [SerializeField] private float intervaloPorCaractere = 0.03f;

    [Header("Textos - editaveis no Inspector")]
    [TextArea(8, 16)]
    [SerializeField] private string fragmento1 = "<size=35><color=#E8C994>FRAGMENTO I - O ÚLTIMO LEGADO</color></size>\n\nAntes que os reinos fossem consumidos pela Fome, Peste, Guerra e Morte, existiam aqueles que enfrentavam as sombras: os caçadores.\n\nKaya cresceu entre eles, aprendendo que coragem não é ausência de medo, mas a escolha de proteger.\n\nAos dezoito anos, ela ainda não sabia quanto custaria carregar esse legado.\n\n<size=18><color=#B8B6B1>ESPAÇO OU CLIQUE: revelar texto / continuar</color></size>";

    [TextArea(8, 16)]
    [SerializeField] private string fragmento2 = "<size=35><color=#E8C994>FRAGMENTO II - A NOITE DAS CINZAS</color></size>\n\nA família de Kaya não desapareceu por acaso. Em uma única noite, sua casa foi destruída e uma linhagem inteira de caçadores foi exterminada.\n\nSeus pais morreram tentando resistir. Kaya foi a única a sobreviver.\n\nRestou-lhe uma lembrança que não se apaga... e uma pergunta: quem levou a morte até sua porta?\n\n<size=18><color=#B8B6B1>ESPAÇO OU CLIQUE: revelar texto / continuar</color></size>";

    [TextArea(8, 16)]
    [SerializeField] private string fragmento3 = "<size=35><color=#E8C994>FRAGMENTO III - O RASTRO DA VERDADE</color></size>\n\nA cada domínio atravessado, Kaya encontrou sinais de que o massacre era parte de algo maior. A mesma força que espalhava a Fome e a Peste alimentava a Guerra.\n\nOs fragmentos da Colt, uma arma lendária, poderiam romper esse ciclo.\n\nEla já não procurava apenas vingança. Procurava uma verdade que o mundo parecia temer.\n\n<size=18><color=#B8B6B1>ESPAÇO OU CLIQUE: revelar texto / continuar</color></size>";

    [TextArea(8, 16)]
    [SerializeField] private string fragmento4 = "<size=35><color=#E8C994>FRAGMENTO IV - DIANTE DA MORTE</color></size>\n\nDepois da Guerra, resta um último domínio. Nele espera a sombra por trás do massacre que levou os pais de Kaya: a Morte.\n\nAgora ela conhece o inimigo que precisa enfrentar. Mas sua última batalha será maior que a vingança.\n\nSe vencer, talvez consiga libertar os reinos do ciclo de destruição — e dar ao legado de sua família um destino além da dor.\n\n<size=18><color=#B8B6B1>ESPAÇO OU CLIQUE: revelar texto / continuar</color></size>";

    [Header("Destino padrao quando a cena e executada diretamente")]
    [SerializeField] private string nomeDaProximaCena = "FaseFome";

    private Coroutine digitacao;
    private bool terminouTexto;
    private bool trocandoCena;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void LimparAoIniciarPlay()
    {
        CancelarFragmentoPendente();
    }

    // Chamado somente quando uma NOVA fase e alcancada, nunca no load/respawn.
    public static void ExibirAntesDaFase(string nomeCenaDestino)
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
        LevelLoader.CarregarComFade(CenaExplicacao);
    }

    public static void CancelarFragmentoPendente()
    {
        fragmentoPendente = -1;
        proximaCenaPendente = null;
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

    private void Start()
    {
        // A cortina global e unica e controla o fade da entrada e da saida.
        if (cortinaTransicao != null)
        {
            cortinaTransicao.enabled = false;
            cortinaTransicao.raycastTarget = false;
        }

        int indice = fragmentoPendente >= 0
            ? fragmentoPendente : Mathf.Clamp(fragmentoParaTeste - 1, 0, 3);
        if (!string.IsNullOrEmpty(proximaCenaPendente))
            nomeDaProximaCena = proximaCenaPendente;

        // A informacao ja foi consumida; nao vaza para futuras transicoes.
        CancelarFragmentoPendente();

        if (textoTela == null)
            textoTela = FindFirstObjectByType<TextMeshProUGUI>();
        if (textoTela == null)
        {
            Debug.LogError("[ExplicacaoManager] Text (TMP) nao foi encontrado.", this);
            terminouTexto = true;
            return;
        }

        string historia = indice == 0 ? fragmento1 :
                          indice == 1 ? fragmento2 :
                          indice == 2 ? fragmento3 : fragmento4;
        textoTela.text = historia;
        textoTela.maxVisibleCharacters = 0;
        textoTela.ForceMeshUpdate();
        digitacao = StartCoroutine(RevelarTexto());
    }

    private IEnumerator RevelarTexto()
    {
        // Sem WaitForSeconds escalado: funciona mesmo que a fase anterior
        // tenha pausado Time.timeScale, como na tela de vitoria.
        yield return null;
        int quantidade = textoTela.textInfo.characterCount;
        for (int i = 1; i <= quantidade; i++)
        {
            textoTela.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(Mathf.Max(0.005f, intervaloPorCaractere));
        }

        terminouTexto = true;
        digitacao = null;
    }

    private void Update()
    {
        if (trocandoCena) return;

        bool avancar = Input.GetKeyDown(KeyCode.Space) ||
                       Input.GetKeyDown(KeyCode.Return) ||
                       Input.GetMouseButtonDown(0);
        if (!avancar) return;

        if (!terminouTexto)
        {
            // Primeiro clique mostra a pagina completa.
            if (digitacao != null) StopCoroutine(digitacao);
            digitacao = null;
            if (textoTela != null)
                textoTela.maxVisibleCharacters = int.MaxValue;
            terminouTexto = true;
            return;
        }

        // Segundo clique avanca com a mesma cortina global do jogo.
        trocandoCena = true;
        LevelLoader.CarregarComFade(nomeDaProximaCena);
    }
}
