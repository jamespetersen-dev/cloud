using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Cloud : MonoBehaviour
{
    public Color color;
    public bool solid;

    private Vector3 previousPosition, previousLocalScale;
    public Vector3 GetSize() {
        return transform.localScale;
    }
    public Vector3 GetPosition() {
        return transform.position;
    }
    public Color GetColor() {
        return color;
    }
    private void Start() {
        previousPosition = transform.position;
        previousLocalScale = transform.localScale;
    }
    private void Update() {
        if (previousPosition != transform.position || previousLocalScale != transform.localScale) {
            previousPosition = transform.position;
            previousLocalScale = transform.localScale;
            FindObjectOfType<Raymarcher>().UpdateCloud();
        }
    }
    private void OnDrawGizmos() {
        Vector3 position = transform.position;
        Vector3 size = transform.localScale;

        if (solid) {
            Gizmos.color = color;
            Gizmos.DrawCube(position, size);
        }
        else {
            Gizmos.color = new Color(color.r, color.g, color.b, 1);
            Gizmos.DrawWireCube(position, size);
        }
    }
}
