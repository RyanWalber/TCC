using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movimentacao")]
    [SerializeField] private float velocidade = 6f;

    [Header("Pulo e Pulo Duplo")]
    [SerializeField] private float forcaDoPulo = 12f;
    [SerializeField] private float tempoCoyote = 0.15f; // Tolerância para imperfeições do chão (em segundos)

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField, Min(0.01f)] private float groundCheckRaio = 0.12f;
    [SerializeField] private LayerMask chaoLayers = 1 << 3; // Layer "Ground" do projeto
    [SerializeField] private bool desenharGroundCheck = true;
    private Collider2D colliderJogador;

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
    private PlataformaMovel plataformaSuporte;
    private float impulsoVerticalExtraPlataforma;

    private SistemaFuria sistemaFuria;

    public bool EstaDandoDash => estaDandoDash;
    public bool EstaMovendoNoChao => estaNoChao && !estaDandoDash && Mathf.Abs(inputHorizontal) > 0.1f;
    private KayaAudioManager audioKaya;

    // Chamado apenas quando os pes estao em contato com o topo da plataforma.
    public void DefinirPlataformaMovel(PlataformaMovel plataforma)
    {
        if (plataforma != null && !estaSubindoPulo)
            plataformaSuporte = plataforma;
    }

    public void RemoverPlataformaMovel(PlataformaMovel plataforma)
    {
        if (plataformaSuporte == plataforma)
            plataformaSuporte = null;
    }

    void Start()
    {
        audioKaya = GetComponent<KayaAudioManager>();
        rb = GetComponent<Rigidbody2D>();
        colliderJogador = GetComponent<Collider2D>();
        sistemaFuria = GetComponent<SistemaFuria>();

        if (groundCheck == null)
        {
            Transform encontrado = transform.Find("GroundCheck");
            if (encontrado == null)
                encontrado = transform.Find("GroundCheck (1)");

            if (encontrado != null)
                groundCheck = encontrado;
        }
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
        // Detecção contínua abaixo dos pés: mais estável nas junções entre plataformas.
        estaNoChao = VerificarChao() || (plataformaSuporte != null && !estaSubindoPulo);

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

        Vector2 velocidadePlataforma = plataformaSuporte != null && estaNoChao
            ? plataformaSuporte.VelocidadeAtual : Vector2.zero;
        float velocidadeY = rb.linearVelocity.y;

        if (!estaSubindoPulo && velocidadeY > 0f)
            velocidadeY = Mathf.Max(0f, velocidadePlataforma.y);

        // Nao limita o impulso herdado da plataforma no instante do pulo.
        float limitePulo = forcaDoPulo + impulsoVerticalExtraPlataforma;
        if (velocidadeY > limitePulo && estaSubindoPulo)
            velocidadeY = limitePulo;

        float velocidadeX = inputHorizontal * velocidadeEfetiva + velocidadePlataforma.x;
        rb.linearVelocity = new Vector2(velocidadeX, velocidadeY);
    }

    void AtualizarAnimacoes()
    {
        if (playerAnimation == null) return;

        bool estaMovimentando = !estaDandoDash && Mathf.Abs(inputHorizontal) > 0.01f;
        // O tempo de coyote serve apenas para permitir o pulo, não para
        // manter a animação de chão quando a Kaya já está no ar.
        bool noChaoParaAnimacao = estaNoChao;

        // Atualiza os três parâmetros juntos e detecta a aterrissagem.
        // AtualizarEstados força a transição para andar/ociosa ao tocar o chão,
        // evitando que a Kaya fique presa no estado de pulo em determinadas fases.
        playerAnimation.AtualizarEstados(
            estaMovimentando,
            noChaoParaAnimacao,
            estaDandoDash
        );
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

        if (audioKaya != null) audioKaya.TocarPulo();

        // Ao pular de uma plataforma que sobe, herda seu movimento vertical.
        // Sem isso, a plataforma rapida alcanca a Kaya e cancela o salto.
        impulsoVerticalExtraPlataforma = plataformaSuporte != null
            ? Mathf.Max(0f, plataformaSuporte.VelocidadeAtual.y) : 0f;
        plataformaSuporte = null;
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

        rb.linearVelocity = new Vector2(rb.linearVelocity.x,
            puloEfetivo + impulsoVerticalExtraPlataforma);
    }

    private bool VerificarChao()
    {
        Vector2 ponto;

        if (groundCheck != null)
        {
            ponto = groundCheck.position;
        }
        else if (colliderJogador != null)
        {
            // Fallback se a cena ainda não tiver o Transform GroundCheck atribuído.
            Bounds limites = colliderJogador.bounds;
            ponto = new Vector2(limites.center.x, limites.min.y + groundCheckRaio * 0.5f);
        }
        else
        {
            return false;
        }

        Collider2D[] contatos = Physics2D.OverlapCircleAll(ponto, groundCheckRaio);
        foreach (Collider2D chao in contatos)
        {
            if (chao == null || chao.isTrigger || chao == colliderJogador ||
                chao.transform.IsChildOf(transform)) continue;

            bool camadaChao = (chaoLayers.value & (1 << chao.gameObject.layer)) != 0;
            bool plataforma = chao.GetComponentInParent<PlataformaMovel>() != null ||
                              chao.GetComponentInParent<PlataformaDeQueda>() != null;
            if (camadaChao || plataforma) return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!desenharGroundCheck) return;

        Vector2 ponto;
        Collider2D col = GetComponent<Collider2D>();

        if (groundCheck != null)
            ponto = groundCheck.position;
        else if (col != null)
            ponto = new Vector2(col.bounds.center.x, col.bounds.min.y + groundCheckRaio * 0.5f);
        else
            ponto = transform.position;

        Gizmos.color = estaNoChao ? Color.green : Color.red;
        Gizmos.DrawWireSphere(ponto, groundCheckRaio);
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
                if (!estaNoChao && !estaDandoDash && audioKaya != null)
                    audioKaya.TocarAterrissagem();
                estaNoChao = true;
                pulosRestantes = maxPulos;
                impulsoVerticalExtraPlataforma = 0f;

                if (rb.linearVelocity.y <= 0.1f)
                {
                    estaSubindoPulo = false;
                }
                return;
            }
        }
    }

    private IEnumerator ExecutarDash()
    {
        if (audioKaya != null) audioKaya.TocarDash();
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