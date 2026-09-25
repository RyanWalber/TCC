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

    [Header("Vida")]
    public int vidaMaxima = 3;
    private int vidaAtual;
    private bool estaMorto = false;

    [Header("Impacto no Jogador")]
    public int danoNoJogador = 1;
    public float forcaImpacto = 8f;
    public float cooldownImpacto = 0.8f;
    private float tempoProximoImpacto;

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

        GarantirReferenciaVisual();

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void GarantirReferenciaVisual()
    {
        if (armatureComponent == null)
            armatureComponent = GetComponentInChildren<UnityArmatureComponent>();
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

    private void OnCollisionEnter2D(Collision2D collision) => ProcessarInteracao(collision.gameObject);
    private void OnCollisionStay2D(Collision2D collision) => ProcessarInteracao(collision.gameObject);
    private void OnTriggerEnter2D(Collider2D collision) => ProcessarInteracao(collision.gameObject);
    private void OnTriggerStay2D(Collider2D collision) => ProcessarInteracao(collision.gameObject);

    private void ProcessarInteracao(GameObject obj)
    {
        if (estaMorto) return;

        if (obj.CompareTag("Player"))
        {
            TentarAplicarImpacto(obj);
        }
        else if (obj.CompareTag("Ataque") || obj.CompareTag("Projetil"))
        {
            TomarDano(1);
        }
    }

    private void TentarAplicarImpacto(GameObject jogadorObj)
    {
        if (Time.time < tempoProximoImpacto) return;

        tempoProximoImpacto = Time.time + cooldownImpacto;

        jogadorObj.SendMessage("Machucar", danoNoJogador, SendMessageOptions.DontRequireReceiver);

        Rigidbody2D rbPlayer = jogadorObj.GetComponent<Rigidbody2D>();
        if (rbPlayer != null)
        {
            Vector2 direcaoImpacto = (jogadorObj.transform.position - transform.position).normalized;
            direcaoImpacto.y = Mathf.Clamp(direcaoImpacto.y + 0.3f, 0.4f, 0.8f);

            rbPlayer.linearVelocity = new Vector2(rbPlayer.linearVelocity.x, 0);
            rbPlayer.AddForce(direcaoImpacto * forcaImpacto, ForceMode2D.Impulse);
        }
    }

    public void TomarDano(int quantidadeDano)
    {
        ReceberDano(quantidadeDano);
    }

    public void ReceberDano(int quantidadeDano = 1)
    {
        if (estaMorto) return;

        vidaAtual -= quantidadeDano;
        StartCoroutine(PiscarVermelho());

        if (vidaAtual <= 0)
        {
            Morrer();
        }
    }

    private void Morrer()
    {
        if (estaMorto) return;
        estaMorto = true;

        rb.linearVelocity = Vector2.zero;
        if (GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = false;

        Destroy(gameObject, 0.18f);
    }

    private IEnumerator PiscarVermelho()
    {
        GarantirReferenciaVisual();

        SpriteRenderer[] renderersNormais = GetComponentsInChildren<SpriteRenderer>();

        if (armatureComponent != null)
        {
            DragonBones.ColorTransform corVermelhaDB = new DragonBones.ColorTransform
            {
                redMultiplier = 1f,
                greenMultiplier = 0f,
                blueMultiplier = 0f,
                redOffset = 255
            };

            armatureComponent.color = corVermelhaDB;
        }

        foreach (var sr in renderersNormais)
        {
            if (sr != null) sr.color = Color.red;
        }

        yield return new WaitForSeconds(0.15f);

        if (armatureComponent != null)
        {
            DragonBones.ColorTransform corNormalDB = new DragonBones.ColorTransform();
            armatureComponent.color = corNormalDB;
        }

        foreach (var sr in renderersNormais)
        {
            if (sr != null) sr.color = Color.white;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 centroDeteccao = transform.position + (Vector3)offsetDeteccao;
        Gizmos.DrawWireSphere(centroDeteccao, raioDeteccao);
    }
}