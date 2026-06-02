// ============================================================
// ProtocolService.API / DTOs / ProtocolDTOs.cs
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace LifeTrack.ProtocolService.DTOs
{
    // ── Phase DTO ─────────────────────────────────────────────────────────────

    public class PhaseDto
    {
        public int PhaseNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    // ── Protocol response DTO ─────────────────────────────────────────────────

    public class ProtocolDto
    {
        public long ProtocolID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;  // Always server-calculated
        public int SiteCount { get; set; }
        public List<PhaseDto> Phases { get; set; } = new();
    }

    // ── Create request ────────────────────────────────────────────────────────

    public class CreateProtocolRequest : IValidatableObject
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        public DateTime EndDate { get; set; }

        // NOTE: Status is NOT here — always computed server-side from dates.

        [Required(ErrorMessage = "At least one phase is required.")]
        public List<PhaseDto> Phases { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            // 1. Protocol start date must not be in the past
            if (StartDate.Date < DateTime.UtcNow.Date)
                yield return new ValidationResult(
                    "Protocol start date cannot be in the past.",
                    new[] { nameof(StartDate) });

            // 2. End date must be after start date
            if (EndDate <= StartDate)
            {
                yield return new ValidationResult(
                    "End date must be after start date.",
                    new[] { nameof(EndDate) });
                yield break;
            }

            // 3. At least 1 phase required (no upper limit)
            if (Phases == null || Phases.Count == 0)
            {
                yield return new ValidationResult(
                    "At least 1 phase is required.",
                    new[] { nameof(Phases) });
                yield break;
            }

            var ordered = Phases.OrderBy(p => p.PhaseNumber).ToList();

            // 4. Phase numbers must be sequential starting from 1
            for (int i = 0; i < ordered.Count; i++)
                if (ordered[i].PhaseNumber != i + 1)
                {
                    yield return new ValidationResult(
                        "Phases must be numbered sequentially starting from 1.",
                        new[] { nameof(Phases) });
                    yield break;
                }

            // 5. Phase 1 start must equal protocol start date
            if (ordered.First().StartDate.Date != StartDate.Date)
                yield return new ValidationResult(
                    "Phase 1 start date must equal the protocol start date.",
                    new[] { nameof(Phases) });

            // 6. Last phase end must equal protocol end date
            if (ordered.Last().EndDate.Date != EndDate.Date)
                yield return new ValidationResult(
                    $"Phase {ordered.Count} end date must equal the protocol end date.",
                    new[] { nameof(Phases) });

            // 7. Per-phase checks
            for (int i = 0; i < ordered.Count; i++)
            {
                var phase = ordered[i];

                // Each phase end > start
                if (phase.EndDate <= phase.StartDate)
                    yield return new ValidationResult(
                        $"Phase {phase.PhaseNumber}: end date must be after start date.",
                        new[] { nameof(Phases) });

                // Phase dates must fall within protocol range
                if (phase.StartDate.Date < StartDate.Date || phase.EndDate.Date > EndDate.Date)
                    yield return new ValidationResult(
                        $"Phase {phase.PhaseNumber}: dates must be within protocol start and end dates.",
                        new[] { nameof(Phases) });

                // Next phase start must be on or after previous phase end
                if (i > 0 && phase.StartDate.Date < ordered[i - 1].EndDate.Date)
                    yield return new ValidationResult(
                        $"Phase {phase.PhaseNumber} start date must be on or after " +
                        $"Phase {ordered[i - 1].PhaseNumber} end date.",
                        new[] { nameof(Phases) });
            }
        }
    }

    // ── Update request ────────────────────────────────────────────────────────

    public class UpdateProtocolRequest : IValidatableObject
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        public DateTime EndDate { get; set; }

        // NOTE: Status is NOT here — always computed server-side from dates.

        [Required(ErrorMessage = "At least one phase is required.")]
        public List<PhaseDto> Phases { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            // 1. End date must be after start date
            if (EndDate <= StartDate)
            {
                yield return new ValidationResult(
                    "End date must be after start date.",
                    new[] { nameof(EndDate) });
                yield break;
            }

            if (Phases == null || Phases.Count == 0)
            {
                yield return new ValidationResult(
                    "At least 1 phase is required.",
                    new[] { nameof(Phases) });
                yield break;
            }

            var ordered = Phases.OrderBy(p => p.PhaseNumber).ToList();

            // 2. Phase numbers sequential
            for (int i = 0; i < ordered.Count; i++)
                if (ordered[i].PhaseNumber != i + 1)
                {
                    yield return new ValidationResult(
                        "Phases must be numbered sequentially starting from 1.",
                        new[] { nameof(Phases) });
                    yield break;
                }

            // 3. Phase 1 start = protocol start
            if (ordered.First().StartDate.Date != StartDate.Date)
                yield return new ValidationResult(
                    "Phase 1 start date must equal the protocol start date.",
                    new[] { nameof(Phases) });

            // 4. Last phase end = protocol end
            if (ordered.Last().EndDate.Date != EndDate.Date)
                yield return new ValidationResult(
                    $"Phase {ordered.Count} end date must equal the protocol end date.",
                    new[] { nameof(Phases) });

            // 5. Per-phase checks
            for (int i = 0; i < ordered.Count; i++)
            {
                var phase = ordered[i];

                if (phase.EndDate <= phase.StartDate)
                    yield return new ValidationResult(
                        $"Phase {phase.PhaseNumber}: end date must be after start date.",
                        new[] { nameof(Phases) });

                if (phase.StartDate.Date < StartDate.Date || phase.EndDate.Date > EndDate.Date)
                    yield return new ValidationResult(
                        $"Phase {phase.PhaseNumber}: dates must be within protocol start and end dates.",
                        new[] { nameof(Phases) });

                if (i > 0 && phase.StartDate.Date < ordered[i - 1].EndDate.Date)
                    yield return new ValidationResult(
                        $"Phase {phase.PhaseNumber} start date must be on or after " +
                        $"Phase {ordered[i - 1].PhaseNumber} end date.",
                        new[] { nameof(Phases) });
            }
        }
    }

    // ── Filter DTO ────────────────────────────────────────────────────────────

    public class ProtocolFilterDto
    {
        public string? Title { get; set; }
        public string? Phase { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}