/*--------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *-------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Unity.Analyzers.Tests
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class DiagnosticAnalyzerMock : DiagnosticAnalyzer
	{
		public DiagnosticAnalyzerMock(string id, SyntaxKind syntaxKind, Predicate<SyntaxNode> filter = null)
		{
			Descriptor = new DiagnosticDescriptor(id, nameof(DiagnosticAnalyzerMock), null, nameof(DiagnosticAnalyzerMock), DiagnosticSeverity.Info, true);
			Filter = filter ?? (n => true);
			SyntaxKind = syntaxKind;
		}

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind);
		}

		private SyntaxKind SyntaxKind { get; }
		private Predicate<SyntaxNode> Filter { get; }
		private DiagnosticDescriptor Descriptor { get; }

		private void Analyze(SyntaxNodeAnalysisContext context)
		{
			var node = context.Node;
			if (!Filter(node))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Descriptor, GetLocation(node)));
		}

		private static Location GetLocation(SyntaxNode node)
		{
			// special case to mimic IDE analyzers
			return node switch
			{
				MethodDeclarationSyntax mds => mds.Identifier.GetLocation(),
				FieldDeclarationSyntax fds => fds.Declaration.Variables.First().Identifier.GetLocation(),
				ParameterSyntax ps => ps.Identifier.GetLocation(),
				_ => node.GetLocation(),
			};
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);
	}
}
