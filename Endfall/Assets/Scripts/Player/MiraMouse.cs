using UnityEngine;

public class MiraMouse : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteArma;
    
    [Header("Referência do Braço")]
    [Tooltip("Arraste o SpriteRenderer do Braço da Kaya aqui.")]
    [SerializeField] private SpriteRenderer spriteBraco;

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
        // (Isso inverte a escala X do pai imediatamente)
        if (playerController != null)
        {
            playerController.SetFacing(!pointingLeft, true);
        }

        // 4. O SEGREDO DE OURO: Direção Local!
        // Convertendo o ponto do mouse para o "espaço local" do pai, a Unity 
        // entende perfeitamente que o corpo está invertido e ajusta o cálculo sozinha.
        Vector3 direcaoLocal = direcaoGlobal;
        
        if (transform.parent != null)
        {
            Vector3 alvoLocal = transform.parent.InverseTransformPoint(posicaoMundo);
            direcaoLocal = alvoLocal - transform.localPosition;
            direcaoLocal.z = 0f;
        }

        // 5. Calcular o ângulo local
        float anguloLocal = Mathf.Atan2(direcaoLocal.y, direcaoLocal.x) * Mathf.Rad2Deg;

        // 6. Aplicar APENAS a rotação local (localRotation)
        transform.localRotation = Quaternion.Euler(0f, 0f, anguloLocal);

        // 7. Removemos qualquer FlipY! 
        // Como o corpo inteiro (Pai) virou (Scale X = -1), a arma já vai ser 
        // espelhada perfeitamente na vertical pela própria Unity sem ficar de ponta-cabeça.
        if (spriteArma != null) spriteArma.flipY = false;
        if (spriteBraco != null) spriteBraco.flipY = false;
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