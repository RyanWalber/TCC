using UnityEngine;

public class FlechaBoss : MonoBehaviour
{
    [SerializeField] private float velocidade = 12f;
    [SerializeField] private float tempoVida = 5f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, tempoVida);
    }

    public void DefinirDirecao(Vector2 direcao)
    {
        if (rb != null)
        {
            rb.linearVelocity = direcao.normalized * velocidade;
        }

        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angulo, Vector3.forward);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
        else if (collision.CompareTag("Chao") || collision.CompareTag("Parede"))
        {
            Destroy(gameObject);
        }
    }
}