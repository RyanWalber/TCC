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
        if (jaColidiu) return;

        if (collision.CompareTag("Player") ||
            collision.transform.root.CompareTag("Player") ||
            collision.name.Contains("Borda") ||
            collision.CompareTag("MainCamera"))
        {
            return;
        }

        jaColidiu = true;

        // Tag definida no projeto: "Inimigo". Colidir com chão, parede ou objetos
        // comuns destrói o projétil, mas não concede Fúria.
        bool acertouInimigo = collision.CompareTag("Inimigo") ||
                              collision.transform.root.CompareTag("Inimigo");

        int danoCalculado = dano;
        SistemaFuria furia = FindFirstObjectByType<SistemaFuria>();
        if (furia != null && furia.EstaEmFuria)
            danoCalculado = Mathf.RoundToInt(dano * furia.multDano);

        if (acertouInimigo && furia != null && !furia.EstaEmFuria)
            furia.AdicionarFuria(furiaPorAcerto);

        collision.SendMessageUpwards("TomarDano", danoCalculado, SendMessageOptions.DontRequireReceiver);

        Destroy(gameObject);
    }
}