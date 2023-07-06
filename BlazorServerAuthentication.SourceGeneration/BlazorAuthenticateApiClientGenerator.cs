using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace BlazorAuthenticate.SourceGeneration
{
    [Generator]
    public class BlazorAuthenticateApiClientGenerator : ISourceGenerator
    {
        private const string classAttributeName = "BlazorAuthenticatedApiClient";
        private const string methodAttributeName = "BlazorAuthenticate";

        private const string classAttributeText = @"
using System;

namespace BlazorAuthenticate
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class BlazorAuthenticatedApiClientAttribute : Attribute
    {
        public BlazorAuthenticatedApiClientAttribute()
        {
        }
    }
}
";
        
        private const string methodAttributeText = @"
using System;

namespace BlazorAuthenticate
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class BlazorAuthenticateAttribute : Attribute
    {
        public BlazorAuthenticateAttribute()
        {
        }
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
/*#if DEBUG
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif*/
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // add the attribute text
            context.AddSource("BlazorAuthenticatedApiClientAttribute", classAttributeText);
            context.AddSource("BlazorAuthenticateAttribute", methodAttributeText);

            var treesWithClassWithAttributes = context.Compilation.SyntaxTrees
                .Where(st => st.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Any(p => p.DescendantNodes().OfType<AttributeSyntax>().Any()));

            foreach (var tree in treesWithClassWithAttributes)
            {
                var semanticModel = context.Compilation.GetSemanticModel(tree);

                var declaredClasses = tree
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(cd => cd.DescendantNodes().OfType<AttributeSyntax>().Any());

                foreach (var declaredClass in declaredClasses)
                {
                    var nodes = declaredClass
                        .DescendantNodes()
                        .OfType<AttributeSyntax>()
                        .FirstOrDefault(a => a.DescendantTokens().Any(dt => dt.IsKind(SyntaxKind.IdentifierToken) && semanticModel.GetTypeInfo(dt.Parent).Type.Name == classAttributeName))
                        ?.DescendantTokens()
                        ?.Where(dt => dt.IsKind(SyntaxKind.IdentifierToken))
                        ?.ToList();

                    if (nodes == null)
                    {
                        continue;
                    }

                    var methods = new List<string>();
                    foreach (MethodDeclarationSyntax classMethod in declaredClass.Members.Where(m => m.IsKind(SyntaxKind.MethodDeclaration)).OfType<MethodDeclarationSyntax>())
                    {
                        var methodNodes = classMethod
                            .DescendantNodes()
                            .OfType<AttributeSyntax>()
                            .FirstOrDefault(a => a.DescendantTokens().Any(dt => dt.IsKind(SyntaxKind.IdentifierToken) && semanticModel.GetTypeInfo(dt.Parent).Type.Name == methodAttributeName))
                            ?.DescendantTokens()
                            ?.Where(dt => dt.IsKind(SyntaxKind.IdentifierToken))
                            ?.ToList();

                        if (methodNodes == null)
                        {
                            continue;
                        }

                        var method = GenerateMethod(classMethod);
                        methods.Add(method);
                    }

                    var generatedClass = GenerateClass(declaredClass, methods);

                    context.AddSource($"{declaredClass.Identifier}_authenticated.cs", SourceText.From(generatedClass, Encoding.UTF8));
                }
            }
        }

        private string GenerateMethod(MethodDeclarationSyntax methodDeclaration)
        {
            var builder = new StringBuilder();

            var methodName = methodDeclaration.Identifier.ToString().Replace("_", "");
            var methodModifiers = methodDeclaration.Modifiers.ToString().Replace("private", "public");
            builder.Append($"       {methodModifiers} {methodDeclaration.ReturnType} {methodName}(");

            var parameters = methodDeclaration.ParameterList.Parameters;
            foreach (var parameter in parameters)
            {
                builder.Append($", {parameter.ToString()}");
            }
            builder.AppendLine(")");
            builder.AppendLine("        {");
            builder.AppendLine("            await _httpClientAuthenticator.PrepareHttpClientAsync(_httpClient);");
            builder.AppendLine(string.Empty);

            var parameterNames = string.Join(", ", parameters.Select(x => x.Identifier.ToString()).ToList());
            var returnTypes = GetReturnTypeList(methodDeclaration);
            /*var genericReturnTypes = string.Empty;
            if (returnTypes != null)
            {
                genericReturnTypes = $"<{returnTypes}>";
            }*/

            //builder.AppendLine($"           return await {methodDeclaration.Identifier}{genericReturnTypes}({parameterNames});");
            builder.AppendLine($"           {(returnTypes != null ? "return" : "")} await {methodDeclaration.Identifier}({parameterNames});");
            builder.Append("        }");

            return builder.ToString();
        }

        private string GetReturnTypeList(MethodDeclarationSyntax methodDeclaration)
        {
            var descendantNodes = methodDeclaration.ReturnType.DescendantNodes();
            var typeArgumentList = (TypeArgumentListSyntax)descendantNodes
                .FirstOrDefault(x => x is TypeArgumentListSyntax);

            if (typeArgumentList != null)
            {
                var types = typeArgumentList.Arguments.Select(x => x.ToString());
                return string.Join(", ", types);
            }

            return null;
        }

        private string GenerateClass(ClassDeclarationSyntax classSymbol, List<string> methods)
        {
            var sb = new StringBuilder();

            var namespaceDeclarationSyntax = classSymbol.Parent as NamespaceDeclarationSyntax;
            var namespaceName = namespaceDeclarationSyntax.Name.ToString();

            var methodSb = new StringBuilder();
            foreach (var method in methods)
            {
                methodSb.AppendLine(method);
            }

            sb.Append($@"using BlazorServerAuthentication;

namespace {namespaceName}
{{
    public partial class {classSymbol.Identifier}
    {{
        private readonly HttpClient _httpClient;
        private readonly IHttpClientAuthenticator _httpClientAuthenticator;

        public {classSymbol.Identifier}(HttpClient httpClient, IHttpClientAuthenticator httpClientAuthenticator)
        {{
            _httpClient = httpClient;
            _httpClientAuthenticator = httpClientAuthenticator;
        }}

{methodSb.ToString()}
    }}
}}");

            return sb.ToString();
        }
    }
}
