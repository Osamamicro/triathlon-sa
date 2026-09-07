using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Domain.Content;

namespace Triathlon.Web.Data.Seed;

/// <summary>
/// Transcribes the prototype's navigation, governance structure and editable pages
/// (<c>join.html</c>, <c>rules.html</c>, <c>training.html</c> and the contact details in the footer)
/// into the CMS tables, so the header, the footer and every block-built page render from rows.
/// <para>
/// Each of the three aggregates guards its own table rather than the seeder guarding one of them:
/// a database seeded by an earlier task already has events and documents in it, and must still pick
/// up the navigation, the committees and the pages on its next start. The <c>home</c> page is not
/// here — Task 2.3.A seeds it with the rest of the home page.
/// </para>
/// </summary>
public static class SeedPages
{
    public static async Task RunAsync(AppDbContext db, CancellationToken ct)
    {
        await NavigationAsync(db, ct);
        await CommitteesAsync(db, ct);
        await ContentPagesAsync(db, ct);
    }

    // ---------------------------------------------------------------------------------------
    // Navigation
    // ---------------------------------------------------------------------------------------

    private static async Task NavigationAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.NavItems.AnyAsync(ct)) return;

        var order = 0;
        NavItem N(NavLocation location, string href, (string En, string Ar) label) => new()
        {
            Location = location, Href = href, LabelEn = label.En, LabelAr = label.Ar,
            SortOrder = ++order, IsPublished = true,
        };

        db.NavItems.AddRange(
            // Header. Sections shipping in later weeks are already listed; they 404 until they land.
            N(NavLocation.Header, "", ("Home", "الرئيسية")),
            N(NavLocation.Header, "events", ("Events", "الفعاليات")),
            N(NavLocation.Header, "events/timeline", ("Season", "الموسم")),
            N(NavLocation.Header, "join", ("Join", "الانضمام")),
            N(NavLocation.Header, "training", ("Training", "التدريب")),
            N(NavLocation.Header, "rules", ("Rules", "اللوائح")),
            N(NavLocation.Header, "governance", ("Governance", "الحوكمة")),
            N(NavLocation.Header, "statistics", ("Statistics", "الإحصائيات")),

            // Footer, first column.
            N(NavLocation.FooterCompete, "events", ("Events & calendar", "الفعاليات والتقويم")),
            N(NavLocation.FooterCompete, "events/timeline", ("Season timeline", "الجدول الزمني للموسم")),
            N(NavLocation.FooterCompete, "rules", ("Rules & regulations", "اللوائح والأنظمة")),
            N(NavLocation.FooterCompete, "statistics", ("Federation statistics", "إحصائيات الاتحاد")),

            // Footer, second column.
            N(NavLocation.FooterInvolved, "register", ("Athlete registration", "تسجيل الرياضيين")),
            N(NavLocation.FooterInvolved, "join", ("Become an athlete", "كن رياضياً")),
            N(NavLocation.FooterInvolved, "training", ("Training guide", "دليل التدريب")),
            N(NavLocation.FooterInvolved, "join#clubs", ("Affiliated clubs", "الأندية المنتسبة")),
            N(NavLocation.FooterInvolved, "governance", ("Governance & transparency", "الحوكمة والشفافية")));

        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------------------------------
    // Committees
    // ---------------------------------------------------------------------------------------

    private static async Task CommitteesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Committees.AnyAsync(ct)) return;

        var order = 0;
        Committee C((string En, string Ar) kind, (string En, string Ar) name, (string En, string Ar) description) => new()
        {
            KindEn = kind.En, KindAr = kind.Ar, NameEn = name.En, NameAr = name.Ar,
            DescriptionEn = description.En, DescriptionAr = description.Ar,
            SortOrder = ++order, IsPublished = true,
        };

        db.Committees.AddRange(
            C(("Board", "المجلس"), ("Board of Directors", "مجلس الإدارة"),
                ("Elected board setting strategy and budget, meeting quarterly. Minutes are published in the library below.",
                 "مجلس منتخب يضع الاستراتيجية والميزانية ويجتمع كل ربع سنة. تُنشر المحاضر في المكتبة أدناه.")),
            C(("Technical", "الفنية"), ("Technical Committee", "اللجنة الفنية"),
                ("Competition rules, officials' certification, and course sanctioning for every STF event.",
                 "قوانين المنافسات، واعتماد الحكام، وترخيص المسارات لجميع فعاليات الاتحاد.")),
            C(("Audit", "المراجعة"), ("Audit & Governance", "المراجعة والحوكمة"),
                ("Independent oversight of finances and compliance; commissions the annual external audit.",
                 "إشراف مستقل على المالية والامتثال، وتكليف المراجعة الخارجية السنوية.")),
            C(("Community", "المجتمع"), ("Athletes' Commission", "لجنة الرياضيين"),
                ("Elected athlete voice on selection policy, safeguarding, and the race calendar.",
                 "صوت الرياضيين المنتخب في سياسات الاختيار والحماية وتقويم السباقات.")));

        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------------------------------
    // Pages
    // ---------------------------------------------------------------------------------------

    private static async Task ContentPagesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Pages.AnyAsync(ct)) return;

        db.Pages.AddRange(Join(), Contact(), Rules(), Training());

        await db.SaveChangesAsync(ct);
    }

    private static Page Join()
    {
        var page = new Page
        {
            Slug = "join",
            TitleEn = "Become an Athlete", TitleAr = "كن رياضياً",
            MetaDescriptionEn = "Four steps from curious to licensed: pick a club, register with the federation, receive your athlete licence and enter your first event.",
            MetaDescriptionAr = "أربع خطوات من الفضول إلى الرخصة: اختر نادياً، وسجّل لدى الاتحاد، واستلم رخصتك الرياضية، وشارك في أول فعالية.",
            IsPublished = true,
        };

        page.Blocks.AddRange(
        [
            Block(1, BlockType.Hero,
                eyebrow: ("Start your journey", "ابدأ رحلتك"),
                title: ("Become an Athlete", "كن رياضياً"),
                body: ("You don't need a racing background — you need a start line. Four steps take you from curious to licensed, and a clear pathway leads from community events to the national team.",
                       "لا تحتاج إلى خلفية تنافسية — تحتاج فقط إلى خط انطلاق. أربع خطوات تنقلك من الفضول إلى الرخصة الرياضية، ومسار واضح يمتد من الفعاليات المجتمعية إلى المنتخب الوطني."),
                cta: (("Register online", "سجّل إلكترونياً"), "register"),
                secondary: (("Find a club first", "اعثر على نادٍ أولاً"), "#clubs")),

            Block(2, BlockType.Steps,
                eyebrow: ("Registration", "التسجيل"),
                title: ("Four steps to your license", "أربع خطوات إلى رخصتك"),
                items:
                [
                    new BlockItem(
                        TitleEn: "Pick a club near you", TitleAr: "اختر نادياً قريباً منك",
                        BodyEn: "Seventeen affiliated clubs across nine regions run coached sessions for every level — see the list below.",
                        BodyAr: "سبعة عشر نادياً منتسباً في تسع مناطق تقدم حصصاً بإشراف مدربين لجميع المستويات — انظر القائمة أدناه.",
                        Color: "swim"),
                    new BlockItem(
                        TitleEn: "Register with the federation", TitleAr: "سجّل لدى الاتحاد",
                        BodyEn: "Submit your national ID / iqama, a medical declaration, and your category. Registration is done once and renewed each season.",
                        BodyAr: "قدّم الهوية الوطنية / الإقامة وإقراراً طبياً وفئتك. يتم التسجيل مرة واحدة ويُجدد كل موسم.",
                        Color: "bike"),
                    new BlockItem(
                        TitleEn: "Receive your athlete license", TitleAr: "استلم رخصتك الرياضية",
                        BodyEn: "Your license number is your entry key to every sanctioned race — competition and community alike.",
                        BodyAr: "رقم رخصتك هو مفتاح دخولك لكل سباق معتمد — في البطولات والفعاليات المجتمعية على حد سواء.",
                        Color: "run"),
                    new BlockItem(
                        TitleEn: "Enter your first event", TitleAr: "شارك في أول فعالية",
                        BodyEn: "Start with an aquathlon or a try-a-tri — every community event has a first-timer wave with pacing volunteers.",
                        BodyAr: "ابدأ بأكواثلون أو سباق تجريبي — كل فعالية مجتمعية تضم دفعة للمبتدئين يرافقهم متطوعون.",
                        Color: "bike"),
                ]),

            Block(3, BlockType.Table,
                eyebrow: ("Categories", "الفئات"),
                title: ("Find your category", "اعرف فئتك"),
                items:
                [
                    // The first item is the header row.
                    Row(["Category", "Ages", "Typical distance", "Entry route"],
                        ["الفئة", "الأعمار", "المسافة المعتادة", "طريقة الدخول"]),
                    Row(["Youth", "U13 · U15 · U19", "100–400m / 3–10km / 1–3km", "Club or schools programme"],
                        ["الناشئون", "U13 · U15 · U19", "100–400m / 3–10km / 1–3km", "النادي أو برنامج المدارس"]),
                    Row(["Age Group", "20–69 (5-yr bands)", "Sprint · Olympic", "Open registration"],
                        ["الفئات العمرية", "20–69 (5-yr bands)", "Sprint · Olympic", "تسجيل مفتوح"]),
                    Row(["Elite", "16+", "Sprint · Olympic (draft-legal)", "Qualification standard"],
                        ["النخبة", "16+", "Sprint · Olympic (draft-legal)", "معيار تأهيل"]),
                    Row(["Para triathlon", "16+", "Sprint (adapted)", "Classification + open registration"],
                        ["ترايثلون ذوي الإعاقة", "16+", "Sprint (adapted)", "تصنيف + تسجيل مفتوح"]),
                    Row(["Community / Open", "8+", "Try-a-tri · Aquathlon", "No license needed — day pass"],
                        ["مجتمعي / مفتوح", "8+", "Try-a-tri · Aquathlon", "دون رخصة — تصريح يوم واحد"]),
                ]),

            Block(4, BlockType.Cards, variant: "pathway",
                eyebrow: ("Athlete pathway", "مسار الرياضي"),
                title: ("From first swim to the flag", "من أول سباحة إلى رفع العلم"),
                items:
                [
                    new BlockItem(
                        EyebrowEn: "01", EyebrowAr: "01",
                        TitleEn: "Community", TitleAr: "المجتمع",
                        BodyEn: "Try-a-tri, aquathlons, club sessions",
                        BodyAr: "سباقات تجريبية وأكواثلون وحصص الأندية",
                        Color: "run"),
                    new BlockItem(
                        EyebrowEn: "02", EyebrowAr: "02",
                        TitleEn: "Regional", TitleAr: "المناطق",
                        BodyEn: "Regional championships and rankings",
                        BodyAr: "بطولات المناطق والتصنيف",
                        Color: "swim"),
                    new BlockItem(
                        EyebrowEn: "03", EyebrowAr: "03",
                        TitleEn: "National series", TitleAr: "السلسلة الوطنية",
                        BodyEn: "Series rounds, national titles, elite standard",
                        BodyAr: "جولات السلسلة والألقاب الوطنية ومعيار النخبة",
                        Color: "bike"),
                    new BlockItem(
                        EyebrowEn: "04", EyebrowAr: "04",
                        TitleEn: "National team", TitleAr: "المنتخب الوطني",
                        // Card bodies are HTML, so the ampersand is written as an entity.
                        BodyEn: "Asia Triathlon &amp; World Triathlon starts",
                        BodyAr: "مشاركات آسيوية ودولية",
                        Color: "bike"),
                ]),

            Block(5, BlockType.Clubs, anchor: "clubs",
                eyebrow: ("Where to train", "أين تتدرب"),
                title: ("Affiliated clubs", "الأندية المنتسبة")),

            Block(6, BlockType.Cta,
                title: ("Questions before you start?", "لديك أسئلة قبل البدء؟"),
                body: ("Write to the federation and we'll route you to the right club and category.",
                       "راسل الاتحاد وسنوجهك إلى النادي والفئة المناسبين لك."),
                cta: (("info@triathlon.sa", "info@triathlon.sa"), "mailto:info@triathlon.sa")),
        ]);

        return page;
    }

    private static Page Contact()
    {
        var page = new Page
        {
            Slug = "contact",
            TitleEn = "Contact", TitleAr = "تواصل معنا",
            MetaDescriptionEn = "Reach the Saudi Triathlon Federation by email or social media, or visit the headquarters in Riyadh.",
            MetaDescriptionAr = "تواصل مع الاتحاد السعودي للترايثلون عبر البريد الإلكتروني أو حسابات التواصل، أو زر المقر في الرياض.",
            IsPublished = true,
        };

        page.Blocks.AddRange(
        [
            Block(1, BlockType.Hero,
                eyebrow: ("Get in touch", "تواصل معنا"),
                title: ("Contact the federation", "تواصل مع الاتحاد"),
                body: ("The Saudi Triathlon Federation is based at Prince Faisal Bin Fahad Olympic Complex, Riyadh.",
                       "مقر الاتحاد السعودي للترايثلون في مجمع الأمير فيصل بن فهد الأولمبي بالرياض.")),

            Block(2, BlockType.Cards,
                eyebrow: ("Reach us", "كيف تصل إلينا"),
                title: ("Three ways to get an answer", "ثلاث طرق للوصول إلينا"),
                items:
                [
                    new BlockItem(
                        EyebrowEn: "Email", EyebrowAr: "البريد الإلكتروني",
                        TitleEn: "info@triathlon.sa", TitleAr: "info@triathlon.sa",
                        BodyEn: "General enquiries, media requests and club affiliation.",
                        BodyAr: "الاستفسارات العامة وطلبات الإعلام وانتساب الأندية.",
                        Href: "mailto:info@triathlon.sa", Color: "swim"),
                    new BlockItem(
                        EyebrowEn: "Social", EyebrowAr: "حسابات التواصل",
                        TitleEn: "@triathlonksa", TitleAr: "@triathlonksa",
                        BodyEn: "Instagram, X and TikTok — start lists, race photos and results.",
                        BodyAr: "إنستغرام وإكس وتيك توك — قوائم الانطلاق وصور السباقات والنتائج.",
                        Href: "https://x.com/triathlonksa", Color: "bike"),
                    new BlockItem(
                        EyebrowEn: "Address", EyebrowAr: "العنوان",
                        TitleEn: "Headquarters", TitleAr: "المقر الرئيسي",
                        BodyEn: "Prince Faisal Bin Fahad Olympic Complex, Riyadh, Kingdom of Saudi Arabia.",
                        BodyAr: "مجمع الأمير فيصل بن فهد الأولمبي، الرياض، المملكة العربية السعودية.",
                        Color: "run"),
                ]),

            Block(3, BlockType.Cta,
                title: ("Media & partnerships", "الإعلام والشراكات"),
                body: ("Write to us and we route you to the right committee.",
                       "راسلنا وسنوجّه رسالتك إلى اللجنة المختصة."),
                cta: (("Email the federation", "راسل الاتحاد"), "mailto:info@triathlon.sa")),
        ]);

        return page;
    }

    private static Page Rules()
    {
        var page = new Page
        {
            Slug = "rules",
            TitleEn = "Rules & Regulations", TitleAr = "اللوائح والأنظمة",
            IsPublished = true,
        };

        page.Blocks.AddRange(
        [
            Block(1, BlockType.Cards,
                eyebrow: ("Quick reference", "مرجع سريع"),
                title: ("Race day at a glance", "يوم السباق باختصار"),
                items:
                [
                    new BlockItem(
                        EyebrowEn: "Swim", EyebrowAr: "السباحة",
                        BodyEn: """
                            <ul class="list-check">
                            <li>Wetsuits allowed below 24.5°C water temperature</li>
                            <li>Wave starts by category; caps are mandatory</li>
                            <li>Safety kayaks may assist without disqualification</li>
                            </ul>
                            """,
                        BodyAr: """
                            <ul class="list-check">
                            <li>يسمح ببدلات السباحة عند حرارة ماء أقل من 24.5°</li>
                            <li>انطلاقات على دفعات حسب الفئة؛ قبعة السباحة إلزامية</li>
                            <li>يمكن الاستناد إلى قوارب السلامة دون إقصاء</li>
                            </ul>
                            """,
                        Color: "swim"),
                    new BlockItem(
                        EyebrowEn: "Bike", EyebrowAr: "الدراجة",
                        BodyEn: """
                            <ul class="list-check">
                            <li>Helmet fastened before touching the bike — always</li>
                            <li>Drafting legal for Elite; 10m draft zone for Age Group</li>
                            <li>Blue card: drafting penalty served in the penalty box</li>
                            </ul>
                            """,
                        BodyAr: """
                            <ul class="list-check">
                            <li>اربط الخوذة قبل لمس الدراجة — دائماً</li>
                            <li>التلاحق مسموح للنخبة؛ ومنطقة 10 أمتار للفئات العمرية</li>
                            <li>البطاقة الزرقاء: عقوبة تلاحق تُقضى في منطقة الجزاء</li>
                            </ul>
                            """,
                        Color: "bike"),
                    new BlockItem(
                        EyebrowEn: "Run & transitions", EyebrowAr: "الجري والانتقالات",
                        BodyEn: """
                            <ul class="list-check">
                            <li>Race belt with number worn on the front during the run</li>
                            <li>Equipment only inside your marked transition box</li>
                            <li>Yellow card: conduct warning · two yellows = disqualification</li>
                            </ul>
                            """,
                        BodyAr: """
                            <ul class="list-check">
                            <li>حزام الرقم يُلبس من الأمام أثناء الجري</li>
                            <li>المعدات داخل مربع الانتقال المخصص لك فقط</li>
                            <li>البطاقة الصفراء: إنذار سلوك · بطاقتان = إقصاء</li>
                            </ul>
                            """,
                        Color: "run"),
                ]),

            Block(2, BlockType.RichText,
                body: ("""<p class="muted">This summary is informational — the downloadable Competition Rules are the authoritative text.</p>""",
                       """<p class="muted">هذا الملخص للاطلاع فقط — قوانين المنافسات القابلة للتحميل هي النص المعتمد.</p>""")),
        ]);

        return page;
    }

    private static Page Training()
    {
        var page = new Page
        {
            Slug = "training",
            TitleEn = "Training Guide", TitleAr = "دليل التدريب",
            IsPublished = true,
        };

        page.Blocks.AddRange(
        [
            Block(1, BlockType.Cards,
                eyebrow: ("Foundations", "الأساسيات"),
                title: ("One sport, three skills", "رياضة واحدة، ثلاث مهارات"),
                items:
                [
                    new BlockItem(
                        EyebrowEn: "Swim · 750m", EyebrowAr: "سباحة · 750م",
                        TitleEn: "Comfort before speed", TitleAr: "الارتياح قبل السرعة",
                        BodyEn: "Two pool sessions a week. Master breathing and sighting first — open-water calm wins more time than a faster stroke. Join a club session before your first sea swim.",
                        BodyAr: "حصتان في المسبح أسبوعياً. أتقن التنفس والنظر للأمام أولاً — فالهدوء في المياه المفتوحة يكسبك وقتاً أكثر من سرعة الضربات. انضم لحصة نادٍ قبل أول سباحة بحرية.",
                        Color: "swim"),
                    new BlockItem(
                        EyebrowEn: "Bike · 20km", EyebrowAr: "دراجة · 20كم",
                        TitleEn: "Any bike will do", TitleAr: "أي دراجة تكفي",
                        BodyEn: "Your first race needs a safe bike, not an expensive one. One longer weekend ride plus one short mid-week spin. Practice drinking while riding — race mornings are warm.",
                        BodyAr: "سباقك الأول يحتاج دراجة آمنة لا مكلفة. جولة أطول في نهاية الأسبوع وأخرى قصيرة منتصفه. تدرّب على الشرب أثناء القيادة — فصباحات السباق دافئة.",
                        Color: "bike"),
                    new BlockItem(
                        EyebrowEn: "Run · 5km", EyebrowAr: "جري · 5كم",
                        TitleEn: "Learn the brick", TitleAr: "تعلّم التمرين المركب",
                        BodyEn: "Running off the bike feels strange — train it. Once a week, add a short 10-minute run straight after a ride. Race-day legs will thank you at T2.",
                        BodyAr: "الجري بعد الدراجة شعور غريب — تدرّب عليه. مرة أسبوعياً أضف جرياً قصيراً لعشر دقائق مباشرة بعد الدراجة. ستشكرك ساقاك عند المنطقة الانتقالية الثانية.",
                        Color: "run"),
                ]),

            Block(2, BlockType.Cta,
                title: ("Train with people, not alone", "تدرّب مع الآخرين لا وحدك"),
                body: ("Every affiliated club runs beginner-friendly group sessions in all three disciplines.",
                       "كل نادٍ منتسب يقدم حصصاً جماعية مناسبة للمبتدئين في الرياضات الثلاث."),
                cta: (("Find a club", "اعثر على نادٍ"), "join#clubs")),
        ]);

        return page;
    }

    // ---------------------------------------------------------------------------------------
    // Block helpers
    // ---------------------------------------------------------------------------------------

    private static PageBlock Block(
        int sortOrder,
        BlockType type,
        string? variant = null,
        string? anchor = null,
        (string En, string Ar)? eyebrow = null,
        (string En, string Ar)? title = null,
        (string En, string Ar)? body = null,
        ((string En, string Ar) Label, string Href)? cta = null,
        ((string En, string Ar) Label, string Href)? secondary = null,
        IEnumerable<BlockItem>? items = null) => new()
    {
        SortOrder = sortOrder,
        Type = type,
        Variant = variant,
        Anchor = anchor,
        EyebrowEn = eyebrow?.En, EyebrowAr = eyebrow?.Ar,
        TitleEn = title?.En, TitleAr = title?.Ar,
        BodyEn = body?.En, BodyAr = body?.Ar,
        CtaLabelEn = cta?.Label.En, CtaLabelAr = cta?.Label.Ar, CtaHref = cta?.Href,
        SecondaryLabelEn = secondary?.Label.En, SecondaryLabelAr = secondary?.Label.Ar, SecondaryHref = secondary?.Href,
        ItemsJson = items is null ? "[]" : BlockItem.Serialize(items),
    };

    /// <summary>
    /// One row of a table block. Cells that read the same in both languages — codes, ages, distances
    /// — are repeated verbatim, which is what makes the view render them in the mono face.
    /// </summary>
    private static BlockItem Row(string[] cellsEn, string[] cellsAr) => new(CellsEn: cellsEn, CellsAr: cellsAr);
}
