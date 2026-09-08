using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Areas.Public;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Crm;
using Triathlon.Web.Jobs;

namespace Triathlon.Web.Services;

/// <summary>One athlete's application for a licence, as the public registration form posts it.</summary>
public sealed record AthleteApplication(
    string FullName,
    string Email,
    DateOnly DateOfBirth,
    string? CityKey,
    string Category,
    Guid? ClubId,
    Guid? InterestEventId,
    string Culture);

/// <summary>
/// The five categories the registration form offers, which are also the only values the endpoint
/// will write: a posted category is checked against this list, so a crafted POST cannot invent one.
/// </summary>
public static class AthleteCategories
{
    public const string Youth = "Youth";
    public const string AgeGroup = "Age Group";
    public const string Elite = "Elite";
    public const string Para = "Para";
    public const string Community = "Community";

    public static readonly IReadOnlyList<string> All = [Youth, AgeGroup, Elite, Para, Community];

    /// <summary>
    /// The Arabic label for a category. Delegates to <see cref="Areas.Public.EventCategories.Label"/>
    /// — the same lookup the event-entry form's category dropdown uses — so the two forms cannot
    /// drift apart on what "Youth" or "Community" reads as in Arabic; unknown values come back
    /// unchanged either way.
    /// </summary>
    public static string Arabic(string category) => EventCategories.Label(category).Ar;
}

/// <summary>
/// The membership side of the CRM as Week 2 needs it: turning a public registration into a Pending
/// athlete and telling the applicant it arrived. Approval, licence issuing and the rest of the desk's
/// work arrive with the CRM in Week 5.
/// </summary>
public sealed class CrmService(AppDbContext db, IBackgroundJobClient jobs, TimeProvider clock, ILogger<CrmService> log, ContentCommit commit)
{
    /// <summary>The youngest the federation licences; younger children race under a club's own scheme.</summary>
    public const int YoungestYears = 6;

    /// <summary>A sanity bound rather than a rule: a date beyond it is a typo, not an applicant.</summary>
    public const int OldestYears = 100;

    /// <summary>
    /// The dates of birth an application may carry, as (earliest, latest). One definition for the
    /// endpoint that enforces it and the date picker that offers it, so the two cannot drift apart.
    /// </summary>
    public static (DateOnly Earliest, DateOnly Latest) DateOfBirthRange(DateOnly today) =>
        (today.AddYears(-OldestYears), today.AddYears(-YoungestYears));

    /// <summary>
    /// Records the application and queues its confirmation mail through <see cref="EmailJob"/>, so a
    /// slow or unreachable SMTP server never costs an applicant their application and a failed send
    /// gets Hangfire's retry for free instead of a caught-and-logged exception.
    /// <para>
    /// A pending or approved application already on file for the same email (case-insensitively) is
    /// treated as the same person resubmitting rather than a new athlete: no second row, no second
    /// mail, just the existing record handed back with <c>Created: false</c>. This is a public write
    /// with no dashboard log either way — the desk's own edits are what <see cref="ContentCommit"/> audits.
    /// </para>
    /// </summary>
    public async Task<(Athlete Athlete, bool Created)> ApplyAsync(AthleteApplication application, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(application);

        var email = application.Email.Trim();
        var emailLower = email.ToLowerInvariant();
        var existing = await db.Athletes.FirstOrDefaultAsync(
            a => a.Email.ToLower() == emailLower && a.Status != AthleteStatus.Rejected, ct);
        if (existing is not null)
        {
            log.LogInformation("Duplicate application for {Email} ignored.", email);
            return (existing, false);
        }

        var athlete = new Athlete
        {
            FullName = application.FullName.Trim(),
            Email = email,
            DateOfBirth = application.DateOfBirth,
            CityKey = string.IsNullOrWhiteSpace(application.CityKey) ? null : application.CityKey.Trim(),
            Category = application.Category.Trim(),
            ClubId = application.ClubId,
            InterestEventId = application.InterestEventId,
            Status = AthleteStatus.Pending,
            ConsentAt = clock.GetUtcNow(),
            PreferredCulture = application.Culture == "ar" ? "ar" : "en",
        };

        db.Athletes.Add(athlete);
        await db.SaveChangesAsync(ct);

        jobs.Enqueue<EmailJob>(job => job.SendAsync(Confirmation(athlete), CancellationToken.None));

        return (athlete, true);
    }

