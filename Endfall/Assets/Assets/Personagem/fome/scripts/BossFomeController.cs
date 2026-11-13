using UnityEngine;

public class BossFomeController : MonoBehaviour, IAudioBoss
{
    [Header("Audio da Fome")]
    [SerializeField] private AudioInimigo audioInimigo = new AudioInimigo();
    public void AudioAoReceberHit() => audioInimigo.TocarHitRecebido(this);
    public void AudioAoMorrer() => audioInimigo.TocarMorte(this);

    [Header("Referências")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform pontoDisparo;
    [SerializeField] private GameObject prefabFlecha;

    [Header("Risada da hiena - AO SOLTAR A FLECHA")]
    [Tooltip("Arraste a risada adaptada da Fome aqui. Toca no mesmo instante em que a flecha e criada (Animation Event DispararFlecha).")]
    [SerializeField] private AudioClip risadaHienaAoDisparar;
    [Tooltip("Volume da risada da hiena ao disparar. O audio continua respeitando o Alcance do Audio Inimigo.")]
    [SerializeField, Range(0f, 1f)] private float volumeRisadaHiena = 0.8f;

    [Header("Visual do Boss")]
    [Tooltip("Arraste o objeto 'arcoeflecha_0' aqui")]
    [SerializeField] private GameObject flechaVisualNaMao;

    [Header("Configurações de Ataque")]
    [SerializeField] private float alcanceAtaque = 15f;
    [SerializeField] private float tempoEntreAtaques = 3f;

    [Header("Orientação Inicial")]
    [Tooltip("Marque se o sprite original do boss na cena já começa virado para a direita")]
    [SerializeField] private bool olhandoDireita = false;

    private float cronometroAtaque;
    private bool combatePeloGatilho;
    private float ataqueLiberadoEm = float.PositiveInfinity;

    public void AtivarPeloGatilho(float espera)
    {
        combatePeloGatilho = true;
        ataqueLiberadoEm = Time.time + Mathf.Max(0f, espera);
        cronometroAtaque = Mathf.Max(0f, tempoEntreAtaques);
    }

    private static readonly int HashAtacar = Animator.StringToHash("Atacar");

    private void Start()
    {
        BuscarPlayer();
    }

    private void Update()
    {
        audioInimigo.Atualizar(this);
        if (player == null)
        {
            BuscarPlayer();
            if (player == null) return;
        }

        OlharParaJogador();
        if (!combatePeloGatilho || Time.time < ataqueLiberadoEm) return;

        cronometroAtaque += Time.deltaTime;

        // Flechas podem alcancar Kaya independentemente de distancia apos o gatilho.
        if (cronometroAtaque >= tempoEntreAtaques)
        {
            Atacar();
            cronometroAtaque = 0f;
        }
    }

    private void BuscarPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private void OlharParaJogador()
    {
        if (player.position.x > transform.position.x && !olhandoDireita)
        {
            Virar();
        }
        else if (player.position.x < transform.position.x && olhandoDireita)
        {
            Virar();
        }
    }

    private void Virar()
    {
        olhandoDireita = !olhandoDireita;
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }

    private void Atacar()
    {
        audioInimigo.TocarAtaque(this);
        if (flechaVisualNaMao != null)
        {
            flechaVisualNaMao.SetActive(true);
        }

        if (animator != null)
        {
            animator.SetTrigger(HashAtacar);
        }
    }

    // Chamado pelo Evento de Animação quando o Boss solta a corda do arco
    public void DispararFlecha()
    {
        if (!combatePeloGatilho || Time.time < ataqueLiberadoEm) return;
        if (flechaVisualNaMao != null)
        {
            flechaVisualNaMao.SetActive(false);
        }

        if (prefabFlecha != null && pontoDisparo != null && player != null)
        {
            // Instancia a flecha na posição do pontoDisparo
            GameObject flechaObj = Instantiate(prefabFlecha, pontoDisparo.position, Quaternion.identity);

            // A hiena gargalha NO MOMENTO do disparo, nao ao comecar
            // a puxar o arco. Clip e volume escolhidos no Inspector.
            audioInimigo.TocarEspecial(this, risadaHienaAoDisparar,
                volumeRisadaHiena, pontoDisparo.position);

            // Calcula o vetor direto do Ponto de Disparo até a posição do Player
            Vector2 direcao = (player.position - pontoDisparo.position).normalized;

            if (flechaObj.TryGetComponent<FlechaBoss>(out FlechaBoss flecha))
            {
                flecha.DefinirDirecao(direcao);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, alcanceAtaque);
    }
}