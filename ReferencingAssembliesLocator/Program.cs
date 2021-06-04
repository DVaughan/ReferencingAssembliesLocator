using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReferencingAssembliesLocator
{
	class Program
	{
		async static Task Main(string[] args)
		{
			int argsLength = args.Length;
			string directory;

			if (argsLength > 0)
			{
				directory = args[0];
			}
			else
			{
				directory = Directory.GetCurrentDirectory();
			}

			var locator = new Locator();

			Dictionary<string, HashSet<string>> references;

			using (var progress = new ProgressBar())
			{
				references = await locator.GetReferencedAssembliesAsync(directory, progress);
			}

			if (!references.Any())
			{
				Console.WriteLine("No results.");
				Console.ReadKey();
				goto quit;
			}

			void OutputResults(Dictionary<string, HashSet<string>> result)
			{
				Console.ResetColor();
				Console.WriteLine();

				foreach (KeyValuePair<string, HashSet<string>> pair in result)
				{
					string referencingAssembly = pair.Key;
					Console.ForegroundColor = ConsoleColor.White;
					Console.BackgroundColor = ConsoleColor.DarkYellow;
					Console.WriteLine(referencingAssembly);
					Console.ResetColor();
					Console.WriteLine("  Has reference to:");
					foreach (string reference in pair.Value)
					{
						Console.WriteLine("  " + reference);
					}
					
					Console.WriteLine();
				}
			}

			while (true)
			{
				Console.WriteLine("Enter assembly name pattern.");
				Console.WriteLine("'q' to quit.");
				Console.WriteLine("'cs' to clear screen.");
				Console.WriteLine();

				string assemblyPattern = Console.ReadLine();
				string input = assemblyPattern.Trim();
				if (input.Equals("q", StringComparison.OrdinalIgnoreCase))
				{
					break;
				}

				if (input.Equals("cs", StringComparison.OrdinalIgnoreCase))
				{
					Console.Clear();
					continue;
				}

				var items = locator.GetReferencedAssemblies(assemblyPattern, references);
				OutputResults(items);
			}

			quit:;
		}

	}
}
