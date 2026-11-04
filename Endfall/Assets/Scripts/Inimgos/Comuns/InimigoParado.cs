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

    [Header("Aviso de Emergência")]
    [Tooltip("Tempo entre a poeira aparecer e o inimigo começar a subir.")]
    [Min(0f)] public float tempoAvisoPoeira = 0.8f;
    [Tooltip("Quantidade de partículas de poeira emitidas no aviso.")]
    [Min(1)] public int quantidadeParticulasPoeira = 18;
    [Header("Aparencia da Poeira")]
    [Tooltip("Cor e transparencia usadas pelas particulas criadas por codigo.")]
    public Color corPoeira = new Color(0.07f, 0.07f, 0.07f, 0.85f);
    [Tooltip("Opcional. Se vazio, um material Unlit compativel sera criado em tempo de execucao.")]
    public Material materialPoeira;

    [Tooltip("Opcional: atribua um Particle System próprio. Se ficar vazio, o efeito será criado automaticamente.")]
    public ParticleSystem efeitoPoeira;
    [Tooltip("Deslocamento da poeira em relação ao ponto onde o inimigo estava escondido. Aumente Y para colocar a poeira mais acima da plataforma.")]
    public Vector3 offsetPoeira = new Vector3(0f, 0.5f, 0f);

    private bool preparandoEmergencia;
    private ParticleSystemRenderer renderizadorPoeira;
    private Material materialPoeiraCriado;

    [Header("Vida do Inimigo")]
    public int vidaMaxima = 3;
    private int vidaAtual;

    [Header("Ataque ao Jogador")]
    public int danoNoJogador = 10; 
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

        PrepararEfeitoPoeira();
    }

    private void PrepararEfeitoPoeira()
    {
        bool criarPoeira = efeitoPoeira == null;
        if (criarPoeira)
        {
            GameObject objeto = new GameObject("AvisoPoeiraEmergencia");
            // Emissor como filho: sera eliminado quando o inimigo morrer.
            objeto.transform.SetParent(transform, true);
            objeto.transform.position = posicaoOculta + offsetPoeira;
            efeitoPoeira = objeto.AddComponent<ParticleSystem>();
            efeitoPoeira.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        var main = efeitoPoeira.main;
        // Funciona tambem com um ParticleSystem colocado manualmente no Inspector.
        main.startColor = new ParticleSystem.MinMaxGradient(corPoeira);

        if (criarPoeira)
        {
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.7f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                Mathf.Max(0.5f, alturaEmergir * 0.15f),
                Mathf.Max(1f, alturaEmergir * 0.4f));
            main.startSize = new ParticleSystem.MinMaxCurve(
                Mathf.Max(0.08f, alturaEmergir * 0.025f),
                Mathf.Max(0.15f, alturaEmergir * 0.06f));
            main.maxParticles = Mathf.Max(quantidadeParticulasPoeira, 30);

            var emission = efeitoPoeira.emission;
            emission.enabled = false;
            var shape = efeitoPoeira.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 55f;
            shape.radius = Mathf.Max(0.1f, alturaEmergir * 0.08f);
            var velocity = efeitoPoeira.velocityOverLifetime;
            velocity.enabled = false;
        }

        renderizadorPoeira = efeitoPoeira.GetComponent<ParticleSystemRenderer>();
        if (renderizadorPoeira == null) return;

        if (criarPoeira)
        {
            renderizadorPoeira.renderMode = ParticleSystemRenderMode.Billboard;
            renderizadorPoeira.sortingOrder = 5;
        }

        if (materialPoeira != null && materialPoeira.shader != null &&
            materialPoeira.shader.isSupported)
        {
            renderizadorPoeira.sharedMaterial = materialPoeira;
            return;
        }

        // Um efeito externo com material valido pode manter sua aparencia.
        Material atual = renderizadorPoeira.sharedMaterial;
        if (!criarPoeira && materialPoeira == null && atual != null &&
            atual.shader != null && atual.shader.isSupported)
            return;

        // Sem material, o renderizador pode ficar magenta/roxo.
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null || !shader.isSupported)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null || !shader.isSupported)
            shader = Shader.Find("Sprites/Default");

        if (shader == null || !shader.isSupported)
        {
            Debug.LogError("[InimigoParado] Shader de particulas indisponivel. " +
                "Arraste um Material Unlit compativel ao campo 'Material Poeira'.", this);
            return;
        }

        materialPoeiraCriado = new Material(shader)
        {
            name = "MaterialPoeiraAutomatico",
            hideFlags = HideFlags.DontSave
        };
        renderizadorPoeira.sharedMaterial = materialPoeiraCriado;
    }

    private void OnDestroy()
    {
        // Material criado pelo codigo nao deve acumular na memoria.
        if (materialPoeiraCriado != null)
            Destroy(materialPoeiraCriado);
    }

    private IEnumerator AvisarEEmergir()
    {
        preparandoEmergencia = true;

        if (efeitoPoeira == null)
            PrepararEfeitoPoeira();

        if (efeitoPoeira != null)
        {
            efeitoPoeira.transform.position = posicaoOculta + offsetPoeira;
            var mainPoeira = efeitoPoeira.main;
            mainPoeira.startColor = new ParticleSystem.MinMaxGradient(corPoeira);
            efeitoPoeira.Clear(true);
            efeitoPoeira.Play(true);
            efeitoPoeira.Emit(Mathf.Max(1, quantidadeParticulasPoeira));
        }

        if (tempoAvisoPoeira > 0f)
            yield return new WaitForSeconds(tempoAvisoPoeira);

        jaEmergiu = true;
        preparandoEmergencia = false;
    }

    private void Update()
    {
        if (player == null) return;

        if (!jaEmergiu && !preparandoEmergencia)
        {
            Vector3 centroDeteccao = transform.position + (Vector3)offsetDeteccao;
            float distancia = Vector2.Distance(player.position, centroDeteccao);

            if (distancia <= raioDeteccao)
            {
                StartCoroutine(AvisarEEmergir());
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
        PlayerHealth playerHealth = jogadorObj.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TomarDanoDeInimigo(danoNoJogador);
        }

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
        EstadoMundoCheckpoint.RegistrarMorte(gameObject);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 centroDeteccao = transform.position + (Vector3)offsetDeteccao;
        Gizmos.DrawWireSphere(centroDeteccao, raioDeteccao);
    }
}