using Sivar.Os.Shared.DTOs;
using System.Text.RegularExpressions;

namespace Sivar.Os.Services;

/// <summary>
/// Service for classifying user intent from chat messages.
/// Phase 6: Intent-Based Routing - Uses pattern matching and keyword analysis.
/// </summary>
public interface IIntentClassifier
{
    /// <summary>
    /// Classify the intent of a user message
    /// </summary>
    IntentClassificationDto ClassifyIntent(string message);
}

/// <summary>
/// Rule-based intent classifier using keyword patterns and regex.
/// Designed for Spanish language queries common in El Salvador context.
/// </summary>
public class IntentClassifier : IIntentClassifier
{
    private readonly ILogger<IntentClassifier> _logger;
    private readonly List<IntentRule> _rules;

    public IntentClassifier(ILogger<IntentClassifier> logger)
    {
        _logger = logger;
        _rules = BuildIntentRules();
    }

    public IntentClassificationDto ClassifyIntent(string message)
    {
        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid();
        
        _logger.LogDebug("[IntentClassifier] START - RequestId={RequestId}, Message={Message}", 
            requestId, message);

        try
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return new IntentClassificationDto
                {
                    Intent = UserIntent.Unknown,
                    Confidence = 0f,
                    OriginalQuery = message ?? string.Empty,
                    ProcessingTimeMs = 0
                };
            }

            var normalizedMessage = NormalizeMessage(message);
            var bestMatch = FindBestMatch(normalizedMessage);
            
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            
            var result = new IntentClassificationDto
            {
                Intent = bestMatch.Intent,
                Confidence = bestMatch.Confidence,
                Entity = bestMatch.ExtractedEntity,
                SecondaryIntent = bestMatch.SecondaryIntent,
                Parameters = bestMatch.Parameters,
                OriginalQuery = message,
                ProcessingTimeMs = (long)elapsed
            };

