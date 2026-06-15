using System.Text;
using System.Threading;
using Aria.Engine;
using Aria.Logging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using SystemInfo = UnityEngine.Device.SystemInfo;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using ZLogger;

namespace Aria
{
	public class AriaPlayerInitializer : MonoBehaviour
	{
		static readonly ILogger m_Logger = AriaLogger<AriaPlayerInitializer>.Get();

		[SerializeField] int m_DefaultFps = 60;

		private void Start()
			=> SetupAsync(default).Forget();
		async UniTask SetupAsync(CancellationToken cancellationToken)
		{
#if UNITY_EDITOR
			// 設定を弄っても止められてしまうので
			Application.runInBackground = true;
#endif

			Application.targetFrameRate = m_DefaultFps;

			// デバイススペック出力
			OutputDeviceSpec();

			AriaAssetProvider ariaAssetProvider;
			while (!AriaAssetProvider.TryGetInstance(out ariaAssetProvider))
				await UniTask.NextFrame();

			if (AriaAssetProvider.TryGetInstance(out ariaAssetProvider))
			{
				await ariaAssetProvider.SetupInAppAsync(null, cancellationToken);
				await ariaAssetProvider.PreloadInAppAsync(cancellationToken);
			}

			await SceneController.ChangeSceneAsync("stages/stage01/stage01");
		}

		static void OutputDeviceSpec()
		{
			var builder = new StringBuilder();
			builder.Append($"Device\t: '{SystemInfo.deviceModel}' '{SystemInfo.deviceName}' '{SystemInfo.deviceType}'");
			builder.AppendLine();
			builder.Append($"OS\t: {SystemInfo.operatingSystem} {SystemInfo.operatingSystemFamily}");
			builder.AppendLine();
			builder.Append($"CPU\t: {SystemInfo.processorType} {SystemInfo.processorFrequency / 1000}.{SystemInfo.processorFrequency % 1000 / 10:00}GHz {SystemInfo.processorCount}");
			builder.AppendLine();
			builder.Append($"RAM\t: {SystemInfo.systemMemorySize:#,0}MB");
			builder.AppendLine();
			builder.Append($"GPU\t: {SystemInfo.graphicsDeviceVendor} {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceID})");
			builder.AppendLine();
			builder.Append($"VRAM\t: {SystemInfo.graphicsMemorySize:#,0}MB");
			builder.AppendLine();
			builder.Append($"Graphic\t: {SystemInfo.graphicsDeviceVersion}");
			builder.AppendLine();
			builder.Append($"Shader\t: {SystemInfo.graphicsShaderLevel}");
			builder.AppendLine();
			m_Logger.ZLogInformation($"{builder.ToString()}");
		}
	}
}
