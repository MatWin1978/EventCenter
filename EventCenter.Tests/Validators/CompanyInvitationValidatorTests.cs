using EventCenter.Web.Models;
using EventCenter.Web.Validators;
using Xunit;

namespace EventCenter.Tests.Validators;

public class CompanyInvitationValidatorTests
{
    private readonly CompanyInvitationValidator _validator;

    public CompanyInvitationValidatorTests()
    {
        _validator = new CompanyInvitationValidator();
    }

    [Fact]
    public void Validate_ValidModel_Passes()
    {
        // Arrange
        var model = new CompanyInvitationFormModel
        {
            EventId = 1,
            CompanyName = "Test Company GmbH",
            ContactEmail = "contact@company.com",
            ContactPhone = "+49 123 456789",
            PercentageDiscount = 10,
            PersonalMessage = "We look forward to seeing you!",
            AgendaItemPrices = new List<CompanyAgendaItemPriceModel>
            {
                new() { AgendaItemId = 1, BasePrice = 100m, ManualOverride = 85m }
            }
        };

        // Act
        var result = _validator.Validate(model);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_EmptyCompanyName_Fails()
    {
        // Arrange
        var model = new CompanyInvitationFormModel
        {
            EventId = 1,
            CompanyName = "",
            ContactEmail = "contact@company.com"
        };

        // Act
        var result = _validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Firmenname ist erforderlich");
    }

    [Fact]
    public void Validate_InvalidEmail_Fails()
    {
        // Arrange
        var model = new CompanyInvitationFormModel
        {
            EventId = 1,
            CompanyName = "Test Company",
            ContactEmail = "not-an-email"
        };

        // Act
        var result = _validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Ungültige E-Mail-Adresse");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(150)]
    public void Validate_InvalidPercentageDiscount_Fails(decimal invalidDiscount)
    {
        // Arrange
        var model = new CompanyInvitationFormModel
        {
            EventId = 1,
            CompanyName = "Test Company",
            ContactEmail = "contact@company.com",
            PercentageDiscount = invalidDiscount
        };

        // Act
        var result = _validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Rabatt muss zwischen"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_ValidPercentageDiscount_Passes(decimal validDiscount)
    {
        // Arrange
        var model = new CompanyInvitationFormModel
        {
            EventId = 1,
            CompanyName = "Test Company",
            ContactEmail = "contact@company.com",
            PercentageDiscount = validDiscount
        };

        // Act
        var result = _validator.Validate(model);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NegativeManualOverride_Fails()
    {
        // Arrange
        var model = new CompanyInvitationFormModel
        {
            EventId = 1,
            CompanyName = "Test Company",
            ContactEmail = "contact@company.com",
            AgendaItemPrices = new List<CompanyAgendaItemPriceModel>
            {
                new() { AgendaItemId = 1, BasePrice = 100m, ManualOverride = -5m }
            }
        };

        // Act
        var result = _validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Preis darf nicht negativ sein");
    }

    [Fact]
    public void Validate_NullPercentageDiscount_Passes()
    {
        // Arrange
        var model = new CompanyInvitationFormModel
        {
            EventId = 1,
            CompanyName = "Test Company",
            ContactEmail = "contact@company.com",
            PercentageDiscount = null
        };

        // Act
        var result = _validator.Validate(model);

        // Assert
        Assert.True(result.IsValid);
    }
}
