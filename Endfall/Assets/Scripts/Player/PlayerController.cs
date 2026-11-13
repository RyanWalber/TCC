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

    [Header("Protecao do pulo e das quinas")]
    [Tooltip("Limita o impulso extra de plataformas que sobem. 0.2 = ate 20% da forca de pulo.")]
    [Range(0f, 0.5f)] [SerializeField] private float maxHerancaVerticalPlataforma = 0.2f;

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
    private bool controlesBloqueadosPorDialogo;
    private bool controlesBloqueadosPelaIntroBoss;

    // O mesmo estado e consultado por TiroPlayer e SistemaFuria:
    // a intro tambem bloqueia tiro e Furia, sem afetar a trava dos dialogos.
    public bool ControlesBloqueadosPorDialogo =>
        controlesBloqueadosPorDialogo || controlesBloqueadosPelaIntroBoss;

    public void DefinirBloqueioIntroBoss(bool bloquear)
    {
        if (controlesBloqueadosPelaIntroBoss == bloquear) return;
        bool estavaBloqueado = ControlesBloqueadosPorDialogo;
        controlesBloqueadosPelaIntroBoss = bloquear;
        AtualizarEstadoBloqueio(estavaBloqueado);
    }

    // Desativa somente os comandos: PlayerController, gravidade e Animator continuam ativos.
    public void DefinirBloqueioDialogo(bool bloquear)
    {
        if (controlesBloqueadosPorDialogo == bloquear) return;
        bool estavaBloqueado = ControlesBloqueadosPorDialogo;
        controlesBloqueadosPorDialogo = bloquear;
        AtualizarEstadoBloqueio(estavaBloqueado);
    }

    private void AtualizarEstadoBloqueio(bool estavaBloqueado)
    {
        inputHorizontal = 0f;

        if (!estavaBloqueado && ControlesBloqueadosPorDialogo && rb != null)
        {
            if (estaDandoDash)
            {
                StopAllCoroutines();
                estaDandoDash = false;
                podeDarDash = true;
                direcaoDash = 0f;
                rb.gravityScale = gravidadeOriginal;
                if (playerAnimation != null)
                    playerAnimation.DefinirDash(false);
            }

            // Para na horizontal e cancela a subida do pulo, sem congelar
            // a fisica. Kaya continua podendo cair sobre a plataforma.
            float velocidadeVertical = Mathf.Min(0f, rb.linearVelocity.y);
            rb.linearVelocity = new Vector2(0f, velocidadeVertical);
            rb.WakeUp();
        }

        if (isActiveAndEnabled)
            AtualizarAnimacoes();
    }

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

        if (ControlesBloqueadosPorDialogo)
        {
            inputHorizontal = 0f;
            AtualizarAnimacoes();
            return;
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

        if (ControlesBloqueadosPorDialogo)
        {
            // Sem input, mas preservando gravidade e movimento das plataformas.
            float movimentoPlataforma = plataformaSuporte != null && estaNoChao
                ? plataformaSuporte.VelocidadeAtual.x : 0f;
            rb.linearVelocity = new Vector2(movimentoPlataforma, rb.linearVelocity.y);
            return;
        }

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

        // Protecao global: qualquer impulso vertical inesperado da fisica
        // (por exemplo, na quina de um collider) respeita o limite do pulo.
        // O limite acompanha a Furia e a heranca permitida da plataforma.
        float limitePulo = forcaDoPulo;
        if (sistemaFuria != null && sistemaFuria.EstaEmFuria)
            limitePulo *= Mathf.Max(1f, sistemaFuria.multPulo);
        limitePulo += impulsoVerticalExtraPlataforma;

        // Aplica tambem fora do salto: um contato lateral nao pode lancar a
        // Kaya acima da velocidade que o proprio pulo permitiria.
        if (velocidadeY > limitePulo)
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
        // Mesmo com o coyote time ativo, um salto sem cargas restantes
        // nunca deve aplicar outro impulso.
        if (pulosRestantes <= 0) return;

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

        float puloEfetivo = forcaDoPulo;
        if (sistemaFuria != null && sistemaFuria.EstaEmFuria)
            puloEfetivo *= Mathf.Max(1f, sistemaFuria.multPulo);

        // Preserva o pulo de plataformas moveis, mas limita o adicional:
        // uma plataforma muito rapida jamais deve arremessar a Kaya.
        float velocidadeSuporte = plataformaSuporte != null
            ? Mathf.Max(0f, plataformaSuporte.VelocidadeAtual.y) : 0f;
        impulsoVerticalExtraPlataforma = Mathf.Min(
            velocidadeSuporte, puloEfetivo * maxHerancaVerticalPlataforma);

        plataformaSuporte = null;
        contadorCoyote = 0f;
        estaSubindoPulo = true;
        estaNoChao = false;

        float gravidadeG = Mathf.Max(0.01f, Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale);
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
        if (colliderJogador == null) return false;

        // Durante a subida, o GroundCheck ainda pode estar encostado no
        // chao por alguns frames; isso NAO deve recarregar o pulo duplo.
        if (estaSubindoPulo && rb.linearVelocity.y > 0.1f)
            return false;

        Bounds corpo = colliderJogador.bounds;
        float yOrigem = corpo.min.y + Mathf.Max(0.35f, groundCheckRaio * 0.5f);
        float xCentro = corpo.center.x;
        if (groundCheck != null)
        {
            yOrigem = Mathf.Max(yOrigem, groundCheck.position.y);
            xCentro = groundCheck.position.x;
        }

        // Procura apenas um pouco abaixo dos pes, nao nas laterais da Kaya.
        float distancia = yOrigem - corpo.min.y + Mathf.Max(0.25f, groundCheckRaio * 0.2f);
        float lateral = corpo.extents.x * 0.6f;

        // Tres raios mantem o pulo responsivo nas bordas das plataformas.
        // Apenas superficies com normal voltada para CIMA valem como chao.
        for (int i = -1; i <= 1; i++)
        {
            Vector2 origem = new Vector2(xCentro + i * lateral, yOrigem);
            RaycastHit2D[] acertos = Physics2D.RaycastAll(origem, Vector2.down, distancia);
            foreach (RaycastHit2D acerto in acertos)
            {
                Collider2D chao = acerto.collider;
                if (chao == null || chao.isTrigger || chao == colliderJogador ||
                    chao.transform.IsChildOf(transform) || acerto.fraction <= 0f ||
                    acerto.normal.y < 0.7f)
                    continue;

                bool camadaChao = (chaoLayers.value & (1 << chao.gameObject.layer)) != 0;
                bool plataforma = chao.GetComponentInParent<PlataformaMovel>() != null ||
                                  chao.GetComponentInParent<PlataformaDeQueda>() != null;
                if (camadaChao || plataforma)
                    return true;
            }
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

        float limiteDosPes = colliderJogador != null
            ? colliderJogador.bounds.min.y + Mathf.Max(1f, groundCheckRaio * 0.6f)
            : transform.position.y;

        foreach (ContactPoint2D contato in collision.contacts)
        {
            // A quina/lateral da plataforma nao deve contar como aterrissagem.
            // Exige normal principalmente vertical E contato na sola dos pes.
            if (contato.normal.y >= 0.7f && contato.point.y <= limiteDosPes)
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