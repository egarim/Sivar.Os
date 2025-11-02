using MudBlazor;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace Sivar.Os.Client.Services;

/// <summary>
/// Custom MudBlazor localization service that provides translations for MudBlazor components.
/// Supports English (en) and Spanish (es) cultures.
/// </summary>
public class MudLocalizerService : MudLocalizer
{
    private Dictionary<string, Dictionary<string, string>> _localization;

    public MudLocalizerService()
    {
        _localization = new()
        {
            {
                "en", new Dictionary<string, string>()
                {
                    // MudDataGrid
                    { "MudDataGrid.AddFilter", "Add Filter" },
                    { "MudDataGrid.Apply", "Apply" },
                    { "MudDataGrid.Cancel", "Cancel" },
                    { "MudDataGrid.Clear", "Clear" },
                    { "MudDataGrid.CollapseAllGroups", "Collapse all groups" },
                    { "MudDataGrid.Column", "Column" },
                    { "MudDataGrid.Columns", "Columns" },
                    { "MudDataGrid.Contains", "Contains" },
                    { "MudDataGrid.EndsWith", "Ends with" },
                    { "MudDataGrid.Equal", "Equal" },
                    { "MudDataGrid.ExpandAllGroups", "Expand all groups" },
                    { "MudDataGrid.False", "False" },
                    { "MudDataGrid.Filter", "Filter" },
                    { "MudDataGrid.FilterValue", "Filter value" },
                    { "MudDataGrid.GreaterThan", "Greater than" },
                    { "MudDataGrid.GreaterThanOrEqual", "Greater than or equal" },
                    { "MudDataGrid.Group", "Group" },
                    { "MudDataGrid.Hide", "Hide" },
                    { "MudDataGrid.HideAll", "Hide All" },
                    { "MudDataGrid.Is", "Is" },
                    { "MudDataGrid.IsEmpty", "Is empty" },
                    { "MudDataGrid.IsNot", "Is not" },
                    { "MudDataGrid.IsNotEmpty", "Is not empty" },
                    { "MudDataGrid.LessThan", "Less than" },
                    { "MudDataGrid.LessThanOrEqual", "Less than or equal" },
                    { "MudDataGrid.NotContains", "Not contains" },
                    { "MudDataGrid.NotEqual", "Not equal" },
                    { "MudDataGrid.Operator", "Operator" },
                    { "MudDataGrid.RefreshData", "Refresh Data" },
                    { "MudDataGrid.ShowAll", "Show All" },
                    { "MudDataGrid.Sort", "Sort" },
                    { "MudDataGrid.StartsWith", "Starts with" },
                    { "MudDataGrid.True", "True" },
                    { "MudDataGrid.Ungroup", "Ungroup" },
                    { "MudDataGrid.Unsort", "Unsort" },
                    { "MudDataGrid.Value", "Value" },
                    
                    // MudTable
                    { "MudTable.Equals", "Equals" },
                    { "MudTable.NotEquals", "Not Equals" },
                    
                    // Common
                    { "MudPagination.First", "First" },
                    { "MudPagination.Previous", "Previous" },
                    { "MudPagination.Next", "Next" },
                    { "MudPagination.Last", "Last" },
                }
            },
            {
                "es", new Dictionary<string, string>()
                {
                    // MudDataGrid
                    { "MudDataGrid.AddFilter", "Agregar Filtro" },
                    { "MudDataGrid.Apply", "Aplicar" },
                    { "MudDataGrid.Cancel", "Cancelar" },
                    { "MudDataGrid.Clear", "Limpiar" },
                    { "MudDataGrid.CollapseAllGroups", "Contraer todos los grupos" },
                    { "MudDataGrid.Column", "Columna" },
                    { "MudDataGrid.Columns", "Columnas" },
                    { "MudDataGrid.Contains", "Contiene" },
                    { "MudDataGrid.EndsWith", "Termina con" },
                    { "MudDataGrid.Equal", "Igual" },
                    { "MudDataGrid.ExpandAllGroups", "Expandir todos los grupos" },
                    { "MudDataGrid.False", "Falso" },
                    { "MudDataGrid.Filter", "Filtro" },
                    { "MudDataGrid.FilterValue", "Valor del filtro" },
                    { "MudDataGrid.GreaterThan", "Mayor que" },
                    { "MudDataGrid.GreaterThanOrEqual", "Mayor o igual que" },
                    { "MudDataGrid.Group", "Agrupar" },
                    { "MudDataGrid.Hide", "Ocultar" },
                    { "MudDataGrid.HideAll", "Ocultar Todo" },
                    { "MudDataGrid.Is", "Es" },
                    { "MudDataGrid.IsEmpty", "Está vacío" },
                    { "MudDataGrid.IsNot", "No es" },
                    { "MudDataGrid.IsNotEmpty", "No está vacío" },
                    { "MudDataGrid.LessThan", "Menor que" },
                    { "MudDataGrid.LessThanOrEqual", "Menor o igual que" },
                    { "MudDataGrid.NotContains", "No contiene" },
                    { "MudDataGrid.NotEqual", "No igual" },
                    { "MudDataGrid.Operator", "Operador" },
                    { "MudDataGrid.RefreshData", "Actualizar Datos" },
                    { "MudDataGrid.ShowAll", "Mostrar Todo" },
                    { "MudDataGrid.Sort", "Ordenar" },
                    { "MudDataGrid.StartsWith", "Comienza con" },
                    { "MudDataGrid.True", "Verdadero" },
                    { "MudDataGrid.Ungroup", "Desagrupar" },
                    { "MudDataGrid.Unsort", "Desordenar" },
                    { "MudDataGrid.Value", "Valor" },
                    
                    // MudTable
                    { "MudTable.Equals", "Igual a" },
                    { "MudTable.NotEquals", "No Igual a" },
                    
                    // Common
                    { "MudPagination.First", "Primero" },
                    { "MudPagination.Previous", "Anterior" },
                    { "MudPagination.Next", "Siguiente" },
                    { "MudPagination.Last", "Último" },
                }
            }
        };
    }

    public override LocalizedString this[string key]
    {
        get
        {
            var currentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            
            // Default to English if culture not found
            if (!_localization.ContainsKey(currentCulture))
            {
                currentCulture = "en";
            }

            // Try to get translation
            if (_localization[currentCulture].TryGetValue(key, out var translation))
            {
                return new LocalizedString(key, translation);
            }

            // Fallback to English if key not found in current culture
            if (currentCulture != "en" && _localization["en"].TryGetValue(key, out var fallback))
            {
                return new LocalizedString(key, fallback);
            }

            // Return key if no translation found
            return new LocalizedString(key, key, true);
        }
    }
}
