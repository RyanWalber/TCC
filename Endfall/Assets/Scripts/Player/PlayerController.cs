using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movimentacao")]
    [SerializeField] private float velocidade = 6f;

    [Header("Pulo e Pulo Duplo")]
    [SerializeField] private float forcaDoPulo = 12f;

    [Header("Animacao")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private float inputHorizontal;
    private bool estaNoChao;
    private int pulosRestantes;
    private int maxPulos = 2;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        inputHorizontal = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(rb.linearVelocity.y) < 0.01f)
        {
            estaNoChao = true;
            pulosRestantes = maxPulos;
        }
        else
        {
            estaNoChao = false;
        }

        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
        {
            if (estaNoChao || pulosRestantes > 0)
            {
                Pular();
            }
        }

        if (animator != null)
        {
            if (Mathf.Abs(inputHorizontal) > 0.1f)
            {
                animator.speed = 1f;
            }
            else
            {
                animator.speed = 0f;
            }
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(inputHorizontal * velocidade, rb.linearVelocity.y);
    }

    void Pular()
    {
        if (!estaNoChao)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, forcaDoPulo);
        pulosRestantes--;
    }
}