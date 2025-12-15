using Microsoft.Extensions.Logging;
using Moq;
using Sivar.Os.Services;
using Sivar.Os.Shared.DTOs;
using Xunit;

namespace Sivar.Os.Tests.Services;

public class IntentClassifierTests
{
    private readonly IntentClassifier _classifier;

    public IntentClassifierTests()
    {
        var logger = new Mock<ILogger<IntentClassifier>>();
        _classifier = new IntentClassifier(logger.Object);
    }

    [Theory]
    [InlineData("hola", UserIntent.Greeting)]
    [InlineData("buenos días", UserIntent.Greeting)]
    [InlineData("hola qué tal", UserIntent.Greeting)]
    public void ClassifyIntent_Greeting_ReturnsGreetingIntent(string message, UserIntent expected)
    {
        var result = _classifier.ClassifyIntent(message);
        Assert.Equal(expected, result.Intent);
        Assert.True(result.Confidence > 0.5f);
    }

    [Theory]
    [InlineData("busco pizzerías en San Salvador", UserIntent.BusinessSearch)]
    [InlineData("restaurantes cerca de mí", UserIntent.BusinessSearch)]
    [InlineData("dónde puedo comer pupusas", UserIntent.BusinessSearch)]
    [InlineData("cafeterías en Santa Ana", UserIntent.BusinessSearch)]
    public void ClassifyIntent_BusinessSearch_ReturnsBusinessSearchIntent(string message, UserIntent expected)
    {
        var result = _classifier.ClassifyIntent(message);
        Assert.Equal(expected, result.Intent);
    }

    [Theory]
    [InlineData("cuál es el teléfono de la alcaldía", UserIntent.ContactLookup)]
    [InlineData("número de contacto del banco", UserIntent.ContactLookup)]
    [InlineData("dame el WhatsApp del restaurante", UserIntent.ContactLookup)]
    public void ClassifyIntent_ContactLookup_ReturnsContactLookupIntent(string message, UserIntent expected)
    {
        var result = _classifier.ClassifyIntent(message);
        Assert.Equal(expected, result.Intent);
    }

    [Theory]
    [InlineData("requisitos para sacar DUI", UserIntent.ProcedureHelp)]
    [InlineData("cómo renovar el pasaporte", UserIntent.ProcedureHelp)]
    [InlineData("pasos para licencia de conducir", UserIntent.ProcedureHelp)]
    [InlineData("qué necesito para partida de nacimiento", UserIntent.ProcedureHelp)]
    public void ClassifyIntent_ProcedureHelp_ReturnsProcedureHelpIntent(string message, UserIntent expected)
    {
        var result = _classifier.ClassifyIntent(message);
        Assert.Equal(expected, result.Intent);
    }

    [Theory]
    [InlineData("cómo llego al aeropuerto", UserIntent.DirectionsRequest)]
    [InlineData("dónde queda el Metrocentro", UserIntent.DirectionsRequest)]
    [InlineData("dirección de la embajada", UserIntent.DirectionsRequest)]
    public void ClassifyIntent_DirectionsRequest_ReturnsDirectionsIntent(string message, UserIntent expected)
    {
        var result = _classifier.ClassifyIntent(message);
        Assert.Equal(expected, result.Intent);
    }

    [Theory]
    [InlineData("a qué hora abre el banco", UserIntent.HoursQuery)]
    [InlineData("horario de atención de la alcaldía", UserIntent.HoursQuery)]
    [InlineData("está abierto el restaurante ahorita", UserIntent.HoursQuery)]
    public void ClassifyIntent_HoursQuery_ReturnsHoursQueryIntent(string message, UserIntent expected)
    {
        var result = _classifier.ClassifyIntent(message);
        Assert.Equal(expected, result.Intent);
    }

    [Theory]
    [InlineData("eventos este fin de semana", UserIntent.EventSearch)]
    [InlineData("conciertos en diciembre", UserIntent.EventSearch)]
    [InlineData("qué hay para hacer hoy", UserIntent.EventSearch)]
    public void ClassifyIntent_EventSearch_ReturnsEventSearchIntent(string message, UserIntent expected)
    {
        var result = _classifier.ClassifyIntent(message);
        Assert.Equal(expected, result.Intent);
    }

    [Fact]
    public void ClassifyIntent_ExtractsEntity_FromBusinessSearch()
    {
        var result = _classifier.ClassifyIntent("busco pizzerías en San Salvador");
        
        Assert.Equal(UserIntent.BusinessSearch, result.Intent);
        // Entity should contain the extracted business type or location
        Assert.NotNull(result.Entity);
    }

    [Fact]
    public void ClassifyIntent_EmptyMessage_ReturnsUnknown()
    {
        var result = _classifier.ClassifyIntent("");
        Assert.Equal(UserIntent.Unknown, result.Intent);
        Assert.Equal(0f, result.Confidence);
    }

    [Fact]
    public void ClassifyIntent_NullMessage_ReturnsUnknown()
    {
        var result = _classifier.ClassifyIntent(null!);
        Assert.Equal(UserIntent.Unknown, result.Intent);
    }

    [Fact]
    public void ClassifyIntent_IncludesProcessingTime()
    {
        var result = _classifier.ClassifyIntent("hola");
        Assert.True(result.ProcessingTimeMs >= 0);
    }

    [Fact]
    public void ClassifyIntent_PreservesOriginalQuery()
    {
        var message = "Busco PIZZERÍAS en San Salvador!!!";
        var result = _classifier.ClassifyIntent(message);
        Assert.Equal(message, result.OriginalQuery);
    }
}
