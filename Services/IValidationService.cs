using System.ComponentModel.DataAnnotations;

namespace phoenix_sangam_api.Services;

/// <summary>
/// Validation service interface
/// </summary>
public interface IValidationService
{
    ValidationResult ValidateObject<T>(T obj);
    Task<ValidationResult> ValidateObjectAsync<T>(T obj);
    bool IsValid<T>(T obj, out List<string> errors);
    Task<bool> IsValidAsync<T>(T obj);
    List<string> GetValidationErrors<T>(T obj);
} 