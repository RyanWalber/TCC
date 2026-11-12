using System.Collections;
using UnityEngine;

public class PlataformaDeQueda : MonoBehaviour
{
    public float tempoParaCair = 0.5f;
    public float intensidadeTreme = 0.05f;
    public float tempoParaRenascer = 3f;
    public bool renascerPlataforma = true;

    [Header("Audio desta plataforma de queda")]
    [SerializeField] private AudioClip somTremor;
    [SerializeField] private AudioClip somQueda;
    [SerializeField] private AudioClip somRetorno;
    [SerializeField, Range(0f, 1f)] private float volumeAudio = 0.7f;

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

        if (!collision.gameObject.CompareTag("Player")) return;

        // Usa todos os contatos: o primeiro nem sempre corresponde aos pes.
        foreach (ContactPoint2D contato in collision.contacts)
        {
            if (contato.normal.y < -0.5f)
            {
                StartCoroutine(SequenciaDeQueda());
                break;
            }
        }
    }

    private IEnumerator SequenciaDeQueda()
    {
        estaCaindo = true;
        AudioObjeto2D.Tocar(somTremor, volumeAudio, transform.position, "Plataforma tremendo");

        float tempo = 0f;
        // Move o corpo cinemático usando a física, sem teleportar o Transform
        // a cada frame. Mantém a intensidade configurada no Inspector intacta.
        while (tempo < tempoParaCair)
        {
            Vector2 deslocamento = Random.insideUnitCircle * intensidadeTreme;
            if (rb != null)
                rb.MovePosition((Vector2)posicaoInicial + deslocamento);
            else
                transform.position = posicaoInicial + (Vector3)deslocamento;
            tempo += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (rb != null)
            rb.position = posicaoInicial;
        else
            transform.position = posicaoInicial;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        AudioObjeto2D.Tocar(somQueda, volumeAudio, transform.position, "Plataforma caindo");

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
        AudioObjeto2D.Tocar(somRetorno, volumeAudio, posicaoInicial, "Plataforma retornando");
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = posicaoInicial;
            rb.rotation = rotacaoInicial.eulerAngles.z;
        }

        transform.position = posicaoInicial;
        transform.rotation = rotacaoInicial;
        estaCaindo = false;
    }
}