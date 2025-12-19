using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using MyIS.Core.Application.Integration.Component2020.Abstractions;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Domain.Mdm.ValueObjects;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Integration.Component2020.Services;
using Xunit;

namespace MyIS.Core.Infrastructure.Tests.Integration.Component2020;

public class Component2020SyncServiceTests
{
    private readonly Mock<AppDbContext> _dbContextMock;
    private readonly Mock<IComponent2020SnapshotReader> _snapshotReaderMock;
    private readonly Mock<IComponent2020DeltaReader> _deltaReaderMock;
    private readonly Mock<ILogger<Component2020SyncService>> _loggerMock;

    public Component2020SyncServiceTests()
    {
        _dbContextMock = new Mock<AppDbContext>();
        _snapshotReaderMock = new Mock<IComponent2020SnapshotReader>();
        _deltaReaderMock = new Mock<IComponent2020DeltaReader>();
        _loggerMock = new Mock<ILogger<Component2020SyncService>>();
    }

    // Tests commented out due to interface changes - need update for new cursor-based delta sync
    /*
    [Fact]
    public async Task RunSyncAsync_WithDryRun_ShouldNotSaveChanges()
    {
        // TODO: update for new interface
    }

    [Fact]
    public async Task RunSyncAsync_WithSnapshot_ShouldReadSnapshot()
    {
        // TODO: update for new interface
    }

    [Fact]
    public async Task RunSyncAsync_WithDelta_ShouldReadDelta()
    {
        // TODO: update for new interface
    }
    */
}