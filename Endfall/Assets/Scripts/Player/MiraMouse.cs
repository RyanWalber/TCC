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

        if (aniKaya != null)
        {
            Vector3 alvoLocal = aniKaya.InverseTransformPoint(posicaoMouseMundo);
            Vector3 direcaoLocal = alvoLocal - transform.localPosition;
            float anguloLocal = Mathf.Atan2(direcaoLocal.y, direcaoLocal.x) * Mathf.Rad2Deg;
            
            transform.localRotation = Quaternion.Euler(0f, 0f, anguloLocal);
        }
    }
}
