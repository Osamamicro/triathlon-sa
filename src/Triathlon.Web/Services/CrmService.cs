using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Triathlon.Web.Data;
using Triathlon.Web.Domain.Crm;

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

    /// <summary>The Arabic label for a category; unknown values come back unchanged.</summary>
    public static string Arabic(string category) => category switch
    {
        Youth => "الناشئون",
        AgeGroup => "الفئات العمرية",
        Elite => "النخبة",
        Para => "ذوو الإعاقة",
        Community => "مجتمعي",
        _ => category,
    };
}

/// <summary>
/// The membership side of the CRM as Week 2 needs it: turning a public registration into a Pending
/// athlete and telling the applicant it arrived. Approval, licence issuing and the rest of the desk's
/// work arrive with the CRM in Week 5.
/// </summary>
public sealed class CrmService(AppDbContext db, IEmailSender email, TimeProvider clock, ILogger<CrmService> log)
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
    /// Records the application and confirms it by mail. The record is saved first and the mail is
    /// sent afterwards inside a try/catch: a mail server that is slow, misconfigured or down must
    /// not lose an application the athlete believes they submitted. Week 5 moves the send into a
    /// Hangfire job, at which point the retry comes for free.
    /// </summary>
    public async Task<Athlete> ApplyAsync(AthleteApplication application, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(application);

        var athlete = new Athlete
        {
            FullName = application.FullName.Trim(),
            Email = application.Email.Trim(),
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

        try
        {
            await email.SendAsync(Confirmation(athlete), ct);
        }
        catch (Exception ex)
        {
            // Deliberately broad: every failure mode of an SMTP client — socket, protocol,
            // authentication, timeout — has the same consequence here, and none of them is a reason
            // to fail a request whose real work is already committed.
            log.LogWarning(ex, "Could not send the registration confirmation for athlete {AthleteId}.", athlete.Id);
        }

        return athlete;
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
            ? "الاتحاد السعودي للترايثلون — استلمنا طلب تسجيلك"
            : "Saudi Triathlon Federation — we received your registration";

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
