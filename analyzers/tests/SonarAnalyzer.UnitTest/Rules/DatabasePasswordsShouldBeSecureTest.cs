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

#if NET

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Common;
using SonarAnalyzer.UnitTest.MetadataReferences;
using SonarAnalyzer.UnitTest.TestFramework;
using CS = SonarAnalyzer.Rules.CSharp;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class DatabasePasswordsShouldBeSecureTest
    {
        [DataTestMethod]
        [DataRow("3.1.11", "3.19.80")]
        [DataRow("5.0.2", "5.21.1")]
        [TestCategory("Rule")]
        public void DatabasePasswordsShouldBeSecure_CS(string entityFrameworkCoreVersion, string oracleVersion) =>
            Verifier.VerifyAnalyzer(@"TestCases\DatabasePasswordsShouldBeSecure.cs",
                                    new CS.DatabasePasswordsShouldBeSecure(),
                                    ParseOptionsHelper.FromCSharp8,
                                    GetReferences(entityFrameworkCoreVersion, oracleVersion));

        [TestMethod]
        public void DatabasePasswordsShouldBeSecure_Net5_CS() =>
            Verifier.VerifyAnalyzer(@"TestCases\DatabasePasswordsShouldBeSecure.Net5.cs",
                                    new CS.DatabasePasswordsShouldBeSecure(),
                                    ParseOptionsHelper.FromCSharp8,
                                    GetReferences("5.0.2", "5.21.1"));

        [TestMethod]
        public void DatabasePasswordsShouldBeSecure_NetCore3_CS() =>
            Verifier.VerifyAnalyzer(@"TestCases\DatabasePasswordsShouldBeSecure.NetCore31.cs",
                                    new CS.DatabasePasswordsShouldBeSecure(),
                                    ParseOptionsHelper.FromCSharp8,
                                    GetReferences("3.1.11", "3.19.80"));

        [DataTestMethod]
        [DataRow(@"TestCases\WebConfig\DatabasePasswordsShouldBeSecure\Values")]
        [DataRow(@"TestCases\WebConfig\DatabasePasswordsShouldBeSecure\UnexpectedContent")]
        [TestCategory("Rule")]
        public void DatabasePasswordsShouldBeSecure_CS_WebConfig(string root)
        {
            var webConfigPath = GetWebConfigPath(root);
            DiagnosticVerifier.VerifyExternalFile(
                CreateCompilation(),
                new CS.DatabasePasswordsShouldBeSecure(),
                File.ReadAllText(webConfigPath),
                TestHelper.CreateSonarProjectConfig(root, TestHelper.CreateFilesToAnalyze(root, webConfigPath)));
        }

        [TestMethod]
        [TestCategory("Rule")]
        public void DatabasePasswordsShouldBeSecure_CS_ExternalConnection()
        {
            var root = @"TestCases\WebConfig\DatabasePasswordsShouldBeSecure\ExternalConfig";
            var webConfigPath = GetWebConfigPath(root);
            var externalConfigPath = Path.Combine(root, "external.config");
            DiagnosticVerifier.VerifyExternalFile(
                CreateCompilation(),
                new CS.DatabasePasswordsShouldBeSecure(),
                File.ReadAllText(webConfigPath),
                TestHelper.CreateSonarProjectConfig(root, TestHelper.CreateFilesToAnalyze(root, webConfigPath, externalConfigPath)));
        }

        [TestMethod]
        [TestCategory("Rule")]
        public void DatabasePasswordsShouldBeSecure_CS_CorruptAndNonExistingWebConfigs_ShouldNotFail()
        {
            var root = @"TestCases\WebConfig\DatabasePasswordsShouldBeSecure\Corrupt";
            var missingDirectory = @"TestCases\WebConfig\DatabasePasswordsShouldBeSecure\NonExistingDirectory";
            var corruptFilePath = GetWebConfigPath(root);
            var nonExistingFilePath = GetWebConfigPath(missingDirectory);
            DiagnosticVerifier.VerifyExternalFile(
                CreateCompilation(),
                new CS.DatabasePasswordsShouldBeSecure(),
                File.ReadAllText(corruptFilePath),
                TestHelper.CreateSonarProjectConfig(root, TestHelper.CreateFilesToAnalyze(root, corruptFilePath, nonExistingFilePath)));
        }

        private static string GetWebConfigPath(string rootFolder) => Path.Combine(rootFolder, "Web.config");

        private static Compilation CreateCompilation() => SolutionBuilder.Create().AddProject(AnalyzerLanguage.CSharp).GetCompilation();

        private static IEnumerable<MetadataReference> GetReferences(string entityFrameworkCoreVersion, string oracleVersion) =>
            Enumerable.Empty<MetadataReference>()
                      .Concat(MetadataReferenceFacade.SystemData)
                      .Concat(MetadataReferenceFacade.SystemComponentModelPrimitives)
                      .Concat(NuGetMetadataReference.MicrosoftEntityFrameworkCore(entityFrameworkCoreVersion))
                      .Concat(NuGetMetadataReference.MicrosoftEntityFrameworkCoreSqliteCore(entityFrameworkCoreVersion))
                      .Concat(NuGetMetadataReference.MicrosoftEntityFrameworkCoreSqlServer(entityFrameworkCoreVersion))
                      .Concat(NuGetMetadataReference.OracleEntityFrameworkCore(oracleVersion))
                      .Concat(NuGetMetadataReference.MySqlDataEntityFrameworkCore())
                      .Concat(NuGetMetadataReference.NpgsqlEntityFrameworkCorePostgreSQL(entityFrameworkCoreVersion));
    }
}

#endif
