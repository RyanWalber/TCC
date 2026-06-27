using System.Collections.Generic;
using UnityEngine;

public class MiraMouse : MonoBehaviour
{
    [Header("Lista de Todos os Membros da Mira")]
    [Tooltip("Arraste aqui todos os ossos/membros que devem girar com o mouse (Braço, Antebraço, Arma, etc.)")]
    [SerializeField] private List<Transform> membrosDaMira = new List<Transform>();

    private PlayerController playerController;

    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
    }

    void Update()
    {
        // 1. Pegar a posição do mouse no mundo
        Vector3 posicaoMouse = Input.mousePosition;
        Vector3 posicaoMundo = Camera.main.ScreenToWorldPoint(new Vector3(posicaoMouse.x, posicaoMouse.y, transform.position.z - Camera.main.transform.position.z));

        // 2. Direção global pura apenas para saber para qual lado ela deve olhar
        Vector3 direcaoGlobal = posicaoMundo - transform.position;
        direcaoGlobal.z = 0f;

        if (direcaoGlobal.sqrMagnitude < 0.0001f) return;

        bool pointingLeft = direcaoGlobal.x < 0;

        // 3. Avisa o PlayerController para virar o corpo da Kaya
        if (playerController != null)
        {
            playerController.SetFacing(!pointingLeft, true);
        }

        // 4. O SEGREDO DE OURO: Direção Local!
        Vector3 direcaoLocal = direcaoGlobal;
        
        if (transform.parent != null)
        {
            Vector3 alvoLocal = transform.parent.InverseTransformPoint(posicaoMundo);
            direcaoLocal = alvoLocal - transform.localPosition;
            direcaoLocal.z = 0f;
        }

        // 5. Calcular o ângulo local
        float anguloLocal = Mathf.Atan2(direcaoLocal.y, direcaoLocal.x) * Mathf.Rad2Deg;

        // 6. Aplicar a rotação local em todos os membros adicionados na lista
        foreach (Transform membro in membrosDaMira)
        {
            if (membro != null)
            {
                membro.localRotation = Quaternion.Euler(0f, 0f, anguloLocal);
            }
        }
    }

    void OnDisable()
    {
        if (playerController != null) playerController.ClearFacingOverride();
    }

    void OnDestroy()
    {
        if (playerController != null) playerController.ClearFacingOverride();
    }
}