using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TextureGenerator : MonoBehaviour
{
    [SerializeField] bool _2D;
    protected Texture texture;
    protected virtual Texture2D Generate2DTexture() {
        return null; 
    }
    protected virtual Texture3D Generate3DTexture() {
        return null;
    }
    void SetTexture(Texture2D texture) {
        this.texture = texture;
        ApplyTextureToQuad(texture);
    }
    void SetTexture(Texture3D texture) {
        this.texture = texture;
        //ApplyTextureToQuad(texture);
    }
    void ApplyTextureToQuad(Texture2D texture) {
        // Get the Renderer component from the GameObject the script is attached to
        Renderer renderer = GetComponent<Renderer>();
        if (texture != null) {
            Debug.Log("Apply Texture");
        }
        // Assign the generated texture to the quad's material
        if (renderer != null && renderer.material != null) {
            renderer.material.mainTexture = texture;
        }
    }
    public Texture3D GetTexture() {
        if (texture != null) {
            return texture as Texture3D;
        }
        else {
            Texture3D t = Generate3DTexture();
            this.texture = t;
            return t;
        }
    }
    void ApplyTextureToQuad(Texture3D texture) {
        // Get the Renderer component from the GameObject the script is attached to
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.material != null) {
            Color[] color = texture.GetPixels(0);
            Texture2D tempTexture = new Texture2D(texture.width, texture.height);
            tempTexture.SetPixels(color);
            tempTexture.Apply();
            renderer.material.mainTexture = tempTexture;
            Debug.Log("Applied");
        }
    }
}
