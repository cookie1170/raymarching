using System;
using JetBrains.Annotations;
using NaughtyAttributes;
using UnityEngine;

namespace Rendering.Shapes
{
    public class Shape : MonoBehaviour
    {
        [field: SerializeField, OnValueChanged("RefreshShapes")] public int Priority { get; private set; }
        [field: SerializeField] public Color Colour { get; private set; } = Color.white;
        [field: SerializeField] public Vector3 Dimensions { get; private set; } = Vector3.one;
        [field: SerializeField, Min(0)] public float BlendAmount { get; private set; } = 1.0f;
        [field: SerializeField] public ComputeRenderer.ShapeType Type { get; private set; }
        [field: SerializeField] public ComputeRenderer.Operation Operation { get; private set; }

        [SerializeField, HideInInspector] private Mesh sphereMesh;
        [SerializeField, HideInInspector] private Mesh boxMesh;
        [SerializeField, HideInInspector] private Mesh planeMesh;
        [SerializeField, HideInInspector] private Material material;

        private Color _lastColour;
        private MeshFilter _mesh;
        private MeshRenderer _meshRenderer;

        private void Awake() => GetComponent<MeshRenderer>().enabled = false;

        private void OnEnable()
        {
            StartCoroutine(ComputeRenderer.RegisterShape(this));
        }

        private void OnDisable()
        {
            ComputeRenderer.DeregisterShape(this);
        }

        private void OnValidate()
        {
            if (!_mesh) _mesh = GetComponent<MeshFilter>();
            if (!_meshRenderer) _meshRenderer = GetComponent<MeshRenderer>();

            switch (Type)
            {
                case ComputeRenderer.ShapeType.Sphere:
                    transform.localScale = Vector3.one * Dimensions.x * 2;
                    _mesh.mesh = sphereMesh;
                    break;
                case ComputeRenderer.ShapeType.Box:
                    transform.localScale = Dimensions;
                    _mesh.mesh = boxMesh;
                    break;
                case ComputeRenderer.ShapeType.Plane:
                    Dimensions = transform.up.normalized;
                    _mesh.mesh = planeMesh;
                    transform.localScale = new Vector3(10, 1, 10);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (Colour != _lastColour)
            {
                _meshRenderer.sharedMaterial = new(material)
                {
                    color = Colour
                };
                _lastColour = Colour;
            }
        }

        [UsedImplicitly]
        private void RefreshShapes() => ComputeRenderer.RefreshShapesStatic();
    }
}