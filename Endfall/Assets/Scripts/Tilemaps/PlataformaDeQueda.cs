using System.Collections;
using UnityEngine;

public class PlataformaDeQueda : MonoBehaviour
{
    public float tempoParaCair = 0.5f;
    public float intensidadeTreme = 0.05f;
    public float tempoParaRenascer = 3f;
    public bool renascerPlataforma = true;

    private Rigidbody2D rb;
    private Vector3 posicaoInicial;
    private Quaternion rotacaoInicial;
    private bool estaCaindo = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        posicaoInicial = transform.position;
        rotacaoInicial = transform.rotation;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (estaCaindo) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.contacts[0].normal.y < -0.5f)
            {
                StartCoroutine(SequenciaDeQueda());
            }
        }
    }

    private IEnumerator SequenciaDeQueda()
    {
        estaCaindo = true;

        float tempo = 0f;
        while (tempo < tempoParaCair)
        {
            transform.position = posicaoInicial + (Vector3)Random.insideUnitCircle * intensidadeTreme;
            tempo += Time.deltaTime;
            yield return null;
        }

        transform.position = posicaoInicial;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        yield return new WaitForSeconds(tempoParaRenascer);

        if (renascerPlataforma)
        {
            ResetarPlataforma();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void ResetarPlataforma()
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        transform.position = posicaoInicial;
        transform.rotation = rotacaoInicial;
        estaCaindo = false;
    }
}