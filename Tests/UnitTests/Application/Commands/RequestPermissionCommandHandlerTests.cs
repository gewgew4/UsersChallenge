using Application.Commands;
using Application.Dtos;
using Application.Interfaces;
using AutoMapper;
using Common.Constants;
using Common.Dtos;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Helpers;
using Xunit;

namespace Tests.UnitTests.Application.Commands;

public class RequestPermissionCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IElasticSearchService> _mockElasticSearchService;
    private readonly Mock<IKafkaProducer> _mockKafkaProducer;
    private readonly Mock<ILogger<RequestPermissionCommandHandler>> _mockLogger;
    private readonly Mock<IPermissionRepository> _mockPermissionRepository;
    private readonly RequestPermissionCommandHandler _handler;

    public RequestPermissionCommandHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockElasticSearchService = new Mock<IElasticSearchService>();
        _mockKafkaProducer = new Mock<IKafkaProducer>();
        _mockLogger = new Mock<ILogger<RequestPermissionCommandHandler>>();
        _mockPermissionRepository = new Mock<IPermissionRepository>();

        _mockUnitOfWork.Setup(x => x.PermissionRepository).Returns(_mockPermissionRepository.Object);

        _handler = new RequestPermissionCommandHandler(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockElasticSearchService.Object,
            _mockKafkaProducer.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreatePermissionAndReturnId()
    {
        // Arrange
        var command = TestDataBuilders.CreateValidRequestCommand();
        var permission = TestDataBuilders.CreatePermissionWithType();
        var permissionDto = TestDataBuilders.PermissionDtoFaker.Generate();

        _mockMapper.Setup(x => x.Map<Permission>(command))
            .Returns(permission);

        _mockPermissionRepository.Setup(x => x.Add(It.IsAny<Permission>()))
            .ReturnsAsync(permission);

        _mockUnitOfWork.Setup(x => x.SaveAsync())
            .ReturnsAsync(1);

        _mockPermissionRepository.Setup(x => x.GetById(permission.Id, false, "PermissionType"))
            .ReturnsAsync(permission);

        _mockMapper.Setup(x => x.Map<PermissionDto>(permission))
            .Returns(permissionDto);

        _mockElasticSearchService.Setup(x => x.IndexPermissionAsync(It.IsAny<PermissionDto>()))
            .Returns(Task.CompletedTask);

        _mockKafkaProducer.Setup(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(permission.Id);

        _mockPermissionRepository.Verify(x => x.Add(It.IsAny<Permission>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveAsync(), Times.Once);
        _mockElasticSearchService.Verify(x => x.IndexPermissionAsync(It.IsAny<PermissionDto>()), Times.Once);
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.Is<KafkaMessageDto>(m => m.NameOperation == OperationNames.Request)), Times.Once);
    }

    [Fact]
    public async Task Handle_KafkaFails_ShouldStillCompleteSuccessfully()
    {
        // Arrange
        var command = TestDataBuilders.CreateValidRequestCommand();
        var permission = TestDataBuilders.CreatePermissionWithType();
        var permissionDto = TestDataBuilders.PermissionDtoFaker.Generate();

        _mockMapper.Setup(x => x.Map<Permission>(command)).Returns(permission);
        _mockPermissionRepository.Setup(x => x.Add(It.IsAny<Permission>())).ReturnsAsync(permission);
        _mockUnitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);
        _mockPermissionRepository.Setup(x => x.GetById(permission.Id, false, "PermissionType")).ReturnsAsync(permission);
        _mockMapper.Setup(x => x.Map<PermissionDto>(permission)).Returns(permissionDto);
        _mockElasticSearchService.Setup(x => x.IndexPermissionAsync(It.IsAny<PermissionDto>())).Returns(Task.CompletedTask);
        _mockKafkaProducer.Setup(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>())).ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(permission.Id);
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PermissionWithTypeNotFound_ShouldStillReturnId()
    {
        // Arrange
        var command = TestDataBuilders.CreateValidRequestCommand();
        var permission = TestDataBuilders.CreatePermissionWithType();

        _mockMapper.Setup(x => x.Map<Permission>(command)).Returns(permission);
        _mockPermissionRepository.Setup(x => x.Add(It.IsAny<Permission>())).ReturnsAsync(permission);
        _mockUnitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);
        _mockPermissionRepository.Setup(x => x.GetById(permission.Id, false, "PermissionType")).ReturnsAsync((Permission?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(permission.Id);
        _mockElasticSearchService.Verify(x => x.IndexPermissionAsync(It.IsAny<PermissionDto>()), Times.Never);
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DatabaseException_ShouldThrowException()
    {
        // Arrange
        var command = TestDataBuilders.CreateValidRequestCommand();
        var permission = TestDataBuilders.CreatePermissionWithType();

        _mockMapper.Setup(x => x.Map<Permission>(command)).Returns(permission);
        _mockPermissionRepository.Setup(x => x.Add(It.IsAny<Permission>())).ReturnsAsync(permission);

        // Database save fails
        _mockUnitOfWork.Setup(x => x.SaveAsync()).ThrowsAsync(new Exception("Database error"));

        // Act
        var ex = await Record.ExceptionAsync(async () => await _handler.Handle(command, CancellationToken.None));

        // Assert
        ex.Should().NotBeNull();
        ex.Should().BeOfType<Exception>();
        _mockElasticSearchService.Verify(x => x.IndexPermissionAsync(It.IsAny<PermissionDto>()), Times.Never);
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ElasticSearchException_ShouldThrowException()
    {
        // Arrange
        var command = TestDataBuilders.CreateValidRequestCommand();
        var permission = TestDataBuilders.CreatePermissionWithType();
        var permissionDto = TestDataBuilders.PermissionDtoFaker.Generate();

        _mockMapper.Setup(x => x.Map<Permission>(command)).Returns(permission);
        _mockPermissionRepository.Setup(x => x.Add(It.IsAny<Permission>())).ReturnsAsync(permission);
        _mockUnitOfWork.Setup(x => x.SaveAsync()).ReturnsAsync(1);
        _mockPermissionRepository.Setup(x => x.GetById(permission.Id, false, "PermissionType")).ReturnsAsync(permission);
        _mockMapper.Setup(x => x.Map<PermissionDto>(permission)).Returns(permissionDto);

        // ElasticSearch fails
        _mockElasticSearchService.Setup(x => x.IndexPermissionAsync(It.IsAny<PermissionDto>()))
            .ThrowsAsync(new Exception("ElasticSearch error"));

        // Act
        var ex = await Record.ExceptionAsync(async () => await _handler.Handle(command, CancellationToken.None));

        // Assert
        ex.Should().NotBeNull();
        ex.Should().BeOfType<Exception>();
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()), Times.Never);
    }
}