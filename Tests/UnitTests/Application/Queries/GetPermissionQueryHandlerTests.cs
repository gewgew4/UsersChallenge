using Application.Dtos;
using Application.Interfaces;
using Application.Queries;
using AutoMapper;
using Common.Constants;
using Common.Dtos;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Helpers;
using Xunit;

namespace Tests.UnitTests.Application.Queries;

public class GetPermissionsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IKafkaProducer> _mockKafkaProducer;
    private readonly Mock<ILogger<GetPermissionsQueryHandler>> _mockLogger;
    private readonly Mock<IPermissionRepository> _mockPermissionRepository;
    private readonly GetPermissionsQueryHandler _handler;

    public GetPermissionsQueryHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockKafkaProducer = new Mock<IKafkaProducer>();
        _mockLogger = new Mock<ILogger<GetPermissionsQueryHandler>>();
        _mockPermissionRepository = new Mock<IPermissionRepository>();

        _mockUnitOfWork.Setup(x => x.PermissionRepository).Returns(_mockPermissionRepository.Object);

        _handler = new GetPermissionsQueryHandler(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockKafkaProducer.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ShouldReturnAllPermissions()
    {
        // Arrange
        var query = new GetPermissionsQuery();
        var permissions = TestDataBuilders.CreateMultiplePermissions(3).ToList();
        var permissionDtos = TestDataBuilders.PermissionDtoFaker.Generate(3);

        _mockPermissionRepository.Setup(x => x.GetAll(false, "PermissionType"))
            .Returns(permissions);
        _mockMapper.Setup(x => x.Map<IEnumerable<PermissionDto>>(permissions))
            .Returns(permissionDtos);
        _mockKafkaProducer.Setup(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(permissionDtos);

        _mockPermissionRepository.Verify(x => x.GetAll(false, "PermissionType"), Times.Once);
        _mockMapper.Verify(x => x.Map<IEnumerable<PermissionDto>>(permissions), Times.Once);
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.Is<KafkaMessageDto>(m => m.NameOperation == OperationNames.Get)), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ShouldReturnEmptyCollection()
    {
        // Arrange
        var query = new GetPermissionsQuery();
        var emptyPermissions = new List<Permission>();
        var emptyPermissionDtos = new List<PermissionDto>();

        _mockPermissionRepository.Setup(x => x.GetAll(false, "PermissionType"))
            .Returns(emptyPermissions);
        _mockMapper.Setup(x => x.Map<IEnumerable<PermissionDto>>(emptyPermissions))
            .Returns(emptyPermissionDtos);
        _mockKafkaProducer.Setup(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _mockPermissionRepository.Verify(x => x.GetAll(false, "PermissionType"), Times.Once);
        _mockMapper.Verify(x => x.Map<IEnumerable<PermissionDto>>(emptyPermissions), Times.Once);
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.Is<KafkaMessageDto>(m => m.NameOperation == OperationNames.Get)), Times.Once);
    }

    [Fact]
    public async Task Handle_KafkaFails_ShouldStillReturnPermissions()
    {
        // Arrange
        var query = new GetPermissionsQuery();
        var permissions = TestDataBuilders.CreateMultiplePermissions(2).ToList();
        var permissionDtos = TestDataBuilders.PermissionDtoFaker.Generate(2);

        _mockPermissionRepository.Setup(x => x.GetAll(false, "PermissionType"))
            .Returns(permissions);
        _mockMapper.Setup(x => x.Map<IEnumerable<PermissionDto>>(permissions))
            .Returns(permissionDtos);
        _mockKafkaProducer.Setup(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(permissionDtos);

        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RepositoryException_ShouldThrowException()
    {
        // Arrange
        var query = new GetPermissionsQuery();

        _mockPermissionRepository.Setup(x => x.GetAll(false, "PermissionType"))
            .Throws(new Exception("Repository error"));

        // Act
        var ex = await Record.ExceptionAsync(async () => await _handler.Handle(query, CancellationToken.None));

        // Assert
        ex.Should().NotBeNull();
        ex.Should().BeOfType<Exception>();

        _mockMapper.Verify(x => x.Map<IEnumerable<PermissionDto>>(It.IsAny<IEnumerable<Permission>>()), Times.Never);
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MapperException_ShouldThrowException()
    {
        // Arrange
        var query = new GetPermissionsQuery();
        var permissions = TestDataBuilders.CreateMultiplePermissions(3).ToList();

        _mockPermissionRepository.Setup(x => x.GetAll(false, "PermissionType"))
            .Returns(permissions);
        _mockMapper.Setup(x => x.Map<IEnumerable<PermissionDto>>(permissions))
            .Throws(new Exception("Mapping error"));

        // Act
        var ex = await Record.ExceptionAsync(async () => await _handler.Handle(query, CancellationToken.None));

        // Assert
        ex.Should().NotBeNull();
        ex.Should().BeOfType<Exception>();

        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidQuery_ShouldCallRepositoryWithCorrectParameters()
    {
        // Arrange
        var query = new GetPermissionsQuery();
        var permissions = TestDataBuilders.CreateMultiplePermissions(1).ToList();
        var permissionDtos = TestDataBuilders.PermissionDtoFaker.Generate(1);

        _mockPermissionRepository.Setup(x => x.GetAll(false, "PermissionType"))
            .Returns(permissions);
        _mockMapper.Setup(x => x.Map<IEnumerable<PermissionDto>>(permissions))
            .Returns(permissionDtos);
        _mockKafkaProducer.Setup(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockPermissionRepository.Verify(x => x.GetAll(
            It.Is<bool>(tracking => tracking == false),
            It.Is<string[]>(includes => includes.Length == 1 && includes[0] == "PermissionType")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidQuery_ShouldGenerateKafkaMessageWithCorrectOperation()
    {
        // Arrange
        var query = new GetPermissionsQuery();
        var permissions = TestDataBuilders.CreateMultiplePermissions(1).ToList();
        var permissionDtos = TestDataBuilders.PermissionDtoFaker.Generate(1);

        _mockPermissionRepository.Setup(x => x.GetAll(false, "PermissionType"))
            .Returns(permissions);
        _mockMapper.Setup(x => x.Map<IEnumerable<PermissionDto>>(permissions))
            .Returns(permissionDtos);
        _mockKafkaProducer.Setup(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.Is<KafkaMessageDto>(m =>
            m.NameOperation == OperationNames.Get &&
            m.Id != Guid.Empty)),
            Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Handle_VariousPermissionCounts_ShouldReturnCorrectCount(int permissionCount)
    {
        // Arrange
        var query = new GetPermissionsQuery();
        var permissions = TestDataBuilders.CreateMultiplePermissions(permissionCount).ToList();
        var permissionDtos = TestDataBuilders.PermissionDtoFaker.Generate(permissionCount);

        _mockPermissionRepository.Setup(x => x.GetAll(false, "PermissionType"))
            .Returns(permissions);
        _mockMapper.Setup(x => x.Map<IEnumerable<PermissionDto>>(permissions))
            .Returns(permissionDtos);
        _mockKafkaProducer.Setup(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(permissionCount);
    }
}