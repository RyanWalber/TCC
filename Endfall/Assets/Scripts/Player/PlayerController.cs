using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movimentacao")]
    [SerializeField] private float velocidade = 6f;

    [Header("Pulo e Pulo Duplo")]
    [SerializeField] private float forcaDoPulo = 12f;

    [Header("Dash")]
    [SerializeField] private float forcaDoDash = 20f;
    [SerializeField] private float duracaoDash = 0.2f;
    [SerializeField] private float tempoEsperaDash = 1f;

    [Header("Animacao")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private float inputHorizontal;
    private bool estaNoChao;
    private int pulosRestantes;
    private int maxPulos = 2;

    private bool podeDarDash = true;
    private bool estaDandoDash;
    private float gravidadeOriginal;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        gravidadeOriginal = rb.gravityScale;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        if (estaDandoDash) return;

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

        if (Input.GetKeyDown(KeyCode.LeftShift) && podeDarDash && Mathf.Abs(inputHorizontal) > 0.1f)
        {
            StartCoroutine(ExecutarDash());
        }

        if (animator != null)
        {
            animator.speed = 1f;
        }
    }

    void FixedUpdate()
    {
        if (estaDandoDash) return;

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

    private IEnumerator ExecutarDash()
    {
        podeDarDash = false;
        estaDandoDash = true;
        
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(inputHorizontal * forcaDoDash, 0f);

        yield return new WaitForSeconds(duracaoDash);

        rb.gravityScale = gravidadeOriginal;
        estaDandoDash = false;

        yield return new WaitForSeconds(tempoEsperaDash);
        podeDarDash = true;
    }
}