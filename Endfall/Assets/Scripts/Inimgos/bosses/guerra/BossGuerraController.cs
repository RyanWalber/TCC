using UnityEngine;

public class BossGuerraController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform jogador;
    [SerializeField] private BossAnimation bossAnimation;

    [Header("Distancias")]
    [SerializeField] private float distanciaBravo = 8f;
    [SerializeField] private float distanciaAtaque = 3f;

    [Header("Ataque")]
    [SerializeField] private float tempoEntreAtaques = 2f;
    private float tempoProximoAtaque;

    private bool estaBravo;

    void Start()
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

    void Update()
    {
        if (jogador == null) return;

        float distancia = Vector2.Distance(transform.position, jogador.position);

        estaBravo = distancia <= distanciaBravo;

        if (bossAnimation != null)
        {
            bossAnimation.DefinirBravo(estaBravo);
        }

        if (estaBravo && distancia <= distanciaAtaque && Time.time >= tempoProximoAtaque)
        {
            Atacar();
            tempoProximoAtaque = Time.time + tempoEntreAtaques;
        }
    }

    void Atacar()
    {
        if (bossAnimation != null)
        {
            bossAnimation.DispararAtaque();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaBravo);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaAtaque);
    }
}