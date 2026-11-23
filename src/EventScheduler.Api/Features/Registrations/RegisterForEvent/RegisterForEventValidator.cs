using EventScheduler.Application.Registrations;
using FluentValidation;

namespace EventScheduler.Api.Features.Registrations.RegisterForEvent;

/// <summary>
/// Validates <see cref="RegisterForEventRequest"/>: a well-formed email of at most
/// <see cref="Registration.EmailMaxLength"/> characters.
/// </summary>
public sealed class RegisterForEventValidator : AbstractValidator<RegisterForEventRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterForEventValidator"/> class.
    /// </summary>
    public RegisterForEventValidator()
    {
        RuleFor(r => r.Email).NotEmpty().MaximumLength(Registration.EmailMaxLength).EmailAddress();
    }
}
