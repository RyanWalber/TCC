using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ProjetilSimulado : MonoBehaviour
{
    public float velocidade = 15f;
    public float tempoDeVida = 3f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, tempoDeVida);
    }

    public void ConfigurarDirecao(float direcaoX)
    {
        float sentido = direcaoX >= 0 ? 1f : -1f;
        rb.linearVelocity = new Vector2(sentido * velocidade, 0f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Inimigo") || collision.CompareTag("Untagged"))
        {
            Destroy(gameObject);
        }
    }
}