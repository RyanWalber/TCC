using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projetil : MonoBehaviour
{
    public float velocidade = 20f;
    public int dano = 1;

    [Header("Destruicao fora da camera")]
    [Tooltip("Margem extra fora da area visivel (0.05 = 5% da tela).")]
    [Range(0f, 0.25f)] public float margemCamera = 0.05f;

    [Header("Furia")]
    public float furiaPorAcerto = 8f;

    private Rigidbody2D rb;
    private Camera cameraDaFase;
    private bool jaColidiu;
    private bool esteveDentroDaCamera;
    private float instanteCriacao;
    private readonly HashSet<Transform> inimigosAtingidos = new HashSet<Transform>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        instanteCriacao = Time.time;
        cameraDaFase = Camera.main;
    }

    public void Disparar()
    {
        rb.linearVelocity = transform.right * velocidade;
    }

    private void LateUpdate()
    {
        if (jaColidiu) return;
        if (cameraDaFase == null) cameraDaFase = Camera.main;
        if (cameraDaFase == null) return;

        Vector3 p = cameraDaFase.WorldToViewportPoint(transform.position);
        bool dentroDaCamera = p.z > 0f &&
            p.x >= -margemCamera && p.x <= 1f + margemCamera &&
            p.y >= -margemCamera && p.y <= 1f + margemCamera;

        if (dentroDaCamera)
        {
            esteveDentroDaCamera = true;
            return;
        }

        // Pequena tolerancia quando o tiro nasce na borda da camera.
        if (esteveDentroDaCamera || Time.time - instanteCriacao >= 0.2f)
        {
            jaColidiu = true;
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ProcessarColisao(collision);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null) ProcessarColisao(collision.collider);
    }

    private void ProcessarColisao(Collider2D collision)
    {
        if (jaColidiu || collision == null) return;

        if (collision.CompareTag("Player") ||
            collision.transform.root.CompareTag("Player") ||
            collision.name.Contains("Borda") ||
            collision.CompareTag("MainCamera"))
            return;

        bool acertouInimigo = collision.CompareTag("Inimigo") ||
                              collision.transform.root.CompareTag("Inimigo");

        // Itens, checkpoints e outras zonas trigger nao bloqueiam o tiro.
        if (collision.isTrigger && !acertouInimigo)
            return;

        jaColidiu = true;
        if (acertouInimigo)
            AplicarDanoAoInimigo(collision);

        // O primeiro inimigo (inclusive trigger) ou parede encerra o tiro.
        Destroy(gameObject);
    }

    private void AplicarDanoAoInimigo(Collider2D collision)
    {
        Transform inimigo = collision.transform.root;
        if (!inimigosAtingidos.Add(inimigo)) return;

        int danoCalculado = dano;
        SistemaFuria furia = FindFirstObjectByType<SistemaFuria>();
        if (furia != null && furia.EstaEmFuria)
            danoCalculado = Mathf.RoundToInt(dano * furia.multDano);

        if (furia != null && !furia.EstaEmFuria)
            furia.AdicionarFuria(furiaPorAcerto);

        collision.SendMessageUpwards("TomarDano", danoCalculado,
            SendMessageOptions.DontRequireReceiver);
    }
}
