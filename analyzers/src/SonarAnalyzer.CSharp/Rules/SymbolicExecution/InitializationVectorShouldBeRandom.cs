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

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Extensions;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.SymbolicExecution;
using SonarAnalyzer.SymbolicExecution.Common.Checks;
using SonarAnalyzer.SymbolicExecution.Common.Constraints;

namespace SonarAnalyzer.Rules.SymbolicExecution
{
    internal sealed class InitializationVectorShouldBeRandom : ISymbolicExecutionAnalyzer
    {
        internal const string DiagnosticId = "S3329";
        private const string MessageFormat = "Use a dynamically-generated, random IV.";

        private static readonly DiagnosticDescriptor Rule = DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public IEnumerable<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public ISymbolicExecutionAnalysisContext AddChecks(CSharpExplodedGraph explodedGraph, SyntaxNodeAnalysisContext context) =>
            new AnalysisContext(explodedGraph);

        private sealed class AnalysisContext : DefaultAnalysisContext<Location>
        {
            public AnalysisContext(AbstractExplodedGraph explodedGraph)
            {
                explodedGraph.AddExplodedGraphCheck(new ByteArrayCheck(explodedGraph));
                explodedGraph.AddExplodedGraphCheck(new InitializationVectorCheck(explodedGraph, this));
            }

            protected override Diagnostic CreateDiagnostic(Location location) =>
                Diagnostic.Create(Rule, location);
        }

        private sealed class InitializationVectorCheck : ExplodedGraphCheck
        {
            private readonly AnalysisContext context;

            public InitializationVectorCheck(AbstractExplodedGraph explodedGraph, AnalysisContext context) : base(explodedGraph) =>
                this.context = context;

            public override ProgramState PreProcessInstruction(ProgramPoint programPoint, ProgramState programState) =>
                programPoint.CurrentInstruction is InvocationExpressionSyntax invocation
                    ? InvocationExpressionPreProcess(invocation, programState)
                    : programState;

            public override ProgramState PostProcessInstruction(ProgramPoint programPoint, ProgramState programState) =>
                programPoint.CurrentInstruction switch
                {
                    InvocationExpressionSyntax invocation => InvocationExpressionPostProcess(invocation, programState),
                    AssignmentExpressionSyntax assignment => AssignmentExpressionPostProcess(assignment, programState),
                    _ => programState
                };

            private ProgramState InvocationExpressionPostProcess(InvocationExpressionSyntax invocation, ProgramState programState)
            {
                if (IsSymmetricAlgorithmGenerateIVMethod(invocation, semanticModel)
                    && GetSymbolicValue(invocation, programState) is {} ivSymbolicValue)
                {
                    programState = programState.SetConstraint(ivSymbolicValue, CryptographyIVSymbolicValueConstraint.Initialized);
                }

                return programState;
            }

            private ProgramState AssignmentExpressionPostProcess(AssignmentExpressionSyntax assignment, ProgramState programState) =>
                assignment.Left is MemberAccessExpressionSyntax memberAccess
                && IsSymmetricAlgorithmIVMemberAccess(memberAccess)
                && GetSymbolicValue(memberAccess.Expression, programState) is {} leftSymbolicValue
                && GetSymbolicValue(assignment.Right, programState) is {} rightSymbolicValue
                && programState.HasConstraint(rightSymbolicValue, ByteArraySymbolicValueConstraint.Constant)
                    ? programState.SetConstraint(leftSymbolicValue, CryptographyIVSymbolicValueConstraint.NotInitialized)
                    : programState;

            private ProgramState InvocationExpressionPreProcess(InvocationExpressionSyntax invocation, ProgramState programState)
            {
                if (IsSymmetricAlgorithmCreateEncryptorMethod(invocation, semanticModel))
                {
                    if (invocation.ArgumentList.Arguments.Count == 0)
                    {
                        var symbolicValue = GetSymbolicValue(invocation, programState);
                        if (symbolicValue != null && HasNotInitializedIVConstraint(symbolicValue, programState))
                        {
                            context.AddLocation(invocation.GetLocation());
                        }
                    }
                    else if (IsInvalidIV(invocation.ArgumentList.Arguments[1], programState))
                    {
                        context.AddLocation(invocation.GetLocation());
                    }
                }

                return programState;
            }

            private bool IsInvalidIV(ArgumentSyntax argument, ProgramState programState) =>
                argument.Expression switch
                {
                    MemberAccessExpressionSyntax memberAccess => memberAccess.NameIs("IV")
                                                                 && programState.GetSymbolValue(semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol) is {} symbolicValue
                                                                 && HasNotInitializedIVConstraint(symbolicValue, programState),
                    IdentifierNameSyntax identifier => programState.GetSymbolValue(semanticModel.GetSymbolInfo(identifier).Symbol) is {} symbolicValue
                                                       && programState.HasConstraint(symbolicValue, ByteArraySymbolicValueConstraint.Constant),
                    ArrayCreationExpressionSyntax _ => true,
                    _ => false
                };

            private SymbolicValue GetSymbolicValue(InvocationExpressionSyntax invocation, ProgramState programState) =>
                GetSymbolicValue(((MemberAccessExpressionSyntax)invocation.Expression).Expression, programState);

            private SymbolicValue GetSymbolicValue(ExpressionSyntax expression, ProgramState programState) =>
                semanticModel.GetSymbolInfo(expression).Symbol is {} symbol
                && programState.GetSymbolValue(symbol) is {} symbolicValue
                    ? symbolicValue
                    : null;

            private bool IsSymmetricAlgorithmIVMemberAccess(MemberAccessExpressionSyntax memberAccess) =>
                memberAccess.IsMemberAccessOnKnownType("IV", KnownType.System_Security_Cryptography_SymmetricAlgorithm, semanticModel);

            private static bool HasNotInitializedIVConstraint(SymbolicValue value, ProgramState programState) =>
                programState.Constraints[value].HasConstraint(CryptographyIVSymbolicValueConstraint.NotInitialized);

            private static bool IsSymmetricAlgorithmCreateEncryptorMethod(InvocationExpressionSyntax invocation, SemanticModel semanticModel) =>
                invocation.IsMemberAccessOnKnownType("CreateEncryptor", KnownType.System_Security_Cryptography_SymmetricAlgorithm, semanticModel);

            private static bool IsSymmetricAlgorithmGenerateIVMethod(InvocationExpressionSyntax invocation, SemanticModel semanticModel) =>
                invocation.IsMemberAccessOnKnownType("GenerateIV", KnownType.System_Security_Cryptography_SymmetricAlgorithm, semanticModel);
        }
    }
}
