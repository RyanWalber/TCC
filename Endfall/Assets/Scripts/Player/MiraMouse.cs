using UnityEngine;

public class MiraMouse : MonoBehaviour
{
    [Header("Configuracao do Corpo")]
    [SerializeField] private Transform aniKaya;
    [SerializeField] private float ajusteCentroX = 0f;

    [Header("Mecanica estilo Plazma Burst")]
    [SerializeField] private float tempoManterMira = 0.4f;
    [SerializeField] private float velocidadeTransicao = 12f;

    private Camera cameraPrincipal;
    private Vector3 posicaoOriginalKaya;
    private Transform centroDoPersonagem;

    private float timerTiro = 0f;
    private float pesoMira = 0f;

    void Start()
    {
        cameraPrincipal = Camera.main;
        centroDoPersonagem = transform.root;

        if (aniKaya == null && transform.parent != null)
        {
            aniKaya = transform.parent;
        }

        if (aniKaya != null)
        {
            posicaoOriginalKaya = aniKaya.localPosition;
        }
    }

    void LateUpdate()
    {
        if (cameraPrincipal == null || aniKaya == null) return;

        Quaternion rotacaoAnimacao = transform.localRotation;

        Vector3 posicaoMouseTela = Input.mousePosition;
        Vector3 posicaoMouseMundo = cameraPrincipal.ScreenToWorldPoint(new Vector3(
            posicaoMouseTela.x,
            posicaoMouseTela.y,
            -cameraPrincipal.transform.position.z
        ));

        bool olhandoEsquerda = posicaoMouseMundo.x < centroDoPersonagem.position.x;

        if (olhandoEsquerda)
        {
            aniKaya.localRotation = Quaternion.Euler(0f, 180f, 0f);
            aniKaya.localPosition = new Vector3(posicaoOriginalKaya.x + ajusteCentroX, posicaoOriginalKaya.y, posicaoOriginalKaya.z);
        }
        else
        {
            aniKaya.localRotation = Quaternion.Euler(0f, 0f, 0f);
            aniKaya.localPosition = posicaoOriginalKaya;
        }

        bool apertandoAtirar = Input.GetMouseButton(0);

        if (apertandoAtirar)
        {
            timerTiro = tempoManterMira;
        }
        else if (timerTiro > 0)
        {
            timerTiro -= Time.deltaTime;
        }

        bool miraAtiva = apertandoAtirar || timerTiro > 0;
        float pesoAlvo = miraAtiva ? 1f : 0f;
        pesoMira = Mathf.MoveTowards(pesoMira, pesoAlvo, Time.deltaTime * velocidadeTransicao);

        if (pesoMira <= 0f)
        {
            transform.localRotation = rotacaoAnimacao;
            return;
        }

        Vector2 direcaoMundo = posicaoMouseMundo - transform.position;
        float anguloMundo = Mathf.Atan2(direcaoMundo.y, direcaoMundo.x) * Mathf.Rad2Deg;
        float anguloMira = olhandoEsquerda ? (180f - anguloMundo) : anguloMundo;

        Quaternion rotacaoMira = Quaternion.Euler(0f, 0f, anguloMira);

        transform.localRotation = Quaternion.Slerp(rotacaoAnimacao, rotacaoMira, pesoMira);
    }
}