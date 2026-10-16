using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Level progress on the screen wipe: one block per level, lit up as the levels go by. When a level starts, the
// wipe holds still while that level's block brightens from grey to white and settles to an off-white shine, then
// slides away. On every 9th level the full row of 8 instead slides together and is masked into a single shape (a square).
// The level after that opens on that shape, fades it away, and starts a fresh, unlit row.
// Only runs in scenes named "Level_<number>"; anywhere else the wipe is left exactly as authored.
[RequireComponent(typeof(Animator))]
public class LevelProgressView : MonoBehaviour
{
    [Tooltip("One block per level, left to right.")]
    public Image[] blocks;
    public Color unlitColor = new Color(0.226f, 0.226f, 0.226f, 1f);
    [Tooltip("Brightest point of the shine, just before settling.")]
    public Color flashColor = Color.white;
    [Tooltip("Color a lit block settles to.")]
    public Color litColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    [Header("Light up")]
    [Tooltip("Seconds the new block takes to brighten from unlit to the flash color.")]
    public float fadeTime = 0.6f;
    [Tooltip("Seconds it takes to settle from the flash color to the lit color.")]
    public float shineTime = 0.4f;

    [Header("Combine (every 9th level)")]
    [Tooltip("Shape the full row merges into.")]
    public Sprite combinedShape;
    [Tooltip("Size of the finished shape.")]
    public float combinedSize = 300f;
    [Tooltip("Seconds the blocks take to slide together into the shape.")]
    public float combineTime = 1.5f;
    [Tooltip("Seconds the shape takes to fade away at the start of the next round.")]
    public float fadeAwayTime = 0.8f;
    [Tooltip("Seconds the fresh, unlit row takes to fade in after the shape is gone.")]
    public float rowAppearTime = 0.4f;

    [Tooltip("Seconds to hold after the animation, before the wipe slides away.")]
    public float holdAfter = 0.25f;

