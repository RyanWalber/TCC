using UnityEngine;

public class SistemaFuria : MonoBehaviour
{
    [Header("Configurações da Fúria")]
    public float furiaMaxima = 100f;
    public float furiaAtual = 0f;
    public float duracaoFuria = 5f;

    [Header("Bônus em Fúria")]
    public float multiplicadorDano = 1.5f; 
    public float multiplicadorCadencia = 0.5f;
    [Header("Controles")]
    public KeyCode teclaFuria = KeyCode.Q;

    private bool estaEmFuria = false;
    private float tempoRestanteFuria;

    public bool EstaEmFuria => estaEmFuria;

    private void Update()
    {
        if (Input.GetKeyDown(teclaFuria) && !estaEmFuria && furiaAtual >= furiaMaxima)
        {
            AtivarFuria();
        }

        if (estaEmFuria)
        {
            tempoRestanteFuria -= Time.deltaTime;
            if (tempoRestanteFuria <= 0)
            {
                DesativarFuria();
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            AdicionarFuria(25f);
        }
    }

    public void AdicionarFuria(float quantidade)
    {
        if (estaEmFuria) return; 

        furiaAtual += quantidade;
        furiaAtual = Mathf.Clamp(furiaAtual, 0f, furiaMaxima);
        Debug.Log($"Fúria Atual: {furiaAtual}/{furiaMaxima}");
    }

    private void AtivarFuria()
    {
        estaEmFuria = true;
        tempoRestanteFuria = duracaoFuria;
        furiaAtual = 0f; 
        Debug.Log(">>> FÚRIA ATIVADA! <<<");
    }

    private void DesativarFuria()
    {
        estaEmFuria = false;
        Debug.Log("Fúria Finalizada.");
    }
}