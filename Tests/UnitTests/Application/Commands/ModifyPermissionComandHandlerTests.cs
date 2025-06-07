using Application.Commands;
using Application.Dtos;
using Application.Interfaces;
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

namespace Tests.UnitTests.Application.Commands;

public class ModifyPermissionCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IElasticSearchService> _mockElasticSearchService;
    private readonly Mock<IKafkaProducer> _mockKafkaProducer;
    private readonly Mock<ILogger<ModifyPermissionCommandHandler>> _mockLogger;
    private readonly Mock<IPermissionRepository> _mockPermissionRepository;
    private readonly ModifyPermissionCommandHandler _handler;

    public ModifyPermissionCommandHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockElasticSearchService = new Mock<IElasticSearchService>();
        _mockKafkaProducer = new Mock<IKafkaProducer>();
        _mockLogger = new Mock<ILogger<ModifyPermissionCommandHandler>>();
        _mockPermissionRepository = new Mock<IPermissionRepository>();

        _mockUnitOfWork.Setup(x => x.PermissionRepository).Returns(_mockPermissionRepository.Object);

        _handler = new ModifyPermissionCommandHandler(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockElasticSearchService.Object,
            _mockKafkaProducer.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldUpdatePermissionSuccessfully()
    {
        // Arrange
        var command = TestDataBuilders.CreateValidModifyCommand();
        var existingPermission = TestDataBuilders.CreatePermissionWithType();
        existingPermission.Id = command.Id;
        var permissionDto = TestDataBuilders.PermissionDtoFaker.Generate();

        _mockPermissionRepository.Setup(x => x.GetById(command.Id, true))
            .ReturnsAsync(existingPermission);
        _mockPermissionRepository.Setup(x => x.Update(It.IsAny<Permission>()))
            .ReturnsAsync(existingPermission);
        _mockUnitOfWork.Setup(x => x.SaveAsync())
            .ReturnsAsync(1);
        _mockPermissionRepository.Setup(x => x.GetById(command.Id, false, "PermissionType"))
            .ReturnsAsync(existingPermission);
        _mockMapper.Setup(x => x.Map<PermissionDto>(existingPermission))
            .Returns(permissionDto);
        _mockElasticSearchService.Setup(x => x.UpdatePermissionAsync(permissionDto))
            .Returns(Task.CompletedTask);
        _mockKafkaProducer.Setup(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        existingPermission.EmployeeForename.Should().Be(command.EmployeeForename);
        existingPermission.EmployeeSurname.Should().Be(command.EmployeeSurname);
        existingPermission.PermissionTypeId.Should().Be(command.PermissionTypeId);
        existingPermission.PermissionDate.Should().Be(command.PermissionDate);

        _mockPermissionRepository.Verify(x => x.Update(existingPermission), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveAsync(), Times.Once);
        _mockElasticSearchService.Verify(x => x.UpdatePermissionAsync(permissionDto), Times.Once);
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.Is<KafkaMessageDto>(m => m.NameOperation == OperationNames.Modify)), Times.Once);
    }

    [Fact]
    public async Task Handle_PermissionNotFound_ShouldThrowValidationException()
    {
        // Arrange
        var command = TestDataBuilders.CreateValidModifyCommand();

        _mockPermissionRepository.Setup(x => x.GetById(command.Id, true))
            .ReturnsAsync((Permission?)null);

        // Act
        var ex = await Record.ExceptionAsync(async () => await _handler.Handle(command, CancellationToken.None));

        // Assert
        ex.Should().NotBeNull();
        ex.Should().BeOfType<ValidationException>();
        ex.Message.Should().Contain($"Permission with ID {command.Id} not found");

        _mockPermissionRepository.Verify(x => x.Update(It.IsAny<Permission>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveAsync(), Times.Never);
        _mockElasticSearchService.Verify(x => x.UpdatePermissionAsync(It.IsAny<PermissionDto>()), Times.Never);
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DatabaseException_ShouldThrowException()
    {
        // Arrange
        var command = TestDataBuilders.CreateValidModifyCommand();
        var existingPermission = TestDataBuilders.CreatePermissionWithType();
        existingPermission.Id = command.Id;

        _mockPermissionRepository.Setup(x => x.GetById(command.Id, true))
            .ReturnsAsync(existingPermission);
        _mockPermissionRepository.Setup(x => x.Update(It.IsAny<Permission>()))
            .ReturnsAsync(existingPermission);
        _mockUnitOfWork.Setup(x => x.SaveAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var ex = await Record.ExceptionAsync(async () => await _handler.Handle(command, CancellationToken.None));

        // Assert
        ex.Should().NotBeNull();
        ex.Should().BeOfType<Exception>();
        _mockElasticSearchService.Verify(x => x.UpdatePermissionAsync(It.IsAny<PermissionDto>()), Times.Never);
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ElasticSearchException_ShouldThrowException()
    {
        // Arrange
        var command = TestDataBuilders.CreateValidModifyCommand();
        var existingPermission = TestDataBuilders.CreatePermissionWithType();
        existingPermission.Id = command.Id;
        var permissionDto = TestDataBuilders.PermissionDtoFaker.Generate();

        _mockPermissionRepository.Setup(x => x.GetById(command.Id, true))
            .ReturnsAsync(existingPermission);
        _mockPermissionRepository.Setup(x => x.Update(It.IsAny<Permission>()))
            .ReturnsAsync(existingPermission);
        _mockUnitOfWork.Setup(x => x.SaveAsync())
            .ReturnsAsync(1);
        _mockPermissionRepository.Setup(x => x.GetById(command.Id, false, "PermissionType"))
            .ReturnsAsync(existingPermission);
        _mockMapper.Setup(x => x.Map<PermissionDto>(existingPermission))
            .Returns(permissionDto);
        _mockElasticSearchService.Setup(x => x.UpdatePermissionAsync(permissionDto))
            .ThrowsAsync(new Exception("ElasticSearch error"));

        // Act
        var ex = await Record.ExceptionAsync(async () => await _handler.Handle(command, CancellationToken.None));

        // Assert
        ex.Should().NotBeNull();
        ex.Should().BeOfType<Exception>();
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_KafkaFails_ShouldStillCompleteSuccessfully()
    {
        // Arrange
        var command = TestDataBuilders.CreateValidModifyCommand();
        var existingPermission = TestDataBuilders.CreatePermissionWithType();
        existingPermission.Id = command.Id;
        var permissionDto = TestDataBuilders.PermissionDtoFaker.Generate();

        _mockPermissionRepository.Setup(x => x.GetById(command.Id, true))
            .ReturnsAsync(existingPermission);
        _mockPermissionRepository.Setup(x => x.Update(It.IsAny<Permission>()))
            .ReturnsAsync(existingPermission);
        _mockUnitOfWork.Setup(x => x.SaveAsync())
            .ReturnsAsync(1);
        _mockPermissionRepository.Setup(x => x.GetById(command.Id, false, "PermissionType"))
            .ReturnsAsync(existingPermission);
        _mockMapper.Setup(x => x.Map<PermissionDto>(existingPermission))
            .Returns(permissionDto);
        _mockElasticSearchService.Setup(x => x.UpdatePermissionAsync(permissionDto))
            .Returns(Task.CompletedTask);
        _mockKafkaProducer.Setup(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()))
            .ReturnsAsync(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdatedPermissionWithTypeNotFound_ShouldCompleteWithoutElasticSearchUpdate()
    {
        // Arrange
        var command = TestDataBuilders.CreateValidModifyCommand();
        var existingPermission = TestDataBuilders.CreatePermissionWithType();
        existingPermission.Id = command.Id;

        _mockPermissionRepository.Setup(x => x.GetById(command.Id, true))
            .ReturnsAsync(existingPermission);
        _mockPermissionRepository.Setup(x => x.Update(It.IsAny<Permission>()))
            .ReturnsAsync(existingPermission);
        _mockUnitOfWork.Setup(x => x.SaveAsync())
            .ReturnsAsync(1);
        _mockPermissionRepository.Setup(x => x.GetById(command.Id, false, "PermissionType"))
            .ReturnsAsync((Permission?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockPermissionRepository.Verify(x => x.Update(existingPermission), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveAsync(), Times.Once);
        _mockElasticSearchService.Verify(x => x.UpdatePermissionAsync(It.IsAny<PermissionDto>()), Times.Never);
        _mockKafkaProducer.Verify(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()), Times.Never);
    }

    [Theory]
    [InlineData("UpdatedFirstName", "UpdatedLastName", 3, "2025-12-25")]
    [InlineData("John", "Smith", 1, "2026-01-01")]
    public async Task Handle_ValidCommand_ShouldUpdateAllFields(string firstName, string lastName, int typeId, string dateString)
    {
        // Arrange
        var permissionDate = DateTime.Parse(dateString);
        var command = new ModifyPermissionCommand
        {
            Id = 5,
            EmployeeForename = firstName,
            EmployeeSurname = lastName,
            PermissionTypeId = typeId,
            PermissionDate = permissionDate
        };

        var existingPermission = TestDataBuilders.CreatePermissionWithType();
        existingPermission.Id = command.Id;
        var permissionDto = TestDataBuilders.PermissionDtoFaker.Generate();

        _mockPermissionRepository.Setup(x => x.GetById(command.Id, true))
            .ReturnsAsync(existingPermission);
        _mockPermissionRepository.Setup(x => x.Update(It.IsAny<Permission>()))
            .ReturnsAsync(existingPermission);
        _mockUnitOfWork.Setup(x => x.SaveAsync())
            .ReturnsAsync(1);
        _mockPermissionRepository.Setup(x => x.GetById(command.Id, false, "PermissionType"))
            .ReturnsAsync(existingPermission);
        _mockMapper.Setup(x => x.Map<PermissionDto>(existingPermission))
            .Returns(permissionDto);
        _mockElasticSearchService.Setup(x => x.UpdatePermissionAsync(permissionDto))
            .Returns(Task.CompletedTask);
        _mockKafkaProducer.Setup(x => x.ProduceAsync(It.IsAny<KafkaMessageDto>()))
            .ReturnsAsync(true);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        existingPermission.EmployeeForename.Should().Be(firstName);
        existingPermission.EmployeeSurname.Should().Be(lastName);
        existingPermission.PermissionTypeId.Should().Be(typeId);
        existingPermission.PermissionDate.Should().Be(permissionDate);
    }
}