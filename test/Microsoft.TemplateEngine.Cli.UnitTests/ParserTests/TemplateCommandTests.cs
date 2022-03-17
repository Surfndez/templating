﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using FakeItEasy;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.Commands;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Mocks;
using Microsoft.TemplateEngine.TestHelper;
using VerifyXunit;
using Xunit;

namespace Microsoft.TemplateEngine.Cli.UnitTests.ParserTests
{
    [UsesVerify]
    public class TemplateCommandTests : IClassFixture<VerifyFixture>
    {   
        private readonly VerifyFixture _verifySettings;

        public TemplateCommandTests(VerifyFixture verifySettings)
        {
            _verifySettings = verifySettings;
        }

        [Fact]
        public Task CannotCreateCommandForInvalidParameter()
        {
            var template = new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group")
                .WithParameters("have:colon", "n1", "n2");

            var paramSymbolInfo = new Dictionary<string, string>()
            {
                { "longName", "name" },
                { "shortName", "n" }
            };
            var symbolInfo = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                { "n1", paramSymbolInfo },
                { "n2", paramSymbolInfo }
            };

            var hostDataLoader = A.Fake<IHostSpecificDataLoader>();
            A.CallTo(() => hostDataLoader.ReadHostSpecificTemplateData(template)).Returns(new HostSpecificTemplateData(symbolInfo));

            TemplateGroup templateGroup = TemplateGroup.FromTemplateList(
                CliTemplateInfo.FromTemplateInfo(new[] { template }, hostDataLoader))
                .Single();

            ITemplateEngineHost host = TestHost.GetVirtualHost();
            IEngineEnvironmentSettings settings = new EngineEnvironmentSettings(host, virtualizeSettings: true);
            TemplatePackageManager packageManager = A.Fake<TemplatePackageManager>();

            NewCommand myCommand = (NewCommand)NewCommandFactory.Create("new", _ => host, _ => new TelemetryLogger(null, false), new NewCommandCallbacks());
            var parseResult = myCommand.Parse($" new foo");
            InstantiateCommandArgs args = InstantiateCommandArgs.FromNewCommandArgs(new NewCommandArgs(myCommand, parseResult));

            try
            {
                _ = new TemplateCommand(myCommand, settings, packageManager, templateGroup, templateGroup.Templates.Single());
            }
            catch (InvalidTemplateParametersException e)
            {
                Assert.Equal(2, e.ParameterErrors.Count);
                Assert.Equal(templateGroup.Templates.Single(), e.Template);

                return Verifier.Verify(e.Message, _verifySettings.Settings);
            }

            Assert.True(false, "should not land here");
            return Task.FromResult(1);
            
        }

    }
}