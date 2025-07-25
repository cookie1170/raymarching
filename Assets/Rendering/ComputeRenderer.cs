using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Rendering.Shapes;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rendering
{
	public class ComputeRenderer : MonoBehaviour
	{
		private static ComputeRenderer _inst;
		
		private static readonly int ScreenTextureID = Shader.PropertyToID("screenTexture");
		private static readonly int CamRotationID = Shader.PropertyToID("camRotation");
		private static readonly int ResolutionID = Shader.PropertyToID("resolution");
		private static readonly int CameraPosID = Shader.PropertyToID("cameraPos");
		private static readonly int ShapesID = Shader.PropertyToID("shapes");
		private static readonly int TimeID = Shader.PropertyToID("time");

		[SerializeField] private ComputeShader shader;
		
		public enum ShapeType
		{
			[UsedImplicitly] Sphere,
			[UsedImplicitly] Box,
			[UsedImplicitly] Plane
		}

		public enum Operation
		{
			[UsedImplicitly] Union,
			[UsedImplicitly] Subtraction,
			[UsedImplicitly] Intersection
		}
		
		struct ShapeStruct
		{
			[UsedImplicitly] public Color Colour;
			[UsedImplicitly] public ShapeTransform Transform;
			[UsedImplicitly] public ShapeInfo Info;
			[UsedImplicitly] public ShapeType Type;
			[UsedImplicitly] public Operation Operation;
		}

		struct ShapeTransform
		{
			[UsedImplicitly] public Vector3 Position;
			[UsedImplicitly] public Vector3 Rotation;
		}

		struct ShapeInfo
		{
			[UsedImplicitly] public Vector3 Dimensions;
			[UsedImplicitly] public float BlendAmount;
		}

		private readonly List<Shape> _shapes = new();
		private ShapeStruct[] _shapeStructs;
		private ComputeBuffer _shapeBuffer;
		private RenderTexture _rt;
		private int _kernelIndex;
		private bool _hasBeenRenderedThisFrame;

		private void Awake()
		{
			_inst = this;
			RenderPipelineManager.endContextRendering += OnEndContextRendering;
			RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
			_kernelIndex = shader.FindKernel("csMain");
		}

		private void OnBeginContextRendering(ScriptableRenderContext ctx, List<Camera> cameras)
		{
			_hasBeenRenderedThisFrame = false;
			int width = GetNextMultipleOfEight(Screen.width);
			int height = GetNextMultipleOfEight(Screen.height);
			if (!_rt || _rt?.width != width || _rt?.height != height)
			{
				_rt?.Release();
				_rt = new(width, height, 24)
				{
					enableRandomWrite = true
				};
				Debug.Log("Size of render texture does not match size of screen, creating new render texture");
			}

			UpdateShapes();
		}

		private void OnEndContextRendering(ScriptableRenderContext ctx, List<Camera> cameras)
		{
			if (_hasBeenRenderedThisFrame || _shapes.Count <= 0) return;
			_hasBeenRenderedThisFrame = true;

			Vector2 resolution = new(GetNextMultipleOfEight(Screen.width), GetNextMultipleOfEight(Screen.height));

			_shapeBuffer.SetData(_shapeStructs);
			
			shader.SetFloat(TimeID, Time.time);
			shader.SetVector(ResolutionID, resolution);
			shader.SetVector(CameraPosID, transform.position);
			shader.SetVector(CamRotationID, new Vector2(transform.eulerAngles.x, transform.eulerAngles.y) * Mathf.Deg2Rad);
			shader.SetTexture(_kernelIndex, ScreenTextureID, _rt);
			shader.SetBuffer(_kernelIndex, ShapesID, _shapeBuffer);

			shader.Dispatch(_kernelIndex, _rt.width / 8, _rt.height / 8, 1);

			Graphics.Blit(_rt, dest: (RenderTexture)null);
		}

		private void UpdateShapes()
		{
			for (int i = 0; i < _shapes.Count; i++)
			{
				Shape shape = _shapes[i];
				_shapeStructs[i].Transform.Position = shape.transform.position;
				_shapeStructs[i].Transform.Rotation = shape.transform.eulerAngles * Mathf.Deg2Rad;
				_shapeStructs[i].Type = shape.Type;
				_shapeStructs[i].Operation = shape.Operation;
				_shapeStructs[i].Info.Dimensions = shape.Dimensions;
			}
		}
		
		private void RefreshShapes()
		{
			_shapeStructs = new ShapeStruct[_shapes.Count];
			_shapes.Sort((a, b) => a.Priority - b.Priority);
			for (int i = 0; i < _shapes.Count; i++)
			{
				Shape shape = _shapes[i];
				ShapeStruct shapeStruct = new()
				{
					Colour = shape.Colour,
					Operation = shape.Operation,
					Type = shape.Type,
					Info = new()
					{
						Dimensions = shape.Dimensions,
						BlendAmount = shape.BlendAmount,
					},
					Transform = new()
					{
						Position = shape.transform.position,
						Rotation = shape.transform.eulerAngles * Mathf.Deg2Rad,
					}
				};
				_shapeStructs[i] = shapeStruct;
			}

			if (_shapes.Count > 0)
			{
				_shapeBuffer?.Release();
				_shapeBuffer = new(_shapes.Count, sizeof(float) * 14 + sizeof(int) * 2);
			}
		}

		private int GetNextMultipleOfEight(int i)
		{
			return Mathf.CeilToInt(i / 8f) * 8;
		}
		
		private void OnDestroy()
		{
			RenderPipelineManager.endContextRendering -= OnEndContextRendering;
			RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
			_rt?.Release();
			_shapeBuffer?.Release();
		}

		public static IEnumerator RegisterShape(Shape shape)
		{
			while (true)
			{
				if (_inst)
				{
					_inst?._shapes.Add(shape);
					_inst?.RefreshShapes();
					break;
				}

				yield return null;
			}
		}

		public static void DeregisterShape(Shape shape)
		{
			_inst?._shapes.Remove(shape);
			_inst?.RefreshShapes();
		}

		public static void RefreshShapesStatic()
		{
			_inst?.RefreshShapes();
		}
	}
}