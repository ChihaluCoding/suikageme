using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Fruit : MonoBehaviour
{
    private const float PixelsPerUnit = 100f;
    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

    private FruitDefinition definition;
    private GameManager manager;
    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private Color baseColor;
    private bool hasEnteredPlayfield;

    public int TypeIndex { get; private set; }
    public bool IsMerging { get; private set; }
    public float Radius => definition != null ? definition.radius : 0.5f;
    public bool HasEnteredPlayfield => hasEnteredPlayfield;

    public void Setup(GameManager owner, int typeIndex, FruitDefinition def, bool isPreview)
    {
        manager = owner;
        TypeIndex = typeIndex;
        definition = def;
        hasEnteredPlayfield = false;

        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();

        float scale = 1f;
        if (def.sprite != null)
        {
            spriteRenderer.sprite = def.sprite;
            baseColor = Color.white;
            scale = GetSpriteScale(def.sprite, def.radius);
        }
        else
        {
            spriteRenderer.sprite = GetCircleSprite(def.radius, def.color);
            baseColor = def.color;
        }

        transform.localScale = new Vector3(scale, scale, 1f);
        circleCollider.radius = def.radius / scale;
        spriteRenderer.color = isPreview
            ? new Color(baseColor.r, baseColor.g, baseColor.b, 0.75f)
            : baseColor;

        manager.RegisterFruit(this);
    }

    public void MarkEnteredPlayfield()
    {
        hasEnteredPlayfield = true;
    }

    public void SetPreview(bool preview)
    {
        if (spriteRenderer == null || definition == null)
        {
            return;
        }

        spriteRenderer.color = preview
            ? new Color(baseColor.r, baseColor.g, baseColor.b, 0.75f)
            : baseColor;
    }

    public void MarkMerging()
    {
        IsMerging = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsMerging || manager == null)
        {
            return;
        }

        Fruit other = collision.collider.GetComponent<Fruit>();
        if (other == null || other.IsMerging || other.TypeIndex != TypeIndex)
        {
            return;
        }

        manager.TryMerge(this, other);
    }

    private void OnDestroy()
    {
        if (manager != null)
        {
            manager.UnregisterFruit(this);
        }
    }

    private static Sprite GetCircleSprite(float radius, Color color)
    {
        string key = radius.ToString("F3") + ":" + ColorUtility.ToHtmlStringRGBA(color);
        if (SpriteCache.TryGetValue(key, out Sprite cached))
        {
            return cached;
        }

        int diameter = Mathf.Max(8, Mathf.CeilToInt(radius * 2f * PixelsPerUnit));
        Texture2D texture = new Texture2D(diameter, diameter, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float r = diameter / 2f;
        float rSq = r * r;
        Color clear = new Color(color.r, color.g, color.b, 0f);

        for (int y = 0; y < diameter; y++)
        {
            for (int x = 0; x < diameter; x++)
            {
                float dx = x + 0.5f - r;
                float dy = y + 0.5f - r;
                bool inside = dx * dx + dy * dy <= rSq;
                texture.SetPixel(x, y, inside ? color : clear);
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, diameter, diameter), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        SpriteCache[key] = sprite;
        return sprite;
    }

    private static float GetSpriteScale(Sprite sprite, float radius)
    {
        if (sprite == null)
        {
            return 1f;
        }

        float targetDiameter = radius * 2f;
        float spriteDiameter = Mathf.Max(0.0001f, sprite.bounds.size.x);
        return targetDiameter / spriteDiameter;
    }
}
