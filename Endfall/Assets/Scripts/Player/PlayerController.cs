using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;
    public int maxJumps = 2;

    [Header("Física e Chão")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    
    [Header("Mecânica de Dash")]
    public float dashSpeed = 22f;
    public float dashDuration = 0.15f;

    private Rigidbody2D rb;
    private Animator anim;
    private float horizontal;
    private bool isGrounded;
    private int jumpsLeft;
    private bool isDashing;
    private float dashTimer;
    private float savedGravity;
    private bool facingRight = true;
    private bool overrideFacing = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // Pega o Animator que está no objeto ou nos filhos (como o Ani Kaya)
        anim = GetComponentInChildren<Animator>();
        savedGravity = rb.gravityScale;
        facingRight = transform.localScale.x >= 0f;
    }

    void Update()
    {
        // Se estiver dando Dash, ignora os comandos temporariamente
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
                rb.gravityScale = savedGravity;
            }
            return;
        }

        // 1. Pega o comando das setas ou A/D do teclado
        horizontal = Input.GetAxisRaw("Horizontal");

        // 2. CONTROLE DA ANIMAÇÃO (O vai e vem que conversamos!)
        if (anim != null)
        {
            // Se horizontal for diferente de 0, significa que está apertando o teclado
            bool estaAndando = (horizontal != 0);
            anim.SetBool("isWalking", estaAndando); 
        }

        // 3. Controla a direção do corpo baseada no teclado (se o mouse não estiver controlando)
        if (!overrideFacing)
        {
            if (horizontal > 0)
            {
                facingRight = true;
                UpdateLocalScale();
            }
            else if (horizontal < 0)
            {
                facingRight = false;
                UpdateLocalScale();
            }
        }
    }

    // O FixedUpdate é onde a mágica da física acontece! Faz ela andar de verdade.
    void FixedUpdate()
    {
        if (isDashing) return;

        // Aplica a velocidade horizontal mantendo a gravidade caindo normal
        rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
    }

    // Atualiza o lado que o boneco está olhando na tela
    private void UpdateLocalScale()
    {
        Vector3 scale = transform.localScale;
        scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    // --- FUNÇÕES QUE A MIRA DO MOUSE PRECISA (Isso evita os erros no console!) ---
    
    public void SetFacing(bool faceRight, bool isOverriding)
    {
        overrideFacing = isOverriding;
        facingRight = faceRight;
        UpdateLocalScale();
    }

    public void ClearFacingOverride()
    {
        overrideFacing = false;
    }
}