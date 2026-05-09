using UnityEngine;

using UnityEngine;

public class MiraMouse : MonoBehaviour
{
    // Crie um campo para arrastar o sprite do braço/arma aqui na Unity
    [SerializeField] private SpriteRenderer spriteArma;

    void Update()
    {
        // 1. Pega a posição do mouse na tela e converte para o mundo
        Vector3 posicaoMouse = Input.mousePosition;
        Vector3 posicaoMundo = Camera.main.ScreenToWorldPoint(new Vector3(posicaoMouse.x, posicaoMouse.y, transform.position.z - Camera.main.transform.position.z));

        // 2. Calcula a direção (Mouse - Posição do Objeto)
        Vector2 direcao = (Vector2)posicaoMundo - (Vector2)transform.position;

        // 3. Calcula o ângulo em graus
        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;

        // 4. Aplica a rotação SEMPRE baseada no ângulo do mundo
        transform.rotation = Quaternion.Euler(0, 0, angulo);

        // 5. O SEGREDO DO FLIP:
        // Se o mouse estiver à esquerda do personagem
        if (posicaoMundo.x < transform.position.x)
        {
            // Vira o sprite da arma para a esquerda (flipY) para ela não ficar de ponta-cabeça
            // e mantém a rotação do mundo correta.
            if (spriteArma != null) spriteArma.flipY = true;
        }
        else // Se o mouse estiver à direita
        {
            // Volta o sprite para o normal
            if (spriteArma != null) spriteArma.flipY = false;
        }
    }
}