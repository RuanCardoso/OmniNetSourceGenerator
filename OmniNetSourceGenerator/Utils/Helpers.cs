using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SourceGenerator.Utils
{
	internal static class Helpers
	{
		public static string CreateClass(string modifier, string classname, string baseclassname = null, Func<string> OnCreated = null)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendLine("\t" + (baseclassname != null ? $"{modifier} class {classname} : {baseclassname}" : $"{modifier} class {classname}"));
			builder.AppendLine("\t{");
			builder.AppendLine("\t" + OnCreated?.Invoke());
			builder.AppendLine("\t}");
			return builder.ToString();
		}

		private static void CreateNamespace(string namespacename, Func<string> OnCreated, StringBuilder builder)
		{
			builder.AppendLine($"namespace {namespacename}");
			builder.AppendLine("{");
			builder.AppendLine(OnCreated?.Invoke());
			builder.AppendLine("}");
		}

		public static string CreateNamespace(string namespacename, IEnumerable<string> usings, Func<string> OnCreated = null)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendLine(string.Join("\r\n", usings));
			if (!string.IsNullOrEmpty(namespacename))
			{
				CreateNamespace(namespacename, OnCreated, builder);
				return builder.ToString();
			}
			else
			{
				builder.AppendLine(OnCreated?.Invoke());
				return builder.ToString();
			}
		}

		public static string CreateNamespace(string namespacename, Func<string> OnCreated = null)
		{
			if (!string.IsNullOrEmpty(namespacename))
			{
				StringBuilder builder = new StringBuilder();
				CreateNamespace(namespacename, OnCreated, builder);
				return builder.ToString();
			}
			else return OnCreated?.Invoke();
		}

		public static string GetHintName(GeneratorExecutionContext context)
		{
			return $"{context.Compilation.AssemblyName}_g";
		}

		public static void Log(string name, string message, bool append = true)
		{
			using (StreamWriter writer = new StreamWriter("_source_gen_log.txt", append))
			{
				writer.WriteLine($"{DateTime.Now} - {name} - {message} [END]\n\n");
			}
		}

		public static void DeleteLog()
		{
			File.Delete("_source_gen_log.txt");
		}
	}
}
