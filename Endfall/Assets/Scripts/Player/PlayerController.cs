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
    [SerializeField] private string nomeParametroVelocidade = "Velocidade";
    [SerializeField] private string nomeParametroChao = "estaNoChao";

    private Rigidbody2D rb;
    private float inputHorizontal;
    private bool estaNoChao;
    private int pulosRestantes;
    private int maxPulos = 2;

    private bool podeDarDash = true;
    private bool estaDandoDash;
    private float direcaoDash;
    private float gravidadeOriginal;
    private bool estaSubindoPulo;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        gravidadeOriginal = rb.gravityScale;
        pulosRestantes = maxPulos;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        if (estaDandoDash) return;

        inputHorizontal = Input.GetAxisRaw("Horizontal");

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

        AtualizarAnimacoes();
    }

    void FixedUpdate()
    {
        if (estaDandoDash)
        {
            rb.linearVelocity = new Vector2(direcaoDash * forcaDoDash, 0f);
            return;
        }

        float velocidadeY = rb.linearVelocity.y;

        if (!estaSubindoPulo && velocidadeY > 0f)
        {
            velocidadeY = 0f;
        }

        if (velocidadeY > forcaDoPulo)
        {
            velocidadeY = forcaDoPulo;
        }

        rb.linearVelocity = new Vector2(inputHorizontal * velocidade, velocidadeY);
    }

    void AtualizarAnimacoes()
    {
        if (animator != null)
        {
            animator.SetFloat(nomeParametroVelocidade, Mathf.Abs(inputHorizontal));
            animator.SetBool(nomeParametroChao, estaNoChao);
        }
    }

    void Pular()
    {
        estaSubindoPulo = true;
        estaNoChao = false;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, forcaDoPulo);
        pulosRestantes--;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        foreach (ContactPoint2D contato in collision.contacts)
        {
            if (contato.normal.y < -0.2f)
            {
                estaSubindoPulo = false;
                if (rb.linearVelocity.y > 0f)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                }
            }

            if (contato.normal.y > 0.5f)
            {
                estaNoChao = true;
                pulosRestantes = maxPulos;

                if (rb.linearVelocity.y <= 0.1f)
                {
                    estaSubindoPulo = false;
                }
                return;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        estaNoChao = false;
    }

    private IEnumerator ExecutarDash()
    {
        podeDarDash = false;
        estaDandoDash = true;
        direcaoDash = inputHorizontal;

        rb.gravityScale = 0f;

        yield return new WaitForSeconds(duracaoDash);

        rb.gravityScale = gravidadeOriginal;
        estaDandoDash = false;

        yield return new WaitForSeconds(tempoEsperaDash);
        podeDarDash = true;
    }
}