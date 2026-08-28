using System.Collections;
using UnityEngine;
using DragonBones;
using Transform = UnityEngine.Transform;

[RequireComponent(typeof(Rigidbody2D))]
public class InimigoBixao : MonoBehaviour
{
    private enum Estado { Esperando, Rodeando, Preparando, Avancando }

    [Header("CONFIGURAÇÃO VISUAL E ANIMAÇÕES")]
    public Transform pivoVisual;
    public UnityArmatureComponent armatureComponent;
    public string animIdle = "idle";
    public string animVoar = "fly";
    public string animPrepara = "prepare";
    public string animAtaque = "attack";
    public bool spriteInvertido = false;

    [Header("Detecção e Raios")]
    public float raioAtivacao = 200f;
    public float raioRodeio = 50f;
    public Vector2 offsetDeteccao;
    public Vector2 offsetPlayer = new Vector2(0f, 5f);
    public Transform player;

    [Header("Velocidades")]
    public float velocidadeOscilacao = 2f;
    public float velocidadeDeslocamento = 50f;
    public float velocidadeBote = 80f;

    [Header("Tempos do Ataque")]
    public float tempoRodeando = 4.0f;
    public float tempoPreparacao = 0.6f;
    public float tempoBote = 0.8f;
    public float cooldownAtaque = 1.2f;

    [Header("Vida e Impacto")]
    public int vidaMaxima = 5;
    private int vidaAtual;
    public int danoNoJogador = 2;
    public float forcaImpacto = 15f;
    public float cooldownImpacto = 0.8f;
    private float tempoProximoImpacto;

