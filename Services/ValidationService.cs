using System.ComponentModel.DataAnnotations;

namespace phoenix_sangam_api.Services;

/// <summary>
/// Validation service implementation
/// </summary>
public class ValidationService : IValidationService
{
    public ValidationResult ValidateObject<T>(T obj)
    {
        if (obj == null)
        {
            return new ValidationResult("Object cannot be null");
        }
        
        var context = new ValidationContext(obj);
        var results = new List<ValidationResult>();
        
        if (Validator.TryValidateObject(obj, context, results, true))
        {
            return ValidationResult.Success;
        }
        
        return new ValidationResult(string.Join("; ", results.Select(r => r.ErrorMessage ?? "Validation error")));
    }

    public Task<ValidationResult> ValidateObjectAsync<T>(T obj)
    {
        return Task.FromResult(ValidateObject(obj));
    }

    public bool IsValid<T>(T obj, out List<string> errors)
    {
        errors = GetValidationErrors(obj);
        return !errors.Any();
    }

    public Task<bool> IsValidAsync<T>(T obj)
    {
        return Task.FromResult(IsValid(obj, out _));
    }

    public List<string> GetValidationErrors<T>(T obj)
    {
        if (obj == null)
        {
            return new List<string> { "Object cannot be null" };
        }
        
        var context = new ValidationContext(obj);
        var results = new List<ValidationResult>();
        
        Validator.TryValidateObject(obj, context, results, true);
        
        return results.Select(r => r.ErrorMessage ?? "Validation error").ToList();
    }
} 