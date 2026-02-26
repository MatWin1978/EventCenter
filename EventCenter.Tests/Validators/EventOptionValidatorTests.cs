using EventCenter.Web.Domain.Entities;
using EventCenter.Web.Validators;
using Xunit;

namespace EventCenter.Tests.Validators;

public class EventOptionValidatorTests
{
    private readonly EventOptionValidator _validator = new();

    [Fact]
    public void Name_Required()
    {
        // Arrange
        var option = new EventOption
        {
            Name = "",
            Price = 10
        };

        // Act
        var result = _validator.Validate(option);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void Price_NotNegative()
    {
        // Arrange
        var option = new EventOption
        {
            Name = "Test Option",
            Price = -5
        };

        // Act
        var result = _validator.Validate(option);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Price");
    }

    [Fact]
    public void ValidOption_Passes()
    {
        // Arrange
        var option = new EventOption
        {
            Name = "Valid Option",
            Price = 25
        };

        // Act
        var result = _validator.Validate(option);

        // Assert
        Assert.True(result.IsValid);
    }
}
