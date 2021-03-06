﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2021 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [Rule(DiagnosticId)]
    public sealed class StaticFieldVisible : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S2223";
        private const string MessageFormat = "Change the visibility of '{0}' or make it 'const' or 'readonly'.";

        private static readonly DiagnosticDescriptor Rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var fieldDeclaration = (FieldDeclarationSyntax)c.Node;
                    foreach (var field in fieldDeclaration.Declaration.Variables
                        .Select(variableDeclaratorSyntax => new
                        {
                            Syntax = variableDeclaratorSyntax,
                            Symbol = c.SemanticModel.GetDeclaredSymbol(variableDeclaratorSyntax) as IFieldSymbol
                        })
                        .Where(f => FieldIsRelevant(f.Symbol)))
                    {
                        c.ReportDiagnosticWhenActive(Diagnostic.Create(Rule, field.Syntax.Identifier.GetLocation(), field.Syntax.Identifier.ValueText));
                    }
                },
                SyntaxKind.FieldDeclaration);

        private static bool FieldIsRelevant(IFieldSymbol fieldSymbol) =>
            fieldSymbol is { IsStatic: true, IsConst: false, IsReadOnly: false }
            && fieldSymbol.DeclaredAccessibility != Accessibility.Private;
    }
}
