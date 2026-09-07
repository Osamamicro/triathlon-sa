using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Domain.Content;

namespace Triathlon.Web.Data.Seed;

/// <summary>
/// Three demonstration news posts written around the seeded season — a race recap, the calendar
/// announcement, and an entries-open notice — so the news list, the post page and the home page's
/// news band have something real to render. Idempotent on its own table.
/// </summary>
public static class SeedNews
{
    public static async Task RunAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.NewsPosts.AnyAsync(ct)) return;

        db.NewsPosts.AddRange(
            new NewsPost
            {
                Slug = "kasc-sunset-aquathlon-recap",
                PublishedOn = new DateOnly(2026, 5, 12),
                IsPublished = true,
                TitleEn = "KASC Sunset Aquathlon welcomes 340 first-timers",
                TitleAr = "أكواثلون الغروب بمدينة الملك عبدالله يستقبل 340 مشاركاً في تجربتهم الأولى",
                SummaryEn = "An evening swim-run at King Abdullah Sports City drew the largest first-timer field the federation has hosted.",
                SummaryAr = "سباق مسائي للسباحة والجري في مدينة الملك عبدالله الرياضية يجمع أكبر عدد من المشاركين الجدد في تاريخ الاتحاد.",
                BodyEn = """
                    <p>Three hundred and forty athletes finished their first multisport race on Saturday evening in Jeddah, where a 300m sea swim and a 2km run along the King Abdullah Sports City promenade made up the shortest event on the federation's calendar — and, by entries, its most popular.</p>
                    <p>Two thirds of the field had never raced before. Every wave started behind a pacing volunteer, club coaches ran a warm-up on the beach an hour before the gun, and finishers left with a timing chip result and an invitation to the nearest affiliated club. The federation will repeat the format in Yanbu and Dammam next season.</p>
                    """,
                BodyAr = """
                    <p>أنهى 340 رياضياً أول سباق متعدد الرياضات في حياتهم مساء السبت في جدة، في سباق يجمع 300 متر سباحة في البحر و2 كيلومتر جرياً على ممشى مدينة الملك عبدالله الرياضية — وهو أقصر سباقات تقويم الاتحاد، وأكثرها إقبالاً بعدد المسجلين.</p>
                    <p>لم يسبق لثلثي المشاركين خوض أي سباق من قبل. انطلقت كل دفعة خلف متطوع يضبط الإيقاع، وأدار مدربو الأندية إحماءً على الشاطئ قبل الانطلاق بساعة، وغادر المشاركون بنتيجة موثقة بشريحة توقيت ودعوة لأقرب نادٍ منتسب. ويعتزم الاتحاد تكرار هذه الصيغة في ينبع والدمام الموسم المقبل.</p>
                    """,
            },
            new NewsPost
            {
                Slug = "national-series-2026-27-calendar",
                PublishedOn = new DateOnly(2026, 6, 30),
                IsPublished = true,
                TitleEn = "2026–27 national series calendar announced: eight cities, six rounds",
                TitleAr = "اعتماد تقويم السلسلة الوطنية 2026–27: ثماني مدن وست جولات",
                SummaryEn = "The season runs from September to March and closes with the national championship in Riyadh.",
                SummaryAr = "الموسم يمتد من سبتمبر إلى مارس ويختتم ببطولة المملكة في الرياض.",
                BodyEn = """
                    <p>The federation has approved the 2026–27 national series: six scoring rounds across Riyadh, Jeddah, AlUla, Dammam, Yanbu and NEOM, opening with the Riyadh Sprint Triathlon in October and closing with the national championship in March.</p>
                    <p>Alongside the series, the calendar carries community events in every host city — aquathlons, try-a-tri waves and open-water festivals that need no licence to enter. The Dammam round hosts this season's para-triathlon championship on a fully accessible course, and series points from all six rounds feed national-team selection.</p>
                    """,
                BodyAr = """
                    <p>اعتمد الاتحاد تقويم السلسلة الوطنية لموسم 2026–27: ست جولات محتسبة في الرياض وجدة والعُلا والدمام وينبع ونيوم، تنطلق بترايثلون الرياض للمسافة القصيرة في أكتوبر وتختتم ببطولة المملكة في مارس.</p>
                    <p>وإلى جانب السلسلة، يضم التقويم فعاليات مجتمعية في كل مدينة مستضيفة — سباقات أكواثلون ودفعات تجريبية ومهرجانات للسباحة في المياه المفتوحة لا تتطلب رخصة للمشاركة. وتستضيف جولة الدمام بطولة ترايثلون ذوي الإعاقة هذا الموسم على مسار مهيأ بالكامل، فيما تدخل نقاط الجولات الست جميعها في اختيار المنتخب الوطني.</p>
                    """,
            },
            new NewsPost
            {
                Slug = "riyadh-sprint-registration-open",
                PublishedOn = new DateOnly(2026, 8, 20),
                IsPublished = true,
                TitleEn = "Registration opens for the Riyadh Sprint Triathlon",
                TitleAr = "فتح باب التسجيل في ترايثلون الرياض للمسافة القصيرة",
                SummaryEn = "Round 1 of the national series takes place on closed roads at King Salman Park on 17 October.",
                SummaryAr = "الجولة الأولى من السلسلة الوطنية تقام على طرق مغلقة في حديقة الملك سلمان يوم 17 أكتوبر.",
                BodyEn = """
                    <p>Entries are open for the season opener: a 750m swim, a 20km closed-road bike and a 5km run through King Salman Park on 17 October, raced under World Triathlon sprint rules with a wave start at 06:00.</p>
                    <p>Elite, age-group and junior categories are all on the start list, and a licensed federation athlete can enter online in a few minutes. Athletes without a licence can register with the federation first — the four steps take about a week — or start with a community aquathlon later in the month.</p>
                    """,
                BodyAr = """
                    <p>فُتح باب التسجيل في افتتاحية الموسم: 750 متر سباحة، و20 كيلومتراً على الدراجة في طرق مغلقة، و5 كيلومترات جرياً داخل حديقة الملك سلمان يوم 17 أكتوبر، وفق قوانين الاتحاد الدولي للمسافة القصيرة وبانطلاقة على دفعات في تمام السادسة صباحاً.</p>
                    <p>يشمل السباق فئات النخبة والفئات العمرية والناشئين، ويستطيع الرياضي المرخّص من الاتحاد إتمام تسجيله إلكترونياً خلال دقائق. أما من لا يملك رخصة فبإمكانه التسجيل لدى الاتحاد أولاً — وتستغرق الخطوات الأربع نحو أسبوع — أو البدء بسباق أكواثلون مجتمعي في وقت لاحق من الشهر.</p>
                    """,
            });

        await db.SaveChangesAsync(ct);
    }
}
