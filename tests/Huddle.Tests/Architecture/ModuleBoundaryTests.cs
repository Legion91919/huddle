using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Huddle.Accounts;
using Huddle.Groups;
using Huddle.Payments;
using Huddle.SharedKernel;
using Huddle.Statements;
using Xunit;

namespace Huddle.Tests;

/// <summary>Modules talk only through SharedKernel contracts: no module references another module, and module internals stay internal.</summary>
public class ModuleBoundaryTests
{
    private static readonly Dictionary<string, Assembly> Modules = new()
    {
        ["Huddle.Accounts"] = typeof(AccountsModule).Assembly,
        ["Huddle.Groups"] = typeof(GroupsModule).Assembly,
        ["Huddle.Payments"] = typeof(PaymentsModule).Assembly,
        ["Huddle.Statements"] = typeof(StatementsModule).Assembly,
    };

    [Fact]
    public void No_module_assembly_references_another_module()
    {
        foreach (var (name, assembly) in Modules)
        {
            var references = assembly.GetReferencedAssemblies().Select(r => r.Name!).ToList();
            var others = Modules.Keys.Where(k => k != name);
            Assert.DoesNotContain(references, r => others.Contains(r));
        }
    }

    [Fact]
    public void No_module_project_file_references_another_module_project()
    {
        var root = RepoRoot();
        foreach (var name in Modules.Keys)
        {
            var folder = name.Split('.')[1];
            var project = XDocument.Load(Path.Combine(root, "src", "Modules", folder, $"{name}.csproj"));
            var refs = project.Descendants("ProjectReference").Select(e => e.Attribute("Include")!.Value).ToList();
            Assert.All(refs, r => Assert.Contains("Huddle.SharedKernel", r));
        }
    }

    [Fact]
    public void Each_module_exposes_only_its_registration_class()
    {
        foreach (var (name, assembly) in Modules)
        {
            var exported = assembly.GetExportedTypes().Select(t => t.FullName!).ToList();
            Assert.Equal(new[] { $"{name}.{name.Split('.')[1]}Module" }, exported.ToArray());
        }
    }

    [Fact]
    public void Shared_kernel_depends_on_no_module_and_no_web_framework()
    {
        var references = typeof(Money).Assembly.GetReferencedAssemblies().Select(r => r.Name!).ToList();

        Assert.DoesNotContain(references, r => r.StartsWith("Huddle.", StringComparison.Ordinal));
        Assert.DoesNotContain(references, r => r.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Money_never_uses_floating_point()
    {
        var floating = new[] { typeof(double), typeof(float) };
        var members = typeof(Money).GetProperties().Select(p => p.PropertyType)
            .Concat(typeof(Money).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Select(f => f.FieldType));

        Assert.DoesNotContain(members, t => floating.Contains(t));
    }

    private static string RepoRoot([CallerFilePath] string path = "")
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(path)!);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Huddle.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Huddle.sln not found above " + path);
    }
}
