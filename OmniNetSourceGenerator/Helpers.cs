using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OmniNetSourceGenerator
{
	internal static class Helpers
	{
		public static string CreateClass(string modifier, string className, string baseClassName = null, Func<string> onClassCreated = null)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendLine(baseClassName != null ? $"{modifier} class {className} : {baseClassName}" : $"{modifier} class {className}");
			builder.AppendLine("{");
			builder.AppendLine(onClassCreated?.Invoke());
			builder.AppendLine("}");
			return builder.ToString();
		}

		private static void CreateNamespace(string @namespace, Func<string> onNamespaceCreated, StringBuilder builder)
		{
			builder.AppendLine($"namespace {@namespace}");
			builder.AppendLine("{");
			builder.AppendLine(onNamespaceCreated?.Invoke());
			builder.AppendLine("}");
		}

		public static string CreateNamespace(string @namespace, List<string> usings, Func<string> onNamespaceCreated = null)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendLine(string.Join("\r\n", usings));
			if (!string.IsNullOrEmpty(@namespace))
			{
				CreateNamespace(@namespace, onNamespaceCreated, builder);
				return builder.ToString();
			}
			else
			{
				builder.AppendLine(onNamespaceCreated?.Invoke());
				return builder.ToString();
			}
		}

		public static string CreateNamespace(string @namespace, Func<string> onNamespaceCreated = null)
		{
			if (!string.IsNullOrEmpty(@namespace))
			{
				StringBuilder builder = new StringBuilder();
				CreateNamespace(@namespace, onNamespaceCreated, builder);
				return builder.ToString();
			}
			else return onNamespaceCreated?.Invoke();
		}

		public static string GetHintName(GeneratorExecutionContext context)
		{
			return $"{context.Compilation.AssemblyName}_g";
		}

		public static void Log(string message, bool append = true)
		{
			using (StreamWriter writer = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "log.txt"), append))
			{
				writer.WriteLine($"{DateTime.Now} - {message}");
			}
		}

		public static void ClearLog()
		{
			File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "log.txt"));
		}
	}
}
