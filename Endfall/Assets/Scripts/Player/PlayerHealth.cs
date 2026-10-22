using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configurações de Vida")]
    [SerializeField] private float vidaMaxima = 100f;
    private float vidaAtual;
    public float VidaAtual => vidaAtual;
    public float VidaMaxima => vidaMaxima;

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
        sistemaFuria = GetComponent<SistemaFuria>();
        hudJuice = FindFirstObjectByType<HUDJuice>();

        float vidaSalva = SaveManager.Instance != null
            ? SaveManager.Instance.GetVidaKaya()
            : vidaMaxima;
        vidaAtual = Mathf.Clamp(vidaSalva, 1f, vidaMaxima);

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

        // Mantém o estado atual pronto para salvamentos manuais/autosaves.
        if (SaveManager.Instance != null)
        {
            float furiaAtual = sistemaFuria != null
                ? sistemaFuria.FuriaAtual
                : SaveManager.Instance.GetFuriaKaya();
            SaveManager.Instance.AtualizarStatusKaya(vidaAtual, furiaAtual);
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

    public void TomarDanoDeInimigo(float quantidadeDano)
    {
        if (estaMorta || estaInvencivel) return;

        // Soma a Fúria antes do dano, assim até um golpe fatal conta como dano recebido.
        if (sistemaFuria != null)
            sistemaFuria.AdicionarFuria(furiaPorDanoTomado);

        TomarDano(quantidadeDano);
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
        if (estaMorta) return;
        estaMorta = true;

        // A morte restaura o último checkpoint, sem abrir Game Over.
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RenascerNoCheckpoint();
            return;
        }

        // Fallback para cenas de teste que não tenham SaveManager.
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void RecuperarVida(float quantidade)
    {
        if (estaMorta || quantidade <= 0f) return;

        vidaAtual = Mathf.Clamp(vidaAtual + quantidade, 0f, vidaMaxima);
        AtualizarUIImediato();

        if (hudJuice != null)
        {
            hudJuice.AtualizarRetrato(vidaAtual / vidaMaxima);
        }

        if (SaveManager.Instance != null)
        {
            float furiaAtual = sistemaFuria != null ? sistemaFuria.FuriaAtual : SaveManager.Instance.GetFuriaKaya();
            SaveManager.Instance.AtualizarStatusKaya(vidaAtual, furiaAtual);
        }
    }

    private void AtualizarUIImediato()
    {
        float pct = vidaAtual / vidaMaxima;
        if (fillVida != null) fillVida.fillAmount = pct;
        if (fillVidaFantasma != null) fillVidaFantasma.fillAmount = pct;
    }
}