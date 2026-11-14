using System.ComponentModel.DataAnnotations;
using Heimevernet.Web.Models.ViewModels.Account;
using Heimevernet.Web.Models.ViewModels.Need;
using Heimevernet.Web.Models.ViewModels.Resource;
using Heimevernet.Web.Models.ViewModels.Settings;

namespace Heimevernet.Web.UnitTests;

// Tester reglene i Models/InputRules.cs slik serveren bruker dem (samme sjekk som ModelState.IsValid gjør).
// [Theory] + [InlineData] kjører samme test én gang per linje, så vi kan teste mange verdier uten å kopiere kode.
public class InputValidationTests
{
    // Kjører alle valideringsattributtene på modellen og returnerer navnene på feltene som feilet.
    private static List<string> InvalidFields(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results.SelectMany(result => result.MemberNames).ToList();
    }

    private static RegisterViewModel ValidRegistration() => new()
    {
        Name = "Kari Nordmann", Email = "kari@eksempel.no", Password = "Passord123", ConfirmPassword = "Passord123"
    };

    [Theory]
    [InlineData("91234567")]
    [InlineData("912 34 567")]
    [InlineData("+47 912 34 567")]
    [InlineData("+4791234567")]
    public void Phone_AcceptsNorwegianAndInternationalNumbers(string phone)
    {
        var model = ValidRegistration();
        model.PhoneNumber = phone;

        Assert.DoesNotContain(nameof(RegisterViewModel.PhoneNumber), InvalidFields(model));
    }

    [Theory]
    [InlineData("1")]                // For kort
    [InlineData("1234567")]          // 7 siffer, ett for lite
    [InlineData("abcdefgh")]         // Bokstaver
    [InlineData("912-34-567")]       // Bindestrek
    [InlineData("912  34 567")]      // To mellomrom på rad
    [InlineData("9123456789012345")] // 16 siffer, ett for mye
    [InlineData("47+91234567")]      // + bare først
    public void Phone_RejectsThingsThatAreNotPhoneNumbers(string phone)
    {
        var model = ValidRegistration();
        model.PhoneNumber = phone;

        Assert.Contains(nameof(RegisterViewModel.PhoneNumber), InvalidFields(model));
    }

    [Fact]
    public void Phone_IsOptional()
    {
        var model = ValidRegistration();
        model.PhoneNumber = null;

        Assert.Empty(InvalidFields(model));
    }

    [Theory]
    [InlineData("a@b")]           // Mangler domene med punktum
    [InlineData("ola nordmann@eksempel.no")]
    [InlineData("ola@@eksempel.no")]
    [InlineData("ola.eksempel.no")]
    public void Email_RejectsInvalidAddresses(string email)
    {
        var model = ValidRegistration();
        model.Email = email;

        Assert.Contains(nameof(RegisterViewModel.Email), InvalidFields(model));
    }

    [Theory]
    [InlineData("Ærlige Øystein Åsen")]
    [InlineData("Anne-Lise O'Hara")]
    [InlineData("José Müller Jr.")]
    public void Name_AcceptsRealNames(string name)
    {
        var model = new ProfileViewModel { Name = name, Email = "kari@eksempel.no" };

        Assert.Empty(InvalidFields(model));
    }

    [Theory]
    [InlineData("Kari123")]
    [InlineData("<script>")]
    [InlineData("kari@eksempel.no")]
    [InlineData("-Kari")] // Må starte med en bokstav
    public void Name_RejectsDigitsAndSymbols(string name)
    {
        var model = new ProfileViewModel { Name = name, Email = "kari@eksempel.no" };

        Assert.Contains(nameof(ProfileViewModel.Name), InvalidFields(model));
    }

    [Theory]
    [InlineData("4630", "Kristiansand S", true)]
    [InlineData("8622", "Mo i Rana", true)]
    [InlineData("463", "Kristiansand", false)]   // 3 siffer
    [InlineData("46300", "Kristiansand", false)] // 5 siffer
    [InlineData("4630", "1234", false)]          // Poststed med tall
    public void PostalCodeAndPlace_AreChecked(string code, string place, bool valid)
    {
        var model = new ResourceViewModel
        {
            Name = "Vannpost", Description = "Tankbil", Category = "Vann", PostalCode = code, PostalPlace = place
        };

        Assert.Equal(valid, InvalidFields(model).Count == 0);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(100000, true)]
    [InlineData(100001, false)]
    public void Quantity_MustBeWithinLimits(int quantity, bool valid)
    {
        var model = new NeedViewModel { Title = "Vann", Description = "Trengs", Category = "Vann", Quantity = quantity };

        Assert.Equal(valid, InvalidFields(model).Count == 0);
    }

    [Fact]
    public void Deadline_NewNeedCannotBeInThePast()
    {
        var model = new NeedViewModel
        {
            Title = "Vann", Description = "Trengs", Category = "Vann", Deadline = DateTime.Today.AddDays(-1)
        };

        Assert.Contains(nameof(NeedViewModel.Deadline), InvalidFields(model));
    }

    [Fact]
    public void Deadline_ExistingNeedWithPassedDeadlineCanStillBeSaved()
    {
        // Ved endring er Id satt. Et gammelt behov der fristen har gått ut må fortsatt kunne lagres.
        var model = new NeedViewModel
        {
            Id = 7, Title = "Vann", Description = "Trengs", Category = "Vann", Deadline = DateTime.Today.AddDays(-30)
        };

        Assert.Empty(InvalidFields(model));
    }

    [Fact]
    public void Deadline_RejectsTypoYears()
    {
        var model = new NeedViewModel
        {
            Title = "Vann", Description = "Trengs", Category = "Vann", Deadline = DateTime.Today.AddYears(20) // Mer enn 10 år frem, f.eks. en skrivefeil i årstallet.
        };

        Assert.Contains(nameof(NeedViewModel.Deadline), InvalidFields(model));
    }
}
