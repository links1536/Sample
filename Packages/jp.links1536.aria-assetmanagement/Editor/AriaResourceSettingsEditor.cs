using System.Security.Cryptography;
using System.Text;
using Aria.AssetManagement.Hash;
using UnityEngine;
using UnityEditor;
using Aria.AssetManagement;

namespace AriaEditor.AssetManagement
{
	public class AriaResourceSettingsEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.HelpBox("サンプルではサーバー管理等の都合上、アプリにカタログ用のカギを持たせる設計です\nより厳密にしたい場合はカタログを返すAPIを用意するなどがよいです", MessageType.Info);
			EditorGUILayout.HelpBox("AssetBundleはそれぞれ個別のカギを持ちますが、カギ自体はカタログに載せています\nより厳密にしたい場合、ここも改変する必要があります", MessageType.Info);

			if (GUILayout.Button("Create Password"))
			{
				CreatePassword();
			}
			if (GUILayout.Button("Create Salt"))
			{
				CreateSalt();
			}

			serializedObject.ApplyModifiedProperties();
		}

		void CreatePassword()
		{
			const int PasswordLength = 32;
			string password = AriaResourceKeyGenerator.GeneratePassword(PasswordLength);
			serializedObject.FindProperty("CatalogPassword").stringValue = password;
		}

		void CreateSalt()
		{
			var saltSize = serializedObject.FindProperty("SaltSize").intValue;

			var saltProperty = serializedObject.FindProperty("CatalogSalt");
			saltProperty.arraySize = saltSize;

			using var saltGenerator = AriaResourceKeyGenerator.CreateSaltGenerator();
			byte[] bytes = new byte[saltSize];
			AriaResourceKeyGenerator.GetSaltBytes(saltGenerator, bytes);

			for (int i = 0; i < saltSize; i++)
			{
				var element = saltProperty.GetArrayElementAtIndex(i);
				element.intValue = bytes[i];
			}
		}
	}
}
