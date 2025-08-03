# Interest Calculation Logic Guide

## Overview

The Phoenix Sangam API uses a **monthly simple interest** calculation system for loans. The interest is calculated based on the loan type's monthly interest rate and the time period since the loan was issued.

## Interest Calculation Formula

### Basic Formula
```
Interest = Principal × (Monthly Rate / 100) × Number of Months
```

### Detailed Breakdown
1. **Principal**: The original loan amount
2. **Monthly Rate**: Interest rate per month (as a percentage)
3. **Number of Months**: Time period calculated as days since loan issue ÷ 30

### Implementation in Code

```csharp
public decimal CalculateInterest(decimal monthlyRate, decimal principal, DateTime loanDate, DateTime calculationDate)
{
    if (calculationDate <= loanDate)
        return 0;
        
    var daysSinceIssue = (calculationDate - loanDate).Days;
    var monthsSinceIssue = daysSinceIssue / 30.0; // Convert days to months
    
    // Calculate interest: Principal * (Monthly Rate / 100) * Number of Months
    var interestAmount = principal * (monthlyRate / 100) * (decimal)monthsSinceIssue;
    
    return Math.Round(interestAmount, 2);
}
```

## Loan Types and Interest Rates

### Current Loan Types
| Loan Type | Interest Rate | Description |
|-----------|---------------|-------------|
| Marriage Loan | 1.16% per month | Lower rate for marriage-related loans |
| Personal Loan | 2.5% per month | Standard rate for personal loans |

### Database Seeding
```csharp
modelBuilder.Entity<LoanType>().HasData(
    new LoanType { Id = 1, LoanTypeName = "Marriage Loan", InterestRate = 1.16 },
    new LoanType { Id = 2, LoanTypeName = "Personal Loan", InterestRate = 2.5 }
);
```

## Calculation Examples

### Example 1: Marriage Loan
- **Principal**: ₹50,000
- **Interest Rate**: 1.16% per month
- **Duration**: 6 months
- **Calculation**: ₹50,000 × (1.16/100) × 6 = ₹3,480

### Example 2: Personal Loan
- **Principal**: ₹25,000
- **Interest Rate**: 2.5% per month
- **Duration**: 3 months
- **Calculation**: ₹25,000 × (2.5/100) × 3 = ₹1,875

### Example 3: Partial Month Calculation
- **Principal**: ₹10,000
- **Interest Rate**: 1.16% per month
- **Duration**: 45 days (1.5 months)
- **Calculation**: ₹10,000 × (1.16/100) × 1.5 = ₹174

## Key Features

### 1. Simple Interest
- Interest is calculated only on the principal amount
- No compound interest (interest on interest)
- Straightforward and predictable calculations

### 2. Monthly Rate System
- Interest rates are specified as monthly percentages
- Easy to understand and compare across loan types
- Consistent calculation method

### 3. Time-Based Calculation
- Interest is calculated based on actual time elapsed
- Uses 30-day months for simplicity
- Calculation date can be current date or due date

### 4. Rounding
- All interest amounts are rounded to 2 decimal places
- Ensures consistent precision across calculations

## Calculation Scenarios

### 1. Active Loans
- **Calculation Date**: Current date or due date (whichever is later)
- **Purpose**: Shows current interest accrued
- **Usage**: Dashboard displays, loan listings

### 2. Closed Loans
- **Calculation Date**: Closed date
- **Purpose**: Final interest calculation
- **Usage**: Loan history, repayment processing

### 3. Overdue Loans
- **Calculation Date**: Current date
- **Purpose**: Shows additional interest on overdue amounts
- **Usage**: Overdue loan reports, collections

## API Endpoints Using Interest Calculation

### 1. Get All Loans
```http
GET /api/loans
```
- Calculates current interest for all loans
- Shows interest amount and received interest

### 2. Get Loan by ID
```http
GET /api/loans/{id}
```
- Calculates interest for specific loan
- Includes detailed interest breakdown

