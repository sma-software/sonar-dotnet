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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Common;
using SonarAnalyzer.UnitTest.MetadataReferences;
using SonarAnalyzer.UnitTest.TestFramework;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class ExecutingSqlQueriesTest
    {
#if NETFRAMEWORK // System.Data.OracleClient.dll is not available on .Net Core

        [TestMethod]
        [TestCategory("Rule")]
        [TestCategory("Hotspot")]
        public void ExecutingSqlQueries_CS_Net46() =>
            Verifier.VerifyAnalyzer(@"TestCases\Hotspots\ExecutingSqlQueries_Net46.cs",
                                    new CS.ExecutingSqlQueries(AnalyzerConfiguration.AlwaysEnabled),
                                    GetReferencesNet46(Constants.NuGetLatestVersion));

        [TestMethod]
        [TestCategory("Rule")]
        [TestCategory("Hotspot")]
        public void ExecutingSqlQueries_VB_Net46() =>
            Verifier.VerifyAnalyzer(@"TestCases\Hotspots\ExecutingSqlQueries_Net46.vb",
                                    new VB.ExecutingSqlQueries(AnalyzerConfiguration.AlwaysEnabled),
                                    ParseOptionsHelper.FromVisualBasic15,
                                    GetReferencesNet46(Constants.NuGetLatestVersion));

        internal static IEnumerable<MetadataReference> GetReferencesNet46(string sqlServerCeVersion) =>
            NetStandardMetadataReference.Netstandard
                                        .Concat(FrameworkMetadataReference.SystemData)
                                        .Concat(FrameworkMetadataReference.SystemDataOracleClient)
                                        .Concat(NuGetMetadataReference.SystemDataSqlServerCe(sqlServerCeVersion))
                                        .Concat(NuGetMetadataReference.MySqlData("8.0.22"))
                                        .Concat(NuGetMetadataReference.MicrosoftDataSqliteCore())
                                        .Concat(NuGetMetadataReference.SystemDataSQLiteCore());

#else

        [TestMethod]
        [TestCategory("Rule")]
        [TestCategory("Hotspot")]
        public void ExecutingSqlQueries_CS_NetCore() =>
            Verifier.VerifyAnalyzer(@"TestCases\Hotspots\ExecutingSqlQueries_NetCore.cs",
                                    new CS.ExecutingSqlQueries(AnalyzerConfiguration.AlwaysEnabled),
                                    ParseOptionsHelper.FromCSharp8,
                                    GetReferencesNetCore(Constants.DotNetCore220Version));

        [TestMethod]
        [TestCategory("Rule")]
        [TestCategory("Hotspot")]
        public void ExecutingSqlQueries_VB_NetCore() =>
            Verifier.VerifyAnalyzer(@"TestCases\Hotspots\ExecutingSqlQueries_NetCore.vb",
                                    new VB.ExecutingSqlQueries(AnalyzerConfiguration.AlwaysEnabled),
                                    ParseOptionsHelper.FromVisualBasic15,
                                    GetReferencesNetCore(Constants.DotNetCore220Version));

        internal static IEnumerable<MetadataReference> GetReferencesNetCore(string entityFrameworkVersion) =>
            Enumerable.Empty<MetadataReference>()
                      .Concat(NuGetMetadataReference.MicrosoftEntityFrameworkCore(entityFrameworkVersion))
                      .Concat(NuGetMetadataReference.MicrosoftEntityFrameworkCoreRelational(entityFrameworkVersion));
#endif
    }
}
