using System.Collections.Generic;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject[] prefabs;

    [Header("Grid")]
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 5;
    [SerializeField] private float spacing = 2f;

    [Header("Opções")]
    [SerializeField] private bool centralizarNaOrigem = true;
    [SerializeField] private Transform parentContainer;
    [SerializeField] private bool gerarAoIniciar = false;

    [Header("Exclusão Aleatória")]
    [SerializeField] private bool excluirAleatoriamente = false;
    [Range(0f, 1f)]
    [SerializeField] private float chanceDeExclusao = 0.2f;

    [Header("Seed")]
    [SerializeField] private int seed = 0;
    [SerializeField] private bool useFixedSeed = false;

    private void Start()
    {
        if (gerarAoIniciar)
            GerarGrid();
    }

    [ContextMenu("Gerar Grid")]
    public void GerarGrid()
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            return;
        }

        LimparGrid();

        Transform container = parentContainer != null ? parentContainer : transform;

        float larguraTotal = (columns - 1) * spacing;
        float profundidadeTotal = (rows - 1) * spacing;

        Vector3 offset = centralizarNaOrigem
            ? new Vector3(larguraTotal / 2f, 0f, profundidadeTotal / 2f)
            : Vector3.zero;

        List<GameObject> instanciasGeradas = new List<GameObject>();
        System.Random rng = ProceduralUtils.CreateRng(ref seed, useFixedSeed);

        for (int linha = 0; linha < rows; linha++)
        {
            for (int coluna = 0; coluna < columns; coluna++)
            {
                Vector3 posicaoLocal = new Vector3(coluna * spacing, 0f, linha * spacing) - offset;
                Vector3 posicaoMundo = container.position + posicaoLocal;

                GameObject instancia = ProceduralUtils.SpawnRandom(
                    prefabs, posicaoMundo, Quaternion.identity, container, rng,
                    out GameObject prefabEscolhido);

                if (instancia == null) continue;

                instancia.name = $"{prefabEscolhido.name}_{linha}_{coluna}";
                instanciasGeradas.Add(instancia);
            }
        }

        if (excluirAleatoriamente)
            AplicarExclusaoAleatoria(instanciasGeradas, rng);
    }

    private void AplicarExclusaoAleatoria(List<GameObject> instancias, System.Random rng)
    {
        for (int i = instancias.Count - 1; i >= 0; i--)
        {
            if (rng.NextDouble() < chanceDeExclusao)
            {
                GameObject alvo = instancias[i];

                if (Application.isPlaying)
                    Destroy(alvo);
                else
                    DestroyImmediate(alvo);
            }
        }
    }

    [ContextMenu("Limpar Grid")]
    public void LimparGrid()
    {
        Transform container = parentContainer != null ? parentContainer : transform;
        ProceduralUtils.ClearChildren(container);
    }

    private void OnDrawGizmosSelected()
    {
        float larguraTotal = (columns - 1) * spacing;
        float profundidadeTotal = (rows - 1) * spacing;

        Vector3 offset = centralizarNaOrigem
            ? new Vector3(larguraTotal / 2f, 0f, profundidadeTotal / 2f)
            : Vector3.zero;

        Gizmos.color = Color.cyan;

        for (int linha = 0; linha < rows; linha++)
        {
            for (int coluna = 0; coluna < columns; coluna++)
            {
                Vector3 posicaoLocal = new Vector3(coluna * spacing, 0f, linha * spacing) - offset;
                Vector3 posicaoMundo = transform.position + posicaoLocal;
                Gizmos.DrawWireSphere(posicaoMundo, 0.15f);
            }
        }

        Gizmos.color = Color.yellow;
        Vector3 centro = transform.position + (centralizarNaOrigem ? Vector3.zero : new Vector3(larguraTotal / 2f, 0f, profundidadeTotal / 2f));
        Gizmos.DrawWireCube(centro, new Vector3(larguraTotal, 0.05f, profundidadeTotal));
    }
}
