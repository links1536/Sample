using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Aria.AssetManagement
{
	public static class PlatformUtility
	{
		public static string RuntimePlatformIdentifer(RuntimePlatform platform)
		{
			switch (platform)
			{
				case RuntimePlatform.WindowsPlayer:
				case RuntimePlatform.WindowsEditor:
					return "windows";
				case RuntimePlatform.Android:
					return "android";
				case RuntimePlatform.IPhonePlayer:
					return "ios";
			}
			;
			return "none";
		}
	}
}
