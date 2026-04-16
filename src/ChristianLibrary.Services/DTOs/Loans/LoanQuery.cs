using ChristianLibrary.Domain.Enums;
using ChristianLibrary.Services.DTOs.Common;

namespace ChristianLibrary.Services.DTOs.Loans;

public class LoanQuery : PagedQuery
{
    public LoanStatus? Status { get; set; }
}