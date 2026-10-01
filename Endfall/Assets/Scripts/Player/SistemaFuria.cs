using UnityEngine;
using UnityEngine.UI;

public class SistemaFuria : MonoBehaviour
{
    [Header("Configurações da Fúria")]
    [SerializeField] private float furiaMaxima = 100f;
    [SerializeField] private float consumoPorSegundo = 20f;
    private float furiaAtual = 0f;

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

    [Header("Efeitos Visuais de Fúria (NOVOS)")]
    [SerializeField] private Outline luzNeonFuria;        // Arraste o componente Outline do objeto 'furia'
    [SerializeField] private GameObject vinhetaTelaFuria; // Arraste o objeto 'VinhetaFuria' da tela

    [Header("Controles")]
    [SerializeField] private KeyCode teclaFuria = KeyCode.Q;

    private bool estaEmFuria = false;

    public bool EstaEmFuria => estaEmFuria;

    private void Start()
    {
        AtualizarUI();
        AtualizarEfeitosVisuais();
    }

    private void Update()
    {
        if (Input.GetKeyDown(teclaFuria))
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

    private void AtivarFuria()
    {
        estaEmFuria = true;
        AtualizarEfeitosVisuais();
        Debug.Log(">>> MODO FÚRIA ATIVADO! <<<");
    }

    private void DesativarFuria()
    {
        estaEmFuria = false;
        AtualizarEfeitosVisuais();
        Debug.Log("Fúria Desativada.");
    }

    private void AtualizarUI()
    {
        if (fillFuria != null)
        {
            fillFuria.fillAmount = furiaAtual / furiaMaxima;
        }
    }

    // Liga ou desliga os efeitos visuais dependendo de 'estaEmFuria'
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