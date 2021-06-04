using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ReferencingAssembliesLocator
{
	public class Locator
	{
		public async Task<Dictionary<string, HashSet<string>>> GetReferencedAssembliesAsync(string assemblyNamePattern, string directory, IProgress<double> progress = default, CancellationToken cancelationToken = default)
		{
			var references = await GetReferencedAssembliesAsync(directory, progress, cancelationToken);
			return GetReferencedAssemblies(assemblyNamePattern, references);
		}

		public Dictionary<string, HashSet<string>> GetReferencedAssemblies(string assemblyNamePattern, Dictionary<string, HashSet<string>> references)
		{
			Regex regex = new Regex(assemblyNamePattern, RegexOptions.Compiled);

			Dictionary<string, HashSet<string>> result = new Dictionary<string, HashSet<string>>();

			foreach (KeyValuePair<string, HashSet<string>> item in references)
			{
				HashSet<string> referenceCollection = item.Value;

				foreach (string reference in referenceCollection)
				{
					if (regex.IsMatch(reference))
					{
						string referencingAssembly = item.Key;
						if (!result.TryGetValue(referencingAssembly, out var hashSet))
						{
							hashSet = new HashSet<string>();
							result[referencingAssembly] = hashSet;
						}
						hashSet.Add(reference);
					}
				}
			}

			return result;
		}

		public Task<Dictionary<string, HashSet<string>>> GetReferencedAssembliesAsync(string directory, IProgress<double> progress = default, CancellationToken cancelationToken = default)
		{
			ConcurrentDictionary<string, ConcurrentBag<string>> dictionary = new ConcurrentDictionary<string, ConcurrentBag<string>>();

			List<string> dllPaths = Directory.EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories).ToList();
			
			int filesToProgress = dllPaths.Count;
			int counter = 0;

			Parallel.ForEach(dllPaths, 
							new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, 
							(dllPath, state) =>
			{
				cancelationToken.ThrowIfCancellationRequested();

				try
				{
					Assembly assembly = Assembly.LoadFrom(dllPath);
					AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();
					string ownerName = assembly.FullName + $" ({dllPath})";

					foreach (AssemblyName referenceName in referencedAssemblies)
					{
						string fullName = referenceName.FullName;

						var bag = dictionary.GetOrAdd(ownerName, _ => new ConcurrentBag<string>());
						bag.Add(fullName);
					}
				}
				catch (Exception)
				{
					// ignore and continue;
				}

				if (progress != null)
				{
					Interlocked.Increment(ref counter);
					progress.Report(counter / (double)filesToProgress);
				}
			});

			Dictionary<string, HashSet<string>> result = new Dictionary<string, HashSet<string>>();

			foreach (KeyValuePair<string, ConcurrentBag<string>> pair in dictionary)
			{
				result.Add(pair.Key, pair.Value.ToHashSet<string>());
			}

			return Task.FromResult(result);
		}
	}
}
