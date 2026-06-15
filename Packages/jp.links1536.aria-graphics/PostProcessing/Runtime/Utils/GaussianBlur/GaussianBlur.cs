using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Aria.Rendering.Universal
{
	public class GaussianBlurResourceData : ContextItem
	{
		public class Parameter
		{
			[Range(3, 31)] public int KernelSize = 15;
			[Range(0.1f, 10f)] public float Sigma = 3f;
		}

		static readonly int KernelSize = Shader.PropertyToID("_KernelSize");
		static readonly int Sigma = Shader.PropertyToID("_Sigma");
		static readonly int TexelSize = Shader.PropertyToID("_TexelSize");
		static readonly int Source = Shader.PropertyToID("_Source");
		static readonly int Destination = Shader.PropertyToID("_Destination");

		ComputeShader m_ComputeShader;

		int m_KernelH;
		int m_KernelV;

		public bool IsValid()
			=> m_ComputeShader != null;

		internal void Setup(ComputeShader computeShader)
		{
			m_ComputeShader = computeShader;
			if (computeShader == null)
				return;
			m_KernelH = computeShader.FindKernel("GaussianBlurH");
			m_KernelV = computeShader.FindKernel("GaussianBlurV");
		}

		public void Execute(ComputeCommandBuffer cmd, Parameter parameter, TextureHandle source, TextureHandle temp, Vector2Int textureSize)
		{
			int w = textureSize.x;
			int h = textureSize.y;
			// kernelSize を奇数に強制
			int k = (parameter.KernelSize % 2 == 0)
				? parameter.KernelSize + 1
				: parameter.KernelSize;

			cmd.SetComputeIntParam(m_ComputeShader, KernelSize, k);
			cmd.SetComputeFloatParam(m_ComputeShader, Sigma, parameter.Sigma);
			cmd.SetComputeVectorParam(m_ComputeShader, TexelSize, new Vector2(1f / w, 1f / h));

			// --- 水平パス: source -> tempA ---
			cmd.SetComputeTextureParam(m_ComputeShader, m_KernelH, Source, source);
			cmd.SetComputeTextureParam(m_ComputeShader, m_KernelH, Destination, temp);
			cmd.DispatchCompute(m_ComputeShader, m_KernelH, Mathf.CeilToInt(w / 8f), Mathf.CeilToInt(h / 8f), 1);

			// --- 垂直パス: tempA -> tempB ---
			cmd.SetComputeTextureParam(m_ComputeShader, m_KernelV, Source, temp);
			cmd.SetComputeTextureParam(m_ComputeShader, m_KernelV, Destination, source);
			cmd.DispatchCompute(m_ComputeShader, m_KernelV, Mathf.CeilToInt(w / 8f), Mathf.CeilToInt(h / 8f), 1);
		}

		public override void Reset()
		{
			m_ComputeShader = null;
			m_KernelH = 0;
			m_KernelV = 0;
		}
	}
}
