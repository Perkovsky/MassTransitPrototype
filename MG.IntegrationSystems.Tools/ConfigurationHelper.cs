using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;

namespace MG.IntegrationSystems.Tools
{
	public static class ConfigurationHelper
	{
		private static string GetAssemblyBasePath()
		{
			var codeBase = Assembly.GetExecutingAssembly().CodeBase;
			var uri = new UriBuilder(codeBase);
			var path = Uri.UnescapeDataString(uri.Path);
			return Path.GetDirectoryName(path);
		}

		public static IConfigurationRoot GetConfiguration(string filename)
		{
			return new ConfigurationBuilder()
				.SetBasePath(GetAssemblyBasePath())
				.AddJsonFile(filename)
				.Build();
		}
	}
}
