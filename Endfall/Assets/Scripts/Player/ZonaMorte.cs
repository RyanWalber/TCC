using UnityEngine;
using UnityEngine.SceneManagement;

public class ZonaMorte : MonoBehaviour
{
    private bool reinicioSolicitado;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.gameObject.name.Contains("Kaya"))
        {
            RenascerNoCheckpoint();
        }
    }

    public void RenascerNoCheckpoint()
    {
        if (reinicioSolicitado) return;
        reinicioSolicitado = true;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RenascerNoCheckpoint();
        }
        else
        {
            LevelLoader.CarregarComFade(SceneManager.GetActiveScene().name);
        }
    }
}