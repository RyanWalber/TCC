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
    }

    void LateUpdate()
    {
        if (cameraPrincipal == null || aniKaya == null) return;

        Vector3 posicaoMouseTela = Input.mousePosition;
        Vector3 posicaoMouseMundo = cameraPrincipal.ScreenToWorldPoint(new Vector3(
            posicaoMouseTela.x,
            posicaoMouseTela.y,
            transform.position.z - cameraPrincipal.transform.position.z
        ));

        bool olhandoEsquerda = posicaoMouseMundo.x < centroDoPersonagem.position.x;

        if (olhandoEsquerda)
        {
            aniKaya.localScale = new Vector3(-Mathf.Abs(escalaOriginalKaya.x), escalaOriginalKaya.y, escalaOriginalKaya.z);
            aniKaya.localPosition = new Vector3(posicaoOriginalKaya.x + ajusteCentroX, posicaoOriginalKaya.y, posicaoOriginalKaya.z);
        }
        else
        {
            aniKaya.localScale = new Vector3(Mathf.Abs(escalaOriginalKaya.x), escalaOriginalKaya.y, escalaOriginalKaya.z);
            aniKaya.localPosition = posicaoOriginalKaya;
        }

        Vector3 pontoLocalMouse = aniKaya.InverseTransformPoint(posicaoMouseMundo);
        Vector3 direcaoLocal = pontoLocalMouse - transform.localPosition;
        float angulo = Mathf.Atan2(direcaoLocal.y, direcaoLocal.x) * Mathf.Rad2Deg;

        transform.localRotation = Quaternion.Euler(0f, 0f, angulo);
    }
}