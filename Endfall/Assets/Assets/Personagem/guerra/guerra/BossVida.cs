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

    [Tooltip("Arraste aqui o objeto PAI de toda a UI da barra (ex: BarraVidaBoss)")]
    [SerializeField] private GameObject conteinerBarraDeVida;

    [Header("Feedback de Dano (Piscar Vermelho)")]
    [SerializeField] private Color corDano = Color.red;
    [SerializeField] private float duracaoPiscar = 0.15f;

    [Header("Morte")]
    [SerializeField] private float tempoParaDestruir = 0.5f;

    private SpriteRenderer[] todosSprites;
    private Color[] coresOriginais;
    private Coroutine corrotinaPiscar;
    private bool estaMorto = false;

    public bool EstaMorto => estaMorto;

    private void Start()
    {
        vidaAtual = vidaMaxima;

        // Pega todos os SpriteRenderers do Boss e dos filhos
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

    public void ReceberDano(float dano) => TomarDano(dano);

    public void TomarDano(float dano)
    {
        if (estaMorto) return;

        vidaAtual -= dano;
        vidaAtual = Mathf.Clamp(vidaAtual, 0f, vidaMaxima);

        AtualizarBarraUI();

        if (todosSprites != null && todosSprites.Length > 0)
        {
            if (corrotinaPiscar != null) StopCoroutine(corrotinaPiscar);
            corrotinaPiscar = StartCoroutine(RotinaPiscarVermelho());
        }

        if (vidaAtual <= 0f)
        {
            Morrer();
        }
    }

    // Retorna um valor entre 0.0 (0%) e 1.0 (100%)
    public float ObterPercentualVida()
    {
        if (vidaMaxima <= 0f) return 0f;
        return vidaAtual / vidaMaxima;
    }

    private IEnumerator RotinaPiscarVermelho()
    {
        for (int i = 0; i < todosSprites.Length; i++)
        {
            if (todosSprites[i] != null)
                todosSprites[i].color = corDano;
        }

        yield return new WaitForSeconds(duracaoPiscar);

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
        if (estaMorto) return;
        estaMorto = true;

        // Desativa física e colisões
        if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;
        if (TryGetComponent<Rigidbody2D>(out var rb)) rb.simulated = false;

        // Desativa os scripts de comportamento/IA do Boss automaticamente
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this) script.enabled = false;
        }

        // Destrói o objeto pai da barra de vida completa
        if (conteinerBarraDeVida != null)
        {
            Destroy(conteinerBarraDeVida);
        }
        else if (barraDeVida != null)
        {
            Transform pai = barraDeVida.transform.parent;
            Destroy(pai != null ? pai.gameObject : barraDeVida.gameObject);
        }

        Destroy(gameObject, tempoParaDestruir);
    }
}