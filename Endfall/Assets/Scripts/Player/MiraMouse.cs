using UnityEngine;

public class MiraMouse : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteArma;
    
    [Header("Nova Referência do Braço")]
    [Tooltip("Arraste o SpriteRenderer do Braço da Kaya aqui.")]
    [SerializeField] private SpriteRenderer spriteBraco;

    private PlayerController playerController;

    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
    }

    void Update()
    {
        Vector3 posicaoMouse = Input.mousePosition;
        Vector3 posicaoMundo = Camera.main.ScreenToWorldPoint(new Vector3(posicaoMouse.x, posicaoMouse.y, transform.position.z - Camera.main.transform.position.z));

        Vector3 direcao = posicaoMundo - transform.position;
        direcao.z = 0f;

        if (direcao.sqrMagnitude < 0.0001f) return;

        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        
        bool pointingLeft = angulo > 90f || angulo < -90f;

        if (playerController != null)
        {
            playerController.SetFacing(!pointingLeft, true);
        }

        transform.rotation = Quaternion.Euler(0f, 0f, angulo);

        Vector3 novaEscala = Vector3.one;
        if (pointingLeft)
        {
            novaEscala.y = -1f;
        }
        
        if (transform.parent.localScale.x < 0)
        {
            novaEscala.x = -1f;
            novaEscala.y *= -1f;
        }

        transform.localScale = novaEscala;
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