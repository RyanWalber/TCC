using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private bool jaAtivado = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !jaAtivado)
        {
            jaAtivado = true;

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.ProcessarCheckpoint(transform.position);
                Debug.Log("Progresso salvo no Checkpoint!");
            }
        }
    }
}