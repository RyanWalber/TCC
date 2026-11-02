using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public class FalaDialogo
{
    [Tooltip("Nome exibido acima da fala.")]
    public string personagem = "Cael";

    [Tooltip("Sprite 2D do rosto/busto do personagem.")]
    public Sprite retrato;

    [TextArea(2, 6)]
    public string texto = "";
}

/// <summary>
/// Controle reutilizavel do painel de dialogo. Nao tem escolhas.
/// Pode ser anexado ao proprio painel: a visibilidade e controlada
/// com CanvasGroup, sem desativar o GameObject que executa o script.
/// </summary>
[DisallowMultipleComponent]
public class PainelDialogo : MonoBehaviour
{
    [Header("Referencias da UI")]
    [SerializeField] private GameObject painel;
    [SerializeField] private Image imagemRetrato;
    [SerializeField] private TMP_Text textoNome;
    [SerializeField] private TMP_Text textoFala;
    [Tooltip("Seta ou mensagem para avancar (opcional).")]
    [SerializeField] private GameObject indicadorContinuar;

    [Header("Falas padrao (editaveis no Inspector)")]
    [SerializeField] private FalaDialogo[] falasPadrao =
    {
        new FalaDialogo { personagem = "Cael", texto = "Kaya, escute. Os reinos estao cheios de perigos. Primeiro, aprenda a se mover." },
        new FalaDialogo { personagem = "Cael", texto = "Use A e D ou as setas para caminhar. Pressione Espaco para pular." },
        new FalaDialogo { personagem = "Kaya", texto = "Entendi. Mas o que acontece se eu encontrar inimigos?" },
        new FalaDialogo { personagem = "Cael", texto = "Esteja atenta a sua vida e a sua Furia. Lute com cuidado para seguir adiante." }
    };

    [Header("Comportamento")]
    [SerializeField] private bool iniciarAutomaticamente = false;
    [SerializeField] private bool pausarJogoDuranteDialogo = true;
    [Min(0.005f)]
    [SerializeField] private float segundosPorCaractere = 0.035f;

    [Header("Quando terminar (opcional)")]
    [SerializeField] private UnityEvent aoTerminarDialogo = new UnityEvent();

    private CanvasGroup grupoPainel;
    private FalaDialogo[] falasAtuais;
    private Coroutine rotinaDigitacao;
    private int indiceFala;
    private int ultimoFrameDeAvanco = -1;
    private bool dialogoAberto;
    private bool digitando;
    private bool alterouTempo;
    private float tempoAnterior;

    public bool EstaAberto => dialogoAberto;

    private void Awake()
    {
        if (painel == null)
            painel = gameObject;

        grupoPainel = painel.GetComponent<CanvasGroup>();
        if (grupoPainel == null)
            grupoPainel = painel.AddComponent<CanvasGroup>();

        AjustarVisibilidade(false);

        if (textoFala == null || textoNome == null || imagemRetrato == null)
            Debug.LogWarning("[PainelDialogo] Vincule Retrato, Nome e Fala no Inspector.", this);
    }

    private void Start()
    {
        if (iniciarAutomaticamente)
            IniciarDialogo();
    }

    /// <summary>Abre as falas configuradas no proprio painel.</summary>
    public void IniciarDialogo()
    {
        IniciarDialogo(null);
    }

    /// <summary>
    /// Permite que diferentes gatilhos reutilizem o MESMO painel com suas
    /// proprias falas e retratos. Array vazio usa as falas padrao.
    /// </summary>
    public void IniciarDialogo(FalaDialogo[] falasDoGatilho)
    {
        if (dialogoAberto || !isActiveAndEnabled)
            return;

        if (textoFala == null || textoNome == null || imagemRetrato == null)
        {
            Debug.LogError("[PainelDialogo] Faltam referencias de UI no Inspector.", this);
            return;
        }

        falasAtuais = falasDoGatilho != null && falasDoGatilho.Length > 0
            ? falasDoGatilho : falasPadrao;

        if (falasAtuais == null || falasAtuais.Length == 0)
        {
            Debug.LogWarning("[PainelDialogo] Nenhuma fala configurada.", this);
            return;
        }

        dialogoAberto = true;
        indiceFala = 0;
        ultimoFrameDeAvanco = Time.frameCount;

        if (pausarJogoDuranteDialogo)
        {
            tempoAnterior = Time.timeScale;
            Time.timeScale = 0f;
            alterouTempo = true;
        }

        AjustarVisibilidade(true);
        MostrarFalaAtual();
    }