    private static readonly Regex LevelName = new Regex(@"^Level_(\d+)$");

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        var match = LevelName.Match(SceneManager.GetActiveScene().name);
        if (!match.Success || blocks.Length == 0)
        {
            enabled = false;
            return;
        }
        // Freeze the wipe on its first frame, covering the screen, until the animation below is done
        _animator.speed = 0f;
    }

    private void Start()
    {
        var level = int.Parse(LevelName.Match(SceneManager.GetActiveScene().name).Groups[1].Value);
        // Levels come in rounds of blocks.Length light-ups followed by one combine
        var round = blocks.Length + 1;
        var slot = (level - 1) % round;

        for (var i = 0; i < blocks.Length; i++)
        {
            blocks[i].color = i < slot ? litColor : unlitColor;
        }

        if (slot == blocks.Length) StartCoroutine(Combine());
        else if (slot == 0 && level > round) StartCoroutine(NewRound());
        else StartCoroutine(LightUp(blocks[slot]));
    }

    /**
    * Input: block
    * Purpose: Brighten this level's block from unlit to the flash color, settle it to lit, then let the wipe go
    */
    private IEnumerator LightUp(Image block)
    {
        yield return Shine(new[] { block }, unlitColor);
        yield return Release();
    }

    //Purpose: Slide the lit row together inside a shrinking mask until it reads as one shape, then shine
    private IEnumerator Combine()
    {
        var mask = CreateShape("Combined Shape");
        mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        // Start far too big to clip anything
        var startMaskSize = combinedSize * 8f;
        mask.sizeDelta = Vector2.one * startMaskSize;

        // Each block ends as one vertical slice of a solid square the size of the shape
        var sliceWidth = combinedSize / blocks.Length;
        var fromPosition = new Vector2[blocks.Length];
        var fromSize = new Vector2[blocks.Length];
        for (var i = 0; i < blocks.Length; i++)
        {
            var rect = blocks[i].rectTransform;
            rect.SetParent(mask, true);
            var size = rect.rect.size;
            var position = (Vector2)mask.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.localPosition = position;
            fromPosition[i] = position;
            fromSize[i] = size;
        }

        for (var time = 0f; time < combineTime; time += Time.deltaTime)
        {
            var t = Mathf.SmoothStep(0f, 1f, time / combineTime);
            for (var i = 0; i < blocks.Length; i++)
            {
                var to = new Vector2(-combinedSize / 2f + sliceWidth * (i + 0.5f), 0f);
                blocks[i].rectTransform.localPosition = Vector2.Lerp(fromPosition[i], to, t);
                // A hair wider than the slice so no seam shows between neighbours
                blocks[i].rectTransform.sizeDelta = Vector2.Lerp(fromSize[i], new Vector2(sliceWidth + 1f, combinedSize), t);
            }
            mask.sizeDelta = Vector2.one * Mathf.Lerp(startMaskSize, combinedSize, t);
            yield return null;
        }
        mask.sizeDelta = Vector2.one * combinedSize;

        yield return Shine(blocks, litColor);
        yield return Release();
    }

    //Purpose: Start of a new round: the last round's shape fades away, then the empty row fades in and block 1 lights
    private IEnumerator NewRound()
    {
        var shape = CreateShape("Previous Round").GetComponent<Image>();
        shape.color = litColor;
        SetAlpha(blocks, 0f);

        for (var time = 0f; time < fadeAwayTime; time += Time.deltaTime)
        {
            var color = litColor;
            color.a = 1f - Mathf.SmoothStep(0f, 1f, time / fadeAwayTime);
            shape.color = color;
            yield return null;
        }
        Destroy(shape.gameObject);

        for (var time = 0f; time < rowAppearTime; time += Time.deltaTime)
        {
            SetAlpha(blocks, Mathf.SmoothStep(0f, 1f, time / rowAppearTime));
            yield return null;
        }
        SetAlpha(blocks, 1f);

        yield return LightUp(blocks[0]);
    }

    /**
    * Input: images, from
    * Purpose: Solid shine: brighten from `from` up to the flash color, then settle to the lit color
    */
    private IEnumerator Shine(Image[] images, Color from)
    {
        for (var time = 0f; time < fadeTime; time += Time.deltaTime)
        {
            SetColor(images, Color.Lerp(from, flashColor, Mathf.SmoothStep(0f, 1f, time / fadeTime)));
            yield return null;
        }
        for (var time = 0f; time < shineTime; time += Time.deltaTime)
        {
            SetColor(images, Color.Lerp(flashColor, litColor, Mathf.SmoothStep(0f, 1f, time / shineTime)));
            yield return null;
        }
        SetColor(images, litColor);
    }

    //Purpose: Hold for a moment, then let the wipe slide off the screen
    private IEnumerator Release()
    {
        yield return new WaitForSeconds(holdAfter);
        _animator.speed = 1f;
    }

    /**
    * Input: shapeName
    * Output: a combinedSize shape image centered on the row, just behind the first block
    */
    private RectTransform CreateShape(string shapeName)
    {
        var center = Vector3.zero;
        foreach (var block in blocks) center += block.rectTransform.localPosition;
        center /= blocks.Length;

        var shape = new GameObject(shapeName, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        shape.SetParent(blocks[0].rectTransform.parent, false);
        shape.SetSiblingIndex(blocks[0].rectTransform.GetSiblingIndex());
        shape.anchorMin = shape.anchorMax = shape.pivot = new Vector2(0.5f, 0.5f);
        shape.localPosition = center;
        shape.sizeDelta = Vector2.one * combinedSize;

        var image = shape.GetComponent<Image>();
        image.sprite = combinedShape;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return shape;
    }

    private static void SetColor(Image[] images, Color color)
    {
        foreach (var image in images) image.color = color;
    }

    private static void SetAlpha(Image[] images, float alpha)
    {
        foreach (var image in images)
        {
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}