### 3. Dashboard Summary
```http
GET /api/dashboard/summary
```
- Calculates total interest across all loans
- Shows interest statistics

### 4. Loans Due
```http
GET /api/dashboard/loans-due
```
- Calculates interest for loans due soon
- Helps with collection planning

## Interest Tracking

### Database Fields
- **InterestReceived**: Amount of interest already paid
- **InterestAmount**: Calculated interest (not stored, computed on-demand)
- **InterestRate**: Monthly rate from loan type

### Interest vs Principal
- **Principal**: Original loan amount (stored in `Amount` field)
- **Interest**: Calculated based on time and rate
- **Total Due**: Principal + Interest - InterestReceived

## Business Logic

### 1. Interest Accrual
- Interest starts accruing from loan issue date
- Continues until loan is closed or repaid
- No interest on closed loans

### 2. Interest Payment
- Interest can be paid separately from principal
- `InterestReceived` tracks paid interest
- Remaining interest = Calculated Interest - InterestReceived

### 3. Loan Status Impact
- **Active**: Interest continues to accrue
- **Closed**: Interest calculation stops at closed date
- **Overdue**: Additional interest on overdue amounts

## Validation Rules

### 1. Rate Validation
- Interest rates must be positive
- Rates are stored as percentages (e.g., 1.16 for 1.16%)
- Maximum rate validation can be added

### 2. Date Validation
- Calculation date must be after loan date
- Returns 0 if calculation date is before loan date
- Handles timezone differences

### 3. Amount Validation
- Principal amount must be positive
- Interest amounts are rounded to 2 decimal places
- Handles decimal precision correctly

## Performance Considerations

### 1. Calculation Efficiency
- Simple arithmetic operations
- No complex mathematical functions
- Fast calculation even for large datasets

### 2. Caching Strategy
- Interest calculations are computed on-demand
- No caching implemented (can be added if needed)
- Real-time accuracy

### 3. Database Optimization
- Interest rates stored in loan type table
- No redundant calculations stored
- Efficient queries with includes

## Future Enhancements

### 1. Compound Interest
- Could implement compound interest option
- More complex but potentially more profitable

### 2. Variable Rates
- Interest rates that change over time
- Rate adjustment capabilities

### 3. Grace Periods
- Interest-free periods for certain loan types
- Configurable grace period settings

### 4. Early Payment Discounts
- Reduced interest for early repayment
- Incentive-based interest calculations

## Testing Scenarios

### 1. Basic Calculation
```csharp
// Test: ₹10,000 loan at 1.16% for 3 months
var interest = CalculateInterest(1.16m, 10000m, new DateTime(2024, 1, 1), new DateTime(2024, 4, 1));
// Expected: ₹348.00
```

### 2. Zero Interest
```csharp
// Test: Calculation date before loan date
var interest = CalculateInterest(1.16m, 10000m, new DateTime(2024, 2, 1), new DateTime(2024, 1, 1));
// Expected: ₹0.00
```

### 3. Partial Month
```csharp
// Test: 45 days (1.5 months)
var interest = CalculateInterest(1.16m, 10000m, new DateTime(2024, 1, 1), new DateTime(2024, 2, 15));
// Expected: ₹174.00
```

## Troubleshooting

### Common Issues

1. **Incorrect Interest Amounts**
   - Check loan date and calculation date
   - Verify interest rate from loan type
   - Ensure proper decimal handling

2. **Zero Interest on Active Loans**
   - Verify loan date is in the past
   - Check if calculation date is correct
   - Ensure loan type has valid interest rate

3. **Rounding Issues**
   - All amounts rounded to 2 decimal places
   - Check for floating-point precision issues
   - Use decimal type for financial calculations

### Debug Information
- Log calculation parameters for debugging
- Include loan type information in error messages
- Validate all input parameters before calculation 