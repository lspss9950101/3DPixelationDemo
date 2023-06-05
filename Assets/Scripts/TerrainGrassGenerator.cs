using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(Terrain))]
public class TerrainGrassGenerator : MonoBehaviour {
    [Header("Grass Model")]
    public Mesh mesh;
    public Material material;
    public float minScale;
    public float maxScale;
    [Header("Procedural Settings")]
    private TerrainData _terrainData;
    public bool draw;
    public LayerMask renderLayer;
    [Range(1, 1023)]
    public int subdivision;
    private int _subdivision;
    public Transform viewer;
    public float viewRadius;

    private Terrain _terrain;
    private Matrix4x4[] _mat;

    private void Awake() {
        _terrain = GetComponent<Terrain>();
        _terrainData = _terrain.terrainData;
        SetMatrix();
    }

    private void SetMatrix() {
        _mat = new Matrix4x4[subdivision*subdivision];
        Vector3 position = _terrain.GetPosition();
        Vector2 stepSize = new Vector2(_terrainData.size.x, _terrainData.size.z) / subdivision;
        for (int idxX = 0; idxX < subdivision; idxX++) {
            for (int idxY = 0; idxY < subdivision; idxY++) {
                float x = position.x + stepSize.x * idxX;
                float z = position.z + stepSize.y * idxY;
                float y = _terrain.SampleHeight(new Vector3(x, 0, z));
                float x_noise = stepSize.x * Mathf.PerlinNoise(x + 19936, z);
                float z_noise = stepSize.y * Mathf.PerlinNoise(x, z - 19936);
                x += x_noise;
                z += z_noise;
                Vector3 normal = _terrainData.GetInterpolatedNormal(x / _terrainData.size.x, z / _terrainData.size.z);

                float scale = Mathf.PerlinNoise(x, z) * (maxScale - minScale) + minScale;
                _mat[idxX + idxY*subdivision] = Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.FromToRotation(Vector3.up, normal), new Vector3(scale, scale, scale));
            }
        }
    }

    private int ToLayerNumber(LayerMask mask) {
        for (int i = 0; i < 32; i++) {
            if ((1 << i) == mask.value)
                return i;
        }
        return -1;
    }

    private void Update() {
        if (subdivision != _subdivision) {
            SetMatrix();
            _subdivision = subdivision;
        }
        if (draw && mesh != null && material != null && viewer != null) {
            RenderParams rps = new RenderParams(material);
            rps.layer = ToLayerNumber(renderLayer);
            rps.receiveShadows = true;
            rps.shadowCastingMode = ShadowCastingMode.Off;
            rps.worldBounds = new Bounds(viewer.position, new Vector3(viewRadius, viewRadius, viewRadius) * 2);

            Graphics.RenderMeshInstanced(rps, mesh, 0, _mat);
        }
    }
}
