using System;
using DreamArena.Identity;
using UnityEngine;

namespace DreamArena.Diagnostics;

internal static class Log
{
	public static void Info(string message)
	{
		Debug.Log(ModIdentity.LogPrefix + message);
	}

	public static void Warn(string message)
	{
		Debug.LogWarning(ModIdentity.LogPrefix + message);
	}

	public static void Error(string message, Exception exception)
	{
		Debug.LogError(ModIdentity.LogPrefix + message + Environment.NewLine + exception);
	}
}
