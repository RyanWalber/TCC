using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string nomeCena = "FaseFome";
    public int faseAtual = 1;
    public bool passouCheckpoint = false;
    public bool temCheckpoint => passouCheckpoint;
    public Vector3 posicaoCheckpoint = Vector3.zero;
    public int moedasNoCheckpoint = 0;
    public List<string> moedasColetadasIDs = new List<string>();

    public float vidaKaya = 100f;
    public float furiaKaya = 0f;

    // Estado separado para que morrer restaure os atributos do último checkpoint.
    public string nomeCenaCheckpoint = "";
    public float vidaKayaCheckpoint = 100f;
    public float furiaKayaCheckpoint = 0f;
    public bool checkpointStatusRegistrado = false;

    public SaveData()
    {
        nomeCena = "FaseFome";
        faseAtual = 1;
        passouCheckpoint = false;
        posicaoCheckpoint = Vector3.zero;
        moedasNoCheckpoint = 0;
        moedasColetadasIDs = new List<string>();

        vidaKaya = 100f;
        furiaKaya = 0f;
        nomeCenaCheckpoint = "";
        vidaKayaCheckpoint = 100f;
        furiaKayaCheckpoint = 0f;
        checkpointStatusRegistrado = false;
    }
}