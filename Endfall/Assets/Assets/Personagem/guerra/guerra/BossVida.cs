using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossVida : MonoBehaviour
{
    [Header("Configurações de Vida")]
    [SerializeField] private float vidaMaxima = 100f;
    private float vidaAtual;

    [Header("UI da Barra de Vida")]
    [SerializeField] private Slider barraDeVida;

    [Header("Feedback de Dano (Piscar Vermelho)")]
    [SerializeField] private Color corDano = Color.red;
    [SerializeField] private float duracaoPiscar = 0.15f;

    // Guarda TODOS os sprites do corpo fatiado do Boss
    private SpriteRenderer[] todosSprites;
    private Color[] coresOriginais;
    private Coroutine corrotinaPiscar;
    private bool estaMorto = false;

    private void Start()
    {
        vidaAtual = vidaMaxima;

        // Pega todos os SpriteRenderers do Boss e dos filhos (corpo, braços, cabeça, etc)
        todosSprites = GetComponentsInChildren<SpriteRenderer>();

        if (todosSprites != null && todosSprites.Length > 0)
        {
            coresOriginais = new Color[todosSprites.Length];
            for (int i = 0; i < todosSprites.Length; i++)
            {
                coresOriginais[i] = todosSprites[i].color;
            }
        }

        AtualizarBarraUI();
    }

    public void TomarDano(float dano)
    {
        if (estaMorto) return;

        vidaAtual -= dano;
        vidaAtual = Mathf.Clamp(vidaAtual, 0, vidaMaxima);

        AtualizarBarraUI();

        // Pisca o corpo INTEIRO do Boss em vermelho
        if (todosSprites != null && todosSprites.Length > 0)
        {
            if (corrotinaPiscar != null) StopCoroutine(corrotinaPiscar);
            corrotinaPiscar = StartCoroutine(RotinaPiscarVermelho());
        }

        if (vidaAtual <= 0)
        {
            Morrer();
        }
    }

    private IEnumerator RotinaPiscarVermelho()
    {
        // Pinta todas as partes do corpo de vermelho
        for (int i = 0; i < todosSprites.Length; i++)
        {
            if (todosSprites[i] != null)
                todosSprites[i].color = corDano;
        }

        yield return new WaitForSeconds(duracaoPiscar);

        // Volta todas as partes para a cor original
        for (int i = 0; i < todosSprites.Length; i++)
        {
            if (todosSprites[i] != null)
                todosSprites[i].color = coresOriginais[i];
        }
    }

    private void AtualizarBarraUI()
    {
        if (barraDeVida != null)
        {
            barraDeVida.maxValue = vidaMaxima;
            barraDeVida.value = vidaAtual;
        }
    }

    private void Morrer()
    {
        estaMorto = true;

        // Desativa a inteligência/ataques do Boss
        if (TryGetComponent<BossFomeController>(out var controller))
        {
            controller.enabled = false;
        }

        // Esconde a barra de vida da UI
        if (barraDeVida != null)
        {
            barraDeVida.gameObject.SetActive(false);
        }

        // Destrói o Boss sem trocar de cena
        Destroy(gameObject, 0.5f);
    }
}