using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configurações de Vida")]
    [SerializeField] private float vidaMaxima = 100f;
    private float vidaAtual;

    [Header("Imagens da UI")]
    [SerializeField] private Image fillVida;          // Barra Vermelha (vida)
    [SerializeField] private Image fillVidaFantasma;  // Barra Amarela/Branca (vidaFantasma)

    [Header("Velocidade da Barra Fantasma")]
    [SerializeField] private float velocidadeFantasma = 1.5f;

    [Header("Fúria e Efeitos")]
    [SerializeField] private float furiaPorDanoTomado = 15f;
    private SistemaFuria sistemaFuria;
    private HUDJuice hudJuice;

    [Header("Game Over e Feedback de Dano")]
    [SerializeField] private GameObject painelGameOver;
    [SerializeField] private Color corDano = Color.red;
    [SerializeField] private float tempoInvencivel = 0.8f;

    // Lista de todos os Sprites do corpo fatiado da Kaya
    private SpriteRenderer[] todosSprites;
    private Color[] coresOriginais;

    private bool estaInvencivel = false;
    private bool estaMorta = false;

    private void Start()
    {
        vidaAtual = vidaMaxima;
        sistemaFuria = GetComponent<SistemaFuria>();
        hudJuice = FindFirstObjectByType<HUDJuice>();

        // Pega todos os sprites do corpo da Kaya
        todosSprites = GetComponentsInChildren<SpriteRenderer>();
        if (todosSprites != null && todosSprites.Length > 0)
        {
            coresOriginais = new Color[todosSprites.Length];
            for (int i = 0; i < todosSprites.Length; i++)
            {
                coresOriginais[i] = todosSprites[i].color;
            }
        }

        if (painelGameOver != null)
        {
            painelGameOver.SetActive(false);
        }

        AtualizarUIImediato();
    }

    private void Update()
    {
        if (fillVidaFantasma != null && fillVida != null)
        {
            if (fillVidaFantasma.fillAmount > fillVida.fillAmount)
            {
                fillVidaFantasma.fillAmount -= velocidadeFantasma * Time.deltaTime;
            }
        }
    }

    public void TomarDano(float quantidadeDano)
    {
        if (estaMorta || estaInvencivel) return;

        vidaAtual -= quantidadeDano;
        vidaAtual = Mathf.Clamp(vidaAtual, 0f, vidaMaxima);

        if (fillVida != null)
        {
            fillVida.fillAmount = vidaAtual / vidaMaxima;
        }

        if (sistemaFuria != null)
        {
            sistemaFuria.AdicionarFuria(furiaPorDanoTomado);
        }

        if (hudJuice != null)
        {
            hudJuice.TremerHUD();
            hudJuice.AtualizarRetrato(vidaAtual / vidaMaxima);
        }

        if (vidaAtual <= 0f)
        {
            Morrer();
        }
        else
        {
            StartCoroutine(RotinaInvencibilidade());
        }
    }

    private IEnumerator RotinaInvencibilidade()
    {
        estaInvencivel = true;

        // Pinta todo o corpo da Kaya de vermelho
        for (int i = 0; i < todosSprites.Length; i++)
        {
            if (todosSprites[i] != null) todosSprites[i].color = corDano;
        }

        yield return new WaitForSeconds(0.15f);

        // Restaura as cores originais do corpo
        for (int i = 0; i < todosSprites.Length; i++)
        {
            if (todosSprites[i] != null) todosSprites[i].color = coresOriginais[i];
        }

        yield return new WaitForSeconds(tempoInvencivel - 0.15f);

        estaInvencivel = false;
    }

    private void Morrer()
    {
        estaMorta = true;

        if (painelGameOver != null)
        {
            painelGameOver.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    private void AtualizarUIImediato()
    {
        float pct = vidaAtual / vidaMaxima;
        if (fillVida != null) fillVida.fillAmount = pct;
        if (fillVidaFantasma != null) fillVidaFantasma.fillAmount = pct;
    }
}