using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configurações de Vida")]
    [SerializeField] private float vidaMaxima = 100f;
    private float vidaAtual;

    [Header("Configurações de Fúria")]
    [SerializeField] private float furiaMaxima = 100f;
    private float furiaAtual;

    [Header("Referências da UI")]
    [SerializeField] private Image fillVida;
    [SerializeField] private Image fillFuria;

    [Header("Feedback de Dano")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color corDano = Color.red;
    [SerializeField] private float tempoPiscar = 0.15f;
    [SerializeField] private float tempoInvulnerabilidade = 0.5f;

    private Color corOriginal;
    private bool estaInvulneravel = false;

    private void Start()
    {
        vidaAtual = vidaMaxima;
        furiaAtual = 0f; 

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            corOriginal = spriteRenderer.color;
        }

        AtualizarUI();
    }

    public void TomarDano(float quantidadeDano)
    {
        if (estaInvulneravel) return;

        vidaAtual -= quantidadeDano;
        vidaAtual = Mathf.Clamp(vidaAtual, 0f, vidaMaxima);

        AtualizarUI();

        if (vidaAtual <= 0f)
        {
            Morrer();
        }
        else
        {
            StartCoroutine(EfeitoPiscarDano());
        }
    }

    public void AdicionarFuria(float quantidade)
    {
        furiaAtual += quantidade;
        furiaAtual = Mathf.Clamp(furiaAtual, 0f, furiaMaxima);
        AtualizarUI();
    }

    public bool UsarFuria(float quantidade)
    {
        if (furiaAtual >= quantidade)
        {
            furiaAtual -= quantidade;
            AtualizarUI();
            return true;
        }
        return false;
    }

    private void AtualizarUI()
    {
        if (fillVida != null)
        {
            fillVida.fillAmount = vidaAtual / vidaMaxima;
        }

        if (fillFuria != null)
        {
            fillFuria.fillAmount = furiaAtual / furiaMaxima;
        }
    }

    private IEnumerator EfeitoPiscarDano()
    {
        estaInvulneravel = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = corDano;
            yield return new WaitForSeconds(tempoPiscar);
            spriteRenderer.color = corOriginal;
        }

        yield return new WaitForSeconds(tempoInvulnerabilidade - tempoPiscar);

        estaInvulneravel = false;
    }

    private void Morrer()
    {
        Debug.Log("Kaya morreu!");
    }
}