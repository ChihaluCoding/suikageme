using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private const float PixelsPerUnit = 100f;
    // UI左右余白の基本オフセット
    private const float UiMargin = 0.5f;
    // UIパネル枠の太さ
    private const float PanelBorder = 0.08f;
    // NEXTアイコンの目標サイズ
    private const float NextIconSize = 0.9f;
    // チェーンアイコン1個のサイズ
    private const float ChainIconSize = 0.32f;
    // チェーンアイコンの隙間
    private const float ChainIconGap = 0.12f;
    private const int ChainIconsPerRow = 4;
    // 各UIパネルのサイズ
    private static readonly Vector2 ScorePanelSize = new Vector2(3.4f, 1.9f);
    private static readonly Vector2 NextPanelSize = new Vector2(2.2f, 1.9f);
    private static readonly Vector2 ChainPanelSize = new Vector2(2.6f, 2.5f);
    // 背景・UIの色設定
    private static readonly Color BgTopColor = new Color(1f, 0.97f, 0.99f);
    private static readonly Color BgBottomColor = new Color(0.97f, 0.86f, 0.92f);
    private static readonly Color PanelFillColor = new Color(1f, 0.99f, 1f, 0.93f);
    private static readonly Color PanelBorderColor = new Color(0.95f, 0.8f, 0.9f, 1f);
    private static readonly Color AccentColor = new Color(0.95f, 0.36f, 0.6f);
    private static readonly Color AccentSoftColor = new Color(0.98f, 0.72f, 0.86f);
    private static readonly Color TextPrimaryColor = new Color(0.25f, 0.15f, 0.2f);
    private static readonly Color GlassColor = new Color(1f, 0.97f, 1f, 0.2f);
    private static readonly Color GlassBorderColor = new Color(0.93f, 0.65f, 0.78f, 0.9f);
    private static readonly Color ShadowColor = new Color(0.08f, 0.02f, 0.05f, 0.23f);

    private const int BackgroundSortingOrder = -30;
    private const int BinFillSortingOrder = -5;
    private const int BinOutlineSortingOrder = 6;
    private const int UiSortingOrder = 20;
    private const int UiTextSortingOrder = 22;

    private static Sprite solidSprite;

    private struct PlayBounds
    {
        public float left;
        public float right;
        public float floor;
        public float ceiling;
    }

    [Header("Playfield")]
    // ★ 箱の外枠を広げたいときはここを編集（左右・上下の座標）
    public float leftWallX = -4.2f;   // 箱の左端X
    public float rightWallX = 4.2f;   // 箱の右端X
    public float floorY = -4.8f;      // 箱の床Y
    public float ceilingY = 6.2f;     // 箱の天井Y
    // ★ スポーン位置やトップラインを上下に動かしたいとき
    public float spawnY = 4.1f;       // スポーン初期Y
    public float topLineY = 4.9f;     // トップラインY
    public float wallThickness = 0.5f; // 壁コライダーの厚み
    [Header("Box Offset")]
    public float boxOffsetX = 0f; // 箱の横オフセット
    public float boxOffsetY = -0.8f; // 箱の縦オフセット（下へ移動）
    // ★ プレイ範囲の内側余白（半径分に加えてさらに詰める/広げる用。広げたいときは値を小さく/マイナスに）
    public float innerPaddingLeft = 0f;
    public float innerPaddingRight = 0f;
    public float innerPaddingBottom = -0.94f;
    public float innerPaddingTop = -2f;
    [Header("Inner Bounds Min Size")]
    public float minWidthRadiusMultiplier = 5f; // 幅の最小値 = 最大半径 * これ
    public float minWidthExtra = 0.1f; // 幅の最小値に足す固定値
    public float minHeightRadiusMultiplier = 2f; // 高さの最小値 = 最大半径 * これ
    public float minHeightExtra = 0.1f; // 高さの最小値に足す固定値
    // 内側余白は固定値に戻す（必要ならまた有効化）
    // [Header("Playfield Padding (relative to bin)")]
    // [Range(0f, 0.4f)] public float innerLeftNorm = 0.03f;
    // [Range(0f, 0.4f)] public float innerRightNorm = 0.03f;
    // [Range(0f, 0.4f)] public float innerBottomNorm = 0.05f;
    // [Range(0f, 0.4f)] public float innerTopNorm = 0.05f;
    [Header("Layout Offsets")]
    // トップラインとスポーン位置の調整量
    public float topLineMargin = 0.2f;
    public float spawnBelowTopLine = 0.5f;

    // 箱の見た目の外枠（スケール/オフセット反映）
    private PlayBounds GetBoxBounds()
    {
        float baseWidth = rightWallX - leftWallX;
        float baseHeight = ceilingY - floorY;
        float centerX = (leftWallX + rightWallX) * 0.5f + boxOffsetX;
        float centerY = (floorY + ceilingY) * 0.5f + boxOffsetY;
        float halfWidth = baseWidth * 0.5f;
        float halfHeight = baseHeight * 0.5f;

        return new PlayBounds
        {
            left = centerX - halfWidth,
            right = centerX + halfWidth,
            floor = centerY - halfHeight,
            ceiling = centerY + halfHeight
        };
    }

    // 箱内の当たり判定範囲（最大半径ぶん内側に寄せる）
    private PlayBounds GetInnerBounds()
    {
        PlayBounds box = GetBoxBounds();
        float left = box.left + maxFruitRadius + innerPaddingLeft;
        float right = box.right - maxFruitRadius - innerPaddingRight;
        float floor = box.floor + maxFruitRadius + innerPaddingBottom;
        float ceiling = box.ceiling - maxFruitRadius - innerPaddingTop;

        // Safety clamp to avoid inverted bounds
        // 最小サイズ保証（内側が小さすぎるときは左右・上下に広げる）
        float minWidth = maxFruitRadius * minWidthRadiusMultiplier + minWidthExtra;
        float minHeight = maxFruitRadius * minHeightRadiusMultiplier + minHeightExtra;
        if (right - left < minWidth)
        {
            float adjust = (minWidth - (right - left)) * 0.5f;
            left -= adjust;
            right += adjust;
        }
        if (ceiling - floor < minHeight)
        {
            float adjust = (minHeight - (ceiling - floor)) * 0.5f;
            floor -= adjust;
            ceiling += adjust;
        }
        return new PlayBounds
        {
            left = left,
            right = right,
            floor = floor,
            ceiling = ceiling
        };
    }

    [Header("Spawning")]
    [Range(1, 8)]
    public int spawnTypeCount = 5;
    public float nextSpawnDelay = 0.6f;

    [Header("Fruit")]
    public List<FruitDefinition> fruitDefinitions = new List<FruitDefinition>();

    [Header("Tuning")]
    public float dropXClampPadding = 0.15f;
    public float mergeImpulse = 1.5f;

    private readonly HashSet<Fruit> activeFruits = new HashSet<Fruit>();
    private Fruit currentFruit;
    private Camera mainCamera;
    private TextMesh scoreText;
    private TextMesh bestScoreText;
    private Transform uiRoot;
    private SpriteRenderer nextFruitRenderer;
    private bool canDrop = true;
    private bool isGameOver;
    private int score;
    private int bestScore;
    private int nextTypeIndex = -1;
    private float maxFruitRadius = 0.5f;
    private Sprite binBackSprite;
    private Sprite binFrontSprite;

    private void Awake()
    {
        EnsureCamera();
        mainCamera = Camera.main;
        EnsureDefinitions();
        CacheMaxRadius();
        RefreshVerticalLayout();
        EnsureUiRoot();
        LoadBoxSprites();
        CreateBackground();
        CreatePlayfield();
        CreateBinVisual();
        CreateScoreText();
        CreateNextPanel();
        CreateChainPanel();
    }

    private void Start()
    {
        nextTypeIndex = GetRandomSpawnIndex();
        SpawnPreview();
    }

    private void Update()
    {
        if (isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Restart();
            }
            return;
        }

        HandleAim();
        HandleDropInput();
        CheckGameOver();
    }

    public void RegisterFruit(Fruit fruit)
    {
        activeFruits.Add(fruit);
    }

    public void UnregisterFruit(Fruit fruit)
    {
        activeFruits.Remove(fruit);
    }

    public void TryMerge(Fruit a, Fruit b)
    {
        if (a == null || b == null)
        {
            return;
        }

        int nextIndex = a.TypeIndex + 1;
        if (nextIndex >= fruitDefinitions.Count)
        {
            return;
        }

        a.MarkMerging();
        b.MarkMerging();

        Vector3 spawnPos = (a.transform.position + b.transform.position) * 0.5f;
        Destroy(a.gameObject);
        Destroy(b.gameObject);

        Fruit merged = CreateFruit(nextIndex, spawnPos, false);
        Rigidbody2D body = merged.GetComponent<Rigidbody2D>();
        body.AddForce(Vector2.up * mergeImpulse, ForceMode2D.Impulse);

        AddScore(fruitDefinitions[nextIndex].score);
    }

    private void HandleAim()
    {
        if (currentFruit == null)
        {
            return;
        }

        PlayBounds bounds = GetInnerBounds();
        float targetX = currentFruit.transform.position.x;
        if (mainCamera != null)
        {
            Vector3 mouse = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            targetX = mouse.x;
        }
        else
        {
            targetX += Input.GetAxisRaw("Horizontal") * Time.deltaTime * 5f;
        }

        float clampLeft = bounds.left + currentFruit.Radius + dropXClampPadding;
        float clampRight = bounds.right - currentFruit.Radius - dropXClampPadding;
        targetX = Mathf.Clamp(targetX, clampLeft, clampRight);

        currentFruit.transform.position = new Vector3(targetX, spawnY, 0f);
    }

    private void HandleDropInput()
    {
        if (!canDrop || currentFruit == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            DropCurrent();
        }
    }

    private void DropCurrent()
    {
        Rigidbody2D body = currentFruit.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 1f;
        body.angularDrag = 0.05f;
        currentFruit.SetPreview(false);

        currentFruit = null;
        canDrop = false;
        StartCoroutine(SpawnAfterDelay());
    }

    private IEnumerator SpawnAfterDelay()
    {
        yield return new WaitForSeconds(nextSpawnDelay);
        canDrop = true;
        SpawnPreview();
    }

    private void SpawnPreview()
    {
        if (nextTypeIndex < 0)
        {
            nextTypeIndex = GetRandomSpawnIndex();
        }

        int typeIndex = nextTypeIndex;
        PlayBounds bounds = GetInnerBounds();
        float clampedSpawnY = Mathf.Clamp(spawnY, bounds.floor + maxFruitRadius + 0.1f, bounds.ceiling - maxFruitRadius - 0.1f);
        currentFruit = CreateFruit(typeIndex, new Vector3(0f, clampedSpawnY, 0f), true);
        Rigidbody2D body = currentFruit.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        nextTypeIndex = GetRandomSpawnIndex();
        UpdateNextPreview();
    }

    private int GetRandomSpawnIndex()
    {
        int maxType = Mathf.Clamp(spawnTypeCount, 1, fruitDefinitions.Count);
        return Random.Range(0, maxType);
    }

    private Fruit CreateFruit(int typeIndex, Vector3 position, bool isPreview)
    {
        FruitDefinition def = fruitDefinitions[typeIndex];
        GameObject go = new GameObject("Fruit_" + def.name);
        go.transform.position = position;
        go.transform.SetParent(transform);

        go.AddComponent<SpriteRenderer>();
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        Fruit fruit = go.AddComponent<Fruit>();

        body.mass = Mathf.Max(0.1f, def.radius * def.radius * 4f);
        body.drag = 0.05f;
        body.angularDrag = 0.05f;
        collider.radius = def.radius;

        fruit.Setup(this, typeIndex, def, isPreview);
        return fruit;
    }

    private void EnsureCamera()
    {
        if (Camera.main != null)
        {
            return;
        }

        GameObject camObject = new GameObject("Main Camera");
        Camera cam = camObject.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.backgroundColor = BgTopColor;
        cam.clearFlags = CameraClearFlags.SolidColor;
        camObject.tag = "MainCamera";
        camObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private void EnsureDefinitions()
    {
        if (fruitDefinitions != null && fruitDefinitions.Count > 0)
        {
            return;
        }

        fruitDefinitions = new List<FruitDefinition>
        {
            new FruitDefinition { name = "Cherry", radius = 0.35f, color = new Color(0.92f, 0.18f, 0.22f), score = 1 },
            new FruitDefinition { name = "Strawberry", radius = 0.45f, color = new Color(0.95f, 0.35f, 0.45f), score = 3 },
            new FruitDefinition { name = "Grape", radius = 0.55f, color = new Color(0.62f, 0.37f, 0.86f), score = 6 },
            new FruitDefinition { name = "Orange", radius = 0.7f, color = new Color(0.98f, 0.62f, 0.2f), score = 10 },
            new FruitDefinition { name = "Apple", radius = 0.85f, color = new Color(0.74f, 0.9f, 0.34f), score = 15 },
            new FruitDefinition { name = "Pear", radius = 1.0f, color = new Color(0.55f, 0.86f, 0.45f), score = 21 },
            new FruitDefinition { name = "Peach", radius = 1.2f, color = new Color(0.98f, 0.72f, 0.52f), score = 28 },
            new FruitDefinition { name = "Watermelon", radius = 1.45f, color = new Color(0.2f, 0.7f, 0.35f), score = 36 }
        };

        Sprite bigSprite = LoadSpriteResource("test");
        if (bigSprite != null)
        {
            fruitDefinitions[fruitDefinitions.Count - 1].sprite = bigSprite;
        }
    }

    private void CacheMaxRadius()
    {
        maxFruitRadius = 0.5f;
        if (fruitDefinitions == null)
        {
            return;
        }

        foreach (FruitDefinition def in fruitDefinitions)
        {
            if (def != null)
            {
                maxFruitRadius = Mathf.Max(maxFruitRadius, def.radius);
            }
        }
    }

    private void RefreshVerticalLayout()
    {
        PlayBounds bounds = GetInnerBounds();

        float targetTopLine = bounds.ceiling - Mathf.Max(topLineMargin, maxFruitRadius * 0.25f);
        float minTopLine = bounds.floor + maxFruitRadius * 2f + 0.1f;
        topLineY = Mathf.Max(minTopLine, targetTopLine);

        float minSpawn = bounds.floor + maxFruitRadius + 0.1f;
        float maxSpawn = topLineY - maxFruitRadius - 0.05f;
        float desiredSpawn = topLineY - spawnBelowTopLine;
        // Keep spawn between min and max
        spawnY = Mathf.Clamp(desiredSpawn, minSpawn, maxSpawn);
    }

    private Sprite LoadSpriteResource(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName))
        {
            return null;
        }

        Sprite sprite = Resources.Load<Sprite>(resourceName);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourceName);
        if (texture == null)
        {
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
    }

    private void LoadBoxSprites()
    {
        binBackSprite = LoadSpriteFlexible("back_box");
        binFrontSprite = LoadSpriteFlexible("front_box");
    }

    private Sprite LoadSpriteFlexible(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName))
        {
            return null;
        }

        Sprite sprite = Resources.Load<Sprite>(resourceName);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourceName);
        if (texture == null)
        {
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
    }

    private void EnsureUiRoot()
    {
        if (uiRoot != null)
        {
            return;
        }

        GameObject root = new GameObject("UIRoot");
        root.transform.SetParent(transform);
        uiRoot = root.transform;
    }

    private Vector2 GetCameraBounds()
    {
        if (mainCamera == null)
        {
            return Vector2.zero;
        }

        float height = mainCamera.orthographicSize;
        float width = height * mainCamera.aspect;
        return new Vector2(width, height);
    }

    private void CreateBackground()
    {
        if (mainCamera == null)
        {
            return;
        }

        Vector2 bounds = GetCameraBounds();
        float width = bounds.x * 2f;
        float height = bounds.y * 2f;

        GameObject background = new GameObject("Background");
        background.transform.SetParent(transform);
        SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateVerticalGradientSprite(BgTopColor, BgBottomColor);
        renderer.sortingOrder = BackgroundSortingOrder;
        background.transform.position = new Vector3(0f, 0f, 5f);
        Vector2 spriteSize = renderer.sprite.bounds.size;
        background.transform.localScale = new Vector3(width / spriteSize.x, height / spriteSize.y, 1f);

        AddSoftCircle(new Vector3(-width * 0.25f, bounds.y * 0.25f, 4.8f), 2.4f, new Color(1f, 0.88f, 0.95f, 0.45f));
        AddSoftCircle(new Vector3(width * 0.3f, bounds.y * 0.1f, 4.8f), 2.1f, new Color(0.97f, 0.78f, 0.88f, 0.35f));
        AddSoftCircle(new Vector3(0f, bounds.y * 0.4f, 4.8f), 1.8f, new Color(1f, 1f, 1f, 0.28f));

        GameObject vignette = new GameObject("Vignette");
        vignette.transform.SetParent(transform);
        SpriteRenderer vignetteRenderer = vignette.AddComponent<SpriteRenderer>();
        vignetteRenderer.sprite = GetSolidSprite();
        vignetteRenderer.color = new Color(0.1f, 0.05f, 0.08f, 0.08f);
        vignetteRenderer.sortingOrder = BackgroundSortingOrder + 1;
        vignette.transform.position = new Vector3(0f, 0f, 4.7f);
        vignette.transform.localScale = new Vector3(width, height, 1f);
    }

    private void CreateBinVisual()
    {
        PlayBounds bounds = GetBoxBounds();
        float visualWidth = bounds.right - bounds.left;
        float visualHeight = bounds.ceiling - bounds.floor;
        float centerY = (bounds.ceiling + bounds.floor) * 0.5f;

        bool usedSprites = false;

        if (binBackSprite != null)
        {
            GameObject back = new GameObject("BinBackSprite");
            back.transform.SetParent(transform);
            back.transform.position = new Vector3((bounds.left + bounds.right) * 0.5f, centerY, 0f);
            SpriteRenderer backRenderer = back.AddComponent<SpriteRenderer>();
            backRenderer.sprite = binBackSprite;
            backRenderer.sortingOrder = BinFillSortingOrder;
            Vector2 size = backRenderer.sprite.bounds.size;
            back.transform.localScale = new Vector3(visualWidth / size.x, visualHeight / size.y, 1f);
            usedSprites = true;
        }

        if (binFrontSprite != null)
        {
            GameObject front = new GameObject("BinFrontSprite");
            front.transform.SetParent(transform);
            front.transform.position = new Vector3((bounds.left + bounds.right) * 0.5f, centerY, 0f);
            SpriteRenderer frontRenderer = front.AddComponent<SpriteRenderer>();
            frontRenderer.sprite = binFrontSprite;
            frontRenderer.sortingOrder = BinOutlineSortingOrder + 3;
            Vector2 size = frontRenderer.sprite.bounds.size;
            front.transform.localScale = new Vector3(visualWidth / size.x, visualHeight / size.y, 1f);
            usedSprites = true;
        }

        if (!usedSprites)
        {
            Transform panel = CreatePanel("BinPanel", transform, new Vector3((bounds.left + bounds.right) * 0.5f, centerY, 0f), new Vector2(visualWidth, visualHeight),
                GlassColor, GlassBorderColor, BinFillSortingOrder, BinOutlineSortingOrder, 0.12f, true);

            GameObject glassHighlight = new GameObject("BinHighlight");
            glassHighlight.transform.SetParent(panel);
            glassHighlight.transform.localPosition = new Vector3(-visualWidth * 0.35f, 0f, 0f);
            SpriteRenderer highlightRenderer = glassHighlight.AddComponent<SpriteRenderer>();
            highlightRenderer.sprite = GetSolidSprite();
            highlightRenderer.color = new Color(1f, 1f, 1f, 0.18f);
            highlightRenderer.sortingOrder = BinOutlineSortingOrder + 1;
            glassHighlight.transform.localScale = new Vector3(0.12f, visualHeight * 0.85f, 1f);

            GameObject topAccent = new GameObject("BinAccent");
            topAccent.transform.SetParent(panel);
            topAccent.transform.localPosition = new Vector3(0f, visualHeight * 0.5f + 0.05f, 0f);
            SpriteRenderer accentRenderer = topAccent.AddComponent<SpriteRenderer>();
            accentRenderer.sprite = GetSolidSprite();
            accentRenderer.color = AccentSoftColor;
            accentRenderer.sortingOrder = BinOutlineSortingOrder + 2;
            topAccent.transform.localScale = new Vector3(visualWidth + 0.2f, 0.18f, 1f);
        }
    }

    // 箱の壁・床・天井のコライダーを生成
    private void CreatePlayfield()
    {
        PlayBounds bounds = GetInnerBounds();
        float fullHeight = bounds.ceiling - bounds.floor + wallThickness * 2f;
        float fullWidth = bounds.right - bounds.left + wallThickness * 2f;

        // 左右・床・天井のコライダー位置は、leftWallX/rightWallX/floorY/ceilingY を基準に、
        // GetInnerBounds() で最大半径ぶん内側へオフセットしたものを使用
        // 左壁: bounds.left を中心に厚みぶんオフセット
        CreateWall("LeftWall", new Vector2(bounds.left - wallThickness * 0.5f, 0f),
            new Vector2(wallThickness, fullHeight));
        // 右壁: bounds.right を中心に厚みぶんオフセット
        CreateWall("RightWall", new Vector2(bounds.right + wallThickness * 0.5f, 0f),
            new Vector2(wallThickness, fullHeight));
        // 床: bounds.floor を中心に厚みぶんオフセット
        CreateWall("Floor", new Vector2(0f, bounds.floor - wallThickness * 0.5f),
            new Vector2(fullWidth, wallThickness));
        // 天井: bounds.ceiling を中心に厚みぶんオフセット
        CreateWall("Ceiling", new Vector2(0f, bounds.ceiling + wallThickness * 0.5f),
            new Vector2(fullWidth, wallThickness));
    }

    private void CreateWall(string name, Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.position = position;
        wall.transform.SetParent(transform);
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    private void CreateNextPanel()
    {
        if (mainCamera == null)
        {
            return;
        }

        Vector2 bounds = GetCameraBounds();
        float right = bounds.x;
        float top = bounds.y;
        Vector3 panelPos = new Vector3(right - UiMargin - NextPanelSize.x * 0.5f, top - UiMargin - NextPanelSize.y * 0.5f, 0f);

        Transform panel = CreatePanel("NextPanel", uiRoot, panelPos, NextPanelSize,
            PanelFillColor, PanelBorderColor, UiSortingOrder, UiSortingOrder + 1, PanelBorder, true);

        float leftPadding = -NextPanelSize.x * 0.5f + 0.2f;
        float topLine = NextPanelSize.y * 0.5f - 0.35f;

        TextMesh label = CreateUIText("NextLabel", panel, new Vector3(leftPadding, topLine, 0f),
            56, 0.03f, TextAnchor.MiddleLeft, AccentColor, UiTextSortingOrder, true);
        SetTextWithShadow(label, "NEXT");

        GameObject icon = new GameObject("NextFruit");
        icon.transform.SetParent(panel);
        icon.transform.localPosition = new Vector3(0f, -0.1f, 0f);
        nextFruitRenderer = icon.AddComponent<SpriteRenderer>();
        nextFruitRenderer.sortingOrder = UiTextSortingOrder + 1;
    }

    private void CreateChainPanel()
    {
        if (mainCamera == null || fruitDefinitions.Count == 0)
        {
            return;
        }

        Vector2 bounds = GetCameraBounds();
        float right = bounds.x;
        Vector3 panelPos = new Vector3(right - UiMargin - ChainPanelSize.x * 0.5f, 0.2f, 0f);

        Transform panel = CreatePanel("ChainPanel", uiRoot, panelPos, ChainPanelSize,
            PanelFillColor, PanelBorderColor, UiSortingOrder, UiSortingOrder + 1, PanelBorder, true);

        float leftPadding = -ChainPanelSize.x * 0.5f + 0.2f;
        float topLine = ChainPanelSize.y * 0.5f - 0.35f;

        TextMesh label = CreateUIText("ChainLabel", panel, new Vector3(leftPadding, topLine, 0f),
            52, 0.028f, TextAnchor.MiddleLeft, AccentColor, UiTextSortingOrder, true);
        SetTextWithShadow(label, "CHAIN");

        int count = Mathf.Min(fruitDefinitions.Count, 8);
        float startX = -ChainPanelSize.x * 0.5f + 0.25f + ChainIconSize * 0.5f;
        float startY = ChainPanelSize.y * 0.5f - 0.8f - ChainIconSize * 0.5f;
        float stepX = ChainIconSize + ChainIconGap;
        float stepY = ChainIconSize + ChainIconGap;

        for (int i = 0; i < count; i++)
        {
            int row = i / ChainIconsPerRow;
            int col = i % ChainIconsPerRow;

            GameObject icon = new GameObject("ChainIcon_" + i);
            icon.transform.SetParent(panel);
            icon.transform.localPosition = new Vector3(startX + col * stepX, startY - row * stepY, 0f);

            SpriteRenderer iconRenderer = icon.AddComponent<SpriteRenderer>();
            iconRenderer.sprite = GetSolidSprite();
            iconRenderer.color = fruitDefinitions[i].color;
            iconRenderer.sortingOrder = UiTextSortingOrder + 1;
            icon.transform.localScale = new Vector3(ChainIconSize, ChainIconSize, 1f);
        }
    }

    private void UpdateNextPreview()
    {
        if (nextFruitRenderer == null || nextTypeIndex < 0 || nextTypeIndex >= fruitDefinitions.Count)
        {
            return;
        }

        FruitDefinition def = fruitDefinitions[nextTypeIndex];
        Sprite sprite = def.sprite != null ? def.sprite : GetSolidSprite();
        nextFruitRenderer.sprite = sprite;
        nextFruitRenderer.color = def.sprite != null ? Color.white : def.color;

        float spriteSize = Mathf.Max(0.0001f, Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
        float scale = NextIconSize / spriteSize;
        nextFruitRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private Transform CreatePanel(string name, Transform parent, Vector3 position, Vector2 size, Color fillColor, Color borderColor,
        int fillSortingOrder, int borderSortingOrder, float borderThickness, bool addShadow = false)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent);
        panel.transform.position = position;

        if (addShadow)
        {
            GameObject shadow = new GameObject("Shadow");
            shadow.transform.SetParent(panel.transform);
            shadow.transform.localPosition = new Vector3(0.12f, -0.12f, 0f);
            SpriteRenderer shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = GetSolidSprite();
            shadowRenderer.color = ShadowColor;
            shadowRenderer.sortingOrder = fillSortingOrder - 1;
            shadow.transform.localScale = new Vector3(size.x + 0.15f, size.y + 0.15f, 1f);
        }

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(panel.transform);
        fill.transform.localPosition = Vector3.zero;
        SpriteRenderer fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = GetSolidSprite();
        fillRenderer.color = fillColor;
        fillRenderer.sortingOrder = fillSortingOrder;
        fill.transform.localScale = new Vector3(size.x, size.y, 1f);

        CreatePanelEdge(panel.transform, "BorderTop", new Vector2(size.x, borderThickness),
            new Vector3(0f, size.y * 0.5f - borderThickness * 0.5f, 0f), borderColor, borderSortingOrder);
        CreatePanelEdge(panel.transform, "BorderBottom", new Vector2(size.x, borderThickness),
            new Vector3(0f, -size.y * 0.5f + borderThickness * 0.5f, 0f), borderColor, borderSortingOrder);
        CreatePanelEdge(panel.transform, "BorderLeft", new Vector2(borderThickness, size.y),
            new Vector3(-size.x * 0.5f + borderThickness * 0.5f, 0f, 0f), borderColor, borderSortingOrder);
        CreatePanelEdge(panel.transform, "BorderRight", new Vector2(borderThickness, size.y),
            new Vector3(size.x * 0.5f - borderThickness * 0.5f, 0f, 0f), borderColor, borderSortingOrder);

        return panel.transform;
    }

    private void CreatePanelEdge(Transform parent, string name, Vector2 size, Vector3 localPosition, Color color, int sortingOrder)
    {
        GameObject edge = new GameObject(name);
        edge.transform.SetParent(parent);
        edge.transform.localPosition = localPosition;
        SpriteRenderer renderer = edge.AddComponent<SpriteRenderer>();
        renderer.sprite = GetSolidSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        edge.transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private TextMesh CreateUIText(string name, Transform parent, Vector3 localPosition, int fontSize, float characterSize,
        TextAnchor anchor, Color color, int sortingOrder, bool addShadow)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent);
        textObject.transform.localPosition = localPosition;

        TextMesh text = textObject.AddComponent<TextMesh>();
        text.anchor = anchor;
        text.fontSize = fontSize;
        text.characterSize = characterSize;
        text.color = color;

        MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
        renderer.sortingOrder = sortingOrder;

        if (addShadow)
        {
            GameObject shadow = new GameObject(name + "_Shadow");
            shadow.transform.SetParent(parent);
            shadow.transform.localPosition = localPosition + new Vector3(0.04f, -0.04f, 0f);

            TextMesh shadowText = shadow.AddComponent<TextMesh>();
            shadowText.anchor = anchor;
            shadowText.fontSize = fontSize;
            shadowText.characterSize = characterSize;
            shadowText.text = text.text;
            shadowText.color = ShadowColor;
            MeshRenderer shadowRenderer = shadow.GetComponent<MeshRenderer>();
            shadowRenderer.sortingOrder = sortingOrder - 1;
        }

        return text;
    }

    private void SetTextWithShadow(TextMesh text, string value)
    {
        if (text == null)
        {
            return;
        }

        text.text = value;
        Transform parent = text.transform.parent;
        if (parent == null)
        {
            return;
        }

        Transform shadow = parent.Find(text.gameObject.name + "_Shadow");
        if (shadow == null)
        {
            return;
        }

        TextMesh shadowText = shadow.GetComponent<TextMesh>();
        if (shadowText != null)
        {
            shadowText.text = value;
        }
    }

    private static Sprite GetSolidSprite()
    {
        if (solidSprite != null)
        {
            return solidSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        solidSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return solidSprite;
    }

    private static Sprite CreateVerticalGradientSprite(Color topColor, Color bottomColor)
    {
        int width = 32;
        int height = 256;
        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
        {
            float t = y / (float)(height - 1);
            Color row = Color.Lerp(bottomColor, topColor, t);
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, row);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private static Sprite CreateRadialSprite(int resolution = 128)
    {
        int size = Mathf.Max(32, resolution);
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float r = size * 0.5f;
        float rSq = r * r;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - r + 0.5f;
                float dy = y - r + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float t = Mathf.Clamp01(1f - dist / r);
                float alpha = t * t;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private void AddSoftCircle(Vector3 position, float radius, Color color)
    {
        GameObject circle = new GameObject("Glow");
        circle.transform.SetParent(transform);
        circle.transform.position = position;
        SpriteRenderer renderer = circle.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateRadialSprite();
        renderer.color = color;
        renderer.sortingOrder = BackgroundSortingOrder + 2;
        float diameter = radius * 2f;
        renderer.transform.localScale = new Vector3(diameter, diameter, 1f);
    }

    private void CreateScoreText()
    {
        if (mainCamera == null)
        {
            return;
        }

        Vector2 bounds = GetCameraBounds();
        float left = -bounds.x;
        float top = bounds.y;
        Vector3 panelPos = new Vector3(left + UiMargin + ScorePanelSize.x * 0.5f, top - UiMargin - ScorePanelSize.y * 0.5f, 0f);

        Transform panel = CreatePanel("ScorePanel", uiRoot, panelPos, ScorePanelSize,
            PanelFillColor, PanelBorderColor, UiSortingOrder, UiSortingOrder + 1, PanelBorder, true);

        float leftPadding = -ScorePanelSize.x * 0.5f + 0.2f;
        float topLine = ScorePanelSize.y * 0.5f - 0.35f;
        float bottomLine = -ScorePanelSize.y * 0.5f + 0.35f;

        TextMesh label = CreateUIText("ScoreLabel", panel, new Vector3(leftPadding, topLine, 0f),
            60, 0.03f, TextAnchor.MiddleLeft, AccentColor, UiTextSortingOrder, true);
        SetTextWithShadow(label, "SCORE");

        scoreText = CreateUIText("ScoreValue", panel, new Vector3(leftPadding, 0.05f, 0f),
            120, 0.06f, TextAnchor.MiddleLeft, TextPrimaryColor, UiTextSortingOrder, true);

        bestScoreText = CreateUIText("BestScore", panel, new Vector3(leftPadding, bottomLine, 0f),
            46, 0.03f, TextAnchor.MiddleLeft, AccentSoftColor, UiTextSortingOrder, true);

        UpdateScoreText();
    }

    private void AddScore(int amount)
    {
        score += amount;
        if (score > bestScore)
        {
            bestScore = score;
        }
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            SetTextWithShadow(scoreText, score.ToString());
        }

        if (bestScoreText != null)
        {
            SetTextWithShadow(bestScoreText, "BEST " + bestScore);
        }
    }

    private void CheckGameOver()
    {
        PlayBounds bounds = GetInnerBounds();
        float lineY = Mathf.Min(topLineY, bounds.ceiling - 0.05f);

        foreach (Fruit fruit in activeFruits)
        {
            if (fruit == null)
            {
                continue;
            }

            Rigidbody2D body = fruit.GetComponent<Rigidbody2D>();
            if (body != null && body.bodyType == RigidbodyType2D.Dynamic && fruit.transform.position.y > lineY)
            {
                TriggerGameOver();
                return;
            }
        }
    }

    private void TriggerGameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f;

        GameObject overTextObj = new GameObject("GameOverText");
        overTextObj.transform.SetParent(transform);
        overTextObj.transform.position = new Vector3(0f, 1f, 0f);

        TextMesh overText = overTextObj.AddComponent<TextMesh>();
        overText.anchor = TextAnchor.MiddleCenter;
        overText.fontSize = 96;
        overText.characterSize = 0.1f;
        overText.color = new Color(0.9f, 0.15f, 0.15f);
        overText.text = "GAME OVER\nPress R to Restart";
        MeshRenderer renderer = overTextObj.GetComponent<MeshRenderer>();
        renderer.sortingOrder = UiTextSortingOrder + 5;
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        isGameOver = false;
        score = 0;
        UpdateScoreText();

        foreach (Fruit fruit in new List<Fruit>(activeFruits))
        {
            if (fruit != null)
            {
                Destroy(fruit.gameObject);
            }
        }

        currentFruit = null;
        canDrop = true;
        RefreshVerticalLayout();
        nextTypeIndex = GetRandomSpawnIndex();
        SpawnPreview();
    }
}
