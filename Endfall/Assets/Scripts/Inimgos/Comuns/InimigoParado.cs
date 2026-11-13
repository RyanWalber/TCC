using System.Collections;
using UnityEngine;
using DragonBones;
using Transform = UnityEngine.Transform;

[DefaultExecutionOrder(10000)]
public class InimigoParado : MonoBehaviour
{
    [Header("Audio deste inimigo")]
    [SerializeField] private AudioInimigo audioInimigo = new AudioInimigo();

    [Header("Feedback visual ao receber dano")]
    [SerializeField] private FeedbackDanoDragonBones feedbackDano =
        new FeedbackDanoDragonBones();

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

    [Header("Poeira - Terra Voando")]
    [Tooltip("Multiplica a quantidade original. Por exemplo: 18 x 5 = 90 particulas.")]
    [Range(1f, 10f)] public float densidadePoeira = 5f;
    [Tooltip("Aumenta o tamanho dos pedacos de terra, facilitando a visualizacao.")]
    [Range(0.5f, 5f)] public float tamanhoPoeira = 2.2f;
    [Tooltip("Intensidade do movimento de baixo para cima.")]
    [Range(0.25f, 5f)] public float forcaSubidaPoeira = 1.5f;
    [Tooltip("Quanto maior, mais rapidamente a terra desacelera e cai.")]
    [Range(0f, 2f)] public float gravidadePoeira = 0.55f;
    [Tooltip("Largura da faixa do chao de onde as particulas saem.")]
    [Min(0.05f)] public float larguraSaidaPoeira = 0.7f;
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

    [Header("Ocultacao da parte enterrada")]
    [Tooltip("Opcional: marque um Transform exatamente na superficie da plataforma. Se vazio, procura o piso pelo Collider2D.")]
    [SerializeField] private Transform pontoSuperficie;
    [Tooltip("Ajuste fino do nivel real do piso. Valor negativo abaixa o recorte.")]
    [SerializeField] private float ajusteNivelSuperficie = 0f;
    [Tooltip("Layers preferenciais das plataformas. Busca outras layers se necessario.")]
    [SerializeField] private LayerMask camadasDaSuperficie = 1 << 3;
    [Tooltip("Material ja configurado para o shader EndFall/InimigoParadoRecorte.")]
    [SerializeField] private Material materialModeloRecorte;

    [Header("Desenho atras da plataforma")]
    [SerializeField] private string camadaRenderCorpo = "Default";
    [SerializeField] private int ordemRenderCorpo = -500;

    private float superficieChaoY;
    private bool avisoSemCollider;
    private readonly System.Collections.Generic.Dictionary<Material, Material> materiaisRecortados =
        new System.Collections.Generic.Dictionary<Material, Material>();
    private readonly System.Collections.Generic.HashSet<Material> clonesCriados =
        new System.Collections.Generic.HashSet<Material>();
    private MaterialPropertyBlock parametrosRecorte;
    private static readonly int groundId = Shader.PropertyToID("_GroundY");

    [Header("Vida do Inimigo")]
    public int vidaMaxima = 3;
    private int vidaAtual;

    [Header("Ataque ao Jogador")]
    public int danoNoJogador = 10; 
    public float forcaEmpurrao = 12f;

    private Vector3 posicaoOculta;
    private Vector3 posicaoExposta;
    private bool jaEmergiu = false;
    private bool audioDeSurgimentoTocado;

    // Pode ser chamado pelo chefe que invocou; o som e do inimigo, nao do chefe.
    public void TocarAoSerInvocado()
    {
        if (audioDeSurgimentoTocado) return;
        audioDeSurgimentoTocado = true;
        audioInimigo.TocarInvocacao(this);
    }


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

