using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Movimentacao")]
    [SerializeField] private float velocidade = 6f;

    [Header("Pulo e Pulo Duplo")]
    [SerializeField] private float forcaDoPulo = 12f;
    [SerializeField] private float tempoCoyote = 0.15f; // Tolerância para imperfeições do chão (em segundos)

    [Header("Dash")]
    [SerializeField] private float forcaDoDash = 20f;
    [SerializeField] private float duracaoDash = 0.2f;
    [SerializeField] private float tempoEsperaDash = 1f;

    [Header("Animacao")]
    [SerializeField] private PlayerAnimation playerAnimation;

    private Rigidbody2D rb;
    private float inputHorizontal;
    private bool estaNoChao = false;
    private int pulosRestantes;
    private int maxPulos = 2; // Total: 1 do chão + 1 no ar

    private float contadorCoyote;
    private bool podeDarDash = true;
    private bool estaDandoDash;
    private float direcaoDash;
    private float gravidadeOriginal;
    private bool estaSubindoPulo;
    private readonly HashSet<GameObject> objetosChao = new HashSet<GameObject>();

    private SistemaFuria sistemaFuria;

    public bool EstaDandoDash => estaDandoDash;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sistemaFuria = GetComponent<SistemaFuria>();
        gravidadeOriginal = rb.gravityScale;
        pulosRestantes = maxPulos;

        if (playerAnimation == null)
        {
            playerAnimation = GetComponentInChildren<PlayerAnimation>();
        }

        // --- CARREGA STATUS SALVO ---
        CarregarStatusDoSaveManager();
    }

    private void CarregarStatusDoSaveManager()
    {
        if (SaveManager.Instance != null && sistemaFuria != null)
        {
            float furiaSalva = SaveManager.Instance.GetFuriaKaya();
            sistemaFuria.DefinirFuria(furiaSalva);
        }
    }

    public void SalvarStatusKayaNoSaveManager(float vidaAtual)
    {
        if (SaveManager.Instance != null)
        {
            float furiaAtual = sistemaFuria != null ? sistemaFuria.FuriaAtual : 0f;
            SaveManager.Instance.AtualizarStatusKaya(vidaAtual, furiaAtual);
        }
    }

    void Update()
    {
        if (estaNoChao)
        {
            contadorCoyote = tempoCoyote;
        }
        else
        {
            contadorCoyote -= Time.deltaTime;
        }

        if (!estaDandoDash)
        {
            inputHorizontal = Input.GetAxisRaw("Horizontal");

            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
            {
                if (contadorCoyote > 0f || pulosRestantes > 0)
                {
                    Pular();
                }
            }

            if (Input.GetKeyDown(KeyCode.LeftShift) && podeDarDash && Mathf.Abs(inputHorizontal) > 0.1f)
            {
                StartCoroutine(ExecutarDash());
            }
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

        float velocidadeEfetiva = velocidade;
        if (sistemaFuria != null && sistemaFuria.EstaEmFuria)
        {
            velocidadeEfetiva *= sistemaFuria.multVelocidade;
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

        rb.linearVelocity = new Vector2(inputHorizontal * velocidadeEfetiva, velocidadeY);
    }

    void AtualizarAnimacoes()
    {
        if (playerAnimation == null) return;

        bool estaMovimentando = !estaDandoDash && Mathf.Abs(inputHorizontal) > 0.01f;
        // O tempo de coyote serve apenas para permitir o pulo, não para
        // manter a animação de chão quando a Kaya já está no ar.
        bool noChaoParaAnimacao = estaNoChao;

        playerAnimation.DefinirAndando(estaMovimentando);
        playerAnimation.DefinirNoChao(noChaoParaAnimacao);
    }

    void Pular()
    {
        bool puloDoChao = contadorCoyote > 0f && pulosRestantes == maxPulos;

        if (puloDoChao)
        {
            pulosRestantes--;
        }
        else if (pulosRestantes > 0)
        {
            pulosRestantes--;

            if (playerAnimation != null)
            {
                bool estaAndando = Mathf.Abs(inputHorizontal) > 0.01f;
                playerAnimation.ReiniciarPulo(estaAndando);
            }
        }

        contadorCoyote = 0f;
        estaSubindoPulo = true;
        estaNoChao = false;

        float puloEfetivo = forcaDoPulo;
        if (sistemaFuria != null && sistemaFuria.EstaEmFuria)
        {
            puloEfetivo *= sistemaFuria.multPulo;
        }

        float gravidadeG = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        float tempoTotalNoAr = (2f * puloEfetivo) / gravidadeG;

        float duracaoOriginalSprite = 2.0f;
        float multiplicadorVelocidade = duracaoOriginalSprite / tempoTotalNoAr;

        if (playerAnimation != null)
        {
            playerAnimation.AjustarVelocidadePulo(multiplicadorVelocidade);
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, puloEfetivo);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        ProcessarContatoChao(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        ProcessarContatoChao(collision);
    }

    private void ProcessarContatoChao(Collision2D collision)
    {
        if (estaSubindoPulo && rb.linearVelocity.y > 0.1f) return;

        foreach (ContactPoint2D contato in collision.contacts)
        {
            if (contato.normal.y > 0.5f)
            {
                objetosChao.Add(collision.gameObject);
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
        // Só perdemos o estado de chão ao sair de um objeto que realmente
        // fornecia apoio por cima. Sair de uma parede não deve afetar o pulo.
        if (objetosChao.Remove(collision.gameObject) && objetosChao.Count == 0)
        {
            estaNoChao = false;
        }
    }

    private IEnumerator ExecutarDash()
    {
        podeDarDash = false;
        estaDandoDash = true;
        direcaoDash = inputHorizontal;

        if (playerAnimation != null)
        {
            playerAnimation.DefinirDash(true);
        }

        rb.gravityScale = 0f;

        yield return new WaitForSeconds(duracaoDash);

        rb.gravityScale = gravidadeOriginal;
        estaDandoDash = false;

        if (playerAnimation != null)
        {
            playerAnimation.DefinirDash(false);
        }

        yield return new WaitForSeconds(tempoEsperaDash);
        podeDarDash = true;
    }
}