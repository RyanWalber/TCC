using UnityEngine;

public class BossFomeController : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform pontoDisparo;
    [SerializeField] private GameObject prefabFlecha;

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

    // Hash para o parâmetro do Animator (evita processamento de strings repetido)
    private static readonly int HashAtacar = Animator.StringToHash("Atacar");

    private void Start()
    {
        BuscarPlayer();
    }

    private void Update()
    {
        if (player == null)
        {
            BuscarPlayer();
            if (player == null) return;
        }

        OlharParaJogador();

        cronometroAtaque += Time.deltaTime;

        // Otimização: calcula distância ao quadrado para evitar o custo de raiz quadrada
        float distanciaQuadrada = (player.position - transform.position).sqrMagnitude;
        float alcanceQuadrado = alcanceAtaque * alcanceAtaque;

        if (distanciaQuadrada <= alcanceQuadrado && cronometroAtaque >= tempoEntreAtaques)
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
        if (flechaVisualNaMao != null)
        {
            flechaVisualNaMao.SetActive(true);
        }

        if (animator != null)
        {
            animator.SetTrigger(HashAtacar);
        }
    }

    public void DispararFlecha()
    {
        if (flechaVisualNaMao != null)
        {
            flechaVisualNaMao.SetActive(false);
        }

        if (prefabFlecha != null && pontoDisparo != null && player != null)
        {
            Vector2 direcao = (player.position - pontoDisparo.position).normalized;

            float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
            Quaternion rotacao = Quaternion.Euler(0, 0, angulo);

            GameObject flechaObj = Instantiate(prefabFlecha, pontoDisparo.position, rotacao);

            // Otimização API moderna da Unity
            if (flechaObj.TryGetComponent<FlechaBoss>(out FlechaBoss flecha))
            {
                flecha.DefinirDirecao(direcao);
            }
        }
    }

    // Desenha o alcance de ataque na visualização de Scene
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, alcanceAtaque);
    }
}