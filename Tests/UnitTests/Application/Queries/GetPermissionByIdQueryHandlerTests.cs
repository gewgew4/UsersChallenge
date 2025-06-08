using Application.Dtos;
using Application.Interfaces;
using Application.Queries;
using AutoMapper;
using Common.Constants;
using Common.Dtos;
using Common.Exceptions;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Helpers;
using Xunit;

namespace Tests.UnitTests.Application.Queries;

public class GetPermissionByIdQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IKafkaProducer> _mockKafkaProducer;
    private readonly Mock<ILogger<GetPermissionByIdQueryHandler>> _mockLogger;
    private readonly Mock<IPermissionRepository> _mockPermissionRepository;
    private readonly GetPermissionByIdQueryHandler _handler;

    public GetPermissionByIdQueryHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockKafkaProducer = new Mock<IKafkaProducer>();
        _mockLogger = new Mock<ILogger<GetPermissionByIdQueryHandler>>();
        _mockPermissionRepository = new Mock<IPermissionRepository>();

        _mockUnitOfWork.Setup(x => x.PermissionRepository).Returns(_mockPermissionRepository.Object);

        _handler = new GetPermissionByIdQueryHandler(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockKafkaProducer.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidId_ShouldReturnPermissionDto()
    {
        // Arrange
        var permissionId = 5;
        var query = new GetPermissionByIdQuery(permissionId);
        var permission = TestDataBuilders.CreatePermissionWithType();
        permission.Id = permissionId;
        var permissionDto = TestDataBuilders.PermissionDtoFaker.Generate();

        _mockPermissionRepository.Setup(x => x.GetById(permissionId, false, "PermissionType"))
            .ReturnsAsync(permission);
        _mockMapper.Setup(x => x.Map<PermissionDto>(permission))
            .Returns(permissionDto);
        _mockKafkaProducer.Setup(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(permissionDto);

        _mockPermissionRepository.Verify(x => x.GetById(permissionId, false, "PermissionType"), Times.Once);
        _mockMapper.Verify(x => x.Map<PermissionDto>(permission), Times.Once);
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.Is<KafkaMessageDto>(m => m.NameOperation == OperationNames.Get)), Times.Once);
    }

    [Fact]
    public async Task Handle_PermissionNotFound_ShouldThrowValidationException()
    {
        // Arrange
        var permissionId = 999;
        var query = new GetPermissionByIdQuery(permissionId);

        _mockPermissionRepository.Setup(x => x.GetById(permissionId, false, "PermissionType"))
            .ReturnsAsync((Permission?)null);

        // Act
        var ex = await Record.ExceptionAsync(async () => await _handler.Handle(query, CancellationToken.None));

        // Assert
        ex.Should().NotBeNull();
        ex.Should().BeOfType<ValidationException>();
        ex.Message.Should().Contain($"Permission with ID {permissionId} not found");

        _mockMapper.Verify(x => x.Map<PermissionDto>(It.IsAny<Permission>()), Times.Never);
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_KafkaFails_ShouldStillReturnPermission()
    {
        // Arrange
        var permissionId = 3;
        var query = new GetPermissionByIdQuery(permissionId);
        var permission = TestDataBuilders.CreatePermissionWithType();
        permission.Id = permissionId;
        var permissionDto = TestDataBuilders.PermissionDtoFaker.Generate();

        _mockPermissionRepository.Setup(x => x.GetById(permissionId, false, "PermissionType"))
            .ReturnsAsync(permission);
        _mockMapper.Setup(x => x.Map<PermissionDto>(permission))
            .Returns(permissionDto);
        _mockKafkaProducer.Setup(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(permissionDto);

        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RepositoryException_ShouldThrowException()
    {
        // Arrange
        var permissionId = 1;
        var query = new GetPermissionByIdQuery(permissionId);

        _mockPermissionRepository.Setup(x => x.GetById(permissionId, false, "PermissionType"))
            .ThrowsAsync(new Exception("Repository error"));

        // Act
        var ex = await Record.ExceptionAsync(async () => await _handler.Handle(query, CancellationToken.None));

        // Assert
        ex.Should().NotBeNull();
        ex.Should().BeOfType<Exception>();

        _mockMapper.Verify(x => x.Map<PermissionDto>(It.IsAny<Permission>()), Times.Never);
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MapperException_ShouldThrowException()
    {
        // Arrange
        var permissionId = 2;
        var query = new GetPermissionByIdQuery(permissionId);
        var permission = TestDataBuilders.CreatePermissionWithType();
        permission.Id = permissionId;

        _mockPermissionRepository.Setup(x => x.GetById(permissionId, false, "PermissionType"))
            .ReturnsAsync(permission);
        _mockMapper.Setup(x => x.Map<PermissionDto>(permission))
            .Throws(new Exception("Mapping error"));

        // Act
        var ex = await Record.ExceptionAsync(async () => await _handler.Handle(query, CancellationToken.None));

        // Assert
        ex.Should().NotBeNull();
        ex.Should().BeOfType<Exception>();

        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidId_ShouldCallRepositoryWithCorrectParameters()
    {
        // Arrange
        var permissionId = 7;
        var query = new GetPermissionByIdQuery(permissionId);
        var permission = TestDataBuilders.CreatePermissionWithType();
        permission.Id = permissionId;
        var permissionDto = TestDataBuilders.PermissionDtoFaker.Generate();

        _mockPermissionRepository.Setup(x => x.GetById(permissionId, false, "PermissionType"))
            .ReturnsAsync(permission);
        _mockMapper.Setup(x => x.Map<PermissionDto>(permission))
            .Returns(permissionDto);
        _mockKafkaProducer.Setup(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockPermissionRepository.Verify(x => x.GetById(
            It.Is<int>(id => id == permissionId),
            It.Is<bool>(tracking => tracking == false),
            It.Is<string[]>(includes => includes.Length == 1 && includes[0] == "PermissionType")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidId_ShouldGenerateKafkaMessageWithCorrectOperation()
    {
        // Arrange
        var permissionId = 4;
        var query = new GetPermissionByIdQuery(permissionId);
        var permission = TestDataBuilders.CreatePermissionWithType();
        permission.Id = permissionId;
        var permissionDto = TestDataBuilders.PermissionDtoFaker.Generate();

        _mockPermissionRepository.Setup(x => x.GetById(permissionId, false, "PermissionType"))
            .ReturnsAsync(permission);
        _mockMapper.Setup(x => x.Map<PermissionDto>(permission))
            .Returns(permissionDto);
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

    [Fact]
    public async Task Handle_QueryWithIdSetInConstructor_ShouldUseCorrectId()
    {
        // Arrange
        var permissionId = 15;
        var query = new GetPermissionByIdQuery(permissionId);
        var permission = TestDataBuilders.CreatePermissionWithType();
        permission.Id = permissionId;
        var permissionDto = TestDataBuilders.PermissionDtoFaker.Generate();

        _mockPermissionRepository.Setup(x => x.GetById(permissionId, false, "PermissionType"))
            .ReturnsAsync(permission);
        _mockMapper.Setup(x => x.Map<PermissionDto>(permission))
            .Returns(permissionDto);
        _mockKafkaProducer.Setup(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        query.Id.Should().Be(permissionId);
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(999)]
    public async Task Handle_DifferentValidIds_ShouldReturnPermission(int permissionId)
    {
        // Arrange
        var query = new GetPermissionByIdQuery(permissionId);
        var permission = TestDataBuilders.CreatePermissionWithType();
        permission.Id = permissionId;
        var permissionDto = TestDataBuilders.PermissionDtoFaker.Generate();

        _mockPermissionRepository.Setup(x => x.GetById(permissionId, false, "PermissionType"))
            .ReturnsAsync(permission);
        _mockMapper.Setup(x => x.Map<PermissionDto>(permission))
            .Returns(permissionDto);
        _mockKafkaProducer.Setup(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(permissionDto);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public async Task Handle_InvalidIds_ShouldThrowValidationException(int permissionId)
    {
        // Arrange
        var query = new GetPermissionByIdQuery(permissionId);

        _mockPermissionRepository.Setup(x => x.GetById(permissionId, false, "PermissionType"))
            .ReturnsAsync((Permission?)null);

        // Act
        var ex = await Record.ExceptionAsync(async () => await _handler.Handle(query, CancellationToken.None));

        // Assert
        ex.Should().NotBeNull();
        ex.Should().BeOfType<ValidationException>();
        ex.Message.Should().Contain($"Permission with ID {permissionId} not found");
    }

    [Fact]
    public async Task Handle_ValidId_ShouldRethrowValidationException()
    {
        // Arrange
        var permissionId = 1;
        var query = new GetPermissionByIdQuery(permissionId);

        _mockPermissionRepository.Setup(x => x.GetById(permissionId, false, "PermissionType"))
            .ReturnsAsync((Permission?)null);

        // Act
        var ex = await Record.ExceptionAsync(async () => await _handler.Handle(query, CancellationToken.None));

        // Assert
        ex.Should().NotBeNull();
        ex.Should().BeOfType<ValidationException>();
        ex.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_ValidationException_ShouldNotCallMapperOrKafka()
    {
        // Arrange
        var permissionId = 1;
        var query = new GetPermissionByIdQuery(permissionId);

        _mockPermissionRepository.Setup(x => x.GetById(permissionId, false, "PermissionType"))
            .ReturnsAsync((Permission?)null);

        // Act
        var ex = await Record.ExceptionAsync(async () => await _handler.Handle(query, CancellationToken.None));

        // Assert
        ex.Should().NotBeNull();
        ex.Should().BeOfType<ValidationException>();

        _mockMapper.Verify(x => x.Map<PermissionDto>(It.IsAny<Permission>()), Times.Never);
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()), Times.Never);
    }
}