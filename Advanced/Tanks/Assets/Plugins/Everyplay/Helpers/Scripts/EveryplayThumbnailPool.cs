using UnityEngine;
using System.Collections;

public class EveryplayThumbnailPool : MonoBehaviour
{
    public int thumbnailCount = 4;
    public int thumbnailWidth = 128;
    public bool pixelPerfect = false;
    public bool takeRandomShots = true;
    public TextureFormat textureFormat = TextureFormat.RGBA32;
    public bool dontDestroyOnLoad = true;
    public bool allowOneInstanceOnly = true;

    public Texture2D[] thumbnailTextures { get; private set; }

    public int availableThumbnailCount { get; private set; }

    public float aspectRatio { get; private set; }

    public Vector2 thumbnailScale { get; private set; }

    public static EveryplayThumbnailPool instance;

    private bool npotSupported = false;
    private bool initialized = false;
    private int currentThumbnailTextureIndex;
    private float nextRandomShotTime;
    private int thumbnailHeight = 0;

    void Awake()
    {
        if (allowOneInstanceOnly && instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (allowOneInstanceOnly)
            {
                instance = this;
            }

            Everyplay.ReadyForRecording += OnReadyForRecording;
        }
    }

    void Start()
    {
        if (enabled)
        {
            Initialize();
        }
    }

    private void OnReadyForRecording(bool ready)
    {
        if (ready)
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        if (!initialized && Everyplay.IsRecordingSupported())
        {
            // Limit width between 32 and 2048
            thumbnailWidth = Mathf.Clamp(thumbnailWidth, 32, 2048);

            // Thumbnails are always stored landscape in memory
            aspectRatio = (float) Mathf.Min(Screen.width, Screen.height) / (float) Mathf.Max(Screen.width, Screen.height);

            // Calculate height based on aspect ratio
            thumbnailHeight = (int) (thumbnailWidth * aspectRatio);

            // Check for npot support, always use pot textures for older Unitys versions and if npot support is not available
            npotSupported = false;

            #if !(UNITY_3_5  || UNITY_4_0 || UNITY_4_0_1)
            npotSupported = (SystemInfo.npotSupport != NPOTSupport.None);
            #endif
            int thumbnailPotWidth = Mathf.NextPowerOfTwo(thumbnailWidth);
            int thumbnailPotHeight = Mathf.NextPowerOfTwo(thumbnailHeight);

            // Create empty textures for requested amount of thumbnails
            thumbnailTextures = new Texture2D[thumbnailCount];

            for (int i = 0; i < thumbnailCount; i++)
            {
                thumbnailTextures[i] = new Texture2D(npotSupported ? thumbnailWidth : thumbnailPotWidth, npotSupported ? thumbnailHeight : thumbnailPotHeight, textureFormat, false);
                // Always use clamp to assure texture completeness when npot support is restricted
                thumbnailTextures[i].wrapMode = TextureWrapMode.Clamp;
            }

            // Set thumbnail render target to the first texture
            currentThumbnailTextureIndex = 0;

            Everyplay.SetThumbnailTargetTexture(thumbnailTextures[currentThumbnailTextureIndex]);
            SetThumbnailTargetSize();

            // Add thumbnail take listener
            Everyplay.ThumbnailTextureReady += OnThumbnailReady;
            Everyplay.RecordingStarted += OnRecordingStarted;

            initialized = true;
        }
    }

    private void OnRecordingStarted()
    {
        availableThumbnailCount = 0;
        currentThumbnailTextureIndex = 0;

        Everyplay.SetThumbnailTargetTexture(thumbnailTextures[currentThumbnailTextureIndex]);
        SetThumbnailTargetSize();

        if (takeRandomShots)
        {
            Everyplay.TakeThumbnail();
            nextRandomShotTime = Time.time + Random.Range(3.0f, 15.0f);
        }
    }

    void Update()
    {
        if (takeRandomShots && Everyplay.IsRecording() && !Everyplay.IsPaused())
        {
            if (Time.time > nextRandomShotTime)
            {
                Everyplay.TakeThumbnail();
                nextRandomShotTime = Time.time + Random.Range(3.0f, 15.0f);
            }
        }
    }

    private void OnThumbnailReady(Texture2D texture, bool portrait)
    {
        if (thumbnailTextures[currentThumbnailTextureIndex] == texture)
        {
            currentThumbnailTextureIndex++;

            if (currentThumbnailTextureIndex >= thumbnailTextures.Length)
            {
                currentThumbnailTextureIndex = 0;
            }

            if (availableThumbnailCount < thumbnailTextures.Length)
            {
                availableThumbnailCount++;
            }

            Everyplay.SetThumbnailTargetTexture(thumbnailTextures[currentThumbnailTextureIndex]);
            SetThumbnailTargetSize();
        }
    }

    private void SetThumbnailTargetSize()
    {
        // Workaround issue that Unity might say that texture is size of x even it really is x to next power of two
        int thumbnailPotWidth = Mathf.NextPowerOfTwo(thumbnailWidth);
        int thumbnailPotHeight = Mathf.NextPowerOfTwo(thumbnailHeight);

        if (npotSupported)
        {
#pragma warning disable 612, 618
            Everyplay.SetThumbnailTargetTextureWidth(thumbnailWidth);
            Everyplay.SetThumbnailTargetTextureHeight(thumbnailHeight);
#pragma warning restore 612, 618

            thumbnailScale = new Vector2(1, 1);
        }
        else
        {
            if (pixelPerfect)
            {
#pragma warning disable 612, 618
                Everyplay.SetThumbnailTargetTextureWidth(thumbnailWidth);
                Everyplay.SetThumbnailTargetTextureHeight(thumbnailHeight);
#pragma warning restore 612, 618

                thumbnailScale = new Vector2((float) thumbnailWidth / (float) thumbnailPotWidth, (float) thumbnailHeight / (float) thumbnailPotHeight);
            }
            else
            {
#pragma warning disable 612, 618
                Everyplay.SetThumbnailTargetTextureWidth(thumbnailPotWidth);
                Everyplay.SetThumbnailTargetTextureHeight(thumbnailPotHeight);
#pragma warning restore 612, 618

                thumbnailScale = new Vector2(1, 1);
            }
        }
    }

    void OnDestroy()
    {
        Everyplay.ReadyForRecording -= OnReadyForRecording;

        if (initialized)
        {
            // Set Everyplay not to render to a texture anymore and remove event handlers
            Everyplay.SetThumbnailTargetTexture(null);
            Everyplay.RecordingStarted -= OnRecordingStarted;
            Everyplay.ThumbnailTextureReady -= OnThumbnailReady;

            // Destroy thumbnail textures
            foreach (Texture2D texture in thumbnailTextures)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
            }

            thumbnailTextures = null;

            initialized = false;
        }

        if (instance == this)
        {
            instance = null;
        }
    }


    public void Clear()
    {
        availableThumbnailCount = 0;
        currentThumbnailTextureIndex = 0;
    }
}
