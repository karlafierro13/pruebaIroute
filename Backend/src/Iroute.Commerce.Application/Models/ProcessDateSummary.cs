namespace Iroute.Commerce.Application.Models;

public sealed record ProcessDateSummary
{
    public DateOnly PcProcessDate { get; init; }
    public int TotalRecords { get; init; }
}
