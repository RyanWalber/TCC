using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projetil : MonoBehaviour
{
    public float velocidade = 20f;
    public float tempoDeVida = 3f;
    public int dano = 1;

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

        InimigoBixel inimigo = collision.GetComponentInParent<InimigoBixel>();
        if (inimigo != null)
        {
            inimigo.TomarDano(dano);
        }

        Destroy(gameObject);
    }
}