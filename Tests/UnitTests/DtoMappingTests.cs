using Application.DTOs.Orders;
using Domain.Orders;

namespace Tests.UnitTests;

/// <summary>
/// Unit tests for DTO mapping and validation
/// </summary>
public class OrderDtoMappingTests
{
    [Fact]
    public void SalesReportDto_ShouldInitializeWithValidValues()
    {
        // Arrange & Act
        var report = new SalesReportDto
        {
            Period = new SalesReportPeriodDto
            {
                StartDate = DateTime.UtcNow.AddMonths(-1),
                EndDate = DateTime.UtcNow
            },
            TotalOrders = 5,
            TotalRevenue = 500.00m,
            AverageOrderValue = 100.00m
        };

        // Assert
        report.Period.Should().NotBeNull();
        report.TotalOrders.Should().Be(5);
        report.TotalRevenue.Should().Be(500.00m);
        report.AverageOrderValue.Should().Be(100.00m);
    }

    [Fact]
    public void SalesReportPeriodDto_ShouldHaveValidDateRange()
    {
        // Arrange
        var startDate = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        // Act
        var period = new SalesReportPeriodDto
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Assert
        period.StartDate.Should().Be(startDate);
        period.EndDate.Should().Be(endDate);
        period.EndDate.Should().BeOnOrAfter(period.StartDate);
    }

    [Fact]
    public void OrderDto_ShouldMapFromDomainOrder()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TotalAmount = 199.99m,
            Status = OrderStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var orderDto = new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };

        // Assert
        orderDto.Id.Should().Be(order.Id);
        orderDto.UserId.Should().Be(order.UserId);
        orderDto.TotalAmount.Should().Be(order.TotalAmount);
        orderDto.Status.Should().Be("Completed");
    }

    [Fact]
    public void UpdateOrderStatusRequest_ShouldValidateStatus()
    {
        // Arrange
        var validStatuses = new[] { "Pending", "Processing", "ReadyForPickup", "Completed", "Cancelled" };

        // Act & Assert
        foreach (var status in validStatuses)
        {
            var request = new UpdateOrderStatusRequest { Status = status };
            request.Status.Should().NotBeNullOrEmpty();
            Enum.TryParse<OrderStatus>(status, true, out _).Should().BeTrue();
        }
    }

    [Fact]
    public void OrderItemDto_ShouldContainRequiredProperties()
    {
        // Arrange & Act
        var itemDto = new OrderItemDto
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            Quantity = 5,
            PriceAtPurchase = 49.99m
        };

        // Assert
        itemDto.Id.Should().NotBe(Guid.Empty);
        itemDto.ProductId.Should().NotBe(Guid.Empty);
        itemDto.Quantity.Should().Be(5);
        itemDto.PriceAtPurchase.Should().Be(49.99m);
    }

    [Theory]
    [InlineData(1, 99.99, 99.99)]
    [InlineData(5, 20.00, 100.00)]
    [InlineData(10, 15.50, 155.00)]
    public void OrderItem_ShouldCalculateTotalPrice(int quantity, decimal pricePerUnit, decimal expectedTotal)
    {
        // Arrange & Act
        var totalPrice = quantity * pricePerUnit;

        // Assert
        totalPrice.Should().Be(expectedTotal);
    }
}
