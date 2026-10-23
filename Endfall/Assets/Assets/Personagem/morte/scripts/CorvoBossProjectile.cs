using UnityEngine;

/// <summary>
/// Movimento e impacto do corvo lançado pela Morte. É adicionado pelo controller
/// ao prefab Corvo em tempo de execução, então não exige mexer no prefab manualmente.
/// </summary>
[DisallowMultipleComponent]
public class CorvoBossProjectile : MonoBehaviour
{
    private Rigidbody2D corpoFisico;
    private Collider2D[] collidersCorvo;
    private float tempoDeVida = 4f;
    private int dano = 12;
    private bool resolvido;
    private Vector3 escalaOriginal = Vector3.one;

    private void Awake()
    {
        corpoFisico = GetComponent<Rigidbody2D>();
        collidersCorvo = GetComponentsInChildren<Collider2D>(true);
        escalaOriginal = transform.localScale;
    }

    public void Configurar(Vector2 direcao, float velocidade, int danoCausado, float duracao,
                           Collider2D[] collidersDono, bool arteOriginalOlhaEsquerda)
    {
        dano = Mathf.Max(1, danoCausado);
        tempoDeVida = Mathf.Max(0.1f, duracao);

        if (corpoFisico == null)
            corpoFisico = gameObject.AddComponent<Rigidbody2D>();

        corpoFisico.bodyType = RigidbodyType2D.Dynamic;
        corpoFisico.gravityScale = 0f;
        corpoFisico.freezeRotation = true;
        corpoFisico.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        corpoFisico.linearVelocity = direcao.normalized * Mathf.Max(1f, velocidade);

        bool inverter = (direcao.x >= 0f) == arteOriginalOlhaEsquerda;
        float sinalX = inverter ? -1f : 1f;
        transform.localScale = new Vector3(Mathf.Abs(escalaOriginal.x) * sinalX,
                                            escalaOriginal.y, escalaOriginal.z);

        // Evita que o corvo colida com a própria Morte assim que for criado.
        if (collidersDono != null)
        {
            foreach (Collider2D colDono in collidersDono)
            {
                if (colDono == null) continue;
                foreach (Collider2D colCorvo in collidersCorvo)
                {
                    if (colCorvo != null)
                        Physics2D.IgnoreCollision(colDono, colCorvo, true);
                }
            }
        }

        Destroy(gameObject, tempoDeVida);
    }

    private void OnCollisionEnter2D(Collision2D colisao)
    {
        if (colisao == null || colisao.gameObject == null) return;
        TratarContato(colisao.gameObject, true);
    }

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (outro == null) return;
        TratarContato(outro.gameObject, false);
    }

    private void TratarContato(GameObject outro, bool colidirComCenarioDestrui)
    {
        if (resolvido || outro == null) return;

        bool acertouJogador = outro.CompareTag("Player") ||
                              outro.transform.root.CompareTag("Player");

        if (acertouJogador)
        {
            PlayerHealth hp = outro.GetComponentInParent<PlayerHealth>();
            if (hp == null)
                hp = outro.transform.root.GetComponentInChildren<PlayerHealth>();

            if (hp != null)
                hp.TomarDanoDeInimigo(dano);

            DestruirCorvo();
            return;
        }

        if (outro.CompareTag("Ataque") || outro.CompareTag("Projetil") ||
            outro.CompareTag("Tiro") || outro.CompareTag("Bala"))
        {
            DestruirCorvo();
            return;
        }

        // Gatilhos não bloqueiam o corvo. Colisões físicas com chão, parede ou plataforma
        // encerram o projétil para ele não ficar preso no cenário.
        if (colidirComCenarioDestrui)
            DestruirCorvo();
    }

    public void TomarDano(int quantidadeDano = 1)
    {
        if (quantidadeDano > 0)
            DestruirCorvo();
    }

    public void ReceberDano(int quantidadeDano = 1) => TomarDano(quantidadeDano);

    private void DestruirCorvo()
    {
        if (resolvido) return;
        resolvido = true;
        if (corpoFisico != null)
            corpoFisico.linearVelocity = Vector2.zero;
        Destroy(gameObject);
    }

    private void OnBecameInvisible()
    {
        if (!resolvido)
            Destroy(gameObject, 0.5f);
    }
}
