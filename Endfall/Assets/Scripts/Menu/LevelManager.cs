using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private Transform playerTransform;
    [SerializeField] private List<GameObject> moedasNaCena = new List<GameObject>();

    public int moedasFaseAtual = 0;
    public List<string> moedasColetadasTemporarias = new List<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (SaveManager.Instance == null) return;

        SaveData dados = SaveManager.Instance.DadosAtuais;

        if (dados != null && dados.temCheckpoint)
        {
            if (playerTransform != null)
            {
                playerTransform.position = new Vector3(dados.posicaoCheckpoint[0], dados.posicaoCheckpoint[1], dados.posicaoCheckpoint[2]);
            }

            moedasFaseAtual = dados.moedasNoCheckpoint;

            foreach (GameObject moeda in moedasNaCena)
            {
                if (moeda != null && dados.moedasColetadasIDs.Contains(moeda.name))
                {
                    moeda.SetActive(false);
                }
            }
        }
    }

    public void ColetarMoeda(string idMoeda)
    {
        moedasFaseAtual++;
        if (!moedasColetadasTemporarias.Contains(idMoeda))
        {
            moedasColetadasTemporarias.Add(idMoeda);
        }
    }

    public void ColetarMoeda(GameObject moedaObj)
    {
        if (moedaObj != null)
        {
            ColetarMoeda(moedaObj.name);
        }
    }

    public void ProcessarCheckpoint(Vector3 posicaoCentro)
    {
        if (SaveManager.Instance != null)
        {
            SaveData dados = SaveManager.Instance.DadosAtuais;
            dados.moedasNoCheckpoint = moedasFaseAtual;

            foreach (string id in moedasColetadasTemporarias)
            {
                if (!dados.moedasColetadasIDs.Contains(id))
                {
                    dados.moedasColetadasIDs.Add(id);
                }
            }

            SaveManager.Instance.RegistrarCheckpoint(posicaoCentro);
        }
    }
}