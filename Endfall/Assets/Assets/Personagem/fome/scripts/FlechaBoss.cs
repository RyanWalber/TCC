using UnityEngine;

public class FlechaBoss : MonoBehaviour
{
    [Header("Configurações da Flecha")]
    [SerializeField] private float velocidade = 12f;
    [SerializeField] private float dano = 20f; // <--- Esta variável vai criar o campo no Inspector
    [SerializeField] private float tempoVida = 5f;

    [Header("Ajuste de Rotação")]
    [Tooltip("Ajuste esse valor se a flecha sair torta. Teste com -90, 90 ou 180 dependendo de como o sprite foi desenhado.")]
    [SerializeField] private float offsetAngulo = -90f;

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

        // Calcula o ângulo em direção ao jogador
        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;

        // Aplica a rotação corrigida pelo offset do sprite
        transform.rotation = Quaternion.Euler(0, 0, angulo + offsetAngulo);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth))
            {
                playerHealth.TomarDano(dano); // Aplica o valor de dano configurado
            }
            Destroy(gameObject);
        }
        else if (collision.CompareTag("Chao") || collision.CompareTag("Parede"))
        {
            Destroy(gameObject);
        }
    }
}