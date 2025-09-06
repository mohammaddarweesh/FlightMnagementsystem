using FlightBooking.Application.Flights.Commands;
using FluentValidation;

namespace FlightBooking.Application.Flights.Validators;

public class CreateAirportCommandValidator : AbstractValidator<CreateAirportCommand>
{
    public CreateAirportCommandValidator()
    {
        RuleFor(x => x.IataCode)
            .NotEmpty().WithMessage("IATA code is required")
            .Length(3).WithMessage("IATA code must be exactly 3 characters")
            .Matches("^[A-Z]{3}$").WithMessage("IATA code must contain only uppercase letters");

        RuleFor(x => x.IcaoCode)
            .NotEmpty().WithMessage("ICAO code is required")
            .Length(4).WithMessage("ICAO code must be exactly 4 characters")
            .Matches("^[A-Z]{4}$").WithMessage("ICAO code must contain only uppercase letters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Airport name is required")
            .MaximumLength(200).WithMessage("Airport name cannot exceed 200 characters");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required")
            .MaximumLength(100).WithMessage("Country cannot exceed 100 characters");

        RuleFor(x => x.CountryCode)
            .NotEmpty().WithMessage("Country code is required")
            .Length(2).WithMessage("Country code must be exactly 2 characters")
            .Matches("^[A-Z]{2}$").WithMessage("Country code must contain only uppercase letters");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90 degrees");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180 degrees");

        RuleFor(x => x.Elevation)
            .GreaterThanOrEqualTo(-1000).WithMessage("Elevation cannot be less than -1000 feet")
            .LessThanOrEqualTo(30000).WithMessage("Elevation cannot exceed 30,000 feet");

        RuleFor(x => x.TimeZone)
            .NotEmpty().WithMessage("Time zone is required")
            .MaximumLength(50).WithMessage("Time zone cannot exceed 50 characters");

        RuleFor(x => x.Website)
            .Must(BeValidUrl).When(x => !string.IsNullOrEmpty(x.Website))
            .WithMessage("Website must be a valid URL");
    }

    private bool BeValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

public class CreateRouteCommandValidator : AbstractValidator<CreateRouteCommand>
{
    public CreateRouteCommandValidator()
    {
        RuleFor(x => x.DepartureAirportId)
            .NotEmpty().WithMessage("Departure airport is required");

        RuleFor(x => x.ArrivalAirportId)
            .NotEmpty().WithMessage("Arrival airport is required")
            .NotEqual(x => x.DepartureAirportId).WithMessage("Departure and arrival airports must be different");

        RuleFor(x => x.Distance)
            .GreaterThan(0).WithMessage("Distance must be greater than 0 kilometers")
            .LessThanOrEqualTo(20000).WithMessage("Distance cannot exceed 20,000 kilometers");

        RuleFor(x => x.EstimatedFlightTime)
            .GreaterThan(TimeSpan.Zero).WithMessage("Estimated flight time must be greater than 0")
            .LessThanOrEqualTo(TimeSpan.FromHours(20)).WithMessage("Estimated flight time cannot exceed 20 hours");
    }
}