            _logger.LogInformation("[IntentClassifier] CLASSIFIED - RequestId={RequestId}, Intent={Intent}, Confidence={Confidence:F2}, Entity={Entity}, Duration={Duration}ms",
                requestId, result.Intent, result.Confidence, result.Entity, elapsed);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IntentClassifier] ERROR - RequestId={RequestId}", requestId);
            return new IntentClassificationDto
            {
                Intent = UserIntent.Unknown,
                Confidence = 0f,
                OriginalQuery = message,
                ProcessingTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    private string NormalizeMessage(string message)
    {
        // Lowercase and remove extra spaces
        var normalized = message.ToLowerInvariant().Trim();
        
        // Remove punctuation except question marks (useful for intent)
        normalized = Regex.Replace(normalized, @"[^\w\s\?áéíóúñü]", " ");
        
        // Normalize multiple spaces
        normalized = Regex.Replace(normalized, @"\s+", " ");
        
        return normalized;
    }

    private MatchResult FindBestMatch(string normalizedMessage)
    {
        var matches = new List<MatchResult>();

        foreach (var rule in _rules)
        {
            var matchResult = EvaluateRule(rule, normalizedMessage);
            if (matchResult.Confidence > 0)
            {
                matches.Add(matchResult);
            }
        }

        if (!matches.Any())
        {
            // Default to GeneralQuestion if no patterns match
            return new MatchResult
            {
                Intent = UserIntent.GeneralQuestion,
                Confidence = 0.3f
            };
        }

        // Return the highest confidence match
        return matches.OrderByDescending(m => m.Confidence).First();
    }

    private MatchResult EvaluateRule(IntentRule rule, string message)
    {
        float confidence = 0f;
        string? extractedEntity = null;
        var parameters = new Dictionary<string, string>();

        // Check required keywords (must have at least one)
        var requiredMatches = rule.RequiredKeywords
            .Count(kw => message.Contains(kw, StringComparison.OrdinalIgnoreCase));
        
        if (requiredMatches == 0 && rule.RequiredKeywords.Any())
        {
            return new MatchResult { Intent = rule.Intent, Confidence = 0 };
        }

        // Check boost keywords (increase confidence)
        var boostMatches = rule.BoostKeywords
            .Count(kw => message.Contains(kw, StringComparison.OrdinalIgnoreCase));

        // Check negative keywords (decrease confidence or exclude)
        var negativeMatches = rule.NegativeKeywords
            .Count(kw => message.Contains(kw, StringComparison.OrdinalIgnoreCase));

        if (negativeMatches > 0 && rule.NegativeKeywords.Any())
        {
            return new MatchResult { Intent = rule.Intent, Confidence = 0 };
        }

        // Calculate base confidence
        confidence = rule.BaseConfidence;
        
        // Boost for additional keyword matches
        confidence += boostMatches * 0.1f;
        
        // Cap at 1.0
        confidence = Math.Min(confidence, 1.0f);

        // Try to extract entity using regex patterns
        foreach (var pattern in rule.EntityPatterns)
        {
            var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                extractedEntity = match.Groups[1].Value.Trim();
                parameters["entity"] = extractedEntity;
                confidence += 0.1f; // Boost for entity extraction
                break;
            }
        }

        return new MatchResult
        {
            Intent = rule.Intent,
            Confidence = Math.Min(confidence, 1.0f),
            ExtractedEntity = extractedEntity,
            Parameters = parameters
        };
    }

    private List<IntentRule> BuildIntentRules()
    {
        return new List<IntentRule>
        {
            // GREETING - highest priority for simple greetings
            new IntentRule
            {
                Intent = UserIntent.Greeting,
                BaseConfidence = 0.95f,
                RequiredKeywords = new[] { "hola", "buenos días", "buenas tardes", "buenas noches", "hey", "saludos", "qué tal", "buenas" },
                BoostKeywords = Array.Empty<string>(),
                NegativeKeywords = new[] { "busco", "quiero", "necesito", "dónde", "cómo", "teléfono", "horario" },
                EntityPatterns = Array.Empty<string>()
            },
            
            // PROCEDURE HELP - Government procedures (high priority for document keywords)
            new IntentRule
            {
                Intent = UserIntent.ProcedureHelp,
                BaseConfidence = 0.85f,
                RequiredKeywords = new[] { "cómo", "como", "requisitos", "trámite", "tramite", "sacar", "obtener", "renovar", "solicitar", "proceso", "pasos", "necesito para", "qué necesito" },
                BoostKeywords = new[] { "dui", "pasaporte", "licencia", "partida", "nacimiento", "nit", "antecedentes", "penales", "matrimonio", "propiedad", "registro", "documento" },
                NegativeKeywords = Array.Empty<string>(),
                EntityPatterns = new[] 
                { 
                    @"(?:sacar|obtener|renovar|solicitar)\s+(?:el\s+|la\s+|mi\s+)?(.+?)(?:\?|$)",
                    @"requisitos\s+(?:para\s+)?(?:el\s+|la\s+)?(.+?)(?:\?|$)",
                    @"trámite\s+(?:de\s+|del\s+)?(.+?)(?:\?|$)",
                    @"(?:qué|que)\s+necesito\s+(?:para\s+)?(.+?)(?:\?|$)"
                }
            },

            // CONTACT LOOKUP - Looking for phone/email/contact
            new IntentRule
            {
                Intent = UserIntent.ContactLookup,
                BaseConfidence = 0.9f,
                RequiredKeywords = new[] { "teléfono", "telefono", "número", "numero", "contacto", "email", "correo", "llamar", "whatsapp" },
                BoostKeywords = new[] { "de", "del", "la", "el" },
                NegativeKeywords = Array.Empty<string>(),
                EntityPatterns = new[] 
                { 
                    @"(?:teléfono|telefono|número|numero|contacto)\s+(?:de|del|de la)\s+(.+?)(?:\?|$)",
                    @"(?:llamar|contactar)\s+(?:a|al|a la)\s+(.+?)(?:\?|$)"
                }
            },

            // HOURS QUERY - Business hours
            new IntentRule
            {
                Intent = UserIntent.HoursQuery,
                BaseConfidence = 0.9f,
                RequiredKeywords = new[] { "horario", "hora", "abre", "cierra", "abierto", "cerrado", "abren", "cierran" },
                BoostKeywords = new[] { "qué", "a qué hora", "hasta qué hora", "está", "ahora" },
                NegativeKeywords = Array.Empty<string>(),
                EntityPatterns = new[] 
                { 
                    @"horario\s+(?:de|del|de la)\s+(.+?)(?:\?|$)",
                    @"(?:a qué hora|cuando)\s+(?:abre|cierra)\s+(.+?)(?:\?|$)",
                    @"(.+?)\s+(?:está|esta)\s+(?:abierto|cerrado)"
                }
            },

            // BUSINESS SEARCH - General business/place search (HIGHER priority, before DirectionsRequest)
            new IntentRule
            {
                Intent = UserIntent.BusinessSearch,
                BaseConfidence = 0.85f,
                RequiredKeywords = new[] { "busco", "buscar", "encontrar", "cerca", "cercano", "cercanos", "cercanas", "restaurante", "restaurantes", "pizzería", "pizzeria", "pizzerias", "farmacia", "banco", "hotel", "tienda", "supermercado", "gasolinera", "hospital", "clínica", "clinica", "cafetería", "cafeterias", "cafeteria", "pupusería", "pupuserias", "pupuseria", "comer", "comida" },
                BoostKeywords = new[] { "mejor", "mejores", "bueno", "buenos", "económico", "barato", "abierto", "24 horas", "cerca de mí", "cerca de mi", "aquí", "aca", "en" },
                NegativeKeywords = new[] { "teléfono", "telefono", "horario", "dirección", "direccion" },
                EntityPatterns = new[] 
                { 
                    @"(?:busco|buscar|encontrar)\s+(?:un|una|el|la|los|las)?\s*(.+?)(?:\s+cerca|\s+en|\?|$)",
                    @"(.+?)\s+(?:cerca|cercano|cercana|cercanos|cercanas)\s+(?:de\s+)?(?:mí|mi|aquí|acá|aca)?",
                    @"(?:dónde|donde)\s+(?:puedo)?\s*(?:comer|comprar|encontrar)\s+(.+?)(?:\?|$)",
                    @"(.+?)\s+en\s+(.+?)(?:\?|$)"
                }
            },

            // DIRECTIONS REQUEST - Location/how to get there (after BusinessSearch)
            new IntentRule
            {
                Intent = UserIntent.DirectionsRequest,
                BaseConfidence = 0.80f,
                RequiredKeywords = new[] { "dirección", "direccion", "ubicación", "ubicacion", "llegar", "queda", "cómo llego", "como llego", "mapa", "ruta" },
                BoostKeywords = new[] { "está", "esta" },
                NegativeKeywords = new[] { "comer", "comida", "restaurante", "pizzería", "farmacia", "cafetería" },
                EntityPatterns = new[] 
                { 
                    @"(?:dónde|donde)\s+(?:está|esta|queda)\s+(.+?)(?:\?|$)",
                    @"(?:dirección|direccion|ubicación|ubicacion)\s+(?:de|del|de la)\s+(.+?)(?:\?|$)",
                    @"(?:cómo|como)\s+llego\s+(?:a|al|a la)\s+(.+?)(?:\?|$)"
                }
            },

            // EVENT SEARCH - Events and activities (expanded keywords)
            new IntentRule
            {
                Intent = UserIntent.EventSearch,
                BaseConfidence = 0.85f,
                RequiredKeywords = new[] { "evento", "eventos", "concierto", "conciertos", "festival", "actividad", "actividades", "feria", "espectáculo", "hacer hoy", "hacer mañana", "hacer este", "fin de semana", "qué hay" },
                BoostKeywords = new[] { "hoy", "mañana", "semana", "este", "esta", "diciembre", "enero", "febrero" },
                NegativeKeywords = Array.Empty<string>(),
                EntityPatterns = new[] 
                { 
                    @"eventos?\s+(?:en|de|del)\s+(.+?)(?:\?|$)",
                    @"(?:qué|que)\s+(?:hay|hacer)\s+(?:en|este|hoy|mañana)?\s*(.+?)(?:\?|$)"
                }
            },

            // GENERAL QUESTION - Fallback for informational queries (lowest priority)
            new IntentRule
            {
                Intent = UserIntent.GeneralQuestion,
                BaseConfidence = 0.4f,
                RequiredKeywords = new[] { "es un", "es una", "significa", "explicar", "explica" },
                BoostKeywords = Array.Empty<string>(),
                NegativeKeywords = new[] { "busco", "cerca", "teléfono", "horario", "dónde", "comer", "evento", "requisitos" },
                EntityPatterns = new[] 
                { 
                    @"(?:qué|que)\s+es\s+(?:un|una|el|la)?\s*(.+?)(?:\?|$)"
                }
            }
        };
    }

    private class IntentRule
    {
        public UserIntent Intent { get; init; }
        public float BaseConfidence { get; init; }
        public string[] RequiredKeywords { get; init; } = Array.Empty<string>();
        public string[] BoostKeywords { get; init; } = Array.Empty<string>();
        public string[] NegativeKeywords { get; init; } = Array.Empty<string>();
        public string[] EntityPatterns { get; init; } = Array.Empty<string>();
    }

    private class MatchResult
    {
        public UserIntent Intent { get; init; }
        public float Confidence { get; init; }
        public string? ExtractedEntity { get; init; }
        public UserIntent? SecondaryIntent { get; init; }
        public Dictionary<string, string> Parameters { get; init; } = new();
    }
}
