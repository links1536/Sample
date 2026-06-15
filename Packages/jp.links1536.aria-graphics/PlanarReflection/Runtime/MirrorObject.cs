using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aria.Rendering.Universal.PlanarReflection
{
	public enum ReflectionForward
	{
		Up,
		Down,
		Left,
		Right,
		Forward,
		Back,
	}

	[ExecuteAlways]
	public class MirrorObject : MonoBehaviour
	{
		static System.Type[] CameraComponents = new System.Type[]
		{
			typeof(Camera),
			typeof(Skybox),
			typeof(UniversalAdditionalCameraData)
		};

		[SerializeField] LayerMask m_ReflectLayers = -1;
		[SerializeField] float m_ClipPlaneOffset = 0.0f;
		[SerializeField] bool m_EnablePostProcess = true;

		[SerializeField] ReflectionForward Forward = ReflectionForward.Forward;

		Camera m_Camera = null;
		RenderTexture m_ReflectionTexture = null;

		public Camera Camera
			=> m_Camera;

		void OnEnable()
		{
			CreateMirrorCamera();
			if (MirrorObjectManager.TryGetInstance(out var mirrorObjectManager))
				mirrorObjectManager.Register(this);
		}

		void OnDisable()
		{
			if (MirrorObjectManager.TryGetInstance(out var mirrorObjectManager))
				mirrorObjectManager.Unregister(this);

			if (TryGetComponent<Renderer>(out var renderer))
				renderer.SetPropertyBlock(null);

			if (m_Camera != null)
			{
				m_Camera.targetTexture = null;
				DestroyImmediate(m_Camera.gameObject);
			}

			if (m_ReflectionTexture != null)
			{
				m_ReflectionTexture.Release();
				DestroyImmediate(m_ReflectionTexture);
				m_ReflectionTexture = null;
			}
		}

		void CreateMirrorCamera()
		{
			if (m_Camera != null)
				return;
			GameObject cameraObject = new GameObject("MirrorCamera:" + GetEntityId(), CameraComponents);
			cameraObject.hideFlags = HideFlags.HideAndDontSave;
			cameraObject.transform.SetParent(transform);

			m_Camera = cameraObject.GetComponent<Camera>();
			m_Camera.enabled = false;
			m_Camera.transform.SetPositionAndRotation(
				transform.position,
				transform.rotation
			);
			m_Camera.targetTexture = m_ReflectionTexture;
		}

		void UpdateCamera(Camera referenceCamera, int rendererIndex)
		{
			if (m_Camera == null)
				return;

			var targetTexture = m_Camera.targetTexture;
			var cameraType = m_Camera.cameraType;
			m_Camera.CopyFrom(referenceCamera);
			m_Camera.cameraType = cameraType;
			m_Camera.targetTexture = targetTexture;

			if (referenceCamera.clearFlags == CameraClearFlags.Skybox)
			{
				Skybox referenceSkybox = referenceCamera.GetComponent<Skybox>();
				Skybox mirrorSkybox = m_Camera.GetComponent<Skybox>();
				if (!referenceSkybox || !referenceSkybox.material)
				{
					mirrorSkybox.enabled = false;
				}
				else
				{
					if (mirrorSkybox == null)
						mirrorSkybox = mirrorSkybox.gameObject.AddComponent<Skybox>();
					mirrorSkybox.enabled = true;
					mirrorSkybox.material = referenceSkybox.material;
				}
			}

			var additionalCameraData = m_Camera.GetUniversalAdditionalCameraData();
			additionalCameraData.SetRenderer(rendererIndex);
			additionalCameraData.renderPostProcessing = m_EnablePostProcess;
		}

		void UpdateMatrix(Camera referenceCamera)
		{
			if (referenceCamera == null)
				return;

			float3 position = transform.position;
			float3 normal = Forward switch
			{
				ReflectionForward.Up => transform.up,
				ReflectionForward.Down => -transform.up,
				ReflectionForward.Left => -transform.right,
				ReflectionForward.Right => transform.right,
				ReflectionForward.Forward => transform.forward,
				ReflectionForward.Back => -transform.forward,
				_ => Vector3.up,
			};

			MirrorCalculator.CameraSpacePlane(referenceCamera.worldToCameraMatrix, position, normal, -1, m_ClipPlaneOffset, out var clipSpacePlane);
			MirrorCalculator.ReflectionMatrix(position, normal, m_ClipPlaneOffset, out var reflrecitonMatrix);
			m_Camera.worldToCameraMatrix = math.mul(referenceCamera.worldToCameraMatrix, reflrecitonMatrix);
			m_Camera.projectionMatrix = referenceCamera.CalculateObliqueMatrix(clipSpacePlane);
		}

		void CreateTexture()
		{
			int m_TextureWidth = Screen.width;
			int m_TextureHeight = Screen.height;
			if (m_TextureWidth <= 0 || m_TextureHeight <= 0)
				return;
			if (m_ReflectionTexture != null && m_ReflectionTexture.width == m_TextureWidth && m_ReflectionTexture.height == m_TextureHeight)
				return;
			if (m_ReflectionTexture != null)
			{
				m_Camera.targetTexture = null;
				m_ReflectionTexture.Release();
				m_ReflectionTexture = null;
			}
			m_ReflectionTexture = new RenderTexture(m_TextureWidth, m_TextureHeight, 16);
			m_ReflectionTexture.name = "_MirrorReflection" + GetEntityId();
			m_ReflectionTexture.isPowerOfTwo = true;
			m_ReflectionTexture.filterMode = FilterMode.Bilinear;
			m_ReflectionTexture.hideFlags = HideFlags.DontSave;

			m_Camera.targetTexture = m_ReflectionTexture;

			var renderer = GetComponent<Renderer>();
			if (renderer == null)
				return;

#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				var properties = new MaterialPropertyBlock();
				properties.SetTexture(MirrorUtils.ReflectionTextureId, m_ReflectionTexture);
				renderer.SetPropertyBlock(properties);
				return;
			}
#endif
			using var pool = UnityEngine.Pool.ListPool<Material>.Get(out var materialList);
			renderer.GetMaterials(materialList);
			foreach (var material in materialList)
				if (material.HasProperty(MirrorUtils.ReflectionTextureId))
					material.SetTexture(MirrorUtils.ReflectionTextureId, m_ReflectionTexture);
		}

		public void Render(ScriptableRenderContext context, Camera referenceCamera, int rendererIndex)
		{
			if (m_Camera == null)
				return;
			UpdateCamera(referenceCamera, rendererIndex);
			UpdateMatrix(referenceCamera);

			var renderer = GetComponent<Renderer>();
			if (renderer == null)
				return;

			var planes = GeometryUtility.CalculateFrustumPlanes(referenceCamera.projectionMatrix * referenceCamera.worldToCameraMatrix);
			if (!GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
				return;

			CreateTexture();

			// カメラを使って反射用の画面描画
			var request = new UniversalRenderPipeline.SingleCameraRequest();
			request.destination = m_Camera.targetTexture;
			UniversalRenderPipeline.SubmitRenderRequest(m_Camera, request);
		}
	}

}
