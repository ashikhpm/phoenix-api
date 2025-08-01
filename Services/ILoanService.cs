using phoenix_sangam_api.DTOs;
using phoenix_sangam_api.Models;

namespace phoenix_sangam_api.Services;

public interface ILoanService
{
    Task<IEnumerable<LoanWithInterestDto>> GetAllLoansAsync();
    Task<LoanWithInterestDto?> GetLoanByIdAsync(int id);
    Task<LoanWithInterestDto> CreateLoanAsync(CreateLoanDto loanDto);
    Task<LoanWithInterestDto> UpdateLoanAsync(int id, CreateLoanDto loanDto);
    Task<bool> DeleteLoanAsync(int id);
    Task<LoanWithInterestDto> ProcessLoanRepaymentAsync(LoanRepaymentDto repaymentDto);
    Task<IEnumerable<LoanTypeDto>> GetLoanTypesAsync();
    Task<IEnumerable<LoanWithInterestDto>> GetLoansByUserAsync(int userId, bool isSecretary);
    Task<LoanDueResponse> GetLoansDueAsync(int userId, bool isSecretary);
    Task<IEnumerable<LoanRequestResponseDto>> GetLoanRequestsAsync(int userId, bool isSecretary);
    Task<LoanRequestResponseDto> CreateLoanRequestAsync(CreateLoanRequestDto requestDto, int userId);
    Task<LoanRequestResponseDto?> GetLoanRequestByIdAsync(int id, int userId, bool isSecretary);
    Task<bool> DeleteLoanRequestAsync(int id, int userId, bool isSecretary);
    Task<LoanRequestResponseDto> ProcessLoanRequestAsync(int id, string action, int secretaryId);
    decimal CalculateInterest(decimal monthlyRate, decimal principal, DateTime loanDate, DateTime calculationDate);
} 