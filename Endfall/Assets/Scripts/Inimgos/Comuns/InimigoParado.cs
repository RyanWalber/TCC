using System.Collections;
using UnityEngine;
using DragonBones;
using Transform = UnityEngine.Transform;

public class InimigoParado : MonoBehaviour
{
    [Header("CONFIGURAÇÃO VISUAL")]
    public UnityArmatureComponent armatureComponent;

    [Header("Configurações de Emergência")]
    public float raioDeteccao = 4f;
    public Vector2 offsetDeteccao;
    public float alturaEmergir = 1.2f;
    public float velocidadeSubida = 4f;
    public Transform player;

    [Header("Vida do Inimigo")]
    public int vidaMaxima = 3;
    private int vidaAtual;

    [Header("Ataque ao Jogador")]
    public int danoNoJogador = 1;
    public float forcaEmpurrao = 12f;

    private Vector3 posicaoOculta;
    private Vector3 posicaoExposta;
    private bool jaEmergiu = false;

    private void Start()
    {
        vidaAtual = vidaMaxima;

        if (armatureComponent == null)
            armatureComponent = GetComponentInChildren<UnityArmatureComponent>();

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        posicaoOculta = transform.position;
        posicaoExposta = transform.position + new Vector3(0, alturaEmergir, 0);
    }

    private void Update()
    {
        if (player == null) return;

        if (!jaEmergiu)
        {
            Vector3 centroDeteccao = transform.position + (Vector3)offsetDeteccao;
            float distancia = Vector2.Distance(player.position, centroDeteccao);

            if (distancia <= raioDeteccao)
            {
                jaEmergiu = true;
            }
        }

        if (jaEmergiu)
        {
            transform.position = Vector3.Lerp(transform.position, posicaoExposta, Time.deltaTime * velocidadeSubida);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            AplicarDanoEEmpurrar(collision.gameObject);
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
        Rigidbody2D rbPlayer = jogadorObj.GetComponent<Rigidbody2D>();
        if (rbPlayer != null)
        {
            float direcaoX = jogadorObj.transform.position.x >= transform.position.x ? 1f : -1f;

            Vector2 empurrao = new Vector2(direcaoX * 0.8f, 0.6f).normalized;

            rbPlayer.linearVelocity = empurrao * forcaEmpurrao;
        }
    }

    public void TomarDano(int quantidadeDano)
    {
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
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 centroDeteccao = transform.position + (Vector3)offsetDeteccao;
        Gizmos.DrawWireSphere(centroDeteccao, raioDeteccao);
    }
}