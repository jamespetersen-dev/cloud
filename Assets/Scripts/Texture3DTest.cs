using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class Generate3DTexture : MonoBehaviour {
    public int textureSize = 16; // Size of the 3D texture
    private Texture3D texture3D;


    ComputeBuffer pointBuffer, textureBuffer;

    [SerializeField, Range(1, 512)] private int resolution = 256;
    [SerializeField, Range(1, 256)] private int points = 10;
    [SerializeField] private ComputeShader voronoiCompute;

    void Start() {
        texture3D = GenerateNoiseTexture();

        // Assign the texture to the material
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.material = new Material(Shader.Find("Custom/Unlit3DTexture"));
        renderer.material.SetTexture("_MainTex", texture3D);
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


    Texture3D GenerateNoiseTexture() {
        if (voronoiCompute == null) {
            Debug.LogError("Compute Shader is not assigned!");
            return null;
        }

        if (resolution <= 0 || points <= 0) {
            Debug.LogError("Resolution and Points must be greater than zero!");
            return null;
        }

        Vector3[] seedPoints = new Vector3[points];
        for (int i = 0; i < points; i++) {
            seedPoints[i] = new Vector3(
                Random.Range(0, resolution),
                Random.Range(0, resolution),
                Random.Range(0, resolution)
            );
        }

        UpdatePointsBuffer(seedPoints);
        UpdateTextureBuffer();


        int kernelHandle = voronoiCompute.FindKernel("CSMain");
        voronoiCompute.SetInt("resolution", resolution);
        voronoiCompute.SetInt("points", points);

        voronoiCompute.SetBuffer(kernelHandle, "pointData", pointBuffer);
        voronoiCompute.SetBuffer(kernelHandle, "Result", textureBuffer);

        int threadGroupSize = Mathf.CeilToInt(resolution / 4.0f);
        voronoiCompute.Dispatch(kernelHandle, threadGroupSize, threadGroupSize, threadGroupSize);

        Texture3D texture = new Texture3D(resolution, resolution, resolution, TextureFormat.RGBA32, false);

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;

        Color[] resultBuffer = new Color[resolution * resolution * resolution];

        textureBuffer.GetData(resultBuffer);

        texture.SetPixels(resultBuffer, 0);

        texture.Apply();
        return texture;
    }

    void OnDestroy() {
        if (pointBuffer != null) pointBuffer.Release();
        if (textureBuffer != null) textureBuffer.Release();
    }
    private void OnDisable() {
        if (pointBuffer != null) pointBuffer.Release();
        if (textureBuffer != null) textureBuffer.Release();
    }
}