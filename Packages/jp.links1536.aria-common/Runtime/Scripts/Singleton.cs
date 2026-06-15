using System;
using Aria.Logging;
using Microsoft.Extensions.Logging;

namespace Aria.Common
{
	public class NonMonoSingleton<T>
		where T : class, new()
	{
		public static T Instance { get; private set; } = new T();
	}

	public class Singleton<T> : UnityEngine.MonoBehaviour
		where T : UnityEngine.MonoBehaviour
	{
		public static T Instance { get; private set; }

		static readonly protected ILogger m_Logger = AriaLogger<T>.Get();

		protected static void SetInstance(T instance)
		{
			if (instance == null)
				return;
			UnityEngine.Debug.Assert(instance != null, "設定しようとしたインスタンスはnullです");
			UnityEngine.Debug.Assert(Instance == null || Instance.Equals(instance), typeof(T).Name + "は既に設定済みです");
			Instance = instance;
		}

		void OnEnable()
		{
			if (this is not T instance)
				return;
			SetInstance(instance);
		}

		public static bool TryGetInstance(out T instance)
		{
			instance = Instance;
			if (instance != null)
				return true;
			SetInstance(FindAnyObjectByType<T>(UnityEngine.FindObjectsInactive.Include));
			instance = Instance;
			return instance != null;
		}

	}
}
