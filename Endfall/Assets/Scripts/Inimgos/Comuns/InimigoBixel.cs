using System.Collections;
using UnityEngine;
using DragonBones;
using Transform = UnityEngine.Transform;

[RequireComponent(typeof(Rigidbody2D))]
public class InimigoBixel : MonoBehaviour
{
    [Header("CONFIGURAÇÃO VISUAL")]
    public Transform pivoVisual;
    public UnityArmatureComponent armatureComponent;
    public string animAndar = "walk";
    public string animParado = "idle";
    public bool spriteInvertido = false;

    [Header("STATUS")]
    public int vida = 3;
    public int danoNoPlayer = 1;

    [Header("MOVIMENTO (Patrulha)")]
    public float velocidade = 3f;
    public float distanciaPatrulha = 5f;

    private Rigidbody2D rb;
    private Vector3 posicaoInicial;
    private bool indoParaDireita = true;
    private bool estaMorto = false;

    private void Start()
    {
        posicaoInicial = transform.position;
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        if (pivoVisual == null && transform.childCount > 0)
            pivoVisual = transform.GetChild(0);

        if (armatureComponent == null)
            armatureComponent = GetComponentInChildren<UnityArmatureComponent>();

        AtualizarEscala();
    }

    private void FixedUpdate()
    {
        if (estaMorto) return;
        Patrulhar();
    }

    private void Update()
    {
        if (estaMorto) return;
        AtualizarAnimacao();
    }

    private void Patrulhar()
    {
        float limiteDireita = posicaoInicial.x + distanciaPatrulha;
        float limiteEsquerda = posicaoInicial.x - distanciaPatrulha;

        float direcaoX = indoParaDireita ? 1f : -1f;
        rb.linearVelocity = new Vector2(direcaoX * velocidade, rb.linearVelocity.y);

        if (indoParaDireita && transform.position.x >= limiteDireita)
        {
            Virar(false);
        }
        else if (!indoParaDireita && transform.position.x <= limiteEsquerda)
        {
            Virar(true);
        }
    }

    private void Virar(bool irParaDireita)
    {
        indoParaDireita = irParaDireita;
        AtualizarEscala();
    }

    private void AtualizarEscala()
    {
        if (pivoVisual == null) return;

        float sinal = indoParaDireita ? 1f : -1f;
        if (spriteInvertido) sinal *= -1f;

        pivoVisual.localScale = new Vector3(sinal, 1f, 1f);
    }

    private void AtualizarAnimacao()
    {
        if (armatureComponent == null) return;

        bool estaAndando = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        string animDesejada = estaAndando ? animAndar : animParado;

        if (!string.IsNullOrEmpty(animDesejada) && armatureComponent.animation.lastAnimationName != animDesejada)
        {
            armatureComponent.animation.Play(animDesejada, 0);
        }
    }

    public void ReceberDano(int dano = 1)
    {
        if (estaMorto) return;

        vida -= dano;
        StartCoroutine(PiscarVermelhoDragonBones());

        if (vida <= 0) Morrer();
    }

    public void TomarDano(int dano = 1)
    {
        ReceberDano(dano);
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
        rb.simulated = false;

        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D colisao)
    {
        if (estaMorto) return;
        if (colisao.gameObject.CompareTag("Player"))
        {
            colisao.gameObject.SendMessage("Machucar", danoNoPlayer, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (estaMorto) return;

        if (other.CompareTag("Ataque"))
        {
            ReceberDano(1);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 centro = Application.isPlaying ? posicaoInicial : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(centro.x - distanciaPatrulha, centro.y, centro.z), new Vector3(centro.x + distanciaPatrulha, centro.y, centro.z));
    }
}