using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControll : MonoBehaviour {
    public float speed = 0.01f;
    [Range(0, 1)]
    public float scale;

    [SerializeField] private Material _pixelationMaterial;
    private Vector3 _position;
    private Camera _camera;

    public Vector3 position {
        get => _position;
        set {
            _position = value;
            SetSnappedPosition(value);
        }
    }

    private void Start() {
        _camera = GetComponent<Camera>();
        position = transform.position;
    }

    private void SetSnappedPosition(Vector3 position) {
        Vector3 right = transform.right.normalized;
        Vector3 up = transform.up.normalized;
        Vector3 forward = transform.forward.normalized;
        float x = Vector3.Dot(right, position);
        float y = Vector3.Dot(up, position);
        float z = Vector3.Dot(forward, position);

        float height = 2 * _camera.orthographicSize;
        float pixelSize = height / (int)(_camera.pixelHeight * scale);

        float quantized_x = Mathf.Floor(x / pixelSize) * pixelSize;
        float quantized_y = Mathf.Floor(y / pixelSize) * pixelSize;
        
        Vector3 newPos = quantized_x * right + quantized_y * up + z * forward;
        transform.position = newPos;
    }

    private void Update() {
        _pixelationMaterial.SetFloat("_VerticalPixels", _camera.pixelHeight * scale);

        if (Input.GetKey("w")) {
            position += new Vector3(0, 0, speed);
        }
        if (Input.GetKey("s")) {
            position += new Vector3(0, 0, -speed);
        }
        if (Input.GetKey("a")) {
            position += new Vector3(-speed, 0, 0);
        }
        if (Input.GetKey("d")) {
            position += new Vector3(speed, 0, 0);
        }
    }
}
