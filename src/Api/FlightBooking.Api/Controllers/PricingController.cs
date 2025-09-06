using FlightBooking.Application.Pricing.Services;
using FlightBooking.Contracts.Pricing;
using FlightBooking.Domain.Pricing;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FlightBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PricingController : ControllerBase
{
    private readonly IPricingService _pricingService;
    private readonly ILogger<PricingController> _logger;

    public PricingController(IPricingService pricingService, ILogger<PricingController> logger)
    {
        _pricingService = pricingService;
        _logger = logger;
    }

    /// <summary>
    /// Calculate comprehensive fare pricing with all applicable rules and policies
    /// </summary>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(FareCalculationResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(FareCalculationResponseDto), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<FareCalculationResponseDto>> CalculateFare(
        [FromBody] FareCalculationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fareRequest = MapToFareCalculationRequest(request);
            var result = await _pricingService.CalculateFareAsync(fareRequest, cancellationToken);

            var response = MapToFareCalculationResponse(result);

            if (!result.Success)
            {
                return BadRequest(response);
            }

            // Add performance headers
            Response.Headers["X-Calculation-Duration"] = result.CalculationDuration.TotalMilliseconds.ToString("F0");
            Response.Headers["X-Calculation-Id"] = result.CalculationId;
            Response.Headers["X-Rules-Applied"] = result.AppliedRules.Count.ToString();

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid fare calculation request");
            return BadRequest(new FareCalculationResponseDto
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating fare");
            return StatusCode(500, new FareCalculationResponseDto
            {
                Success = false,
                ErrorMessage = "An error occurred while calculating the fare"
            });
        }
    }

    /// <summary>
    /// Validate pricing policies without calculating final fare
    /// </summary>
    [HttpPost("validate-policies")]
    [ProducesResponseType(typeof(PolicyValidationResponseDto), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<PolicyValidationResponseDto>> ValidatePolicies(
        [FromBody] PolicyValidationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fareRequest = MapToPolicyValidationRequest(request);
            var result = await _pricingService.ValidatePoliciesAsync(fareRequest, cancellationToken);

            var response = new PolicyValidationResponseDto
            {
                Success = true,
                IsValid = result.IsValid,
                Violations = result.Violations.Select(v => new PolicyViolationDto
                {
                    PolicyId = v.PolicyId,
                    PolicyName = v.PolicyName,
                    ViolationType = v.ViolationType,
                    Description = v.Description,
                    Severity = v.Severity.ToString(),
                    IsBlocking = v.IsBlocking,
                    Resolution = v.Resolution,
                    Context = v.Context
                }).ToList(),
                Warnings = result.Warnings.Select(w => new PolicyWarningDto
                {
                    PolicyId = w.PolicyId,
                    Message = w.Message,
                    Suggestion = w.Suggestion,
                    CanProceed = w.CanProceed
                }).ToList(),
                Recommendations = result.Recommendations.Select(r => new PolicyRecommendationDto
                {
                    Type = r.Type,
                    Message = r.Message,
                    PotentialSavings = r.PotentialSavings,
                    AlternativeAction = r.AlternativeAction,
                    Priority = r.Priority
                }).ToList(),
                RequiresApproval = result.RequiresApproval,
                ApprovalReason = result.ApprovalReason,
                RequiredDocuments = result.RequiredDocuments
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating policies");
            return StatusCode(500, new PolicyValidationResponseDto
            {
                Success = false,
                ErrorMessage = "An error occurred while validating policies"
            });
        }
    }

    /// <summary>
    /// Get detailed breakdown of taxes and fees for a route
    /// </summary>
    [HttpPost("taxes-and-fees")]
    [ProducesResponseType(typeof(TaxAndFeeBreakdownResponseDto), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<TaxAndFeeBreakdownResponseDto>> GetTaxAndFeeBreakdown(
        [FromBody] TaxAndFeeBreakdownRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _pricingService.GetTaxAndFeeBreakdownAsync(
                request.Route, 
                request.FareClass, 
                request.BaseFare, 
                cancellationToken);

            var response = new TaxAndFeeBreakdownResponseDto
            {
                Success = true,
                Taxes = result.Taxes.Select(t => new TaxDto
                {
                    Code = t.Code,
                    Name = t.Name,
                    Description = t.Description,
                    Amount = t.Amount,
                    Rate = t.Rate,
                    Type = t.Type.ToString(),
                    Authority = t.Authority,
                    IsRefundable = t.IsRefundable
                }).ToList(),
                Fees = result.Fees.Select(f => new FeeDto
                {
                    Code = f.Code,
                    Name = f.Name,
                    Description = f.Description,
                    Amount = f.Amount,
                    Type = f.Type.ToString(),
                    IsOptional = f.IsOptional,
                    IsRefundable = f.IsRefundable,
                    WaiverConditions = f.WaiverConditions
                }).ToList(),
                TotalTaxes = result.TotalTaxes,
                TotalFees = result.TotalFees,
                GrandTotal = result.GrandTotal,
                Currency = result.Currency,
                CalculatedAt = result.CalculatedAt,
                TaxExemptions = result.TaxExemptions,
                FeeWaivers = result.FeeWaivers
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating taxes and fees");
            return StatusCode(500, new TaxAndFeeBreakdownResponseDto
            {
                Success = false,
                ErrorMessage = "An error occurred while calculating taxes and fees"
            });
        }
    }

    /// <summary>
    /// Calculate pricing for extra services (baggage, seats, etc.)
    /// </summary>
    [HttpPost("extra-services")]
    [ProducesResponseType(typeof(ExtraServicesCalculationResponseDto), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<ExtraServicesCalculationResponseDto>> CalculateExtraServices(
        [FromBody] ExtraServicesCalculationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var services = request.Services.Select(s => new ExtraService
            {
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                Type = Enum.Parse<ExtraServiceType>(s.Type),
                Quantity = s.Quantity,
                PassengerReference = s.PassengerReference
            }).ToList();

            var fareRequest = MapToFareCalculationRequest(request.FareRequest);
            var result = await _pricingService.CalculateExtraServicesAsync(services, fareRequest, cancellationToken);

            var response = new ExtraServicesCalculationResponseDto
            {
                Success = true,
                ServiceCharges = result.ServiceCharges.Select(sc => new ExtraServiceChargeDto
                {
                    Service = new ExtraServiceDto
                    {
                        Code = sc.Service.Code,
                        Name = sc.Service.Name,
                        Description = sc.Service.Description,
                        Type = sc.Service.Type.ToString(),
                        Quantity = sc.Service.Quantity,
                        PassengerReference = sc.Service.PassengerReference
                    },
                    UnitPrice = sc.UnitPrice,
                    TotalPrice = sc.TotalPrice,
                    IsRefundable = sc.IsRefundable,
                    Terms = sc.Terms
                }).ToList(),
                TotalCharges = result.TotalCharges,
                UnavailableServices = result.UnavailableServices,
                Recommendations = result.Recommendations.Select(r => new ServiceRecommendationDto
                {
                    ServiceCode = r.ServiceCode,
                    ServiceName = r.ServiceName,
                    Reason = r.Reason,
                    PotentialSavings = r.PotentialSavings,
                    Priority = r.Priority
                }).ToList(),
                BundleDiscounts = result.BundleDiscounts,
                Currency = result.Currency
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating extra services");
            return StatusCode(500, new ExtraServicesCalculationResponseDto
            {
                Success = false,
                ErrorMessage = "An error occurred while calculating extra services"
            });
        }
    }

    /// <summary>
    /// Get available promotional codes for a route and date
    /// </summary>
    [HttpGet("promotions")]
    [ProducesResponseType(typeof(List<AvailablePromotionDto>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<List<AvailablePromotionDto>>> GetAvailablePromotions(
        [FromQuery] string route,
        [FromQuery] DateTime departureDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var promotions = await _pricingService.GetAvailablePromotionsAsync(route, departureDate, cancellationToken);

            var response = promotions.Select(p => new AvailablePromotionDto
            {
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                EstimatedSavings = p.EstimatedSavings,
                ExpiryDate = p.ExpiryDate,
                Terms = p.Terms,
                RequiresMinimumPurchase = p.RequiresMinimumPurchase,
                MinimumPurchaseAmount = p.MinimumPurchaseAmount
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available promotions");
            return StatusCode(500, new List<AvailablePromotionDto>());
        }
    }

    /// <summary>
    /// Validate a promotional code
    /// </summary>
    [HttpPost("validate-promo")]
    [ProducesResponseType(typeof(PromoCodeValidationResponseDto), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<PromoCodeValidationResponseDto>> ValidatePromoCode(
        [FromBody] PromoCodeValidationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fareRequest = MapToFareCalculationRequest(request.FareRequest);
            var result = await _pricingService.ValidatePromoCodeAsync(request.PromoCode, fareRequest, cancellationToken);

            var response = new PromoCodeValidationResponseDto
            {
                Success = true,
                IsValid = result.IsValid,
                ErrorMessage = result.ErrorMessage,
                EstimatedDiscount = result.EstimatedDiscount,
                Terms = result.Terms,
                ExpiryDate = result.ExpiryDate,
                Restrictions = result.Restrictions
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating promo code");
            return StatusCode(500, new PromoCodeValidationResponseDto
            {
                Success = false,
                ErrorMessage = "An error occurred while validating the promotional code"
            });
        }
    }

    /// <summary>
    /// Get pricing explanation for educational purposes
    /// </summary>
    [HttpPost("education")]
    [ProducesResponseType(typeof(PricingEducationResponseDto), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<PricingEducationResponseDto>> GetPricingEducation(
        [FromBody] FareCalculationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fareRequest = MapToFareCalculationRequest(request);
            var result = await _pricingService.GetPricingEducationAsync(fareRequest, cancellationToken);

            var response = new PricingEducationResponseDto
            {
                Success = true,
                Summary = result.Summary,
                Factors = result.Factors.Select(f => new PricingFactorDto
                {
                    Name = f.Name,
                    Description = f.Description,
                    Impact = f.Impact,
                    ImpactType = f.ImpactType,
                    Category = f.Category
                }).ToList(),
                SavingsTips = result.SavingsTips.Select(t => new SavingsTipDto
                {
                    Title = t.Title,
                    Description = t.Description,
                    PotentialSavings = t.PotentialSavings,
                    ActionRequired = t.ActionRequired,
                    Priority = t.Priority
                }).ToList(),
                Comparisons = result.Comparisons.Select(c => new PricingComparisonDto
                {
                    Scenario = c.Scenario,
                    Price = c.Price,
                    Difference = c.Difference,
                    Description = c.Description
                }).ToList(),
                Glossary = result.Glossary
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pricing education");
            return StatusCode(500, new PricingEducationResponseDto
            {
                Success = false,
                ErrorMessage = "An error occurred while generating pricing education"
            });
        }
    }

    private FareCalculationRequest MapToFareCalculationRequest(FareCalculationRequestDto dto)
    {
        return new FareCalculationRequest
        {
            FlightId = dto.FlightId,
            FareClassId = dto.FareClassId,
            BaseFare = dto.BaseFare,
            PassengerCount = dto.PassengerCount,
            BookingDate = dto.BookingDate,
            DepartureDate = dto.DepartureDate,
            DepartureAirport = dto.DepartureAirport,
            ArrivalAirport = dto.ArrivalAirport,
            Route = dto.Route,
            IsRoundTrip = dto.IsRoundTrip,
            ReturnDate = dto.ReturnDate,
            PromoCode = dto.PromoCode,
            PassengerTypes = dto.PassengerTypes,
            RequestedExtras = dto.RequestedExtras.Select(e => new ExtraService
            {
                Code = e.Code,
                Name = e.Name,
                Description = e.Description,
                Type = Enum.Parse<ExtraServiceType>(e.Type),
                Quantity = e.Quantity,
                PassengerReference = e.PassengerReference
            }).ToList(),
            CurrentLoadFactor = dto.CurrentLoadFactor,
            CorporateCode = dto.CorporateCode,
            IsFlexibleDates = dto.IsFlexibleDates,
            Currency = dto.Currency
        };
    }

    private FareCalculationRequest MapToPolicyValidationRequest(PolicyValidationRequestDto dto)
    {
        return new FareCalculationRequest
        {
            FlightId = dto.FlightId,
            FareClassId = dto.FareClassId,
            BookingDate = dto.BookingDate,
            DepartureDate = dto.DepartureDate,
            DepartureAirport = dto.DepartureAirport,
            ArrivalAirport = dto.ArrivalAirport,
            Route = dto.Route,
            IsRoundTrip = dto.IsRoundTrip,
            ReturnDate = dto.ReturnDate,
            PassengerCount = dto.PassengerCount,
            CorporateCode = dto.CorporateCode,
            BaseFare = 0 // Not needed for policy validation
        };
    }

    private FareCalculationResponseDto MapToFareCalculationResponse(FareCalculationResult result)
    {
        return new FareCalculationResponseDto
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            FareBreakdown = new FareBreakdownDto
            {
                BaseFare = result.FareBreakdown.BaseFare,
                AdjustedBaseFare = result.FareBreakdown.AdjustedBaseFare,
                Components = result.FareBreakdown.Components.Select(c => new FareComponentDto
                {
                    Name = c.Name,
                    Description = c.Description,
                    Amount = c.Amount,
                    Type = c.Type.ToString(),
                    RuleId = c.RuleId,
                    IsOptional = c.IsOptional,
                    PassengerCount = c.PassengerCount,
                    UnitAmount = c.UnitAmount
                }).ToList(),
                Taxes = result.FareBreakdown.Taxes.Select(t => new TaxDto
                {
                    Code = t.Code,
                    Name = t.Name,
                    Description = t.Description,
                    Amount = t.Amount,
                    Rate = t.Rate,
                    Type = t.Type.ToString(),
                    Authority = t.Authority,
                    IsRefundable = t.IsRefundable
                }).ToList(),
                Fees = result.FareBreakdown.Fees.Select(f => new FeeDto
                {
                    Code = f.Code,
                    Name = f.Name,
                    Description = f.Description,
                    Amount = f.Amount,
                    Type = f.Type.ToString(),
                    IsOptional = f.IsOptional,
                    IsRefundable = f.IsRefundable,
                    WaiverConditions = f.WaiverConditions
                }).ToList(),
                Extras = result.FareBreakdown.Extras.Select(e => new ExtraServiceChargeDto
                {
                    Service = new ExtraServiceDto
                    {
                        Code = e.Service.Code,
                        Name = e.Service.Name,
                        Description = e.Service.Description,
                        Type = e.Service.Type.ToString(),
                        Quantity = e.Service.Quantity,
                        PassengerReference = e.Service.PassengerReference
                    },
                    UnitPrice = e.UnitPrice,
                    TotalPrice = e.TotalPrice,
                    IsRefundable = e.IsRefundable,
                    Terms = e.Terms
                }).ToList(),
                SubTotal = result.FareBreakdown.SubTotal,
                TotalTaxes = result.FareBreakdown.TotalTaxes,
                TotalFees = result.FareBreakdown.TotalFees,
                TotalExtras = result.FareBreakdown.TotalExtras,
                GrandTotal = result.FareBreakdown.GrandTotal,
                TotalDiscount = result.FareBreakdown.TotalDiscount,
                Currency = result.FareBreakdown.Currency,
                TotalBeforeTaxes = result.FareBreakdown.TotalBeforeTaxes,
                EffectiveRate = result.FareBreakdown.EffectiveRate,
                SavingsAmount = result.FareBreakdown.SavingsAmount,
                HasDiscount = result.FareBreakdown.HasDiscount
            },
            PolicyViolations = result.PolicyViolations.Select(v => new PolicyViolationDto
            {
                PolicyId = v.PolicyId,
                PolicyName = v.PolicyName,
                ViolationType = v.ViolationType,
                Description = v.Description,
                Severity = v.Severity.ToString(),
                IsBlocking = v.IsBlocking,
                Resolution = v.Resolution,
                Context = v.Context
            }).ToList(),
            AppliedRules = result.AppliedRules.Select(r => new AppliedRuleDto
            {
                RuleId = r.RuleId,
                RuleName = r.RuleName,
                RuleType = r.RuleType,
                Description = r.Description,
                Impact = r.Impact,
                ImpactType = r.ImpactType,
                Priority = r.Priority,
                AppliedAt = r.AppliedAt,
                Parameters = r.Parameters,
                Reason = r.Reason
            }).ToList(),
            Explanation = new PricingExplanationDto
            {
                Summary = result.Explanation.Summary,
                KeyFactors = result.Explanation.KeyFactors,
                Steps = result.Explanation.Steps.Select(s => new PricingStepDto
                {
                    Order = s.Order,
                    Description = s.Description,
                    BeforeAmount = s.BeforeAmount,
                    AfterAmount = s.AfterAmount,
                    Change = s.Change,
                    ChangeType = s.ChangeType,
                    RuleApplied = s.RuleApplied
                }).ToList(),
                Recommendations = result.Explanation.Recommendations,
                Metadata = result.Explanation.Metadata
            },
            CalculatedAt = result.CalculatedAt,
            CalculationDuration = result.CalculationDuration,
            CalculationId = result.CalculationId
        };
    }
}