        PrepararRenderizacaoDoCorpo();
        PrepararEfeitoPoeira();
    }

    private void PrepararRenderizacaoDoCorpo()
    {
        if (armatureComponent == null) return;

        // O SpriteMask nao recorta corretamente as malhas DragonBones.
        // Evita duas solucoes de recorte disputando pelo mesmo renderer.
        Transform raizPrefab = transform.parent != null ? transform.parent : transform;
        foreach (SpriteMask mascara in raizPrefab.GetComponentsInChildren<SpriteMask>(true))
            if (mascara != null) mascara.enabled = false;

        LocalizarNivelDaPlataforma();
        AtualizarRenderizacaoDoCorpo();
    }

    private void LocalizarNivelDaPlataforma()
    {
        if (pontoSuperficie != null)
        {
            superficieChaoY = pontoSuperficie.position.y;
            return;
        }

        // O offset da poeira e o ponto visual de onde a terra sai.
        // Nao deve se tornar diretamente o nivel de recorte:
        // o emissor pode estar alguns metros acima da superficie.
        float estimativa = posicaoOculta.y + Mathf.Max(0f, alturaEmergir);
        float origemPesquisaY = Mathf.Max(
            estimativa, posicaoOculta.y + offsetPoeira.y) +
            Mathf.Max(20f, alturaEmergir * 0.6f);
        float comprimento = Mathf.Max(80f, alturaEmergir * 2.5f);
        float tolerancia = Mathf.Max(12f, alturaEmergir * 0.65f);

        // O prefab desloca visualmente o DragonBones em X. Procura o piso
        // sob o ponto da poeira, sob a armadura e sob o transform do inimigo.
        float[] pontosX = {
            posicaoOculta.x + offsetPoeira.x,
            armatureComponent.transform.position.x,
            posicaoOculta.x
        };

        Collider2D candidato = null;
        float alturaCandidata = 0f;
        float melhorPontuacao = float.PositiveInfinity;
        Transform raizPrefab = transform.parent != null ? transform.parent : transform;

        foreach (float x in pontosX)
        {
            RaycastHit2D[] acertos = Physics2D.RaycastAll(
                new Vector2(x, origemPesquisaY), Vector2.down, comprimento);

            foreach (RaycastHit2D acerto in acertos)
            {
                Collider2D col = acerto.collider;
                if (col == null || col.isTrigger || acerto.normal.y < 0.6f ||
                    col.transform.IsChildOf(raizPrefab) ||
                    col.CompareTag("Player") || col.CompareTag("Inimigo"))
                    continue;

                float diferenca = Mathf.Abs(acerto.point.y - estimativa);
                if (diferenca > tolerancia) continue;

                bool ehGround = (camadasDaSuperficie.value &
                    (1 << col.gameObject.layer)) != 0;
                // Prefere o piso real, nao os colliders de decoracoes.
                float pontuacao = diferenca + (ehGround ? 0f : 2f);
                if (pontuacao >= melhorPontuacao) continue;

                melhorPontuacao = pontuacao;
                alturaCandidata = acerto.point.y;
                candidato = col;
            }
        }

        if (candidato != null)
        {
            superficieChaoY = alturaCandidata;
        }
        else
        {
            // Evita repetir o erro antigo de usar offsetPoeira.y (74)
            // como topo do piso. Um recorte MUITO alto comia o corpo.
            superficieChaoY = estimativa - Mathf.Max(2f, alturaEmergir * 0.15f);
            if (!avisoSemCollider)
            {
                Debug.LogWarning(
                    "[InimigoParado] Nenhuma plataforma encontrada abaixo do BixoAlfa. " +
                    "Use Ponto Superficie no Inspector para definir o topo exato do piso.", this);
                avisoSemCollider = true;
            }
        }
    }

    private void AtualizarRenderizacaoDoCorpo()
    {
        if (armatureComponent == null) return;

        Shader shaderCorte = materialModeloRecorte != null ?
            materialModeloRecorte.shader : Shader.Find("EndFall/InimigoParadoRecorte");
        bool shaderValido = shaderCorte != null && shaderCorte.isSupported;
        if (parametrosRecorte == null)
            parametrosRecorte = new MaterialPropertyBlock();

        float nivel = (pontoSuperficie != null ?
            pontoSuperficie.position.y : superficieChaoY) + ajusteNivelSuperficie;

        foreach (MeshRenderer malha in
                 armatureComponent.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (malha == null) continue;

            malha.sortingLayerName = camadaRenderCorpo;
            malha.sortingOrder = ordemRenderCorpo;

            if (shaderValido)
            {
                // A textura ORIGINAL do DragonBones e preservada.
                // O corte atua somente abaixo da superficie detectada.
                Material[] materias = malha.sharedMaterials;
                bool atualizar = false;
                for (int i = 0; i < materias.Length; i++)
                {
                    Material original = materias[i];
                    if (original == null || clonesCriados.Contains(original))
                        continue;

                    if (!materiaisRecortados.TryGetValue(original, out Material recortado))
                    {
                        recortado = new Material(original);
                        recortado.shader = shaderCorte;
                        recortado.name = original.name + " (Ocultacao EndFall)";
                        recortado.hideFlags = HideFlags.DontSave;
                        materiaisRecortados.Add(original, recortado);
                        clonesCriados.Add(recortado);
                    }

                    materias[i] = recortado;
                    atualizar = true;
                }
                if (atualizar)
                    malha.sharedMaterials = materias;

                malha.GetPropertyBlock(parametrosRecorte);
                parametrosRecorte.SetFloat(groundId, nivel);
                malha.SetPropertyBlock(parametrosRecorte);
            }

            // Se faltar shader, nunca mostra o inimigo sob o piso.
            // Quando disponivel, cada pixel abaixo do piso e descartado.
            bool podeMostrar = shaderValido
                ? jaEmergiu
                : jaEmergiu &&
                  Vector3.Distance(transform.position, posicaoExposta) < 0.05f;

            if (malha.enabled != podeMostrar)
                malha.enabled = podeMostrar;
        }
    }

    private void LateUpdate()
    {
        // O DragonBones pode recompor as malhas a qualquer momento.
        AtualizarRenderizacaoDoCorpo();
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
        // Emissao manual em coordenadas 2D (X/Y): sempre de baixo para cima.
        // O componente pode ter sido criado por codigo ou informado no Inspector.
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(corPoeira);
        main.startSpeed = 0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.25f);
        main.gravityModifier = gravidadePoeira;
        main.maxParticles = Mathf.Max(128,
            Mathf.CeilToInt(quantidadeParticulasPoeira * densidadePoeira) * 2);

        var emissao = efeitoPoeira.emission;
        emissao.enabled = false; // Sem emissao automatica duplicada.
        var formato = efeitoPoeira.shape;
        formato.enabled = false; // Cada particula recebe posicao/velocidade proprias.
        var velocidadeExtra = efeitoPoeira.velocityOverLifetime;
        velocidadeExtra.enabled = false;

        if (criarPoeira)
        {
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.7f;
            // A configuracao de direcao e tamanho e feita por EmitParams,
            // para nao depender da rotacao do Particle System em 2D.
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

    private void OnDisable()
    {
        feedbackDano.Restaurar();
    }

    private void OnDestroy()
    {
        if (materialPoeiraCriado != null)
            Destroy(materialPoeiraCriado);

        foreach (Material clone in clonesCriados)
            if (clone != null) Destroy(clone);
        clonesCriados.Clear();
        materiaisRecortados.Clear();
    }

    private void EmitirTerra(int quantidade)
    {
        if (efeitoPoeira == null || quantidade <= 0) return;

        // As posicoes e velocidades estao em espaco WORLD, no plano XY.
        Vector3 origem = posicaoOculta + offsetPoeira;
        float metadeFaixa = Mathf.Max(0.05f, larguraSaidaPoeira);
        float tamanhoMinimo = Mathf.Max(0.08f, alturaEmergir * 0.025f) * tamanhoPoeira;
        float tamanhoMaximo = Mathf.Max(0.15f, alturaEmergir * 0.06f) * tamanhoPoeira;
        float impulso = Mathf.Max(1.8f, alturaEmergir * 0.45f) * forcaSubidaPoeira;

        for (int i = 0; i < quantidade; i++)
        {
            ParticleSystem.EmitParams terra = new ParticleSystem.EmitParams();
            terra.position = origem + new Vector3(
                Random.Range(-metadeFaixa, metadeFaixa),
                Random.Range(-0.08f, 0.08f), 0f);

            // A terra sobe principalmente em Y e se abre um pouco para os lados.
            terra.velocity = new Vector3(
                Random.Range(-impulso * 0.5f, impulso * 0.5f),
                Random.Range(impulso * 0.85f, impulso * 1.5f),
                0f);
            terra.startSize = Random.Range(tamanhoMinimo, tamanhoMaximo);
            terra.startLifetime = Random.Range(0.7f, 1.25f);
            Color cor = corPoeira;
            cor.a *= Random.Range(0.8f, 1f);
            terra.startColor = cor;
            efeitoPoeira.Emit(terra, 1);
        }
    }

    private IEnumerator AvisarEEmergir()
    {
        preparandoEmergencia = true;
        TocarAoSerInvocado();

        if (efeitoPoeira == null)
            PrepararEfeitoPoeira();

        if (efeitoPoeira != null)
        {
            efeitoPoeira.transform.position = posicaoOculta + offsetPoeira;
            var mainPoeira = efeitoPoeira.main;
            mainPoeira.startColor = new ParticleSystem.MinMaxGradient(corPoeira);
            mainPoeira.gravityModifier = gravidadePoeira;
            mainPoeira.maxParticles = Mathf.Max(128,
                Mathf.CeilToInt(quantidadeParticulasPoeira * densidadePoeira) * 2);
            efeitoPoeira.Clear(true);
            efeitoPoeira.Play(true);

            int total = Mathf.Max(1,
                Mathf.RoundToInt(quantidadeParticulasPoeira * densidadePoeira));

            if (tempoAvisoPoeira <= 0f)
            {
                EmitirTerra(total);
            }
            else
            {
                // Varias ondas durante o aviso criam um jato continuo de terra,
                // em vez de poucas particulas em um unico frame.
                int ondas = Mathf.Max(1, Mathf.CeilToInt(tempoAvisoPoeira / 0.08f));
                int jaEmitidas = 0;
                for (int onda = 0; onda < ondas; onda++)
                {
                    int alvo = Mathf.RoundToInt((onda + 1f) * total / ondas);
                    EmitirTerra(alvo - jaEmitidas);
                    jaEmitidas = alvo;
                    yield return new WaitForSeconds(tempoAvisoPoeira / ondas);
                }
            }
        }
        else if (tempoAvisoPoeira > 0f)
        {
            yield return new WaitForSeconds(tempoAvisoPoeira);
        }

        jaEmergiu = true;
        preparandoEmergencia = false;
    }

    private void Update()
    {
        feedbackDano.Atualizar();
        if (vidaAtual <= 0) return;
        audioInimigo.Atualizar(this);
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
            // Lerp com velocidadeSubida=70 completava tudo em um frame.
            // MoveTowards usa unidades por segundo e revela a subida gradualmente.
            transform.position = Vector3.MoveTowards(
                transform.position, posicaoExposta,
                Mathf.Max(0f, velocidadeSubida) * Time.deltaTime);
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
            float vidaAntes = playerHealth.VidaAtual;
            playerHealth.TomarDanoDeInimigo(danoNoJogador);
            if (playerHealth.VidaAtual < vidaAntes) audioInimigo.TocarHitCausado(this);
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
        if (vidaAtual <= 0 || quantidadeDano <= 0) return;
        audioInimigo.TocarHitRecebido(this);
        vidaAtual -= quantidadeDano;
        feedbackDano.ReceberHit(this, armatureComponent);

        if (vidaAtual <= 0)
        {
            Morrer();
        }
    }

    public void ReceberDano(int quantidadeDano = 1)
    {
        TomarDano(quantidadeDano);
    }


    private void Morrer()
    {
        EstadoMundoCheckpoint.RegistrarMorte(gameObject);
        audioInimigo.TocarMorte(this);
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>(true))
            c.enabled = false;
        Destroy(gameObject, feedbackDano.DuracaoVisivel);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 centroDeteccao = transform.position + (Vector3)offsetDeteccao;
        Gizmos.DrawWireSphere(centroDeteccao, raioDeteccao);

        // Linha ciano na altura em que a malha e recortada.
        float y = Application.isPlaying ? superficieChaoY + ajusteNivelSuperficie :
            pontoSuperficie != null ? pontoSuperficie.position.y + ajusteNivelSuperficie :
            transform.position.y + alturaEmergir + ajusteNivelSuperficie;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(transform.position.x - 45f, y, 0f),
                        new Vector3(transform.position.x + 45f, y, 0f));
    }
}