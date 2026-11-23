using EventScheduler.Application.Events;
using FluentValidation;

namespace EventScheduler.Api.Features.Events.UpdateEvent;

/// <summary>
/// Validates <see cref="UpdateEventRequest"/>: the same rules as creating an event.
/// </summary>
public sealed class UpdateEventValidator : AbstractValidator<UpdateEventRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateEventValidator"/> class.
    /// </summary>
    /// <param name="clock">Supplies the current time for the future-start-time rule.</param>
    public UpdateEventValidator(TimeProvider clock)
    {
        // Lengths are checked after trimming because the trimmed text is what gets stored.
        RuleFor(r => r.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(title => title.Trim().Length <= Event.TitleMaxLength)
            .WithMessage($"Title must be {Event.TitleMaxLength} characters or fewer.");
        RuleFor(r => r.Description)
            .Must(description => description is null || description.Trim().Length <= Event.DescriptionMaxLength)
            .WithMessage($"Description must be {Event.DescriptionMaxLength} characters or fewer.");
        RuleFor(r => r.StartsAt)
            .Must(startsAt => startsAt > clock.GetUtcNow())
            .WithMessage("Event date must be in the future.");
        RuleFor(r => r.EndsAt)
            .GreaterThan(r => r.StartsAt)
            .WithMessage("End time must be after the start time.");
        RuleFor(r => r.MaxCapacity).InclusiveBetween(Event.MaxCapacityLowerBound, Event.MaxCapacityUpperBound);
    }
}
