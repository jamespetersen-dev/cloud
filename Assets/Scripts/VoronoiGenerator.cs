using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class VoronoiGenerator : TextureGenerator {
    [SerializeField, Range(1, 512)] private int resolution = 256;
    [SerializeField, Range(1, 64)] private int points = 10;
    [SerializeField] private ComputeShader computeShader;

    private ComputeBuffer pointBuffer, textureBuffer;

    private void UpdatePointsBuffer(Vector3[] points) {
        pointBuffer = new ComputeBuffer(points.Length, Marshal.SizeOf(typeof(Vector3)));
        pointBuffer.SetData(points);
    }

    private void UpdateTextureBuffer() {
        textureBuffer = new ComputeBuffer(resolution * resolution * resolution, Marshal.SizeOf(typeof(Color)));
    }

    override protected Texture3D Generate3DTexture() {
        if (computeShader == null) {
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

        ReleaseBuffer();
        UpdatePointsBuffer(seedPoints);
        UpdateTextureBuffer();


        int kernelHandle = computeShader.FindKernel("CSMain");
        computeShader.SetInt("resolution", resolution);
        computeShader.SetInt("points", points);

        computeShader.SetBuffer(kernelHandle, "pointData", pointBuffer);
        computeShader.SetBuffer(kernelHandle, "Result", textureBuffer);

        int threadGroupSize = Mathf.CeilToInt(resolution / 4.0f);
        computeShader.Dispatch(kernelHandle, threadGroupSize, threadGroupSize, threadGroupSize);

        Texture3D texture = new Texture3D(resolution, resolution, resolution, TextureFormat.ARGB32, false);

        Color[] resultBuffer = new Color[resolution * resolution * resolution];
        
        textureBuffer.GetData(resultBuffer);

        texture.SetPixels(resultBuffer, 0);

        texture.Apply();
        return texture;
    }

    private void ReleaseBuffer() {
        if (pointBuffer != null) {
            pointBuffer.Release();
            pointBuffer = null;
        }
        if (textureBuffer != null) {
            textureBuffer.Release();
            textureBuffer = null;
        }
    }

    private void OnDestroy() => ReleaseBuffer();

    private void OnDisable() => ReleaseBuffer();
}