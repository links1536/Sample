using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AriaEditor.AssetManagement
{
	static class AriaResourceKeyGenerator
	{
		public static string GeneratePassword(int length)
		{
			// 使用可能文字
			const string lowercase = "abcdefghijklmnopqrstuvwxyz";
			const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			const string digits = "0123456789";
			const string symbols = "!@#$%^&*()_-+=[{]};:<>|./?";

			// すべての文字を結合
			string validChars = lowercase + uppercase + digits + symbols;

			StringBuilder password = new StringBuilder();

			// 乱数生成器を作成
			using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
			{
				byte[] uintBuffer = new byte[4];

				while (password.Length < length)
				{
					rng.GetBytes(uintBuffer);
					uint num = System.BitConverter.ToUInt32(uintBuffer, 0);
					int index = (int)(num % (uint)validChars.Length);
					password.Append(validChars[index]);
				}
			}

			return password.ToString();
		}

		public static RNGCryptoServiceProvider CreateSaltGenerator()
			=> new RNGCryptoServiceProvider();

		public static void GetSaltBytes(RNGCryptoServiceProvider generator, byte[] bytes)
		{
			// TODO: ラップしたい気持ちあり
			generator.GetBytes(bytes, 0, bytes.Length);
		}
	}
}
