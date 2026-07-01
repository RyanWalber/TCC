using UnityEngine;

public class MiraMouse : MonoBehaviour
{
    [Header("Configuracao")]
    [SerializeField] private Transform aniKaya;
    [SerializeField] private float ajusteCentroX = 0f;

    private Camera cameraPrincipal;
    private Vector3 escalaOriginalKaya;
    private Vector3 posicaoOriginalKaya;
    private Transform centroDoPersonagem;

    // Variáveis de controle para capturar a animação perfeitamente
    private Quaternion ultimaRotacaoFinal;
    private Quaternion ultimaRotacaoAnimacao = Quaternion.identity;
    private bool iniciado = false;

    void Start()
    {
        cameraPrincipal = Camera.main;
        centroDoPersonagem = transform.root;

        if (aniKaya == null)
        {
            aniKaya = transform.parent;
        }

        if (aniKaya != null)
        {
            escalaOriginalKaya = aniKaya.localScale;
            posicaoOriginalKaya = aniKaya.localPosition;
        }

        ultimaRotacaoFinal = transform.localRotation;
    }

    void LateUpdate()
    {
        Quaternion rotacaoAtual = transform.localRotation;
        Quaternion rotacaoPristinaDaAnimacao;

        // Se for o primeiro frame ou se o Animator aplicou um novo frame de animação real
        if (!iniciado || Quaternion.Angle(rotacaoAtual, ultimaRotacaoFinal) > 0.01f)
        {
            // O Animator rodou! Capturamos o balanço puro do braço vindo da animação
            rotacaoPristinaDaAnimacao = rotacaoAtual;
            ultimaRotacaoAnimacao = rotacaoAtual;
            iniciado = true;
        }
        else
        {
            // A Unity otimizou o frame? Sem problemas, usamos o último balanço conhecido
            rotacaoPristinaDaAnimacao = ultimaRotacaoAnimacao;
        }

        // 1. Sua lógica original de Flip (perfeita)
        Vector3 posicaoMouseTela = Input.mousePosition;
        Vector3 posicaoMouseMundo = cameraPrincipal.ScreenToWorldPoint(new Vector3(
            posicaoMouseTela.x, 
            posicaoMouseTela.y, 
            transform.position.z - cameraPrincipal.transform.position.z
        ));

        if (posicaoMouseMundo.x < centroDoPersonagem.position.x)
        {
            if (aniKaya != null)
            {
                aniKaya.localScale = new Vector3(-Mathf.Abs(escalaOriginalKaya.x), escalaOriginalKaya.y, escalaOriginalKaya.z);
                aniKaya.localPosition = new Vector3(posicaoOriginalKaya.x + ajusteCentroX, posicaoOriginalKaya.y, posicaoOriginalKaya.z);
            }
        }
        else
        {
            if (aniKaya != null)
            {
                aniKaya.localScale = new Vector3(Mathf.Abs(escalaOriginalKaya.x), escalaOriginalKaya.y, escalaOriginalKaya.z);
                aniKaya.localPosition = posicaoOriginalKaya;
            }
        }

        // 2. Lógica de Mira injetando o movimento da animação por dentro
        if (aniKaya != null)
        {
            Vector3 alvoLocal = aniKaya.InverseTransformPoint(posicaoMouseMundo);
            Vector3 direcaoLocal = alvoLocal - transform.localPosition;
            float anguloLocal = Mathf.Atan2(direcaoLocal.y, direcaoLocal.x) * Mathf.Rad2Deg;
            
            // Aqui a mágica acontece: Aplica a mira do mouse E mantém o balanço da corrida ativo
            transform.localRotation = Quaternion.Euler(0f, 0f, anguloLocal) * rotacaoPristinaDaAnimacao;

            // Salva o estado atual para fazer a checagem inteligente no próximo frame
            ultimaRotacaoFinal = transform.localRotation;
        }
    }
}