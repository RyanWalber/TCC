using UnityEngine;
using UnityEngine.SceneManagement;

public class ZonaMorte : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.gameObject.name.Contains("Kaya"))
        {
            RenascerNoCheckpoint();
        }
    }

    public void RenascerNoCheckpoint()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RenascerNoCheckpoint();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}