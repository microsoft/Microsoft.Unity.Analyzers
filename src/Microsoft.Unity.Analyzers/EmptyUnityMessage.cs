﻿using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Unity.Analyzers.Resources;

namespace Microsoft.Unity.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmptyUnityMessageAnalyzer : DiagnosticAnalyzer
    {
        public const string Id = "UNT0001";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            Id,
            title: Strings.EmptyUnityMessageDiagnosticTitle,
            messageFormat: Strings.EmptyUnityMessageDiagnosticMessageFormat,
            category: DiagnosticCategory.Performance,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Strings.EmptyUnityMessageDiagnosticDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(method);

            if (method.Modifiers.Any(SyntaxKind.AbstractKeyword) || method.Modifiers.Any(SyntaxKind.VirtualKeyword))
                return;

            if (method.Body.Statements.Count > 0)
                return;

            var classDeclaration = method.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDeclaration == null)
                return;

            var typeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
            var scriptInfo = new ScriptInfo(typeSymbol);
            if (!scriptInfo.HasMessages)
                return;

            if (!scriptInfo.IsMessage(symbol))
                return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, method.GetLocation(), symbol.Name));
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class EmptyUnityMessageCodeFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(EmptyUnityMessageAnalyzer.Id);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var declaration = root.FindNode(context.Span) as MethodDeclarationSyntax;
            if (declaration == null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    Strings.EmptyUnityMessageCodeFixTitle,
                    ct => DeleteEmptyMessageAsync(context.Document, declaration, ct),
                    declaration.ToFullString()),
                context.Diagnostics);
        }

        private static async Task<Document> DeleteEmptyMessageAsync(Document document, MethodDeclarationSyntax declaration, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.RemoveNode(declaration, SyntaxRemoveOptions.KeepNoTrivia);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
