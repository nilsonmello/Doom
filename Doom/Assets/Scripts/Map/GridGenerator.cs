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
    [SerializeField] private int seed = 0;
    [SerializeField] private bool usarSeedFixa = false;

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
            Debug.LogWarning("GridGenerator: nenhum prefab atribuído no array.");
            return;
        }

        LimparGrid();

        Transform container = parentContainer != null ? parentContainer : transform;

        float larguraTotal = (columns - 1) * spacing;
        float profundidadeTotal = (rows - 1) * spacing;

        Vector3 offset = centralizarNaOrigem
            ? new Vector3(larguraTotal / 2f, 0f, profundidadeTotal / 2f)
            : Vector3.zero;

        System.Collections.Generic.List<GameObject> instanciasGeradas = new System.Collections.Generic.List<GameObject>();
        System.Random rng = usarSeedFixa ? new System.Random(seed) : new System.Random();

        for (int linha = 0; linha < rows; linha++)
        {
            for (int coluna = 0; coluna < columns; coluna++)
            {
                Vector3 posicaoLocal = new Vector3(coluna * spacing, 0f, linha * spacing) - offset;
                Vector3 posicaoMundo = container.position + posicaoLocal;

                GameObject prefabEscolhido = prefabs[rng.Next(prefabs.Length)];
                GameObject instancia = Instantiate(prefabEscolhido, posicaoMundo, Quaternion.identity, container);
                instancia.name = $"{prefabEscolhido.name}_{linha}_{coluna}";
                instanciasGeradas.Add(instancia);
            }
        }

        if (excluirAleatoriamente)
            AplicarExclusaoAleatoria(instanciasGeradas, rng);
    }

    private void AplicarExclusaoAleatoria(System.Collections.Generic.List<GameObject> instancias, System.Random rng)
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

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform filho = container.GetChild(i);
            if (Application.isPlaying)
                Destroy(filho.gameObject);
            else
                DestroyImmediate(filho.gameObject);
        }
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