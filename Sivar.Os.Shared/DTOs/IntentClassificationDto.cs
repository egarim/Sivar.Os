namespace Sivar.Os.Shared.DTOs;

/// <summary>
/// Represents the classified intent of a user's chat message.
/// Phase 6: Intent-Based Routing
/// </summary>
public record IntentClassificationDto
{
    /// <summary>
    /// The primary detected intent category
    /// </summary>
    public UserIntent Intent { get; init; }
    
    /// <summary>
    /// Confidence score from 0.0 to 1.0
    /// </summary>
    public float Confidence { get; init; }
    
    /// <summary>
    /// Extracted entity from the query (e.g., business name, procedure type)
    /// </summary>
    public string? Entity { get; init; }
    
    /// <summary>
    /// Secondary intent if detected (for compound queries)
    /// </summary>
    public UserIntent? SecondaryIntent { get; init; }
    
    /// <summary>
    /// Additional extracted parameters for the handler
    /// </summary>
    public Dictionary<string, string> Parameters { get; init; } = new();
    
    /// <summary>
    /// The original query text
    /// </summary>
    public string OriginalQuery { get; init; } = string.Empty;
    
    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; init; }
}

/// <summary>
/// User intent categories for routing
/// </summary>
public enum UserIntent
{
    /// <summary>
    /// Searching for businesses, restaurants, places
    /// Examples: "pizzerías cerca", "restaurantes italianos", "farmacias abiertas"
    /// </summary>
    BusinessSearch,
    
    /// <summary>
    /// Asking about government procedures or how to do something
    /// Examples: "cómo sacar el DUI", "requisitos para pasaporte", "trámite de licencia"
    /// </summary>
    ProcedureHelp,
    
    /// <summary>
    /// Looking for contact information specifically
    /// Examples: "teléfono del BAC", "número de emergencias", "email de la alcaldía"
    /// </summary>
    ContactLookup,
    
    /// <summary>
    /// Asking for directions or how to get somewhere
    /// Examples: "cómo llego a X", "dirección del hospital", "dónde queda Y"
    /// </summary>
    DirectionsRequest,
    
    /// <summary>
    /// Asking about business hours specifically
    /// Examples: "horario de X", "a qué hora abre Y", "está abierto ahora"
    /// </summary>
    HoursQuery,
    
    /// <summary>
    /// Asking about events or activities
    /// Examples: "eventos este fin de semana", "conciertos en San Salvador"
    /// </summary>
    EventSearch,
    
    /// <summary>
    /// General question that needs LLM response
    /// Examples: "qué es un DUI", "cuáles son los departamentos de El Salvador"
    /// </summary>
    GeneralQuestion,
    
    /// <summary>
    /// Greeting or casual conversation
    /// Examples: "hola", "buenos días", "gracias"
    /// </summary>
    Greeting,
    
    /// <summary>
    /// Intent could not be determined
    /// </summary>
    Unknown
}

/// <summary>
/// Intent pattern for rule-based matching
/// </summary>
public record IntentPattern
{
    public UserIntent Intent { get; init; }
    public string[] Keywords { get; init; } = Array.Empty<string>();
    public string[] Patterns { get; init; } = Array.Empty<string>();
    public float BaseConfidence { get; init; } = 0.7f;
}
