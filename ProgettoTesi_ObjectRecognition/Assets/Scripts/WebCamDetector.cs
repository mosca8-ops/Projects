using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Barracuda;
using UnityEngine.Profiling;
using System.Threading.Tasks;

[RequireComponent(typeof(OnGUICanvasRelativeDrawer))]
public class WebCamDetector : MonoBehaviour
{
    [Tooltip("File of YOLO model. If you want to use another than YOLOv2 tiny, it may be necessary to change some const values in YOLOHandler.cs")]
    public NNModel modelFile;
    [Tooltip("Text file with classes names separated by coma ','")]
    public TextAsset classesFile;
    [Tooltip("Cfg file with anchors as field")]
    public TextAsset anchorsFile;
    [Tooltip("RawImage component which will be used to draw resuls.")]
    public RawImage imageRenderer;
    [Range(0.05f, 1f)]
    [Tooltip("The minimum value of box confidence below which boxes won't be drawn.")]
    public float MinBoxConfidence = 0.5f;

    public string extra = "";
    public string support = "";
    public static bool android = false;
    public static int videoRotationAngle = 0;
    string[] classesNames;
    int count = 0;

    public static YOLOHandler yolo;

    NNHandler nn;
    WebCamTexture camTexture;
    Texture2D displayingTex;
    TextureScaler textureScaler;
    OnGUICanvasRelativeDrawer relativeDrawer;
    PerformanceCounter.StopwatchCounter stopwatch = new PerformanceCounter.StopwatchCounter("Net inference time");
    Quaternion baseRotation;
    Color[] colorArray = new Color[] { Color.green, Color.red, Color.yellow, Color.black};

    void Start()
    {
        camTexture = new WebCamTexture();
        baseRotation = transform.rotation;

        camTexture.Play();

        videoRotationAngle = camTexture.videoRotationAngle;

        imageRenderer.transform.rotation = baseRotation * Quaternion.AngleAxis(videoRotationAngle, Vector3.back); // ruoto la camera in android di -90°
        
        relativeDrawer = GetComponent<OnGUICanvasRelativeDrawer>();
        relativeDrawer.relativeObject = imageRenderer.GetComponent<RectTransform>();

        if (Application.platform == RuntimePlatform.Android ||
            Application.platform == RuntimePlatform.IPhonePlayer)
        {
            android = true;
        }

        classesNames = classesFile.text.Split(',');
        nn = new NNHandler(modelFile, anchorsFile.text);
        yolo = new YOLOHandler(nn, classesNames);

        textureScaler = new TextureScaler(nn.model.inputs[0].shape[BarracudaUtils.WidthIndex], nn.model.inputs[0].shape[BarracudaUtils.HeightIndex]);    
    }


    void Update()
    {


        if (!camTexture || !camTexture.isPlaying)
        {
            return;
        }

        stopwatch.Start();

        CaptureAndPrepareTexture(camTexture, ref displayingTex);
        count++;
        if (count % 2 == 0)
        {
           funcB();
        }
        imageRenderer.texture = displayingTex;

        stopwatch.Stop();
        //Debug.Log("Update: " + stopwatch.Value);
    }

    async Task funcB()
    {
        var boxes = await yolo.RunAsync(displayingTex);
        DrawResults(boxes, displayingTex);
    }
    Texture2D rotateTexture(Texture2D originalTexture, bool clockwise)
    {
        Color32[] original = originalTexture.GetPixels32();
        Color32[] rotated = new Color32[original.Length];
        int w = originalTexture.width;
        int h = originalTexture.height;

        int iRotated, iOriginal;

        for (int j = 0; j < h; ++j)
        {
            for (int i = 0; i < w; ++i)
            {
                iRotated = (i + 1) * h - j - 1;
                iOriginal = clockwise ? original.Length - 1 - (j * w + i) : j * w + i;
                rotated[iRotated] = original[iOriginal];
            }
        }

        Texture2D rotatedTexture = new Texture2D(h, w);
        rotatedTexture.SetPixels32(rotated);
        rotatedTexture.Apply();
        return rotatedTexture;
    }

    private void OnDestroy()
    {
        nn?.Dispose();
        yolo?.Dispose();
        textureScaler?.Dispose();
        camTexture?.Stop();
    }

    private void CaptureAndPrepareTexture(WebCamTexture camTexture, ref Texture2D tex)
    {
        Profiler.BeginSample("Texture processing");

        TextureCropTools.CropToSquare(camTexture, ref tex);
        textureScaler.Scale(tex);

        Profiler.EndSample();
    }

    private  void DrawResults(IEnumerable<YOLOHandler.ResultBox> results, Texture2D img)
    {
        relativeDrawer.Clear();

        imageRenderer.GetComponentInParent<Canvas>().GetComponent<CanvasScaler>().referenceResolution = new Vector2(Screen.width, Screen.height);
        results.ForEach(box => DrawBox(box, img)); //displayingTex));
    }
    private void DrawBox(YOLOHandler.ResultBox box, Texture2D img)
    {
        int bci = box.bestClassIdx;
        if (box.classes[bci] < MinBoxConfidence)
        {
            return;
        }
        Color col = colorArray[bci % colorArray.Length];

        TextureDrawingUtils.DrawRect(img, 
            box.rect, 
            col,
            (int)(box.classes[bci] / MinBoxConfidence), 
            true, 
            true);

        decimal confidence = Math.Round((decimal)box.confidence * 100, 1);
        string label = classesNames[bci] + " " + confidence + "%";
        //Debug.Log(classesNames[bci] + " " + confidence + "%");

        if (extra.Equals(""))
        {
            relativeDrawer.DrawLabel(label, box, col);
        }
        else if (extra.ToLower().Equals("arrow"))
        {
            DrawArrow.SetArrow(relativeDrawer,
            label,
            box,
            col,
            support);
        }
    }

    //sincrono
    /* 
    void Update()
    {
        CaptureAndPrepareTexture(camTexture, ref displayingTex);
        stopwatch.Start();
        var boxes = yolo.Run(displayingTex);
        stopwatch.Stop();
        Debug.Log(stopwatch.Value);
        DrawResults(boxes, displayingTex);
        imageRenderer.texture = displayingTex;
    }*/
    /*
    private async Task DrawResultsAsync(IEnumerable<YOLOHandler.ResultBox> results, Texture2D img)
    {
        relativeDrawer.Clear();
        await Task.Yield();
        results.ForEach(box => DrawBox(box, displayingTex));
    }*/
}
