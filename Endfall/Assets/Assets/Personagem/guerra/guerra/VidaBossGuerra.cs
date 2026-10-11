using System.Collections;
using UnityEngine;

public class VidaBossGuerra : MonoBehaviour
{
    [Header("CONFIGURAÇÃO DE VIDA")]
    [SerializeField] private float vidaMaxima = 500f;
    private float vidaAtual;

    [Header("FEEDBACK DE DANO")]
    [SerializeField] private Color corDano = Color.red;
    [SerializeField] private float duracaoFlash = 0.15f;

    private SpriteRenderer[] todosSprites;
    private Color[] coresOriginais;
    private Coroutine corrotinaFlash;
    private bool estaMorto = false;

    private void Start()
    {
        vidaAtual = vidaMaxima;

        todosSprites = GetComponentsInChildren<SpriteRenderer>();
        if (todosSprites != null && todosSprites.Length > 0)
        {
            coresOriginais = new Color[todosSprites.Length];
            for (int i = 0; i < todosSprites.Length; i++)
            {
                coresOriginais[i] = todosSprites[i].color;
            }
        }
    }

    // Aceita dano em INT (do seu Projetil.cs e inimigos padrão)
    public void TomarDano(int quantidadeDano)
    {
        TomarDano((float)quantidadeDano);
    }

    // Aceita dano em FLOAT (caso algum ataque use valores decimais)
    public void TomarDano(float quantidadeDano)
    {
        if (estaMorto) return;

        vidaAtual -= quantidadeDano;
        vidaAtual = Mathf.Clamp(vidaAtual, 0f, vidaMaxima);

        if (corrotinaFlash != null) StopCoroutine(corrotinaFlash);
        corrotinaFlash = StartCoroutine(EfeitoFlashDano());

        if (vidaAtual <= 0f)
        {
            Morrer();
        }
    }

    private IEnumerator EfeitoFlashDano()
    {
        for (int i = 0; i < todosSprites.Length; i++)
        {
            if (todosSprites[i] != null)
            {
                todosSprites[i].color = corDano;
            }
        }

        yield return new WaitForSeconds(duracaoFlash);

        for (int i = 0; i < todosSprites.Length; i++)
        {
            if (todosSprites[i] != null)
            {
                todosSprites[i].color = coresOriginais[i];
            }
        }
    }

    private void Morrer()
    {
        estaMorto = true;

        BossGuerraController controller = GetComponent<BossGuerraController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("Morte");
        }

        Destroy(gameObject, 2f);
    }

    public float GetVidaAtual() => vidaAtual;
    public float GetVidaMaxima() => vidaMaxima;
}