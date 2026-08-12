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

    [Header("Sprites de Wallrun")]
    [SerializeField] private Sprite spriteWallrunDireita;
    [SerializeField] private Sprite spriteWallrunEsquerda;

    [Header("Inimigo Segurado / Carga de Arremesso")]
    [Tooltip("Imagem usada pra mostrar o sprite do inimigo agarrado. O mesmo campo também é usado como indicador de carga do arremesso, via fillAmount — configure o Image Type dela como \"Filled\" no Inspector se quiser esse preenchimento visual.")]
    [SerializeField] private Image imagemInimigoSegurado;

    private Sprite spriteBaseDireita;
    private Sprite spriteBaseEsquerda;

    private WallSide wallrunAtivo = WallSide.None;

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

    /// <summary>
    /// Mostra o sprite do inimigo que acabou de ser agarrado. Chamado pelo
    /// PlayerGrabController assim que o grab acontece.
    /// </summary>
    public void ShowHeldEnemy(Sprite spriteInimigo)
    {
        if (imagemInimigoSegurado == null) return;

        imagemInimigoSegurado.sprite = spriteInimigo;
        imagemInimigoSegurado.enabled = spriteInimigo != null;
        imagemInimigoSegurado.fillAmount = 0f;
    }

    /// <summary>
    /// Esconde a imagem do inimigo segurado. Chamado pelo PlayerGrabController
    /// quando o inimigo é arremessado, solto, ou deixa de estar agarrado.
    /// </summary>
    public void ClearHeldEnemy()
    {
        if (imagemInimigoSegurado == null) return;

        imagemInimigoSegurado.enabled = false;
        imagemInimigoSegurado.sprite = null;
        imagemInimigoSegurado.fillAmount = 0f;
    }

    /// <summary>
    /// Atualiza o preenchimento (fillAmount) da imagem do inimigo segurado
    /// pra refletir o progresso da carga do arremesso (0 a 1). Requer que o
    /// Image Type de imagemInimigoSegurado esteja configurado como "Filled"
    /// no Inspector pra ter efeito visual.
    /// </summary>
    public void UpdateChargePercent(float percent)
    {
        if (imagemInimigoSegurado == null) return;

        imagemInimigoSegurado.fillAmount = Mathf.Clamp01(percent);
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

        imagemMaoEsquerda.sprite = (wallrunAtivo == WallSide.Left)
            ? spriteWallrunEsquerda
            : spriteBaseEsquerda;
    }
}