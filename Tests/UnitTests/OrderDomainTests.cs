using Domain.Orders;

namespace Tests.UnitTests;

/// <summary>
/// Unit tests for Domain models and enums
/// </summary>
public class OrderDomainTests
{
    [Fact]
    public void OrderStatus_ShouldHaveValidValues()
    {
        // Arrange & Act & Assert
        OrderStatus.Pending.Should().Be(OrderStatus.Pending);
        OrderStatus.Processing.Should().Be(OrderStatus.Processing);
        OrderStatus.ReadyForPickup.Should().Be(OrderStatus.ReadyForPickup);
        OrderStatus.Completed.Should().Be(OrderStatus.Completed);
        OrderStatus.Cancelled.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Order_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var order = new Order();

        // Assert
        order.Id.Should().Be(Guid.Empty);
        order.UserId.Should().Be(Guid.Empty);
        order.TotalAmount.Should().Be(0);
        order.Status.Should().Be(OrderStatus.Pending);
        order.Items.Should().BeEmpty();
        order.StatusHistory.Should().BeEmpty();
    }

    [Fact]
    public void Order_ShouldAddItems()
    {
        // Arrange
        var order = new Order { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };
        var item = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ProductId = Guid.NewGuid(),
            Quantity = 2,
            PriceAtPurchase = 99.99m
        };

        // Act
        order.Items.Add(item);

        // Assert
        order.Items.Should().HaveCount(1);
        order.Items.First().Should().Be(item);
    }

    [Fact]
    public void OrderStatusHistory_ShouldTrackStatusChanges()
    {
        // Arrange
        var order = new Order { Id = Guid.NewGuid() };
        var statusChange = new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Status = OrderStatus.Processing,
            ChangedAt = DateTime.UtcNow
        };

        // Act
        order.StatusHistory.Add(statusChange);
        order.Status = OrderStatus.Processing;

        // Assert
        order.StatusHistory.Should().HaveCount(1);
        order.Status.Should().Be(OrderStatus.Processing);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void OrderStatus_ShouldHaveValidEnumValues(int statusValue)
    {
        // Arrange & Act
        var status = (OrderStatus)statusValue;

        // Assert
        Enum.IsDefined(typeof(OrderStatus), status).Should().BeTrue();
    }
}
