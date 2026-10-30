using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExplicacaoManager : MonoBehaviour
{
    [Header("Texto da introducao")]
    [SerializeField] private TMP_Text textoTela;
    [TextArea(5, 12)]
    [SerializeField] private string textoIntroducao =
        "Bem-vindo a EndFall.\\n\\n" +
        "Em um mundo marcado pela ruina, a jornada de Kaya esta apenas comecando.\\n" +
        "Reinos feridos por calamidades escondem perigos, escolhas e segredos.\\n" +
        "Siga em frente. Nem tudo e o que parece.";
    [Min(0.005f)] [SerializeField] private float intervaloPorCaractere = 0.045f;

    [Header("Proxima fase")]
    [SerializeField] private string nomeDaProximaCena = "FaseFome";
    [Header("Cortina antiga (desativada quando esta cena inicia)")]
    [SerializeField] private Image cortinaTransicao;

    private Coroutine digitacao;
    private bool terminouTexto;
    private bool trocandoCena;

    private void Start()
    {
        if (cortinaTransicao != null)
        {
            cortinaTransicao.enabled = false;
            cortinaTransicao.raycastTarget = false;
        }
        if (textoTela == null)
            textoTela = FindFirstObjectByType<TextMeshProUGUI>();
        if (textoTela == null)
        {
            Debug.LogError("[ExplicacaoManager] Text (TMP) nao encontrado.", this);
            terminouTexto = true;
            return;
        }

        textoTela.text = textoIntroducao.Replace("\\n", "\n");
        textoTela.maxVisibleCharacters = 0;
        textoTela.ForceMeshUpdate();
        digitacao = StartCoroutine(RevelarTexto());
    }

    private IEnumerator RevelarTexto()
    {
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
        if (!Input.GetKeyDown(KeyCode.Space) &&
            !Input.GetKeyDown(KeyCode.Return) &&
            !Input.GetMouseButtonDown(0)) return;

        if (!terminouTexto)
        {
            if (digitacao != null) StopCoroutine(digitacao);
            digitacao = null;
            if (textoTela != null) textoTela.maxVisibleCharacters = int.MaxValue;
            terminouTexto = true;
        }
        else
        {
            trocandoCena = true;
            LevelLoader.CarregarComFade(nomeDaProximaCena);
        }
    }
}
