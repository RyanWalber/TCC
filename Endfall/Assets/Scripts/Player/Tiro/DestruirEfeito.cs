using UnityEngine;

public class DestruirEfeito : MonoBehaviour
{
    public float tempoDeVida = 0.05f;

    private void Start()
    {
        Destroy(gameObject, tempoDeVida);
    }
}