public class CreateFlightCommandValidator : AbstractValidator<CreateFlightCommand>
{
    public CreateFlightCommandValidator()
    {
        RuleFor(x => x.FlightNumber)
            .NotEmpty().WithMessage("Flight number is required")
            .MaximumLength(10).WithMessage("Flight number cannot exceed 10 characters")
            .Matches("^[0-9A-Z]+$").WithMessage("Flight number must contain only numbers and uppercase letters");

        RuleFor(x => x.RouteId)
            .NotEmpty().WithMessage("Route is required");

        RuleFor(x => x.AirlineCode)
            .NotEmpty().WithMessage("Airline code is required")
            .Length(2, 3).WithMessage("Airline code must be 2 or 3 characters")
            .Matches("^[A-Z]+$").WithMessage("Airline code must contain only uppercase letters");

        RuleFor(x => x.AirlineName)
            .NotEmpty().WithMessage("Airline name is required")
            .MaximumLength(100).WithMessage("Airline name cannot exceed 100 characters");

        RuleFor(x => x.AircraftType)
            .NotEmpty().WithMessage("Aircraft type is required")
            .MaximumLength(50).WithMessage("Aircraft type cannot exceed 50 characters");

        RuleFor(x => x.DepartureDate)
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Departure date cannot be in the past");

        RuleFor(x => x.DepartureTime)
            .LessThan(TimeSpan.FromDays(1)).WithMessage("Departure time must be within a 24-hour period");

        RuleFor(x => x.ArrivalTime)
            .LessThan(TimeSpan.FromDays(2)).WithMessage("Arrival time must be within a 48-hour period");

        RuleFor(x => x.Gate)
            .MaximumLength(10).When(x => !string.IsNullOrEmpty(x.Gate))
            .WithMessage("Gate cannot exceed 10 characters");

        RuleFor(x => x.Terminal)
            .MaximumLength(10).When(x => !string.IsNullOrEmpty(x.Terminal))
            .WithMessage("Terminal cannot exceed 10 characters");

        RuleForEach(x => x.FareClasses).SetValidator(new CreateFareClassRequestValidator());

        RuleFor(x => x.FareClasses)
            .Must(HaveUniqueClassNames).When(x => x.FareClasses.Any())
            .WithMessage("Fare class names must be unique within the flight");

        RuleFor(x => x.FareClasses)
            .Must(HavePositiveCapacities).When(x => x.FareClasses.Any())
            .WithMessage("All fare classes must have positive capacity");
    }

    private bool HaveUniqueClassNames(List<CreateFareClassRequest> fareClasses)
    {
        var classNames = fareClasses.Select(fc => fc.ClassName.ToLower()).ToList();
        return classNames.Count == classNames.Distinct().Count();
    }

    private bool HavePositiveCapacities(List<CreateFareClassRequest> fareClasses)
    {
        return fareClasses.All(fc => fc.Capacity > 0);
    }
}

public class CreateFareClassRequestValidator : AbstractValidator<CreateFareClassRequest>
{
    public CreateFareClassRequestValidator()
    {
        RuleFor(x => x.ClassName)
            .NotEmpty().WithMessage("Class name is required")
            .MaximumLength(50).WithMessage("Class name cannot exceed 50 characters");

        RuleFor(x => x.ClassCode)
            .NotEmpty().WithMessage("Class code is required")
            .Length(1, 2).WithMessage("Class code must be 1 or 2 characters")
            .Matches("^[A-Z]+$").WithMessage("Class code must contain only uppercase letters");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be greater than 0")
            .LessThanOrEqualTo(1000).WithMessage("Capacity cannot exceed 1000 seats");

        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage("Base price must be greater than 0")
            .LessThanOrEqualTo(50000).WithMessage("Base price cannot exceed $50,000");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order cannot be negative");
    }
}

public class CreateFareClassCommandValidator : AbstractValidator<CreateFareClassCommand>
{
    public CreateFareClassCommandValidator()
    {
        RuleFor(x => x.FlightId)
            .NotEmpty().WithMessage("Flight ID is required");

        RuleFor(x => x.ClassName)
            .NotEmpty().WithMessage("Class name is required")
            .MaximumLength(50).WithMessage("Class name cannot exceed 50 characters");

        RuleFor(x => x.ClassCode)
            .NotEmpty().WithMessage("Class code is required")
            .Length(1, 2).WithMessage("Class code must be 1 or 2 characters")
            .Matches("^[A-Z]+$").WithMessage("Class code must contain only uppercase letters");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be greater than 0")
            .LessThanOrEqualTo(1000).WithMessage("Capacity cannot exceed 1000 seats");

        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage("Base price must be greater than 0")
            .LessThanOrEqualTo(50000).WithMessage("Base price cannot exceed $50,000");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order cannot be negative");
    }
}

public class AttachAmenityToFareClassCommandValidator : AbstractValidator<AttachAmenityToFareClassCommand>
{
    public AttachAmenityToFareClassCommandValidator()
    {
        RuleFor(x => x.FareClassId)
            .NotEmpty().WithMessage("Fare class ID is required");

        RuleFor(x => x.AmenityId)
            .NotEmpty().WithMessage("Amenity ID is required");

        RuleFor(x => x.AdditionalCost)
            .GreaterThanOrEqualTo(0).When(x => x.AdditionalCost.HasValue)
            .WithMessage("Additional cost cannot be negative");
    }
}
