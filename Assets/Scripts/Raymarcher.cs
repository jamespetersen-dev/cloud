using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class Raymarcher : MonoBehaviour
{
    [SerializeField] int maxStepCount;
    [SerializeField] float maxDistance;
    [SerializeField, Range(0.01f, 1)] float stepSize;
    [SerializeField] Type type;
    //[SerializeField] Texture3D noiseTexture;
    [SerializeField, Range(1, 512)] private int resolution = 256;
    [SerializeField, Range(1, 256)] private int points = 10;
    [SerializeField, Range(1, 256)] private int detailPoints = 10;
    [SerializeField] private ComputeShader voronoiCompute;

    [SerializeField, Range(0.001f, 0.5f)] float sampleSize = 1;
    [SerializeField, Range(0, 1)] float densityMultiplier = 1.0f;
    [SerializeField] Vector3 samplePos;
    [SerializeField, Range(0.001f, 1)] float detailSampleSize = 1;
    [SerializeField, Range(0, 8)] float detailDensityMultiplier = 1.0f;
    [SerializeField] Vector3 detailSamplePos;

    [SerializeField] bool invertNoise;
    [SerializeField, Range(0, 1)] float sigmoidMiddle = 0.5f;
    [SerializeField, Range(0, 100)] float sigmoidScale = 10;

    [SerializeField] ComputeShader computeShader;
    [SerializeField] Shader transparentBlitShader;
    Cloud[] cloud;

    struct BoxData {
        public Vector3 position;
        public Vector3 size;
        public Color color;
    };
    struct CameraData {
        public Vector3 position;
        public Vector3 direction;
        public float fov;
        public float nearPlane;
        public float farPlane;
        public float aspectRatio;
        public int textureWidth;
        public int textureHeight;
        public Quaternion rotation;
    };

    public enum Type {
        Box, Depth, SteppedDepth, SphereNoise, Cloud, Clouds
    };

    ComputeBuffer boxBuffer, cameraBuffer;
    Material transparentBlitMaterial;
    RenderTexture renderTexture;
    ComputeBuffer pointBuffer, textureBuffer, detailTextureBuffer;

    BoxData[] boxData;
    CameraData cameraData;

    void Start() {
        cloud = FindObjectsOfType<Cloud>();

        UpdateCloud();

        UpdateCamera();

        if (renderTexture != null) renderTexture.Release();
        renderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        transparentBlitMaterial = new Material(transparentBlitShader);

        if (textureBuffer == null) {
            textureBuffer = GenerateNoiseTexture(false, points);
        }
        if (detailTextureBuffer == null) {
            detailTextureBuffer = GenerateNoiseTexture(true, detailPoints);
        }
    }

    void UpdatePointsBuffer(Vector3[] points) {
        if (pointBuffer != null) pointBuffer.Release();
        pointBuffer = new ComputeBuffer(points.Length, Marshal.SizeOf(typeof(Vector3)));
        pointBuffer.SetData(points);
    }

    void UpdateTextureBuffer() {
        if (textureBuffer != null) textureBuffer.Release();
        textureBuffer = new ComputeBuffer(resolution * resolution * resolution, Marshal.SizeOf(typeof(Color)));
    }
    void UpdateDetailTextureBuffer() {
        if (detailTextureBuffer != null) detailTextureBuffer.Release();
        detailTextureBuffer = new ComputeBuffer(resolution * resolution * resolution, Marshal.SizeOf(typeof(Color)));
    }

    public void UpdateCloud() {
        if (boxBuffer != null) boxBuffer.Release();
        boxData = new BoxData[cloud.Length];
        for (int i = 0; i < cloud.Length; i++) {
            boxData[i] = new BoxData();
            boxData[i].position = cloud[i].GetPosition();
            boxData[i].size = cloud[i].GetSize();
            boxData[i].color = cloud[i].GetColor();
        }
        boxBuffer = new ComputeBuffer(cloud.Length, Marshal.SizeOf(typeof(BoxData)));
        boxBuffer.SetData(boxData);
    }
    public void UpdateCamera() {
        if (cameraBuffer != null) cameraBuffer.Release();
        cameraData.position = Camera.main.transform.position;
        cameraData.direction = Camera.main.transform.forward;
        cameraData.fov = Camera.main.fieldOfView;
        cameraData.nearPlane = Camera.main.nearClipPlane;
        cameraData.farPlane = Camera.main.farClipPlane;
        cameraData.aspectRatio = Camera.main.aspect;
        cameraData.textureWidth = Screen.width;
        cameraData.textureHeight = Screen.height;
        cameraData.rotation = Camera.main.transform.rotation;

        cameraBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(CameraData)));
        cameraBuffer.SetData(new CameraData[] { cameraData });
    }

    ComputeBuffer GenerateNoiseTexture(bool detail, int pointCount) {
        if (voronoiCompute == null) {
            Debug.LogError("Compute Shader is not assigned!");
            return null;
        }

        if (resolution <= 0 || points <= 0) {
            Debug.LogError("Resolution and Points must be greater than zero!");
            return null;
        }

        Vector3[] seedPoints = new Vector3[pointCount];
        for (int i = 0; i < pointCount; i++) {
            seedPoints[i] = new Vector3(
                Random.Range(0, resolution),
                Random.Range(0, resolution),
                Random.Range(0, resolution)
            );
        }

        UpdatePointsBuffer(seedPoints);
        if (!detail) {
            UpdateTextureBuffer();
        }
        else {
            UpdateDetailTextureBuffer();
        }

        int kernelHandle = voronoiCompute.FindKernel("CSMain");
        voronoiCompute.SetInt("resolution", resolution);

        voronoiCompute.SetInt("points", pointCount);

        voronoiCompute.SetBool("invertNoise", invertNoise);

        voronoiCompute.SetBuffer(kernelHandle, "pointData", pointBuffer);
        if (!detail) {
            voronoiCompute.SetBuffer(kernelHandle, "Result", textureBuffer);
        }
        else {
            voronoiCompute.SetBuffer(kernelHandle, "Result", detailTextureBuffer);
        }
        int threadGroupSize = Mathf.CeilToInt(resolution / 4.0f);
        voronoiCompute.Dispatch(kernelHandle, threadGroupSize, threadGroupSize, threadGroupSize);

        //Texture3D texture = new Texture3D(resolution, resolution, resolution, TextureFormat.ARGB32, false);

        //Color[] resultBuffer = new Color[resolution * resolution * resolution];

        //textureBuffer.GetData(resultBuffer);

        /*texture.SetPixels(resultBuffer, 0);

        texture.Apply();*/
        if (!detail) {
            return textureBuffer;
        }
        return detailTextureBuffer;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (renderTexture.width != source.width || renderTexture.height != source.height) {
            renderTexture.width = source.width;
            renderTexture.height = source.height;
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }

        if (cameraData.position != Camera.main.transform.position || cameraData.direction != Camera.main.transform.forward) {
            UpdateCamera();
        }

        int kernelHandle = 0;
        switch (type) {
            case Type.Depth:
                kernelHandle = computeShader.FindKernel("Depth");
                break;
            case Type.Box:
                kernelHandle = computeShader.FindKernel("Box");
                break;
            case Type.SteppedDepth:
                kernelHandle = computeShader.FindKernel("SteppedDepth");
                break;
            case Type.SphereNoise:
                kernelHandle = computeShader.FindKernel("SphereNoise");
                break;
            case Type.Cloud:
                kernelHandle = computeShader.FindKernel("Cloud");
                break;
            case Type.Clouds:
                kernelHandle = computeShader.FindKernel("Clouds");
                break;
        }
        computeShader.SetInt("boxCount", cloud.Length);
        computeShader.SetInt("maxStepCount", maxStepCount);
        computeShader.SetFloat("maxDistance", maxDistance);
        computeShader.SetFloat("stepSize", stepSize);
        computeShader.SetFloat("noiseResolution", resolution);
        computeShader.SetFloat("sampleSize", sampleSize);
        computeShader.SetFloat("detailSampleSize", detailSampleSize);
        computeShader.SetVector("samplePos", samplePos);
        computeShader.SetVector("detailSamplePos", detailSamplePos);
        computeShader.SetFloat("densityMultiplier", densityMultiplier);
        computeShader.SetFloat("detailDensityMultiplier", detailDensityMultiplier);
        computeShader.SetFloat("sigmoidMiddle", sigmoidMiddle);
        computeShader.SetFloat("sigmoidScale", sigmoidScale);
        computeShader.SetBuffer(kernelHandle, "boxData", boxBuffer);
        computeShader.SetBuffer(kernelHandle, "cameraData", cameraBuffer);
        computeShader.SetTexture(kernelHandle, "Result", renderTexture);
        computeShader.SetBuffer(kernelHandle, "cloudDensity", textureBuffer);
        computeShader.SetBuffer(kernelHandle, "cloudDetailDensity", detailTextureBuffer);
        /*Color[] c = new Color[resolution * resolution * resolution];
        textureBuffer.GetData(c); Reading back data each frame is incredibly slow
        Debug.Log(c[0]);
        Debug.Log(c[5]);
        Debug.Log(c[10]);*/
        computeShader.Dispatch(kernelHandle, Mathf.CeilToInt(source.width / 8.0f), Mathf.CeilToInt(source.height / 8.0f), 1);

        transparentBlitMaterial.SetTexture("_MainTex", source);
        transparentBlitMaterial.SetTexture("_OverlayTex", renderTexture);

        Graphics.Blit(source, destination, transparentBlitMaterial);
    }
    void OnDestroy() {
        if (renderTexture != null) renderTexture.Release();
        if (boxBuffer != null) boxBuffer.Release();
        if (cameraBuffer != null) cameraBuffer.Release();
        if (pointBuffer != null) pointBuffer.Release();
        if (textureBuffer != null) textureBuffer.Release();
        if (detailTextureBuffer != null) detailTextureBuffer.Release();
    }
    private void OnDisable() {
        if (renderTexture != null) renderTexture.Release();
        if (boxBuffer != null) boxBuffer.Release();
        if (cameraBuffer != null) cameraBuffer.Release();
        if (pointBuffer != null) pointBuffer.Release();
        if (textureBuffer != null) textureBuffer.Release();
        if (detailTextureBuffer != null) detailTextureBuffer.Release();
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * maxDistance);
    }

}
