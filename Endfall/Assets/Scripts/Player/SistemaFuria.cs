using UnityEngine;
using UnityEngine.UI;

public class SistemaFuria : MonoBehaviour
{
    [Header("Configurações da Fúria")]
    [SerializeField] private float furiaMaxima = 100f;
    [SerializeField] private float consumoPorSegundo = 20f;
    [SerializeField] private float furiaAtual = 0f;

    // Propriedade pública para leitura externa (Solução para o erro CS0122)
    public float FuriaAtual => furiaAtual;
    public float FuriaMaxima => furiaMaxima;

    [Header("Multiplicadores de Atributos em Fúria")]
    [Tooltip("Ex: 1.4 = +40% de velocidade de movimento")]
    public float multVelocidade = 1.4f;
    [Tooltip("Ex: 1.25 = +25% de altura do pulo")]
    public float multPulo = 1.25f;
    [Tooltip("Ex: 0.5 = atira com metade do tempo de espera (2x mais rápido)")]
    public float multCadencia = 0.5f;
    [Tooltip("Ex: 2.0 = dobro de dano")]
    public float multDano = 2.0f;

    [Header("UI da Fúria")]
    [SerializeField] private Image fillFuria;

    [Header("Efeitos Visuais de Fúria")]
    [SerializeField] private Outline luzNeonFuria;
    [SerializeField] private GameObject vinhetaTelaFuria;

    [Header("Controles")]
    [SerializeField] private KeyCode teclaFuria = KeyCode.Q;

    private bool estaEmFuria = false;
    private KayaAudioManager audioKaya;
    private PlayerController controleKaya;
    private PlayerHealth saudeKaya;
    private HUDJuice retratoHUD;

    public bool EstaEmFuria => estaEmFuria;

    private void Start()
    {
        audioKaya = GetComponent<KayaAudioManager>();
        controleKaya = GetComponent<PlayerController>();
        saudeKaya = GetComponent<PlayerHealth>();
        ConfigurarPainelDecorativo();
        // Carrega a Fúria escolhida no slot ou preservada pelo renascimento.
        if (SaveManager.Instance != null)
            furiaAtual = Mathf.Clamp(SaveManager.Instance.GetFuriaKaya(), 0f, furiaMaxima);
        AtualizarUI();
        AtualizarEfeitosVisuais();
    }

    private void Update()
    {
        // Nao alterna Furia com o menu de pausa aberto.
        if (Time.timeScale <= 0f) return;

        if ((controleKaya == null || !controleKaya.ControlesBloqueadosPorDialogo) &&
            Input.GetKeyDown(teclaFuria))
        {
            if (estaEmFuria)
            {
                DesativarFuria();
            }
            else if (furiaAtual > 0f)
            {
                AtivarFuria();
            }
        }

        if (estaEmFuria)
        {
            furiaAtual -= consumoPorSegundo * Time.deltaTime;
            furiaAtual = Mathf.Max(furiaAtual, 0f);
            AtualizarUI();

            if (furiaAtual <= 0f)
            {
                DesativarFuria();
            }
        }
    }

    public void AdicionarFuria(float quantidade)
    {
        if (estaEmFuria) return;

        furiaAtual += quantidade;
        furiaAtual = Mathf.Clamp(furiaAtual, 0f, furiaMaxima);
        AtualizarUI();
    }

    // Define a quantidade exata ao carregar um save (não soma ao valor atual).
    public void DefinirFuria(float valor)
    {
        furiaAtual = Mathf.Clamp(valor, 0f, furiaMaxima);
        estaEmFuria = false;
        AtualizarUI();
        AtualizarEfeitosVisuais();
    }

    private void AtivarFuria()
    {
        estaEmFuria = true;
        if (audioKaya != null) audioKaya.TocarAtivarFuria();
        AtualizarEfeitosVisuais();
        AtualizarRetratoHUD();
        Debug.Log(">>> MODO FÚRIA ATIVADO! <<<");
    }

    private void DesativarFuria()
    {
        estaEmFuria = false;
        if (audioKaya != null) audioKaya.TocarDesativarFuria();
        AtualizarEfeitosVisuais();
        AtualizarRetratoHUD();
        Debug.Log("Fúria Desativada.");
    }

    private void AtualizarUI()
    {
        if (fillFuria != null)
        {
            fillFuria.fillAmount = furiaAtual / Mathf.Max(1f, furiaMaxima);
        }
    }

    // A vinheta da Furia ocupa a tela inteira, mas e puramente visual.
    // Desativa raycasts para que ela nunca capture interacoes do mouse.
    private void ConfigurarPainelDecorativo()
    {
        if (vinhetaTelaFuria == null) return;

        foreach (Graphic grafico in vinhetaTelaFuria.GetComponentsInChildren<Graphic>(true))
            grafico.raycastTarget = false;

        foreach (CanvasGroup grupo in vinhetaTelaFuria.GetComponentsInChildren<CanvasGroup>(true))
        {
            grupo.blocksRaycasts = false;
            grupo.interactable = false;
        }
    }

    private void AtualizarRetratoHUD()
    {
        if (retratoHUD == null)
            retratoHUD = FindFirstObjectByType<HUDJuice>();

        if (saudeKaya != null && retratoHUD != null)
        {
            float porcentagemVida = saudeKaya.VidaAtual /
                Mathf.Max(1f, saudeKaya.VidaMaxima);
            retratoHUD.AtualizarRetrato(porcentagemVida);
        }
    }

    private void AtualizarEfeitosVisuais()
    {
        if (luzNeonFuria != null)
        {
            luzNeonFuria.enabled = estaEmFuria;
        }

        if (vinhetaTelaFuria != null)
        {
            vinhetaTelaFuria.SetActive(estaEmFuria);
        }
    }
}