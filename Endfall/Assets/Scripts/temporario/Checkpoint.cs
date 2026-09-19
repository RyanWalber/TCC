using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Tooltip("")]
    [SerializeField] private Transform pontoDeRenascer;

    private bool jaAtivado = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!jaAtivado && other.CompareTag("Player"))
        {
            jaAtivado = true;

            Vector3 posicaoSalvar = pontoDeRenascer != null ? pontoDeRenascer.position : other.transform.position;

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.RegistrarCheckpoint(posicaoSalvar);
            }
        }
    }
}