using UnityEngine;

public class BossGuerraController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform jogador;
    [SerializeField] private BossAnimation bossAnimation;

    [Header("Ataque de Barril")]
    [SerializeField] private GameObject prefabBarril;
    [SerializeField] private Transform pontoSpawnBarril;

    [Tooltip("Velocidade horizontal do barril. Pode aumentar que o arco se ajusta sozinho!")]
    [SerializeField] private float velocidadeBarril = 200f;

    [Header("Deteccao (Unica Distancia)")]
    [SerializeField] private float distanciaDetecao = 300f;

    [Header("Tempo de Ataque")]
    [SerializeField] private float tempoEntreAtaques = 2.5f;
    private float tempoProximoAtaque;

    private bool emCombate = false;

    private void Start()
    {
        if (bossAnimation == null)
        {
            bossAnimation = GetComponentInChildren<BossAnimation>();
        }

        if (jogador == null)
        {
            GameObject objJogador = GameObject.FindGameObjectWithTag("Player");
            if (objJogador != null)
            {
                jogador = objJogador.transform;
            }
        }
    }

    private void Update()
    {
        if (jogador == null) return;

        float distancia = Vector2.Distance(transform.position, jogador.position);

        if (distancia <= distanciaDetecao)
        {
            if (!emCombate)
            {
                emCombate = true;
                if (bossAnimation != null)
                {
                    bossAnimation.DefinirBravo(true);
                }
                tempoProximoAtaque = Time.time + 0.8f;
            }

            if (Time.time >= tempoProximoAtaque)
            {
                Atacar();
                tempoProximoAtaque = Time.time + tempoEntreAtaques;
            }
        }
        else
        {
            if (emCombate)
            {
                emCombate = false;
                if (bossAnimation != null)
                {
                    bossAnimation.DefinirBravo(false);
                }
            }
        }
    }

    private void Atacar()
    {
        if (bossAnimation != null)
        {
            bossAnimation.DispararAtaque();
        }
    }

    public void InstanciarBarril()
    {
        if (prefabBarril == null || jogador == null) return;

        Vector3 posSpawn = pontoSpawnBarril != null ? pontoSpawnBarril.position : transform.position;
        GameObject objBarril = Instantiate(prefabBarril, posSpawn, Quaternion.identity);

        Collider2D colBarril = objBarril.GetComponent<Collider2D>();
        Collider2D[] colsBoss = GetComponentsInChildren<Collider2D>();
        if (colBarril != null && colsBoss != null)
        {
            foreach (Collider2D c in colsBoss)
            {
                Physics2D.IgnoreCollision(colBarril, c);
            }
        }

        BarrilGuerra scriptBarril = objBarril.GetComponent<BarrilGuerra>();
        if (scriptBarril != null)
        {
            scriptBarril.Lançar(posSpawn, jogador.position, velocidadeBarril);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaDetecao);
    }
}