using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Domain.Documents;

namespace Triathlon.Web.Data.Seed;

/// <summary>
/// Transcribes the prototype's documents library, rules and training guides
/// (<c>assets/js/data.js</c> lines 236–278, <c>training.html</c>) into the database. Idempotent:
/// skips entirely once any document exists.
/// </summary>
public static class SeedDocuments
{
    public static async Task RunAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Documents.AnyAsync(ct)) return;

        Document D(string id, DocumentCategory category, int year, string sizeMb, (string En, string Ar) title, int sortOrder) => new()
        {
            TitleEn = title.En, TitleAr = title.Ar, Category = category, Year = year,
            FilePath = "/docs/" + id + ".pdf", FileSize = Megabytes(sizeMb), IsPublished = true, SortOrder = sortOrder,
        };

        var documents = new List<Document>
        {
            D("annual-report-2025", DocumentCategory.Governance, 2026, "4.8 MB", ("Annual Report 2025", "التقرير السنوي 2025"), 1),
            D("financial-2025", DocumentCategory.Finance, 2026, "2.1 MB", ("Audited Financial Statements 2025", "القوائم المالية المدققة 2025"), 2),
            D("board-min-q2-2026", DocumentCategory.Minutes, 2026, "0.6 MB", ("Board Meeting Minutes — Q2 2026", "محضر اجتماع مجلس الإدارة — الربع الثاني 2026"), 3),
            D("board-min-q1-2026", DocumentCategory.Minutes, 2026, "0.5 MB", ("Board Meeting Minutes — Q1 2026", "محضر اجتماع مجلس الإدارة — الربع الأول 2026"), 4),
            D("governance-charter", DocumentCategory.Governance, 2025, "1.3 MB", ("Governance & Ethics Charter", "ميثاق الحوكمة والأخلاقيات"), 5),
            D("annual-report-2024", DocumentCategory.Governance, 2025, "4.2 MB", ("Annual Report 2024", "التقرير السنوي 2024"), 6),
            D("financial-2024", DocumentCategory.Finance, 2025, "1.9 MB", ("Audited Financial Statements 2024", "القوائم المالية المدققة 2024"), 7),
            D("safeguarding-policy", DocumentCategory.Governance, 2024, "0.9 MB", ("Athlete Safeguarding Policy", "سياسة حماية الرياضيين"), 8),
            D("board-min-q4-2025", DocumentCategory.Minutes, 2025, "0.5 MB", ("Board Meeting Minutes — Q4 2025", "محضر اجتماع مجلس الإدارة — الربع الرابع 2025"), 9),
        };

        RuleOrGuide R(string slug, string sizeMb, string updated, RuleAudience audience, (string En, string Ar) title, (string En, string Ar) desc, int sortOrder) => new()
        {
            Slug = slug, TitleEn = title.En, TitleAr = title.Ar, DescriptionEn = desc.En, DescriptionAr = desc.Ar,
            Audience = audience, FilePath = "/docs/" + slug + ".pdf", FileSize = Megabytes(sizeMb),
            UpdatedOn = FirstOfMonth(updated), IsPublished = true, SortOrder = sortOrder,
        };

        var rules = new List<RuleOrGuide>
        {
            R("competition-rules-2026", "3.6 MB", "2026-06", RuleAudience.Athletes | RuleAudience.Organizers | RuleAudience.Officials,
                ("STF Competition Rules 2026", "قوانين المنافسات 2026"),
                ("The full rulebook: race conduct, transitions, drafting, penalties, and appeals — aligned with the World Triathlon Competition Rules.",
                 "كتاب القوانين الكامل: سلوك السباق، والانتقالات، والتلاحق الهوائي، والعقوبات، والاستئناف — بما يتوافق مع قوانين الاتحاد الدولي."), 1),
            R("age-group-guide", "1.2 MB", "2026-05", RuleAudience.Athletes,
                ("Age Group Athlete Guide", "دليل رياضيي الفئات العمرية"),
                ("Categories, qualification standards, equipment checks, and race-day procedures for age-group athletes.",
                 "الفئات، ومعايير التأهل، وفحص المعدات، وإجراءات يوم السباق لرياضيي الفئات العمرية."), 2),
            R("event-organizer-manual", "5.1 MB", "2026-04", RuleAudience.Organizers,
                ("Event Organizer Manual", "دليل منظمي الفعاليات"),
                ("Sanctioning requirements, safety plans, course measurement, and officiating for organizers hosting STF events.",
                 "متطلبات الاعتماد، وخطط السلامة، وقياس المسارات، والتحكيم للجهات المنظمة لفعاليات الاتحاد."), 3),
            R("technical-officials", "2.4 MB", "2026-02", RuleAudience.Officials,
                ("Technical Officials Handbook", "دليل الحكام الفنيين"),
                ("Certification pathway, duties, and positioning for technical officials at national events.",
                 "مسار الاعتماد والمهام والتمركز للحكام الفنيين في الفعاليات الوطنية."), 4),
            R("anti-doping-2026", "0.8 MB", "2026-01", RuleAudience.Athletes | RuleAudience.Coaches,
                ("Anti-Doping Policy", "سياسة مكافحة المنشطات"),
                ("Testing procedures, prohibited list references, and athlete whereabouts requirements per SAADC and WADA.",
                 "إجراءات الفحص، ومراجع قائمة المواد المحظورة، ومتطلبات أماكن تواجد الرياضيين وفق اللجنة السعودية والوكالة الدولية لمكافحة المنشطات."), 5),
        };

        var beginnerGuide = new TrainingGuide
        {
            Slug = "beginner-12-weeks",
            TitleEn = "Beginner Training Guide — 12 weeks to your first sprint",
            TitleAr = "دليل تدريب المبتدئين — 12 أسبوعاً حتى أول سباق قصير",
            SummaryEn = "From zero to your first sprint triathlon in 12 weeks. Built by federation coaches as a living reference — more guides will be added over time.",
            SummaryAr = "من الصفر إلى أول ترايثلون قصير خلال 12 أسبوعاً. أعدّه مدربو الاتحاد كمرجع متجدد — وستُضاف أدلة أخرى تباعاً.",
            LevelEn = "Beginner", LevelAr = "للمبتدئين",
            FilePath = "/docs/training-guide-beginner.pdf", FileSize = 2_800_000,
            IsPublished = true, SortOrder = 1,
        };
        beginnerGuide.Chapters.AddRange(
        [
            new()
            {
                SortOrder = 1, TitleEn = "Swim", TitleAr = "سباحة",
                BodyEn = "<p><strong>Comfort before speed.</strong> Two pool sessions a week. Master breathing and sighting first — open-water calm wins more time than a faster stroke. Join a club session before your first sea swim.</p>",
                BodyAr = "<p><strong>الارتياح قبل السرعة.</strong> حصتان في المسبح أسبوعياً. أتقن التنفس والنظر للأمام أولاً — فالهدوء في المياه المفتوحة يكسبك وقتاً أكثر من سرعة الضربات. انضم لحصة نادٍ قبل أول سباحة بحرية.</p>",
            },
            new()
            {
                SortOrder = 2, TitleEn = "Bike", TitleAr = "دراجة",
                BodyEn = "<p><strong>Any bike will do.</strong> Your first race needs a safe bike, not an expensive one. One longer weekend ride plus one short mid-week spin. Practice drinking while riding — race mornings are warm.</p>",
                BodyAr = "<p><strong>أي دراجة تكفي.</strong> سباقك الأول يحتاج دراجة آمنة لا مكلفة. جولة أطول في نهاية الأسبوع وأخرى قصيرة منتصفه. تدرّب على الشرب أثناء القيادة — فصباحات السباق دافئة.</p>",
            },
            new()
            {
                SortOrder = 3, TitleEn = "Run", TitleAr = "جري",
                BodyEn = "<p><strong>Learn the brick.</strong> Running off the bike feels strange — train it. Once a week, add a short 10-minute run straight after a ride. Race-day legs will thank you at T2.</p>",
                BodyAr = "<p><strong>تعلّم التمرين المركب.</strong> الجري بعد الدراجة شعور غريب — تدرّب عليه. مرة أسبوعياً أضف جرياً قصيراً لعشر دقائق مباشرة بعد الدراجة. ستشكرك ساقاك عند المنطقة الانتقالية الثانية.</p>",
            },
            new()
            {
                SortOrder = 4, TitleEn = "The 12-week plan", TitleAr = "خطة الـ12 أسبوعاً",
                BodyEn = """
                    <table>
                    <thead><tr><th>Phase</th><th>Weeks</th><th>Sessions / week</th><th>Focus</th></tr></thead>
                    <tbody>
                    <tr><td>Base</td><td>1–4</td><td>4 (2 swim · 1 bike · 1 run)</td><td>Technique, easy effort, building the habit</td></tr>
                    <tr><td>Build</td><td>5–8</td><td>5 (+1 brick session)</td><td>Longer rides, open-water practice, race-pace intervals</td></tr>
                    <tr><td>Race prep</td><td>9–11</td><td>5</td><td>Full race simulation, transition drills, nutrition rehearsal</td></tr>
                    <tr><td>Taper</td><td>12</td><td>3 (short &amp; easy)</td><td>Rest, kit check, course walk-through — arrive fresh</td></tr>
                    </tbody>
                    </table>
                    """,
                BodyAr = """
                    <table>
                    <thead><tr><th>المرحلة</th><th>الأسابيع</th><th>حصص / أسبوع</th><th>التركيز</th></tr></thead>
                    <tbody>
                    <tr><td>التأسيس</td><td>1–4</td><td>4 (سباحتان · دراجة · جري)</td><td>التقنية، وجهد مريح، وبناء العادة</td></tr>
                    <tr><td>البناء</td><td>5–8</td><td>5 (+ تمرين مركب)</td><td>جولات أطول، وسباحة مياه مفتوحة، وفترات بإيقاع السباق</td></tr>
                    <tr><td>التحضير للسباق</td><td>9–11</td><td>5</td><td>محاكاة كاملة للسباق، وتدريبات الانتقال، وتجربة التغذية</td></tr>
                    <tr><td>التخفيف</td><td>12</td><td>3 (قصيرة وخفيفة)</td><td>راحة، وفحص المعدات، ومعاينة المسار — لتصل بكامل نشاطك</td></tr>
                    </tbody>
                    </table>
                    """,
            },
        ]);

        TrainingGuide Upcoming(string slug, (string En, string Ar) title, (string En, string Ar) summary, (string En, string Ar) level, int sortOrder) => new()
        {
            Slug = slug, TitleEn = title.En, TitleAr = title.Ar, SummaryEn = summary.En, SummaryAr = summary.Ar,
            LevelEn = level.En, LevelAr = level.Ar, IsPublished = false, SortOrder = sortOrder,
        };

        var upcomingGuides = new List<TrainingGuide>
        {
            Upcoming("olympic-progression",
                ("Olympic-distance progression", "التدرج للمسافة الأولمبية"),
                ("Stepping up from sprint to 1500 / 40 / 10.", "الانتقال من المسافة القصيرة إلى 1500 / 40 / 10."),
                ("Intermediate", "متوسط"), 2),
            Upcoming("youth-handbook",
                ("Youth development handbook", "دليل تطوير الناشئين"),
                ("For coaches and parents of U13–U19 athletes.", "للمدربين وأولياء أمور رياضيي 13–19 سنة."),
                ("Coaches & parents", "للمدربين وأولياء الأمور"), 3),
            Upcoming("saudi-summer",
                ("Training in Saudi summer", "التدريب في صيف السعودية"),
                ("Heat adaptation, hydration and indoor alternatives.", "التأقلم مع الحرارة والترطيب والبدائل الداخلية."),
                ("All levels", "لجميع المستويات"), 4),
        };

        db.Documents.AddRange(documents);
        db.Rules.AddRange(rules);
        db.TrainingGuides.Add(beginnerGuide);
        db.TrainingGuides.AddRange(upcomingGuides);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Prototype sizes are given as e.g. <c>"4.8 MB"</c>; <c>"4.8 MB" → 4_800_000</c> bytes.</summary>
    private static long Megabytes(string sizeMb) =>
        (long)(double.Parse(sizeMb.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture) * 1_000_000);

    /// <summary>Prototype "updated" values are <c>"yyyy-MM"</c>; stored as the first of that month.</summary>
    private static DateOnly FirstOfMonth(string yearMonth)
    {
        var parts = yearMonth.Split('-');
        return new DateOnly(int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture), int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture), 1);
    }
}
