using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PainelConfiguracoes : MonoBehaviour
{
    [Header("Controles (criados por voce na Unity)")]
    [Tooltip("Slider de 0 a 1 para controlar TODOS os audios.")]
    [SerializeField] private Slider sliderVolumeGeral;

    [Tooltip("Slider de 0.5 a 2 para a resposta da mira 2D.")]
    [SerializeField] private Slider sliderSensibilidadeMira;

    [Tooltip("Opcional: Toggle para ligar/desligar apenas a musica.")]
    [SerializeField] private Toggle toggleMusica;

    [Header("Textos opcionais - TextMeshPro")]
    [SerializeField] private TMP_Text textoVolumeGeral;
    [SerializeField] private TMP_Text textoSensibilidade;
    [SerializeField] private TMP_Text textoEstadoMusica;

    private void GarantirSliders()
    {
        Slider[] encontrados = GetComponentsInChildren<Slider>(true);

        if (sliderVolumeGeral == null)
        {
            foreach (Slider item in encontrados)
            {
                if (item != null && item != sliderSensibilidadeMira)
                {
                    sliderVolumeGeral = item;
                    break;
                }
            }
        }

        if (sliderSensibilidadeMira == null || sliderSensibilidadeMira == sliderVolumeGeral)
        {
            sliderSensibilidadeMira = null;
            foreach (Slider item in encontrados)
            {
                if (item != null && item != sliderVolumeGeral)
                {
                    sliderSensibilidadeMira = item;
                    break;
                }
            }
        }

        if (sliderVolumeGeral == null || sliderSensibilidadeMira == null)
            Debug.LogWarning("[Configuracoes] Dois Sliders filhos nao encontrados. " +
                "Arraste Volume e Sensibilidade no Inspector.", this);

        if (sliderVolumeGeral != null)
        {
            sliderVolumeGeral.minValue = 0f;
            sliderVolumeGeral.maxValue = 1f;
            sliderVolumeGeral.wholeNumbers = false;
        }
        if (sliderSensibilidadeMira != null)
        {
            sliderSensibilidadeMira.minValue = 0.5f;
            sliderSensibilidadeMira.maxValue = 2f;
            sliderSensibilidadeMira.wholeNumbers = false;
        }
    }

    private void Awake()
    {
        GarantirSliders();
    }

    private void OnEnable()
    {
        GarantirSliders();
        SincronizarInterface();

        if (sliderVolumeGeral != null)
            sliderVolumeGeral.onValueChanged.AddListener(DefinirVolumeGeral);

        if (sliderSensibilidadeMira != null)
            sliderSensibilidadeMira.onValueChanged.AddListener(DefinirSensibilidadeMira);

        if (toggleMusica != null)
            toggleMusica.onValueChanged.AddListener(DefinirMusicaAtiva);
    }

    private void OnDisable()
    {
        if (sliderVolumeGeral != null)
            sliderVolumeGeral.onValueChanged.RemoveListener(DefinirVolumeGeral);

        if (sliderSensibilidadeMira != null)
            sliderSensibilidadeMira.onValueChanged.RemoveListener(DefinirSensibilidadeMira);

        if (toggleMusica != null)
            toggleMusica.onValueChanged.RemoveListener(DefinirMusicaAtiva);

        PlayerPrefs.Save();
    }

    public void SincronizarInterface()
    {
        GarantirSliders();
        if (sliderVolumeGeral != null)
            sliderVolumeGeral.SetValueWithoutNotify(ConfiguracoesJogo.VolumeGeral);

        if (sliderSensibilidadeMira != null)
            sliderSensibilidadeMira.SetValueWithoutNotify(ConfiguracoesJogo.SensibilidadeMira);

        if (toggleMusica != null)
            toggleMusica.SetIsOnWithoutNotify(ConfiguracoesJogo.MusicaAtiva);

        AtualizarTextos();
    }

    public void DefinirVolumeGeral(float valor)
    {
        ConfiguracoesJogo.VolumeGeral = valor;
        AtualizarTextos();
    }

    public void DefinirSensibilidadeMira(float valor)
    {
        ConfiguracoesJogo.SensibilidadeMira = valor;
        AtualizarTextos();
    }

    public void DefinirMusicaAtiva(bool ativa)
    {
        ConfiguracoesJogo.MusicaAtiva = ativa;
        AtualizarTextos();
    }

    public void AlternarMusica()
    {
        ConfiguracoesJogo.MusicaAtiva = !ConfiguracoesJogo.MusicaAtiva;
        if (toggleMusica != null)
            toggleMusica.SetIsOnWithoutNotify(ConfiguracoesJogo.MusicaAtiva);
        AtualizarTextos();
    }

    [ContextMenu("Restaurar padroes: volume 100% e mira 1x")]
    public void RestaurarPadroes()
    {
        ConfiguracoesJogo.VolumeGeral = 1f;
        ConfiguracoesJogo.MusicaAtiva = true;
        ConfiguracoesJogo.SensibilidadeMira = 1f;
        SincronizarInterface();
    }

    public void SalvarConfiguracoes()
    {
        PlayerPrefs.Save();
    }

    private void AtualizarTextos()
    {
        if (textoVolumeGeral != null)
            textoVolumeGeral.text =
                "VOLUME GERAL: " + Mathf.RoundToInt(ConfiguracoesJogo.VolumeGeral * 100f) + "%";

        if (textoSensibilidade != null)
            textoSensibilidade.text =
                "SENSIBILIDADE: " + ConfiguracoesJogo.SensibilidadeMira.ToString("0.00") + "x";

        if (textoEstadoMusica != null)
            textoEstadoMusica.text =
                ConfiguracoesJogo.MusicaAtiva ? "MÚSICA: LIGADA" : "MÚSICA: DESLIGADA";
    }
}
