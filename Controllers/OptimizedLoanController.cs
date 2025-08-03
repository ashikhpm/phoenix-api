using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using phoenix_sangam_api.Data;
using phoenix_sangam_api.DTOs;
using phoenix_sangam_api.Services;

namespace phoenix_sangam_api.Controllers;

/// <summary>
/// Optimized Loan Controller using service layer and base controller
/// </summary>
public class OptimizedLoanController : BaseController
{
    private readonly ILoanService _loanService;

    public OptimizedLoanController(UserDbContext context, ILogger<OptimizedLoanController> logger, ILoanService loanService, IUserActivityService userActivityService, IServiceProvider serviceProvider) 
        : base(context, logger, userActivityService, serviceProvider)
    {
        _loanService = loanService;
    }

    /// <summary>
    /// Get all loans (Secretary only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LoanWithInterestDto>>>> GetAllLoans()
    {
        try
        {
            LogOperation("GetAllLoans");
            var loans = await _loanService.GetAllLoansAsync();
            return Success(loans, "Loans retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException<IEnumerable<LoanWithInterestDto>>(ex, "retrieving loans");
        }
    }

    /// <summary>
    /// Get loan by ID (Secretary only)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<ApiResponse<LoanWithInterestDto>>> GetLoan(int id)
    {
        try
        {
            LogOperation("GetLoan", id);
            var loan = await _loanService.GetLoanByIdAsync(id);
            
            if (loan == null)
                return NotFound<LoanWithInterestDto>($"Loan with ID {id} not found");
            
            return Success(loan, "Loan retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException<LoanWithInterestDto>(ex, "retrieving loan");
        }
    }

    /// <summary>
    /// Create new loan (Secretary only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<ApiResponse<LoanWithInterestDto>>> CreateLoan([FromBody] CreateLoanDto loanDto)
    {
        try
        {
            var validationResult = ValidateModelState<LoanWithInterestDto>();
            if (validationResult != null)
                return validationResult;

            LogOperation("CreateLoan", loanDto);
            var loan = await _loanService.CreateLoanAsync(loanDto);
            return Success(loan, "Loan created successfully");
        }
        catch (ArgumentException ex)
        {
            return Error<LoanWithInterestDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleException<LoanWithInterestDto>(ex, "creating loan");
        }
    }

    /// <summary>
    /// Update loan (Secretary only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<ApiResponse<LoanWithInterestDto>>> UpdateLoan(int id, [FromBody] CreateLoanDto loanDto)
    {
        try
        {
            var validationResult = ValidateModelState<LoanWithInterestDto>();
            if (validationResult != null)
                return validationResult;

            LogOperation("UpdateLoan", id, loanDto);
            var loan = await _loanService.UpdateLoanAsync(id, loanDto);
            return Success(loan, "Loan updated successfully");
        }
        catch (ArgumentException ex)
        {
            return Error<LoanWithInterestDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleException<LoanWithInterestDto>(ex, "updating loan");
        }
    }

    /// <summary>
    /// Delete loan (Secretary only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteLoan(int id)
    {
        try
        {
            LogOperation("DeleteLoan", id);
            var deleted = await _loanService.DeleteLoanAsync(id);
            
            if (!deleted)
                return NotFound<bool>($"Loan with ID {id} not found");
            
            return Success(true, "Loan deleted successfully");
        }
        catch (Exception ex)
        {
            return HandleException<bool>(ex, "deleting loan");
        }
    }

    /// <summary>
    /// Process loan repayment (Secretary only)
    /// </summary>
    [HttpPost("repayment")]
    [Authorize(Roles = "Secretary,President,Treasurer")]
    public async Task<ActionResult<ApiResponse<LoanWithInterestDto>>> LoanRepayment([FromBody] LoanRepaymentDto repaymentDto)
    {
        try
        {
            var validationResult = ValidateModelState<LoanWithInterestDto>();
            if (validationResult != null)
                return validationResult;

            LogOperation("LoanRepayment", repaymentDto);
            var loan = await _loanService.ProcessLoanRepaymentAsync(repaymentDto);
            return Success(loan, "Loan repayment processed successfully");
        }
        catch (ArgumentException ex)
        {
            return Error<LoanWithInterestDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return HandleException<LoanWithInterestDto>(ex, "processing loan repayment");
        }
    }

    /// <summary>
    /// Get loan types
    /// </summary>
    [HttpGet("types")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LoanTypeDto>>>> GetLoanTypes()
    {
        try
        {
            LogOperation("GetLoanTypes");
            var loanTypes = await _loanService.GetLoanTypesAsync();
            return Success(loanTypes, "Loan types retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException<IEnumerable<LoanTypeDto>>(ex, "retrieving loan types");
        }
    }
} 