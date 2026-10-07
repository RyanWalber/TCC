using System.Collections;
using UnityEngine;
using DragonBones;
using Transform = UnityEngine.Transform;

[RequireComponent(typeof(Rigidbody2D))]
public class InimigoBixinho : MonoBehaviour
{
    [Header("CONFIGURAÇÃO VISUAL")]
    public UnityArmatureComponent armatureComponent;

    [Header("Movimentação e Rotação")]
    public float velocidade = 3.5f;
    public float velocidadeRotacao = 10f;
    public float offsetAngulo = 180f;
    public float raioDeteccao = 6f;
    public Vector2 offsetDeteccao;
    public Transform player;

    [Header("Vida do Inimigo")]
    public int vidaMaxima = 3;
    private int vidaAtual;
    private bool estaMorto = false;

    [Header("Ataque ao Jogador")]
    public int danoNoJogador = 10;
    public float forcaEmpurrao = 8f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
    }

    private void Start()
    {
        vidaAtual = vidaMaxima;

        // Pega o componente DragonBones automaticamente caso não esteja arrastado no Inspector
        if (armatureComponent == null)
            armatureComponent = GetComponentInChildren<UnityArmatureComponent>();

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void FixedUpdate()
    {
        if (estaMorto || player == null) return;

        Vector3 centroDeteccao = transform.position + (Vector3)offsetDeteccao;
        float distancia = Vector2.Distance(player.position, centroDeteccao);

        if (distancia <= raioDeteccao)
        {
            Vector2 direcao = ((Vector2)player.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = direcao * velocidade;
            RotacionarParaPlayer(direcao);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void RotacionarParaPlayer(Vector2 direcao)
    {
        float angulo = (Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg) + offsetAngulo;
        Quaternion rotacaoAlvo = Quaternion.Euler(0, 0, angulo);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoAlvo, velocidadeRotacao * Time.fixedDeltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            AplicarDanoEEmpurrar(collision.gameObject);
        }

        if (collision.gameObject.CompareTag("Ataque"))
        {
            TomarDano(1);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            AplicarDanoEEmpurrar(collision.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            AplicarDanoEEmpurrar(collision.gameObject);
        }

        if (collision.CompareTag("Ataque"))
        {
            TomarDano(1);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            AplicarDanoEEmpurrar(collision.gameObject);
        }
    }

    private void AplicarDanoEEmpurrar(GameObject jogadorObj)
    {
        if (estaMorto) return;

        PlayerHealth playerHealth = jogadorObj.GetComponent<PlayerHealth>();
        if (playerHealth == null) playerHealth = jogadorObj.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TomarDano(danoNoJogador);
        }

        Rigidbody2D rbPlayer = jogadorObj.GetComponent<Rigidbody2D>();
        if (rbPlayer == null) rbPlayer = jogadorObj.GetComponentInParent<Rigidbody2D>();

        if (rbPlayer != null)
        {
            float direcaoX = jogadorObj.transform.position.x >= transform.position.x ? 1f : -1f;
            Vector2 empurrao = new Vector2(direcaoX * 0.8f, 0.6f).normalized;

            rbPlayer.linearVelocity = empurrao * forcaEmpurrao;
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

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 centroDeteccao = transform.position + (Vector3)offsetDeteccao;
        Gizmos.DrawWireSphere(centroDeteccao, raioDeteccao);
    }
}