    private Rigidbody2D rb;
    private Estado estadoAtual = Estado.Esperando;
    private float temporizadorEstado;
    private Vector2 direcaoBote;
    private bool olhandoDireita = true;
    private bool estaMorto = false;
    private Vector3 escalaOriginalPivo;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
    }

    private void Start()
    {
        vidaAtual = vidaMaxima;

        if (pivoVisual == null && transform.childCount > 0)
            pivoVisual = transform.GetChild(0);

        if (pivoVisual != null)
            escalaOriginalPivo = pivoVisual.localScale;
        else
            escalaOriginalPivo = transform.localScale;

        if (armatureComponent == null)
            armatureComponent = GetComponentInChildren<UnityArmatureComponent>();

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        TocarAnimacao(animIdle);
    }

    private void Update()
    {
        if (estaMorto || player == null) return;

        AtualizarOrientacao();

        switch (estadoAtual)
        {
            case Estado.Esperando:
                CheckDeteccao();
                break;

            case Estado.Rodeando:
                if (Time.time >= temporizadorEstado)
                {
                    MudarEstado(Estado.Preparando, Time.time + tempoPreparacao);
                    rb.linearVelocity = Vector2.zero;
                }
                break;

            case Estado.Preparando:
                rb.linearVelocity = Vector2.zero;
                if (Time.time >= temporizadorEstado)
                {
                    MudarEstado(Estado.Avancando, Time.time + tempoBote);
                }
                break;

            case Estado.Avancando:
                if (Time.time >= temporizadorEstado)
                {
                    VoltarParaRodeio();
                }
                break;
        }
    }

    private void FixedUpdate()
    {
        if (estaMorto || player == null) return;

        if (estadoAtual == Estado.Rodeando)
        {
            ModoRodeioMeiaLua();
        }
        else if (estadoAtual == Estado.Avancando)
        {
            ModoAvanco();
        }
    }

    private void CheckDeteccao()
    {
        Vector3 centro = transform.position + (Vector3)offsetDeteccao;
        float distancia = Vector2.Distance(player.position, centro);

        if (distancia <= raioAtivacao)
        {
            MudarEstado(Estado.Rodeando, Time.time + tempoRodeando);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            TocarAnimacao(animIdle);
        }
    }

    private void ModoRodeioMeiaLua()
    {
        TocarAnimacao(animVoar);

        float oscilacao = Mathf.Sin(Time.time * velocidadeOscilacao);
        float anguloGraus = 90f + (oscilacao * 50f);
        float anguloRad = anguloGraus * Mathf.Deg2Rad;

        Vector2 centroPlayer = (Vector2)player.position + offsetPlayer;
        Vector2 offsetArco = new Vector2(Mathf.Cos(anguloRad), Mathf.Sin(anguloRad)) * raioRodeio;
        Vector2 posicaoAlvo = centroPlayer + offsetArco;

        Vector2 direcao = posicaoAlvo - (Vector2)transform.position;
        rb.linearVelocity = direcao.normalized * velocidadeDeslocamento;
    }

    private void ModoPreparacao()
    {
        TocarAnimacao(animPrepara);
        rb.linearVelocity = Vector2.zero;

        Vector2 centroPlayer = (Vector2)player.position + offsetPlayer;
        direcaoBote = (centroPlayer - (Vector2)transform.position).normalized;
    }

    private void ModoAvanco()
    {
        TocarAnimacao(animAtaque);
        rb.linearVelocity = direcaoBote * velocidadeBote;
    }

    private void VoltarParaRodeio()
    {
        MudarEstado(Estado.Rodeando, Time.time + cooldownAtaque + tempoRodeando);
    }

    private void MudarEstado(Estado novoEstado, float tempoTermino)
    {
        if (novoEstado == Estado.Preparando)
        {
            ModoPreparacao();
        }

        estadoAtual = novoEstado;
        temporizadorEstado = tempoTermino;
    }

    private void TocarAnimacao(string nomeAnimacao)
    {
        if (armatureComponent == null || armatureComponent.armature == null || string.IsNullOrEmpty(nomeAnimacao)) return;

        if (armatureComponent.animation.lastAnimationName != nomeAnimacao)
        {
            armatureComponent.animation.Play(nomeAnimacao, 0);
        }
    }

    private void AtualizarOrientacao()
    {
        if (pivoVisual == null || player == null) return;

        olhandoDireita = player.position.x >= transform.position.x;

        float absX = Mathf.Abs(escalaOriginalPivo.x);
        float sinalX = olhandoDireita ? absX : -absX;

        if (spriteInvertido)
        {
            sinalX *= -1f;
        }

        pivoVisual.localScale = new Vector3(sinalX, escalaOriginalPivo.y, escalaOriginalPivo.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TentarAplicarImpacto(collision.gameObject);

        if (collision.CompareTag("Ataque"))
        {
            TomarDano(1);
        }
    }

    private void OnTriggerStay2D(Collider2D collision) => TentarAplicarImpacto(collision.gameObject);

    private void TentarAplicarImpacto(GameObject jogadorObj)
    {
        if (estaMorto || !jogadorObj.CompareTag("Player") || Time.time < tempoProximoImpacto) return;

        Rigidbody2D rbPlayer = jogadorObj.GetComponent<Rigidbody2D>();
        if (rbPlayer != null)
        {
            tempoProximoImpacto = Time.time + cooldownImpacto;

            Vector2 direcaoImpacto = (jogadorObj.transform.position - transform.position).normalized;
            direcaoImpacto.y = Mathf.Clamp(direcaoImpacto.y + 0.3f, 0.4f, 0.8f);

            rbPlayer.linearVelocity = Vector2.zero;
            rbPlayer.AddForce(direcaoImpacto * forcaImpacto, ForceMode2D.Impulse);

            if (estadoAtual == Estado.Avancando)
            {
                VoltarParaRodeio();
            }
        }
    }

    public void TomarDano(int quantidadeDano)
    {
        if (estaMorto) return;

        vidaAtual -= quantidadeDano;
        StartCoroutine(PiscarVermelhoDragonBones());

        if (vidaAtual <= 0)
        {
            Morrer();
        }
    }

    public void ReceberDano(int quantidadeDano = 1)
    {
        TomarDano(quantidadeDano);
    }

    private IEnumerator PiscarVermelhoDragonBones()
    {
        if (armatureComponent != null)
        {
            DragonBones.ColorTransform corVermelha = new DragonBones.ColorTransform();
            corVermelha.redMultiplier = 1f;
            corVermelha.greenMultiplier = 0f;
            corVermelha.blueMultiplier = 0f;

            DragonBones.ColorTransform corNormal = new DragonBones.ColorTransform();

            armatureComponent.color = corVermelha;
            yield return new WaitForSeconds(0.15f);
            armatureComponent.color = corNormal;
        }
    }

    private void Morrer()
    {
        if (estaMorto) return;
        estaMorto = true;

        rb.linearVelocity = Vector2.zero;
        if (GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = false;
        rb.simulated = false;

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 centro = transform.position + (Vector3)offsetDeteccao;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(centro, raioAtivacao);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centro, raioRodeio);
    }
}