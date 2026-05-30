using FluentAssertions;
using LangKeep.Application.Services;
using LangKeep.Core.Interfaces;
using LangKeep.Core.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LangKeep.Application.Tests.Services;

public sealed class PreferenceManagementServiceTests
{
    private readonly IPreferenceRepository _repository;
    private readonly PreferenceManagementService _service;

    public PreferenceManagementServiceTests()
    {
        _repository = Substitute.For<IPreferenceRepository>();
        var logger = Substitute.For<ILogger<PreferenceManagementService>>();
        _service = new PreferenceManagementService(_repository, logger);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsFromRepository()
    {
        var expected = new List<LanguagePreference>
        {
            new(new ApplicationIdentity("Code.exe"), new KeyboardLayout("en-US")),
            new(new ApplicationIdentity("Teams.exe"), new KeyboardLayout("de-DE")),
        };

        _repository.GetAllAsync(default)
            .ReturnsForAnyArgs(Task.FromResult<IReadOnlyList<LanguagePreference>>(expected));

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WhenRepositoryThrows_ReturnsEmpty()
    {
        _repository.GetAllAsync(default)
            .ReturnsForAnyArgs(Task.FromException<IReadOnlyList<LanguagePreference>>(new InvalidOperationException()));

        var result = await _service.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetForApplicationAsync_ReturnsFromRepository()
    {
        var app = new ApplicationIdentity("Code.exe");
        var expected = new LanguagePreference(app, new KeyboardLayout("en-US"));

        _repository.GetForApplicationAsync(app, default)
            .ReturnsForAnyArgs(Task.FromResult<LanguagePreference?>(expected));

        var result = await _service.GetForApplicationAsync(app);

        result.Should().NotBeNull();
        result!.Layout.LanguageTag.Should().Be("en-US");
    }

    [Fact]
    public async Task GetForApplicationAsync_WhenRepositoryThrows_ReturnsNull()
    {
        var app = new ApplicationIdentity("Code.exe");

        _repository.GetForApplicationAsync(app, default)
            .ReturnsForAnyArgs(Task.FromException<LanguagePreference?>(new InvalidOperationException()));

        var result = await _service.GetForApplicationAsync(app);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetPreferenceAsync_CallsRepositorySave()
    {
        var app = new ApplicationIdentity("Code.exe");
        var layout = new KeyboardLayout("en-US");

        await _service.SetPreferenceAsync(app, layout);

        await _repository.Received(1).SaveAsync(
            Arg.Is<LanguagePreference>(p =>
                p.Application.Equals(app) && p.Layout.Equals(layout)),
            default);
    }

    [Fact]
    public async Task DeletePreferenceAsync_CallsRepositoryDelete()
    {
        var app = new ApplicationIdentity("Code.exe");

        await _service.DeletePreferenceAsync(app);

        await _repository.Received(1).DeleteAsync(app, default);
    }

    [Fact]
    public async Task ExportAsync_ReturnsJsonFromRepository()
    {
        var expectedJson = "{\"version\":1,\"preferences\":[]}";
        _repository.ExportAsync(default)
            .ReturnsForAnyArgs(Task.FromResult(expectedJson));

        var result = await _service.ExportAsync();

        result.Should().Be(expectedJson);
    }

    [Fact]
    public async Task ExportAsync_WhenRepositoryThrows_ReturnsEmptyJson()
    {
        _repository.ExportAsync(default)
            .ReturnsForAnyArgs(Task.FromException<string>(new InvalidOperationException()));

        var result = await _service.ExportAsync();

        result.Should().Be("{}");
    }

    [Fact]
    public async Task ImportAsync_CallsRepositoryImport()
    {
        var json = "{\"version\":1,\"preferences\":[]}";

        await _service.ImportAsync(json);

        await _repository.Received(1).ImportAsync(json, default);
    }
}
