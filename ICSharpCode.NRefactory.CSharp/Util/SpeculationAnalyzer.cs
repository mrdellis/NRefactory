//
// SpeculationAnalyzer.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.NRefactory6.CSharp
{
	public class SpeculationAnalyzer
	{
		readonly static Type typeInfo;
		readonly static MethodInfo symbolsForOriginalAndReplacedNodesAreCompatibleMethod;
		readonly static MethodInfo replacementChangesSemanticsMethod;
		readonly object instance;

		static SpeculationAnalyzer ()
		{
			Type[] abstractSpeculationAnalyzerGenericParams = new[] {
				Type.GetType ("Microsoft.CodeAnalysis.SyntaxNode" + ReflectionNamespaces.CAAsmName, true),
				Type.GetType ("Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax" + ReflectionNamespaces.CACSharpAsmName, true),
				Type.GetType ("Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax" + ReflectionNamespaces.CACSharpAsmName, true),
				Type.GetType ("Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax" + ReflectionNamespaces.CACSharpAsmName, true),
				Type.GetType ("Microsoft.CodeAnalysis.CSharp.Syntax.ArgumentSyntax" + ReflectionNamespaces.CACSharpAsmName, true),
				Type.GetType ("Microsoft.CodeAnalysis.CSharp.Syntax.ForEachStatementSyntax" + ReflectionNamespaces.CACSharpAsmName, true),
				Type.GetType ("Microsoft.CodeAnalysis.CSharp.Syntax.ThrowStatementSyntax" + ReflectionNamespaces.CACSharpAsmName, true),
				Type.GetType ("Microsoft.CodeAnalysis.SemanticModel" + ReflectionNamespaces.CAAsmName, true)
			};
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.Shared.Utilities.AbstractSpeculationAnalyzer`8" + ReflectionNamespaces.WorkspacesAsmName, true)
				.MakeGenericType (abstractSpeculationAnalyzerGenericParams);

			symbolsForOriginalAndReplacedNodesAreCompatibleMethod = typeInfo.GetMethod ("SymbolsForOriginalAndReplacedNodesAreCompatible", BindingFlags.Public | BindingFlags.Instance);
			replacementChangesSemanticsMethod = typeInfo.GetMethod ("ReplacementChangesSemantics", BindingFlags.Public | BindingFlags.Instance);
			symbolInfosAreCompatibleMethod = typeInfo.GetMethod ("SymbolInfosAreCompatible", BindingFlags.Public | BindingFlags.Static);

			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CSharp.Utilities.SpeculationAnalyzer" + ReflectionNamespaces.CSWorkspacesAsmName, true);
			createSpeculativeSemanticModelForNodeMethod =  typeInfo.GetMethod ("CreateSpeculativeSemanticModelForNode", BindingFlags.Public | BindingFlags.Static, null, new [] {typeof (SyntaxNode), typeof (SyntaxNode), typeof (SemanticModel)}, null);
		}

		public SpeculationAnalyzer (ExpressionSyntax expression, ExpressionSyntax newExpression, SemanticModel semanticModel, CancellationToken cancellationToken, bool skipVerificationForReplacedNode = false, bool failOnOverloadResolutionFailuresInOriginalCode = false)
		{
			instance = Activator.CreateInstance (typeInfo, new object[] {
				expression,
				newExpression,
				semanticModel,
				cancellationToken,
				skipVerificationForReplacedNode,
				failOnOverloadResolutionFailuresInOriginalCode
			});
		}

		static MethodInfo symbolInfosAreCompatibleMethod;

		public static bool SymbolInfosAreCompatible(SymbolInfo originalSymbolInfo, SymbolInfo newSymbolInfo, bool performEquivalenceCheck, bool requireNonNullSymbols = false)
		{
			return (bool)symbolInfosAreCompatibleMethod.Invoke (null, new object[] { originalSymbolInfo, newSymbolInfo, performEquivalenceCheck, requireNonNullSymbols } );
		}

		public bool SymbolsForOriginalAndReplacedNodesAreCompatible ()
		{
			return (bool)symbolsForOriginalAndReplacedNodesAreCompatibleMethod.Invoke (instance, new object[0]);
		}

		public bool ReplacementChangesSemantics ()
		{
			return (bool)replacementChangesSemanticsMethod.Invoke (instance, new object[0]);
		}

		readonly static MethodInfo createSpeculativeSemanticModelForNodeMethod;

		public static SemanticModel CreateSpeculativeSemanticModelForNode(SyntaxNode originalNode, SyntaxNode nodeToSpeculate, SemanticModel semanticModel)
		{
			return (SemanticModel)createSpeculativeSemanticModelForNodeMethod.Invoke (null, new object[] {originalNode, nodeToSpeculate, semanticModel });
		}

		public static bool CanSpeculateOnNode(SyntaxNode node)
		{
			return (node is StatementSyntax && node.Kind() != SyntaxKind.Block) ||
				node is TypeSyntax ||
				node is CrefSyntax ||
				node.Kind() == SyntaxKind.Attribute ||
				node.Kind() == SyntaxKind.ThisConstructorInitializer ||
				node.Kind() == SyntaxKind.BaseConstructorInitializer ||
				node.Kind() == SyntaxKind.EqualsValueClause ||
				node.Kind() == SyntaxKind.ArrowExpressionClause;
		}

	}

}
