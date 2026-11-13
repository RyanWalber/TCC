using UnityEngine;

public class BossGuerraController : MonoBehaviour, IAudioBoss
{
    [Header("Audio da Guerra")]
    [SerializeField] private AudioInimigo audioInimigo = new AudioInimigo();
    public void AudioAoReceberHit() => audioInimigo.TocarHitRecebido(this);
    public void AudioAoMorrer() => audioInimigo.TocarMorte(this);
    public void AudioAoCausarHit() => audioInimigo.TocarHitCausado(this);

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
    private bool combatePeloGatilho;
    private float ataqueLiberadoEm = float.PositiveInfinity;

    public void AtivarPeloGatilho(float espera)
    {
        combatePeloGatilho = true;
        ataqueLiberadoEm = Time.time + Mathf.Max(0f, espera);
        emCombate = true;
        if (bossAnimation == null)
            bossAnimation = GetComponentInChildren<BossAnimation>();
        if (bossAnimation != null) bossAnimation.DefinirBravo(true);
        tempoProximoAtaque = ataqueLiberadoEm;
    }

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
        audioInimigo.Atualizar(this);
        if (!combatePeloGatilho) return;
        if (jogador == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jogador = p.transform;
        }
        if (jogador == null || Time.time < ataqueLiberadoEm) return;

        // Barris continuam sendo lancados mesmo com a Kaya longe.
        if (Time.time >= tempoProximoAtaque)
        {
            Atacar();
            tempoProximoAtaque = Time.time + Mathf.Max(0.1f, tempoEntreAtaques);
        }
    }

    private void Atacar()
    {
        audioInimigo.TocarAtaque(this);
        if (bossAnimation != null)
        {
            bossAnimation.DispararAtaque();
        }
    }

    public void InstanciarBarril()
    {
        if (!combatePeloGatilho || Time.time < ataqueLiberadoEm) return;
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