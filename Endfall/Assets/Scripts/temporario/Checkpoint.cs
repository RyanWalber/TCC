using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    private bool ativado = false;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (ativado || !collision.CompareTag("Player")) return;

        ativado = true;

        if (SaveManager.Instance != null && LevelManager.Instance != null)
        {
            SaveData dados = SaveManager.Instance.dadosAtuais;

            dados.temCheckpoint = true;
            dados.posicaoCheckpoint = transform.position;

            dados.moedasNoCheckpoint = LevelManager.Instance.moedasFaseAtual;

            foreach (string id in LevelManager.Instance.moedasColetadasTemporarias)
            {
                if (!dados.moedasColetadasIDs.Contains(id))
                {
                    dados.moedasColetadasIDs.Add(id);
                }
            }

            SaveManager.Instance.SalvarSlot(0);
        }
    }
}