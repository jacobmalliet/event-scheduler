using EventScheduler.Api.Common;
using FluentValidation;

namespace EventScheduler.Api.Features.Events.ListEvents;

/// <summary>
/// Validates <see cref="ListEventsRequest"/>: page from 1 to <see cref="Paging.MaxPage"/>,
/// page size from 1 to <see cref="Paging.MaxPageSize"/>.
/// </summary>
public sealed class ListEventsValidator : AbstractValidator<ListEventsRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListEventsValidator"/> class.
    /// </summary>
    public ListEventsValidator()
    {
        RuleFor(r => r.Page).InclusiveBetween(1, Paging.MaxPage);
        RuleFor(r => r.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}
