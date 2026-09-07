using Microsoft.EntityFrameworkCore;
using Triathlon.Web.Domain.Events;

namespace Triathlon.Web.Data.Seed;

/// <summary>
/// Transcribes the prototype's demonstration cities and events (<c>assets/js/data.js</c>) into the
/// database. Idempotent: skips entirely once any city exists, so a developer's or a test's database
/// is only ever seeded once and never duplicated by a restart.
/// </summary>
public static class SeedEvents
{
    public static async Task RunAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Cities.AnyAsync(ct)) return;

        var cities = new Dictionary<string, City>
        {
            ["riyadh"] = new() { Key = "riyadh", NameEn = "Riyadh", NameAr = "الرياض", SvgX = 577, SvgY = 365, SortOrder = 1 },
            ["jeddah"] = new() { Key = "jeddah", NameEn = "Jeddah", NameAr = "جدة", SvgX = 236, SvgY = 525, LabelAtEnd = true, SortOrder = 2 },
            ["dammam"] = new() { Key = "dammam", NameEn = "Dammam", NameAr = "الدمام", SvgX = 732, SvgY = 280, SortOrder = 3 },
            ["neom"] = new() { Key = "neom", NameEn = "NEOM", NameAr = "نيوم", SvgX = 78, SvgY = 195, SortOrder = 4 },
            ["alula"] = new() { Key = "alula", NameEn = "AlUla", NameAr = "العلا", SvgX = 177, SvgY = 300, SortOrder = 5 },
            ["abha"] = new() { Key = "abha", NameEn = "Abha", NameAr = "أبها", SvgX = 386, SvgY = 668, SortOrder = 6 },
            ["yanbu"] = new() { Key = "yanbu", NameEn = "Yanbu", NameAr = "ينبع", SvgX = 190, SvgY = 400, SortOrder = 7 },
            ["taif"] = new() { Key = "taif", NameEn = "Taif", NameAr = "الطائف", SvgX = 291, SvgY = 536, LabelDy = 30, SortOrder = 8 },
        };
        db.Cities.AddRange(cities.Values);

        Event E(string slug, EventType type, string status, string date, string time, string city, string season,
            (string En, string Ar) venue, (string En, string Ar) title, (string? Swim, string? Bike, string? Run) d,
            string categories, (string En, string Ar) desc) => new()
        {
            Slug = slug, Type = type, IsPublished = true, Season = season,
            DateStart = DateOnly.Parse(date, System.Globalization.CultureInfo.InvariantCulture),
            StartTime = TimeOnly.Parse(time, System.Globalization.CultureInfo.InvariantCulture),
            City = cities[city], VenueEn = venue.En, VenueAr = venue.Ar, TitleEn = title.En, TitleAr = title.Ar,
            SwimDistance = d.Swim, BikeDistance = d.Bike, RunDistance = d.Run, Categories = categories,
            DescriptionEn = desc.En, DescriptionAr = desc.Ar,
            // prototype status -> registration: "open" = Internal + open, "soon" = Internal + closed, "done" = None
            RegistrationMode = status == "done" ? RegistrationMode.None : RegistrationMode.Internal,
            RegistrationOpen = status == "open",
        };

        var events = new List<Event>
        {
            E("yanbu-openwater-2026", EventType.Community, "open", "2026-09-26", "06:30", "yanbu", "2026-27",
              ("Yanbu Waterfront", "واجهة ينبع البحرية"), ("Yanbu Open Water Festival", "مهرجان ينبع للسباحة في المياه المفتوحة"),
              ("1000m", null, null), "Open,Youth,Masters",
              ("A community open-water swim on the Red Sea — the gateway discipline into triathlon. Coached warm-up, safety kayaks, and timing chips for everyone.",
               "سباحة مجتمعية في المياه المفتوحة على البحر الأحمر — البوابة الأولى نحو الترايثلون. إحماء بإشراف مدربين، قوارب سلامة، وشرائح توقيت لجميع المشاركين.")),

            E("riyadh-sprint-2026", EventType.Competition, "open", "2026-10-17", "06:00", "riyadh", "2026-27",
              ("King Salman Park circuit", "حلبة حديقة الملك سلمان"), ("Riyadh Sprint Triathlon", "ترايثلون الرياض للمسافة القصيرة"),
              ("750m", "20km", "5km"), "Elite,Age Group,Junior",
              ("Round 1 of the national series. A fast, closed-road sprint course in the heart of the capital, raced under World Triathlon sprint rules with a wave start.",
               "الجولة الأولى من السلسلة الوطنية. مسار قصير وسريع على طرق مغلقة في قلب العاصمة، وفق قوانين الاتحاد الدولي للمسافة القصيرة وبانطلاقة على دفعات.")),

            E("riyadh-aquathlon-2026", EventType.Community, "open", "2026-10-31", "07:00", "riyadh", "2026-27",
              ("Diplomatic Quarter lakes", "بحيرات حي السفارات"), ("Riyadh Community Aquathlon", "أكواثلون الرياض المجتمعي"),
              ("400m", null, "2.5km"), "Open,Family,Youth",
              ("Swim-run format with no bike needed — the easiest way to try multisport. Free entry for first-time participants registered through a club.",
               "سباق سباحة وجري دون الحاجة إلى دراجة — أسهل طريقة لتجربة الرياضات المتعددة. الدخول مجاني للمشاركين لأول مرة عبر الأندية.")),

            E("abha-youth-2026", EventType.Community, "soon", "2026-11-14", "08:00", "abha", "2026-27",
              ("Abha Highlands park", "منتزه مرتفعات أبها"), ("Abha Highlands Youth Race", "سباق مرتفعات أبها للناشئين"),
              ("200m", "5km", "1.5km"), "U13,U15,U19",
              ("Youth development race at 2,200m altitude, run with the regional schools programme. Loaner bikes and helmets available on site.",
               "سباق تطوير للناشئين على ارتفاع 2,200 متر بالتعاون مع برنامج المدارس في المنطقة. تتوفر دراجات وخوذات للإعارة في الموقع.")),

            E("jeddah-olympic-2026", EventType.Competition, "open", "2026-11-21", "05:30", "jeddah", "2026-27",
              ("Jeddah Corniche", "كورنيش جدة"), ("Jeddah Corniche Olympic Triathlon", "ترايثلون كورنيش جدة للمسافة الأولمبية"),
              ("1500m", "40km", "10km"), "Elite,Age Group,Relay",
              ("Round 2 of the national series over the full Olympic distance. Red Sea swim start at dawn, a flat four-lap bike on the corniche, and a waterfront run.",
               "الجولة الثانية من السلسلة الوطنية على المسافة الأولمبية الكاملة. انطلاقة سباحة في البحر الأحمر عند الفجر، ومسار دراجات مسطح من أربع لفات على الكورنيش، وجري على الواجهة البحرية.")),

            E("neom-duathlon-2026", EventType.Competition, "soon", "2026-12-05", "07:00", "neom", "2026-27",
              ("NEOM Bay circuit", "حلبة خليج نيوم"), ("NEOM Duathlon Challenge", "تحدي نيوم للدواثلون"),
              (null, "30km", "5km + 2.5km"), "Elite,Age Group",
              ("Run–bike–run format through the mountains of the northwest. Winter conditions, closed roads, and drafting-legal racing for the elite wave.",
               "سباق جري ثم دراجة ثم جري بين جبال الشمال الغربي. أجواء شتوية وطرق مغلقة، ويُسمح بالتلاحق الهوائي لفئة النخبة.")),

            E("taif-family-2026", EventType.Community, "soon", "2026-12-19", "08:30", "taif", "2026-27",
              ("Al Rudaf Park", "منتزه الردف"), ("Taif Family Try-a-Tri", "ترايثلون الطائف العائلي التجريبي"),
              ("100m", "3km", "1km"), "Family,Open",
              ("A festival-style introduction day: mini distances, pacing volunteers on every leg, and a finish-line medal for every athlete — ages 8 and up.",
               "يوم تعريفي بأجواء مهرجانية: مسافات مصغّرة، ومتطوعون مرافقون في كل مرحلة، وميدالية عند خط النهاية لكل مشارك — من عمر 8 سنوات فما فوق.")),

            E("alula-desert-2027", EventType.Competition, "soon", "2027-01-23", "07:30", "alula", "2026-27",
              ("AlUla Old Town course", "مسار البلدة القديمة بالعلا"), ("AlUla Desert Triathlon", "ترايثلون العلا الصحراوي"),
              ("750m", "20km", "5km"), "Elite,Age Group",
              ("Round 3 of the national series. A sprint raced between sandstone canyons and heritage sites — the most photographed course on the calendar.",
               "الجولة الثالثة من السلسلة الوطنية. سباق قصير بين الأخاديد الرملية والمواقع التراثية — المسار الأكثر تصويراً في التقويم.")),

            E("dammam-eastern-2027", EventType.Competition, "soon", "2027-02-13", "06:00", "dammam", "2026-27",
              ("Half Moon Bay", "شاطئ نصف القمر"), ("Eastern Province Championship", "بطولة المنطقة الشرقية"),
              ("1500m", "40km", "10km"), "Elite,Age Group,Para",
              ("Round 4 of the national series on the Gulf coast, including the season's para-triathlon championship on a fully accessible course.",
               "الجولة الرابعة من السلسلة الوطنية على ساحل الخليج، وتشمل بطولة الموسم لترايثلون ذوي الإعاقة على مسار مهيأ بالكامل.")),

            E("riyadh-finals-2027", EventType.Competition, "soon", "2027-03-06", "06:00", "riyadh", "2026-27",
              ("King Salman Park circuit", "حلبة حديقة الملك سلمان"), ("National Championship Finals", "نهائيات البطولة الوطنية"),
              ("1500m", "40km", "10km"), "Elite,Age Group,Junior",
              ("The season decider. National titles in every category, national-team selection points, and the crowning of the 2026–27 series champions.",
               "حسم الموسم. ألقاب وطنية في جميع الفئات، ونقاط اختيار للمنتخب الوطني، وتتويج أبطال سلسلة 2026–27.")),

            // ------- completed events (kept out of current listings automatically) -------

            E("jeddah-opener-2026", EventType.Competition, "done", "2026-03-14", "06:00", "jeddah", "2025-26",
              ("Jeddah Corniche", "كورنيش جدة"), ("Jeddah Season Opener Sprint", "افتتاحية موسم جدة للمسافة القصيرة"),
              ("750m", "20km", "5km"), "Elite,Age Group",
              ("The 2026 season opener on the corniche. 212 finishers and the fastest sprint time recorded on Saudi soil.",
               "افتتاحية موسم 2026 على الكورنيش. 212 متسابقاً أنهوا السباق، مع أسرع زمن للمسافة القصيرة يُسجل على أرض سعودية.")),

            E("dammam-spring-2026", EventType.Competition, "done", "2026-04-25", "06:00", "dammam", "2025-26",
              ("Half Moon Bay", "شاطئ نصف القمر"), ("Dammam Spring Triathlon", "ترايثلون الدمام الربيعي"),
              ("750m", "20km", "5km"), "Elite,Age Group,Junior",
              ("Spring sprint on the Gulf, doubling as junior trials for the Asia Triathlon development camp.",
               "سباق ربيعي قصير على الخليج، أقيم بالتزامن مع تصفيات الناشئين لمعسكر الاتحاد الآسيوي التطويري.")),

            E("jeddah-kasc-aquathlon-2026", EventType.Community, "done", "2026-05-09", "17:00", "jeddah", "2025-26",
              ("King Abdullah Sports City", "مدينة الملك عبدالله الرياضية"), ("KASC Sunset Aquathlon", "أكواثلون الغروب بمدينة الملك عبدالله"),
              ("300m", null, "2km"), "Open,Family",
              ("An evening community swim-run that welcomed 340 first-time multisport athletes.",
               "سباق مجتمعي مسائي للسباحة والجري استقبل 340 رياضياً يخوضون الرياضات المتعددة لأول مرة.")),
        };

        // Results for the completed events (data.js `results` arrays).
        var opener = events.Single(e => e.Slug == "jeddah-opener-2026");
        opener.Results.AddRange(
        [
            new() { Position = 1, AthleteEn = "S. Al-Harbi", AthleteAr = "س. الحربي", ClubEn = "Riyadh Tri Club", ClubAr = "نادي الرياض للترايثلون", Time = "58:41" },
            new() { Position = 2, AthleteEn = "M. Al-Qahtani", AthleteAr = "م. القحطاني", ClubEn = "Jeddah Waves", ClubAr = "أمواج جدة", Time = "59:12" },
            new() { Position = 3, AthleteEn = "F. Al-Otaibi", AthleteAr = "ف. العتيبي", ClubEn = "Eastern Endurance", ClubAr = "تحمّل الشرقية", Time = "59:47" },
            new() { Position = 4, AthleteEn = "A. Al-Ghamdi", AthleteAr = "أ. الغامدي", ClubEn = "Jeddah Waves", ClubAr = "أمواج جدة", Time = "1:00:26" },
            new() { Position = 5, AthleteEn = "K. Al-Shehri", AthleteAr = "ك. الشهري", ClubEn = "Asir Peaks", ClubAr = "قمم عسير", Time = "1:01:03" },
        ]);

        var springDammam = events.Single(e => e.Slug == "dammam-spring-2026");
        springDammam.Results.AddRange(
        [
            new() { Position = 1, AthleteEn = "M. Al-Qahtani", AthleteAr = "م. القحطاني", ClubEn = "Jeddah Waves", ClubAr = "أمواج جدة", Time = "59:58" },
            new() { Position = 2, AthleteEn = "S. Al-Harbi", AthleteAr = "س. الحربي", ClubEn = "Riyadh Tri Club", ClubAr = "نادي الرياض للترايثلون", Time = "1:00:21" },
            new() { Position = 3, AthleteEn = "R. Al-Dossari", AthleteAr = "ر. الدوسري", ClubEn = "Eastern Endurance", ClubAr = "تحمّل الشرقية", Time = "1:00:59" },
        ]);

        db.Events.AddRange(events);
        await db.SaveChangesAsync(ct);
    }
}
