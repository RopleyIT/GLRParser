using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace DynamicCSharp;

/// <summary>
/// Acknowledgements:
/// The code below is based on code published by Samuel Cragg on
/// the CodeProject website, and has been issued under the
/// CodeProject Open Licence.
///
/// This class deals with the recursive resolution of assemblies
/// as each assembly is loaded.
/// </summary>
internal sealed class AssemblyResolver : IDisposable
{
    private readonly ICompilationAssemblyResolver assemblyResolver;
    private readonly DependencyContext dependencyContext;
    private readonly AssemblyLoadContext loadContext;

    /// <summary>
    /// Constructor. Given a path to an assembly file, load the assembly
    /// and build a recursive loader for all its references dependencies
    /// </summary>
    /// <param name="path">The path to the assembly to be loaded</param>

    public AssemblyResolver(string path)
    {
        this.Assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
        this.dependencyContext = DependencyContext.Load(this.Assembly);

        this.assemblyResolver =
            new CompositeCompilationAssemblyResolver
            (
                [
                    new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(path)),
                    new ReferenceAssemblyPathResolver(),
                    new PackageCompilationAssemblyResolver()
                ]
            );

        this.loadContext = AssemblyLoadContext.GetLoadContext(this.Assembly);
        this.loadContext.Resolving += OnResolving;
    }

    public Assembly Assembly { get; }

    public void Dispose()
    {
        this.loadContext.Resolving -= this.OnResolving;
    }

    private Assembly OnResolving(AssemblyLoadContext context, AssemblyName name)
    {
        RuntimeLibrary library =
            this.dependencyContext.RuntimeLibraries.FirstOrDefault
            (
                (rtl) => string.Equals(rtl.Name, name.Name, StringComparison.OrdinalIgnoreCase)
            );

        if (library != null)
        {
            var wrapper = new CompilationLibrary(
                library.Type,
                library.Name,
                library.Version,
                library.Hash,
                library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
                library.Dependencies,
                library.Serviceable);

            var assemblies = new List<string>();
            this.assemblyResolver.TryResolveAssemblyPaths(wrapper, assemblies);
            if (assemblies.Count > 0)
            {
                return this.loadContext.LoadFromAssemblyPath(assemblies[0]);
            }
        }

        return null;
    }
}