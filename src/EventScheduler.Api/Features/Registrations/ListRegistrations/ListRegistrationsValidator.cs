using EventScheduler.Api.Common;
using FluentValidation;

namespace EventScheduler.Api.Features.Registrations.ListRegistrations;

/// <summary>
/// Validates <see cref="ListRegistrationsRequest"/>: page from 1 to <see cref="Paging.MaxPage"/>,
/// page size from 1 to <see cref="Paging.MaxPageSize"/>.
/// </summary>
public sealed class ListRegistrationsValidator : AbstractValidator<ListRegistrationsRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListRegistrationsValidator"/> class.
    /// </summary>
    public ListRegistrationsValidator()
    {
        RuleFor(r => r.Page).InclusiveBetween(1, Paging.MaxPage);
        RuleFor(r => r.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}
