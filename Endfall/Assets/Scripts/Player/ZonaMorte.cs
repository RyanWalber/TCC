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
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}