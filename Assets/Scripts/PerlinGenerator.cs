using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinGenerator : TextureGenerator {
    [SerializeField, Range(1, 1024)] int resolution;

    [Header("Settings")]
    [SerializeField, Range(-1024, 1024)] float sample = 0;
    [SerializeField, Range(0.1f, 100)] float wavelength = 1;
    [SerializeField, Range(1, 4)] float amplitude = 1;

    override protected Texture2D Generate2DTexture() {
        Texture2D texture = new Texture2D(resolution, resolution);
        for (int x = 0; x < resolution; x++) {
            for (int y = 0; y < resolution; y++) {
                float c = Mathf.PerlinNoise((x * 1.0f / resolution * wavelength) + sample, (y * 1.0f / resolution * wavelength) + sample);  // [0, 1]
                if (c <= 0.5f) {
                    c = Mathf.Pow(c, amplitude);
                }
                else {
                    c = 1 - Mathf.Pow(1 - c, amplitude);
                }
                texture.SetPixel(x, y, new Color(c, c, c));
            }
        }
        texture.Apply();
        return texture;
    }
}
