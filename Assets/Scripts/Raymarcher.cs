using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Raymarcher : MonoBehaviour
{
    [SerializeField] int maxStepCount;
    [SerializeField] float maxDistance;
    [SerializeField, Range(0.01f, 1)] float stepSize;
    [SerializeField] Type type;

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
        Box, Depth, SteppedDepth, Cloud
    };

    ComputeBuffer boxBuffer, cameraBuffer;
    Material transparentBlitMaterial;
    RenderTexture renderTexture;

    BoxData[] boxData;
    CameraData cameraData;

    void Start() {
        cloud = FindObjectsByType<Cloud>(FindObjectsSortMode.None);

        UpdateCloud();

        UpdateCamera();

        if (renderTexture != null) renderTexture.Release();
        renderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        transparentBlitMaterial = new Material(transparentBlitShader);
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
            case Type.Cloud:
                kernelHandle = computeShader.FindKernel("Cloud");
                break;
        }
        computeShader.SetInt("boxCount", cloud.Length);
        computeShader.SetInt("maxStepCount", maxStepCount);
        computeShader.SetFloat("maxDistance", maxDistance);
        computeShader.SetFloat("stepSize", stepSize);
        computeShader.SetBuffer(kernelHandle, "boxData", boxBuffer);
        computeShader.SetBuffer(kernelHandle, "cameraData", cameraBuffer);
        computeShader.SetTexture(kernelHandle, "Result", renderTexture);
        computeShader.Dispatch(kernelHandle, Mathf.CeilToInt(source.width / 8.0f), Mathf.CeilToInt(source.height / 8.0f), 1);

        transparentBlitMaterial.SetTexture("_MainTex", source);
        transparentBlitMaterial.SetTexture("_OverlayTex", renderTexture);

        Graphics.Blit(source, destination, transparentBlitMaterial);
    }
    void OnDestroy() {
        if (renderTexture != null) renderTexture.Release();
        if (boxBuffer != null) boxBuffer.Release();
        if (cameraBuffer != null) cameraBuffer.Release();
    }
    private void OnDisable() {
        if (renderTexture != null) renderTexture.Release();
        if (boxBuffer != null) boxBuffer.Release();
        if (cameraBuffer != null) cameraBuffer.Release();
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * maxDistance);
    }
}
