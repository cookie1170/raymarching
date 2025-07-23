using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rendering
{
	public class ComputeRenderer : MonoBehaviour
	{
		private static readonly int ScreenTextureID = Shader.PropertyToID("screenTexture");
		private static readonly int ResolutionID = Shader.PropertyToID("resolution");
		private static readonly int TimeID = Shader.PropertyToID("time");

		[SerializeField] private ComputeShader shader;
		
		private RenderTexture _rt;
		private Camera _cam;
		private int _kernelIndex;

		private bool _hasBeenRenderedThisFrame;

		private void Awake()
		{
			_cam = GetComponent<Camera>();
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
		}

		private void OnEndContextRendering(ScriptableRenderContext ctx, List<Camera> cameras)
		{
			if (_hasBeenRenderedThisFrame) return;
			_hasBeenRenderedThisFrame = true;

			Vector2 resolution = new(GetNextMultipleOfEight(Screen.width), GetNextMultipleOfEight(Screen.height));

			shader.SetFloat(TimeID, Time.time);
			shader.SetVector(ResolutionID, resolution);
			shader.SetTexture(_kernelIndex, ScreenTextureID, _rt);

			shader.Dispatch(_kernelIndex, _rt.width / 8, _rt.height / 8, 1);

			Graphics.Blit(_rt, dest: (RenderTexture)null);
		}

		private int GetNextMultipleOfEight(int i)
		{
			return Mathf.CeilToInt(i / 8f) * 8;
		}
		
		private void OnDestroy()
		{
			RenderPipelineManager.endContextRendering -= OnEndContextRendering;
			RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
		}
	}
}