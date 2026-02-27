using EventCenter.Web.Models;
using EventCenter.Web.Validators;
using Xunit;

namespace EventCenter.Tests.Validators;

public class GuestRegistrationValidatorTests
{
    private readonly GuestRegistrationValidator _validator = new();

    [Fact]
    public void Validate_ValidModel_NoErrors()
    {
        // Arrange
        var model = new GuestRegistrationFormModel
        {
            Salutation = "Herr",
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "max@example.com",
            RelationshipType = "Ehepartner",
            SelectedAgendaItemIds = new List<int> { 1, 2 }
        };

        // Act
        var result = _validator.Validate(model);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_EmptySalutation_HasError()
    {
        // Arrange
        var model = new GuestRegistrationFormModel
        {
            Salutation = "",
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "max@example.com",
            RelationshipType = "Ehepartner",
            SelectedAgendaItemIds = new List<int> { 1 }
        };

        // Act
        var result = _validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Salutation");
    }

    [Fact]
    public void Validate_InvalidSalutation_HasError()
    {
        // Arrange
        var model = new GuestRegistrationFormModel
        {
            Salutation = "Mr", // Not valid - must be "Herr", "Frau", or "Divers"
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "max@example.com",
            RelationshipType = "Ehepartner",
            SelectedAgendaItemIds = new List<int> { 1 }
        };

        // Act
        var result = _validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Salutation" && e.ErrorMessage.Contains("Ungültige"));
    }

    [Fact]
    public void Validate_EmptyFirstName_HasError()
    {
        // Arrange
        var model = new GuestRegistrationFormModel
        {
            Salutation = "Herr",
            FirstName = "",
            LastName = "Mustermann",
            Email = "max@example.com",
            RelationshipType = "Ehepartner",
            SelectedAgendaItemIds = new List<int> { 1 }
        };

        // Act
        var result = _validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void Validate_EmptyLastName_HasError()
    {
        // Arrange
        var model = new GuestRegistrationFormModel
        {
            Salutation = "Herr",
            FirstName = "Max",
            LastName = "",
            Email = "max@example.com",
            RelationshipType = "Ehepartner",
            SelectedAgendaItemIds = new List<int> { 1 }
        };

        // Act
        var result = _validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "LastName");
    }

    [Fact]
    public void Validate_EmptyEmail_HasError()
    {
        // Arrange
        var model = new GuestRegistrationFormModel
        {
            Salutation = "Herr",
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "",
            RelationshipType = "Ehepartner",
            SelectedAgendaItemIds = new List<int> { 1 }
        };

        // Act
        var result = _validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_InvalidEmail_HasError()
    {
        // Arrange
        var model = new GuestRegistrationFormModel
        {
            Salutation = "Herr",
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "not-an-email",
            RelationshipType = "Ehepartner",
            SelectedAgendaItemIds = new List<int> { 1 }
        };

        // Act
        var result = _validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_EmptyRelationshipType_HasError()
    {
        // Arrange
        var model = new GuestRegistrationFormModel
        {
            Salutation = "Herr",
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "max@example.com",
            RelationshipType = "",
            SelectedAgendaItemIds = new List<int> { 1 }
        };

        // Act
        var result = _validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "RelationshipType");
    }

    [Fact]
    public void Validate_EmptyAgendaItems_HasError()
    {
        // Arrange
        var model = new GuestRegistrationFormModel
        {
            Salutation = "Herr",
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "max@example.com",
            RelationshipType = "Ehepartner",
            SelectedAgendaItemIds = new List<int>() // Empty list
        };

        // Act
        var result = _validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SelectedAgendaItemIds");
    }
}
