using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projetil : MonoBehaviour
{
    public float velocidade = 20f;
    public float tempoDeVida = 3f;
    public int dano = 1;

    [Header("Fúria")]
    public float furiaPorAcerto = 8f;

    private Rigidbody2D rb;
    private bool jaColidiu = false;

    // Um inimigo pode ter varios colliders trigger. Evita dano duplicado
    // pelo mesmo projetil enquanto ele atravessa esses hitboxes.
    private readonly HashSet<Transform> inimigosAtingidos = new HashSet<Transform>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
    }

    private void Start()
    {
        Destroy(gameObject, tempoDeVida);
    }

    public void Disparar()
    {
        rb.linearVelocity = transform.right * velocidade;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (jaColidiu || collision == null) return;

        if (collision.CompareTag("Player") ||
            collision.transform.root.CompareTag("Player") ||
            collision.name.Contains("Borda") ||
            collision.CompareTag("MainCamera"))
        {
            return;
        }

        bool acertouInimigo = collision.CompareTag("Inimigo") ||
                              collision.transform.root.CompareTag("Inimigo");

        // Triggers de elixir, moeda, checkpoint etc. nao bloqueiam o tiro.
        // Triggers dos inimigos recebem dano, mas tambem nao param o tiro.
        if (collision.isTrigger)
        {
            if (acertouInimigo)
                AplicarDanoAoInimigo(collision);
            return;
        }

        // Apenas um collider solido (Is Trigger desmarcado) para o tiro.
        jaColidiu = true;
        if (acertouInimigo)
            AplicarDanoAoInimigo(collision);

        Destroy(gameObject);
    }

    private void AplicarDanoAoInimigo(Collider2D collision)
    {
        Transform inimigo = collision.transform.root;
        if (!inimigosAtingidos.Add(inimigo))
            return;

        int danoCalculado = dano;
        SistemaFuria furia = FindFirstObjectByType<SistemaFuria>();
        if (furia != null && furia.EstaEmFuria)
            danoCalculado = Mathf.RoundToInt(dano * furia.multDano);

        if (furia != null && !furia.EstaEmFuria)
            furia.AdicionarFuria(furiaPorAcerto);

        collision.SendMessageUpwards("TomarDano", danoCalculado,
            SendMessageOptions.DontRequireReceiver);
    }
}