    private void MostrarFalaAtual()
    {
        if (rotinaDigitacao != null)
        {
            StopCoroutine(rotinaDigitacao);
            rotinaDigitacao = null;
        }

        FalaDialogo fala = falasAtuais[indiceFala];
        if (fala == null)
            fala = new FalaDialogo();

        textoNome.text = fala.personagem ?? "";
        textoFala.text = fala.texto ?? "";

        imagemRetrato.sprite = fala.retrato;
        imagemRetrato.enabled = fala.retrato != null;
        imagemRetrato.preserveAspect = true;

        textoFala.maxVisibleCharacters = 0;
        textoFala.ForceMeshUpdate();

        if (indicadorContinuar != null)
            indicadorContinuar.SetActive(false);

        digitando = true;
        rotinaDigitacao = StartCoroutine(Digitar());
    }

    private IEnumerator Digitar()
    {
        // maxVisibleCharacters respeita a formatacao rich text do TMP.
        int quantidade = textoFala.textInfo.characterCount;
        for (int i = 1; i <= quantidade; i++)
        {
            textoFala.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(
                Mathf.Max(0.005f, segundosPorCaractere));
        }

        textoFala.maxVisibleCharacters = int.MaxValue;
        digitando = false;
        rotinaDigitacao = null;

        if (indicadorContinuar != null)
            indicadorContinuar.SetActive(true);
    }

    private void Update()
    {
        if (!dialogoAberto)
            return;

        if (Input.GetMouseButtonDown(0) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            AvancarDialogo();
        }
    }

    /// <summary>
    /// Tambem pode ser ligado ao OnClick de um Button.
    /// Evita que o mesmo clique seja contado pelo Button e pelo Update.
    /// </summary>
    public void AvancarDialogo()
    {
        if (!dialogoAberto || ultimoFrameDeAvanco == Time.frameCount)
            return;

        ultimoFrameDeAvanco = Time.frameCount;

        if (digitando)
        {
            if (rotinaDigitacao != null)
            {
                StopCoroutine(rotinaDigitacao);
                rotinaDigitacao = null;
            }

            textoFala.maxVisibleCharacters = int.MaxValue;
            digitando = false;
            if (indicadorContinuar != null)
                indicadorContinuar.SetActive(true);
            return;
        }

        indiceFala++;
        if (indiceFala < falasAtuais.Length)
        {
            MostrarFalaAtual();
        }
        else
        {
            EncerrarDialogo();
        }
    }

    public void EncerrarDialogo()
    {
        if (!dialogoAberto)
            return;

        if (rotinaDigitacao != null)
        {
            StopCoroutine(rotinaDigitacao);
            rotinaDigitacao = null;
        }

        digitando = false;
        dialogoAberto = false;
        falasAtuais = null;
        AjustarVisibilidade(false);
        RestaurarTempo();
        aoTerminarDialogo?.Invoke();
    }

    private void AjustarVisibilidade(bool mostrar)
    {
        if (grupoPainel == null)
            return;

        grupoPainel.alpha = mostrar ? 1f : 0f;
        grupoPainel.interactable = mostrar;
        grupoPainel.blocksRaycasts = mostrar;
    }

    private void RestaurarTempo()
    {
        if (!alterouTempo)
            return;

        Time.timeScale = tempoAnterior;
        alterouTempo = false;
    }

    private void OnDisable()
    {
        if (rotinaDigitacao != null)
        {
            StopCoroutine(rotinaDigitacao);
            rotinaDigitacao = null;
        }

        dialogoAberto = false;
        digitando = false;
        AjustarVisibilidade(false);
        RestaurarTempo();
    }
}