    // ---------------------------------------------------------------- athletes (dashboard)

    public async Task<IReadOnlyList<Athlete>> AthletesForEditAsync(AthleteStatus? status, bool deletedOnly, CancellationToken ct)
    {
        var query = deletedOnly ? db.Athletes.IgnoreQueryFilters().Where(a => a.DeletedAt != null) : db.Athletes.AsQueryable();
        if (status is { } value)
        {
            query = query.Where(a => a.Status == value);
        }

        return await query.AsNoTracking().OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
    }

    public async Task DeleteAthleteAsync(Guid id, CancellationToken ct)
    {
        var athlete = await db.Athletes.SingleOrDefaultAsync(a => a.Id == id, ct);
        if (athlete is null) return;
        db.Athletes.Remove(athlete);
        await commit.ApplyAsync("Athlete", id, "delete", Audit.Snapshot(athlete), null, [], ct);
    }

    public async Task RestoreAthleteAsync(Guid id, CancellationToken ct)
    {
        var athlete = await db.Athletes.IgnoreQueryFilters().SingleOrDefaultAsync(a => a.Id == id && a.DeletedAt != null, ct);
        if (athlete is null) return;
        athlete.DeletedAt = null;
        await commit.ApplyAsync("Athlete", id, "restore", null, Audit.Snapshot(athlete), [], ct);
    }

    /// <summary>
    /// The confirmation mail, in the language the athlete registered in. The name is the only piece
    /// of visitor-supplied text in it, so it is HTML-encoded before it goes into the markup — and
    /// left alone in the plain-text alternative, where entities would only be read literally.
    /// </summary>
    private static EmailMessage Confirmation(Athlete athlete)
    {
        var arabic = athlete.PreferredCulture == "ar";

        var subject = arabic
            ? "الاتحاد السعودي للترايثلون: استلمنا طلب تسجيلك"
            : "Saudi Triathlon Federation: we received your registration";

        var greeting = arabic ? "مرحباً {0}،" : "Dear {0},";

        string[] lines = arabic
            ?
            [
                "استلمنا طلب تسجيلك رياضياً لدى الاتحاد السعودي للترايثلون.",
                "يراجع موظف مختص كل طلب خلال خمسة أيام عمل، ثم يصلك رقم رخصتك على هذا البريد.",
                "لأي استفسار راسلنا على info@triathlon.sa.",
            ]
            :
            [
                "We have received your application to register as an athlete with the Saudi Triathlon Federation.",
                "An officer reviews every application within five working days; your licence number then arrives at this address.",
                "If you have a question in the meantime, write to info@triathlon.sa.",
            ];

        var html = Paragraph(string.Format(CultureInfo.InvariantCulture, greeting, BodyEncoder.Encode(athlete.FullName)))
                   + string.Concat(lines.Select(Paragraph));

        var text = string.Format(CultureInfo.InvariantCulture, greeting, athlete.FullName)
                   + "\n\n" + string.Join("\n\n", lines);

        return new EmailMessage(athlete.Email, subject, html, text);
    }

    private static string Paragraph(string line) => "<p>" + line + "</p>";

    /// <summary>
    /// The site's encoder settings rather than <see cref="HtmlEncoder.Default"/>: the strict default
    /// escapes every non-Latin character, which would turn an Arabic name into a wall of entities in
    /// the one place a person reads it.
    /// </summary>
    private static readonly HtmlEncoder BodyEncoder = HtmlEncoder.Create(
        UnicodeRanges.BasicLatin,
        UnicodeRanges.Latin1Supplement,
        UnicodeRanges.GeneralPunctuation,
        UnicodeRanges.Arabic,
        UnicodeRanges.ArabicSupplement,
        UnicodeRanges.ArabicExtendedA,
        UnicodeRanges.ArabicPresentationFormsA,
        UnicodeRanges.ArabicPresentationFormsB);
}
