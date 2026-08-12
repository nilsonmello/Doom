using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HandUIController : MonoBehaviour
{
    public enum WallSide { None, Left, Right }

    [Header("Referências de UI")]
    [SerializeField] private Image imagemMaoDireita;

    [SerializeField] private Image imagemMaoEsquerda;

    [Header("Sprites Base - Mão Direita")]
    [SerializeField] private Sprite spriteMaoDireitaVazia;

    [Header("Sprites Base - Mão Esquerda")]
    [SerializeField] private Sprite spriteMaoEsquerdaVazia;
    [SerializeField] private Sprite spriteMaoEsquerdaFechada;
    [SerializeField] private Sprite spriteMaoEsquerdaSegurandoInimigo;

    [Header("Sprites de Wallrun")]
    [SerializeField] private Sprite spriteWallrunDireita;
    [SerializeField] private Sprite spriteWallrunEsquerda;

    [Header("Inimigo Segurado / Carga de Arremesso")]
    [SerializeField] private Image imagemInimigoSegurado;

    [Header("Ejeção de Cápsula")]
    [Tooltip("RectTransform vazio posicionado perto da mão direita, marca de onde a cápsula sai.")]
    [SerializeField] private RectTransform pontoEjecaoCapsula;
    [Tooltip("Prefab com RectTransform + Image + BulletShellUI.")]
    [SerializeField] private GameObject prefabCapsula;
    [SerializeField] private float forcaEjecaoVertical = 500f;
    [SerializeField] private float amplitudeLateral = 150f;
    [SerializeField] [Range(0f, 1f)] private float variacaoForca = 0.2f;

    private Sprite spriteBaseDireita;
    private Sprite spriteBaseEsquerda;

    private WallSide wallrunAtivo = WallSide.None;

    private bool segurandoInimigo;

    private Coroutine animacaoMaoDireitaCoroutine;
    private bool tocandoAnimacaoMaoDireita;
    private Sprite frameAtualAnimacao;

    public bool equipArma = false;

    private void Awake()
    {
        spriteBaseDireita = spriteMaoDireitaVazia;
        spriteBaseEsquerda = spriteMaoEsquerdaVazia;

        AtualizarMaoDireita();
        AtualizarMaoEsquerda();

        if (imagemInimigoSegurado != null)
            imagemInimigoSegurado.enabled = false;
    }

    public void EquiparArma(Sprite spriteArma)
    {
        spriteBaseDireita = spriteArma != null ? spriteArma : spriteMaoDireitaVazia;
        AtualizarMaoDireita();
        equipArma = true;
    }

    public void DesequiparArma()
    {
        EquiparArma(null);
        equipArma = false;
    }

    public void SetWeaponSprite(Sprite idleSprite) => EquiparArma(idleSprite);

    public void SetWeaponEmpty() => DesequiparArma();

    public void PlayWeaponFrames(Sprite[] frames, float frameRate, Action onComplete = null)
    {
        if (frames == null || frames.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        if (animacaoMaoDireitaCoroutine != null)
            StopCoroutine(animacaoMaoDireitaCoroutine);

        animacaoMaoDireitaCoroutine = StartCoroutine(RotinaAnimacaoMaoDireita(frames, frameRate, onComplete));
    }

    public void PlayWeaponFramesOverTime(Sprite[] frames, float totalDuration, Action onComplete = null)
    {
        if (frames == null || frames.Length == 0)
        {
            if (totalDuration > 0f)
                StartCoroutine(EsperarEChamar(totalDuration, onComplete));
            else
                onComplete?.Invoke();
            return;
        }

        float frameRate = totalDuration / frames.Length;
        PlayWeaponFrames(frames, frameRate, onComplete);
    }

    private IEnumerator RotinaAnimacaoMaoDireita(Sprite[] frames, float frameRate, Action onComplete)
    {
        tocandoAnimacaoMaoDireita = true;

        foreach (var frame in frames)
        {
            frameAtualAnimacao = frame;
            AtualizarMaoDireita();
            yield return new WaitForSeconds(frameRate);
        }

        tocandoAnimacaoMaoDireita = false;
        frameAtualAnimacao = null;
        animacaoMaoDireitaCoroutine = null;

        AtualizarMaoDireita();
        onComplete?.Invoke();
    }

    private IEnumerator EsperarEChamar(float tempo, Action onComplete)
    {
        yield return new WaitForSeconds(tempo);
        onComplete?.Invoke();
    }

    public void SetAgarrando(bool agarrando)
    {
        spriteBaseEsquerda = agarrando ? spriteMaoEsquerdaFechada : spriteMaoEsquerdaVazia;
        AtualizarMaoEsquerda();
    }

    public void SetWallrun(WallSide lado)
    {
        if (wallrunAtivo == lado) return;

        wallrunAtivo = lado;
        AtualizarMaoDireita();
        AtualizarMaoEsquerda();
    }

    public void ShowHeldEnemy(Sprite spriteInimigo)
    {
        segurandoInimigo = true;
        AtualizarMaoEsquerda();

        if (imagemInimigoSegurado == null) return;

        imagemInimigoSegurado.sprite = spriteInimigo;
        imagemInimigoSegurado.enabled = spriteInimigo != null;
        imagemInimigoSegurado.fillAmount = 0f;
    }

    public void ClearHeldEnemy()
    {
        segurandoInimigo = false;
        AtualizarMaoEsquerda();

        if (imagemInimigoSegurado == null) return;

        imagemInimigoSegurado.enabled = false;
        imagemInimigoSegurado.sprite = null;
        imagemInimigoSegurado.fillAmount = 0f;
    }

    public void UpdateChargePercent(float percent)
    {
        if (imagemInimigoSegurado == null) return;

        imagemInimigoSegurado.fillAmount = Mathf.Clamp01(percent);
    }

    /// <summary>
    /// Instancia e lança uma cápsula de bala na UI, saindo do ponto de ejeção
    /// configurado, com uma curva aleatória pra esquerda/direita.
    /// </summary>
    public void EjectShell(Sprite shellSprite)
    {
        if (prefabCapsula == null || pontoEjecaoCapsula == null) return;

        GameObject obj = Instantiate(prefabCapsula, pontoEjecaoCapsula.parent);

        RectTransform objRect = obj.GetComponent<RectTransform>();
        if (objRect != null)
            objRect.anchoredPosition = pontoEjecaoCapsula.anchoredPosition;

        Image img = obj.GetComponent<Image>();
        if (img != null && shellSprite != null)
            img.sprite = shellSprite;

        BulletShellUI shell = obj.GetComponent<BulletShellUI>();
        shell?.Launch(forcaEjecaoVertical, amplitudeLateral, variacaoForca);
    }

    private void AtualizarMaoDireita()
    {
        if (imagemMaoDireita == null) return;

        if (tocandoAnimacaoMaoDireita && frameAtualAnimacao != null)
        {
            imagemMaoDireita.sprite = frameAtualAnimacao;
        }
        else if (wallrunAtivo == WallSide.Right && !equipArma)
        {
            imagemMaoDireita.sprite = spriteWallrunDireita;
        }
        else
        {
            imagemMaoDireita.sprite = spriteBaseDireita;
        }
    }

    private void AtualizarMaoEsquerda()
    {
        if (imagemMaoEsquerda == null) return;

        if (segurandoInimigo)
        {
            imagemMaoEsquerda.sprite = spriteMaoEsquerdaSegurandoInimigo;
        }
        else if (wallrunAtivo == WallSide.Left)
        {
            imagemMaoEsquerda.sprite = spriteWallrunEsquerda;
        }
        else
        {
            imagemMaoEsquerda.sprite = spriteBaseEsquerda;
        }
    }
}