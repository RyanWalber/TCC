using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Objetos nativos de cada cena, com IDs calculados pela hierarquia da cena.
// O primeiro checkpoint consolida o que ficou para tras.
public static class EstadoMundoCheckpoint
{
    private class Registro
    {
        public GameObject obj;
        public string id;
        public float xInicial;
    }

    private static readonly List<Registro> inimigos = new List<Registro>();
    private static string cenaPreparada;

    private static string Identificador(Transform t)
    {
        string caminho = "";
        while (t != null)
        {
            caminho = "/" + t.GetSiblingIndex() + caminho;
            t = t.parent;
        }
        return t == null ? caminho : caminho;
    }

    private static void Buscar<T>() where T : Component
    {
        Scene cena = SceneManager.GetActiveScene();
        foreach (T componente in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (componente.gameObject.scene != cena) continue;
            GameObject obj = componente.gameObject;
            inimigos.Add(new Registro { obj = obj, id = cena.name + Identificador(obj.transform), xInicial = obj.transform.position.x });
        }
    }

    public static void PrepararCena(SaveData dados, float margemSeguranca)
    {
        cenaPreparada = SceneManager.GetActiveScene().name;
        inimigos.Clear();
        Buscar<InimigoBixel>();
        Buscar<InimigoBixinho>();
        Buscar<InimigoBixao>();
        Buscar<InimigoParado>();

        if (dados == null || !dados.passouCheckpoint ||
            (!string.IsNullOrEmpty(dados.nomeCenaCheckpoint) && dados.nomeCenaCheckpoint != cenaPreparada))
            return;

        float limite = dados.posicaoCheckpoint.x + Mathf.Max(0f, margemSeguranca);
        HashSet<string> mortos = new HashSet<string>(dados.inimigosEliminadosIDsCheckpoint ?? new List<string>());
        foreach (Registro r in inimigos)
            if (r.obj != null && (r.xInicial <= limite || mortos.Contains(r.id)))
                r.obj.SetActive(false);

        LimparColetaveis(dados.posicaoCheckpoint.x);
    }

    public static void RegistrarMorte(GameObject objeto)
    {
        if (objeto == null || objeto.scene.name != cenaPreparada) return;
        foreach (Registro r in inimigos)
        {
            if (r.obj != objeto) continue;
            if (SaveManager.Instance != null) SaveManager.Instance.RegistrarInimigoDerrotado(r.id);
            break;
        }
    }

    public static void ConsolidarCheckpoint(SaveData dados, Vector3 posicao)
    {
        if (dados == null) return;
        if (cenaPreparada != SceneManager.GetActiveScene().name)
            PrepararCena(null, 0f);

        if (dados.inimigosEliminadosIDs == null)
            dados.inimigosEliminadosIDs = new List<string>();
        HashSet<string> removidos = new HashSet<string>(dados.inimigosEliminadosIDs);
        float limite = posicao.x + 4f; // margem para nao renascer encostado em inimigos
        foreach (Registro r in inimigos)
        {
            if (r.obj == null) continue;
            if (r.xInicial <= limite || r.obj.transform.position.x <= limite)
            {
                removidos.Add(r.id);
                r.obj.SetActive(false);
            }
        }
        dados.inimigosEliminadosIDs = new List<string>(removidos);
        LimparColetaveis(posicao.x);
    }

    private static void LimparColetaveis(float limiteX)
    {
        Scene cena = SceneManager.GetActiveScene();
        foreach (Coin moeda in Object.FindObjectsByType<Coin>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (moeda != null && moeda.gameObject.scene == cena && moeda.transform.position.x < limiteX)
                moeda.gameObject.SetActive(false);

        foreach (HealthPickup elixir in Object.FindObjectsByType<HealthPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (elixir != null && elixir.gameObject.scene == cena && elixir.transform.position.x < limiteX)
                elixir.gameObject.SetActive(false);
    }
}
