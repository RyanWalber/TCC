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

    public SaveData()
    {
        nomeCena = "FaseFome";
        faseAtual = 1;
        passouCheckpoint = false;
        posicaoCheckpoint = Vector3.zero;
        moedasNoCheckpoint = 0;
        moedasColetadasIDs = new List<string>();
    }
}