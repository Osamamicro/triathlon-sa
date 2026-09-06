/* ============================================================
   Saudi Triathlon Federation — prototype content source.
   Every figure, event, document and club on the site reads from
   this file. In production this is replaced 1:1 by the dashboard
   API / headless CMS. All values are illustrative placeholders.
   ============================================================ */

const STF = {

  stats: {
    athletes:      { value: 1284,  label: { en: "Registered athletes",       ar: "رياضي مسجل" } },
    elite:         { value: 42,    label: { en: "Elite athletes",            ar: "رياضيو النخبة" } },
    participants:  { value: 18650, label: { en: "Race participations",       ar: "مشاركة في السباقات" } },
    tournaments:   { value: 24,    label: { en: "Tournaments held",          ar: "بطولة أقيمت" } },
    community:     { value: 46,    label: { en: "Community events",          ar: "فعالية مجتمعية" } },
    clubs:         { value: 17,    label: { en: "Affiliated clubs",          ar: "نادياً منتسباً" } },
    regions:       { value: 9,     label: { en: "Active regions",            ar: "مناطق نشطة" } },
    volunteers:    { value: 380,   label: { en: "Trained volunteers",        ar: "متطوع مدرب" } },
    women:         { value: 31,    label: { en: "Women participation",       ar: "مشاركة نسائية" }, suffix: "%" },
    youth:         { value: 27,    label: { en: "Under-19 athletes",         ar: "رياضيون تحت 19" }, suffix: "%" }
  },

  regionAthletes: [
    { key: "riyadh",  name: { en: "Riyadh",           ar: "الرياض" },          value: 512 },
    { key: "makkah",  name: { en: "Makkah (Jeddah)",  ar: "مكة المكرمة (جدة)" }, value: 341 },
    { key: "eastern", name: { en: "Eastern Province", ar: "المنطقة الشرقية" },  value: 214 },
    { key: "madinah", name: { en: "Madinah / Yanbu",  ar: "المدينة / ينبع" },   value: 96 },
    { key: "asir",    name: { en: "Asir (Abha)",      ar: "عسير (أبها)" },      value: 62 },
    { key: "tabuk",   name: { en: "Tabuk / NEOM",     ar: "تبوك / نيوم" },      value: 59 }
  ],

  growth: [
    { year: 2023, athletes: 310 },
    { year: 2024, athletes: 640 },
    { year: 2025, athletes: 980 },
    { year: 2026, athletes: 1284 }
  ],

  cities: {
    riyadh: { x: 577, y: 365, name: { en: "Riyadh",  ar: "الرياض" } },
    jeddah: { x: 236, y: 525, anchor: "end", name: { en: "Jeddah",  ar: "جدة" } },
    dammam: { x: 732, y: 280, name: { en: "Dammam",  ar: "الدمام" } },
    neom:   { x: 78,  y: 195, name: { en: "NEOM",    ar: "نيوم" } },
    alula:  { x: 177, y: 300, name: { en: "AlUla",   ar: "العلا" } },
    abha:   { x: 386, y: 668, name: { en: "Abha",    ar: "أبها" } },
    yanbu:  { x: 190, y: 400, name: { en: "Yanbu",   ar: "ينبع" } },
    taif:   { x: 291, y: 536, dy: 30, name: { en: "Taif",    ar: "الطائف" } }
  },

  events: [
    {
      id: "yanbu-openwater-2026", type: "community", status: "open",
      date: "2026-09-26", time: "06:30",
      city: "yanbu",
      venue: { en: "Yanbu Waterfront", ar: "واجهة ينبع البحرية" },
      title: { en: "Yanbu Open Water Festival", ar: "مهرجان ينبع للسباحة في المياه المفتوحة" },
      distances: { swim: "1000m", bike: null, run: null },
      categories: ["Open", "Youth", "Masters"],
      desc: {
        en: "A community open-water swim on the Red Sea — the gateway discipline into triathlon. Coached warm-up, safety kayaks, and timing chips for everyone.",
        ar: "سباحة مجتمعية في المياه المفتوحة على البحر الأحمر — البوابة الأولى نحو الترايثلون. إحماء بإشراف مدربين، قوارب سلامة، وشرائح توقيت لجميع المشاركين."
      }
    },
    {
      id: "riyadh-sprint-2026", type: "competition", status: "open",
      date: "2026-10-17", time: "06:00",
      city: "riyadh",
      venue: { en: "King Salman Park circuit", ar: "حلبة حديقة الملك سلمان" },
      title: { en: "Riyadh Sprint Triathlon", ar: "ترايثلون الرياض للمسافة القصيرة" },
      distances: { swim: "750m", bike: "20km", run: "5km" },
      categories: ["Elite", "Age Group", "Junior"],
      desc: {
        en: "Round 1 of the national series. A fast, closed-road sprint course in the heart of the capital, raced under World Triathlon sprint rules with a wave start.",
        ar: "الجولة الأولى من السلسلة الوطنية. مسار قصير وسريع على طرق مغلقة في قلب العاصمة، وفق قوانين الاتحاد الدولي للمسافة القصيرة وبانطلاقة على دفعات."
      }
    },
    {
      id: "riyadh-aquathlon-2026", type: "community", status: "open",
      date: "2026-10-31", time: "07:00",
      city: "riyadh",
      venue: { en: "Diplomatic Quarter lakes", ar: "بحيرات حي السفارات" },
      title: { en: "Riyadh Community Aquathlon", ar: "أكواثلون الرياض المجتمعي" },
      distances: { swim: "400m", bike: null, run: "2.5km" },
      categories: ["Open", "Family", "Youth"],
      desc: {
        en: "Swim-run format with no bike needed — the easiest way to try multisport. Free entry for first-time participants registered through a club.",
        ar: "سباق سباحة وجري دون الحاجة إلى دراجة — أسهل طريقة لتجربة الرياضات المتعددة. الدخول مجاني للمشاركين لأول مرة عبر الأندية."
      }
    },
    {
      id: "abha-youth-2026", type: "community", status: "soon",
      date: "2026-11-14", time: "08:00",
      city: "abha",
      venue: { en: "Abha Highlands park", ar: "منتزه مرتفعات أبها" },
      title: { en: "Abha Highlands Youth Race", ar: "سباق مرتفعات أبها للناشئين" },
      distances: { swim: "200m", bike: "5km", run: "1.5km" },
      categories: ["U13", "U15", "U19"],
      desc: {
        en: "Youth development race at 2,200m altitude, run with the regional schools programme. Loaner bikes and helmets available on site.",
        ar: "سباق تطوير للناشئين على ارتفاع 2,200 متر بالتعاون مع برنامج المدارس في المنطقة. تتوفر دراجات وخوذات للإعارة في الموقع."
      }
    },
    {
      id: "jeddah-olympic-2026", type: "competition", status: "open",
      date: "2026-11-21", time: "05:30",
      city: "jeddah",
      venue: { en: "Jeddah Corniche", ar: "كورنيش جدة" },
      title: { en: "Jeddah Corniche Olympic Triathlon", ar: "ترايثلون كورنيش جدة للمسافة الأولمبية" },
      distances: { swim: "1500m", bike: "40km", run: "10km" },
      categories: ["Elite", "Age Group", "Relay"],
      desc: {
        en: "Round 2 of the national series over the full Olympic distance. Red Sea swim start at dawn, a flat four-lap bike on the corniche, and a waterfront run.",
        ar: "الجولة الثانية من السلسلة الوطنية على المسافة الأولمبية الكاملة. انطلاقة سباحة في البحر الأحمر عند الفجر، ومسار دراجات مسطح من أربع لفات على الكورنيش، وجري على الواجهة البحرية."
      }
    },
    {
      id: "neom-duathlon-2026", type: "competition", status: "soon",
      date: "2026-12-05", time: "07:00",
      city: "neom",
      venue: { en: "NEOM Bay circuit", ar: "حلبة خليج نيوم" },
      title: { en: "NEOM Duathlon Challenge", ar: "تحدي نيوم للدواثلون" },
      distances: { swim: null, bike: "30km", run: "5km + 2.5km" },
      categories: ["Elite", "Age Group"],
      desc: {
        en: "Run–bike–run format through the mountains of the northwest. Winter conditions, closed roads, and drafting-legal racing for the elite wave.",
        ar: "سباق جري ثم دراجة ثم جري بين جبال الشمال الغربي. أجواء شتوية وطرق مغلقة، ويُسمح بالتلاحق الهوائي لفئة النخبة."
      }
    },
    {
      id: "taif-family-2026", type: "community", status: "soon",
      date: "2026-12-19", time: "08:30",
      city: "taif",
      venue: { en: "Al Rudaf Park", ar: "منتزه الردف" },
      title: { en: "Taif Family Try-a-Tri", ar: "ترايثلون الطائف العائلي التجريبي" },
      distances: { swim: "100m", bike: "3km", run: "1km" },
      categories: ["Family", "Open"],
      desc: {
        en: "A festival-style introduction day: mini distances, pacing volunteers on every leg, and a finish-line medal for every athlete — ages 8 and up.",
        ar: "يوم تعريفي بأجواء مهرجانية: مسافات مصغّرة، ومتطوعون مرافقون في كل مرحلة، وميدالية عند خط النهاية لكل مشارك — من عمر 8 سنوات فما فوق."
      }
    },
    {
      id: "alula-desert-2027", type: "competition", status: "soon",
      date: "2027-01-23", time: "07:30",
      city: "alula",
      venue: { en: "AlUla Old Town course", ar: "مسار البلدة القديمة بالعلا" },
      title: { en: "AlUla Desert Triathlon", ar: "ترايثلون العلا الصحراوي" },
      distances: { swim: "750m", bike: "20km", run: "5km" },
      categories: ["Elite", "Age Group"],
      desc: {
        en: "Round 3 of the national series. A sprint raced between sandstone canyons and heritage sites — the most photographed course on the calendar.",
        ar: "الجولة الثالثة من السلسلة الوطنية. سباق قصير بين الأخاديد الرملية والمواقع التراثية — المسار الأكثر تصويراً في التقويم."
      }
    },
    {
      id: "dammam-eastern-2027", type: "competition", status: "soon",
      date: "2027-02-13", time: "06:00",
      city: "dammam",
      venue: { en: "Half Moon Bay", ar: "شاطئ نصف القمر" },
      title: { en: "Eastern Province Championship", ar: "بطولة المنطقة الشرقية" },
      distances: { swim: "1500m", bike: "40km", run: "10km" },
      categories: ["Elite", "Age Group", "Para"],
      desc: {
        en: "Round 4 of the national series on the Gulf coast, including the season's para-triathlon championship on a fully accessible course.",
        ar: "الجولة الرابعة من السلسلة الوطنية على ساحل الخليج، وتشمل بطولة الموسم لترايثلون ذوي الإعاقة على مسار مهيأ بالكامل."
      }
    },
    {
      id: "riyadh-finals-2027", type: "competition", status: "soon",
      date: "2027-03-06", time: "06:00",
      city: "riyadh",
      venue: { en: "King Salman Park circuit", ar: "حلبة حديقة الملك سلمان" },
      title: { en: "National Championship Finals", ar: "نهائيات البطولة الوطنية" },
      distances: { swim: "1500m", bike: "40km", run: "10km" },
      categories: ["Elite", "Age Group", "Junior"],
      desc: {
        en: "The season decider. National titles in every category, national-team selection points, and the crowning of the 2026–27 series champions.",
        ar: "حسم الموسم. ألقاب وطنية في جميع الفئات، ونقاط اختيار للمنتخب الوطني، وتتويج أبطال سلسلة 2026–27."
      }
    },

    /* ------- completed events (kept out of current listings automatically) ------- */
    {
      id: "jeddah-opener-2026", type: "competition", status: "done",
      date: "2026-03-14", time: "06:00",
      city: "jeddah",
      venue: { en: "Jeddah Corniche", ar: "كورنيش جدة" },
      title: { en: "Jeddah Season Opener Sprint", ar: "افتتاحية موسم جدة للمسافة القصيرة" },
      distances: { swim: "750m", bike: "20km", run: "5km" },
      categories: ["Elite", "Age Group"],
      desc: {
        en: "The 2026 season opener on the corniche. 212 finishers and the fastest sprint time recorded on Saudi soil.",
        ar: "افتتاحية موسم 2026 على الكورنيش. 212 متسابقاً أنهوا السباق، مع أسرع زمن للمسافة القصيرة يُسجل على أرض سعودية."
      },
      results: [
        { pos: 1, name: { en: "S. Al-Harbi",   ar: "س. الحربي" },   club: { en: "Riyadh Tri Club", ar: "نادي الرياض للترايثلون" }, time: "58:41" },
        { pos: 2, name: { en: "M. Al-Qahtani", ar: "م. القحطاني" }, club: { en: "Jeddah Waves",    ar: "أمواج جدة" },               time: "59:12" },
        { pos: 3, name: { en: "F. Al-Otaibi",  ar: "ف. العتيبي" },  club: { en: "Eastern Endurance", ar: "تحمّل الشرقية" },          time: "59:47" },
        { pos: 4, name: { en: "A. Al-Ghamdi",  ar: "أ. الغامدي" },  club: { en: "Jeddah Waves",    ar: "أمواج جدة" },               time: "1:00:26" },
        { pos: 5, name: { en: "K. Al-Shehri",  ar: "ك. الشهري" },   club: { en: "Asir Peaks",      ar: "قمم عسير" },                time: "1:01:03" }
      ]
    },
    {
      id: "dammam-spring-2026", type: "competition", status: "done",
      date: "2026-04-25", time: "06:00",
      city: "dammam",
      venue: { en: "Half Moon Bay", ar: "شاطئ نصف القمر" },
      title: { en: "Dammam Spring Triathlon", ar: "ترايثلون الدمام الربيعي" },
      distances: { swim: "750m", bike: "20km", run: "5km" },
      categories: ["Elite", "Age Group", "Junior"],
      desc: {
        en: "Spring sprint on the Gulf, doubling as junior trials for the Asia Triathlon development camp.",
        ar: "سباق ربيعي قصير على الخليج، أقيم بالتزامن مع تصفيات الناشئين لمعسكر الاتحاد الآسيوي التطويري."
      },
      results: [
        { pos: 1, name: { en: "M. Al-Qahtani", ar: "م. القحطاني" }, club: { en: "Jeddah Waves",     ar: "أمواج جدة" },                time: "59:58" },
        { pos: 2, name: { en: "S. Al-Harbi",   ar: "س. الحربي" },   club: { en: "Riyadh Tri Club",  ar: "نادي الرياض للترايثلون" },  time: "1:00:21" },
        { pos: 3, name: { en: "R. Al-Dossari", ar: "ر. الدوسري" },  club: { en: "Eastern Endurance", ar: "تحمّل الشرقية" },           time: "1:00:59" }
      ]
    },
    {
      id: "jeddah-kasc-aquathlon-2026", type: "community", status: "done",
      date: "2026-05-09", time: "17:00",
      city: "jeddah",
      venue: { en: "King Abdullah Sports City", ar: "مدينة الملك عبدالله الرياضية" },
      title: { en: "KASC Sunset Aquathlon", ar: "أكواثلون الغروب بمدينة الملك عبدالله" },
      distances: { swim: "300m", bike: null, run: "2km" },
      categories: ["Open", "Family"],
      desc: {
        en: "An evening community swim-run that welcomed 340 first-time multisport athletes.",
        ar: "سباق مجتمعي مسائي للسباحة والجري استقبل 340 رياضياً يخوضون الرياضات المتعددة لأول مرة."
      }
    }
  ],

  documents: [
    { id: "annual-report-2025",  cat: "governance", year: 2026, size: "4.8 MB",
      title: { en: "Annual Report 2025", ar: "التقرير السنوي 2025" } },
    { id: "financial-2025",      cat: "finance",    year: 2026, size: "2.1 MB",
      title: { en: "Audited Financial Statements 2025", ar: "القوائم المالية المدققة 2025" } },
    { id: "board-min-q2-2026",   cat: "minutes",    year: 2026, size: "0.6 MB",
      title: { en: "Board Meeting Minutes — Q2 2026", ar: "محضر اجتماع مجلس الإدارة — الربع الثاني 2026" } },
    { id: "board-min-q1-2026",   cat: "minutes",    year: 2026, size: "0.5 MB",
      title: { en: "Board Meeting Minutes — Q1 2026", ar: "محضر اجتماع مجلس الإدارة — الربع الأول 2026" } },
    { id: "governance-charter",  cat: "governance", year: 2025, size: "1.3 MB",
      title: { en: "Governance & Ethics Charter", ar: "ميثاق الحوكمة والأخلاقيات" } },
    { id: "annual-report-2024",  cat: "governance", year: 2025, size: "4.2 MB",
      title: { en: "Annual Report 2024", ar: "التقرير السنوي 2024" } },
    { id: "financial-2024",      cat: "finance",    year: 2025, size: "1.9 MB",
      title: { en: "Audited Financial Statements 2024", ar: "القوائم المالية المدققة 2024" } },
    { id: "safeguarding-policy", cat: "governance", year: 2024, size: "0.9 MB",
      title: { en: "Athlete Safeguarding Policy", ar: "سياسة حماية الرياضيين" } },
    { id: "board-min-q4-2025",   cat: "minutes",    year: 2025, size: "0.5 MB",
      title: { en: "Board Meeting Minutes — Q4 2025", ar: "محضر اجتماع مجلس الإدارة — الربع الرابع 2025" } }
  ],

  rules: [
    { id: "competition-rules-2026", size: "3.6 MB", updated: "2026-06",
      title: { en: "STF Competition Rules 2026", ar: "قوانين المنافسات 2026" },
      desc: { en: "The full rulebook: race conduct, transitions, drafting, penalties, and appeals — aligned with the World Triathlon Competition Rules.",
              ar: "كتاب القوانين الكامل: سلوك السباق، والانتقالات، والتلاحق الهوائي، والعقوبات، والاستئناف — بما يتوافق مع قوانين الاتحاد الدولي." } },
    { id: "age-group-guide",        size: "1.2 MB", updated: "2026-05",
      title: { en: "Age Group Athlete Guide", ar: "دليل رياضيي الفئات العمرية" },
      desc: { en: "Categories, qualification standards, equipment checks, and race-day procedures for age-group athletes.",
              ar: "الفئات، ومعايير التأهل، وفحص المعدات، وإجراءات يوم السباق لرياضيي الفئات العمرية." } },
    { id: "event-organizer-manual", size: "5.1 MB", updated: "2026-04",
      title: { en: "Event Organizer Manual", ar: "دليل منظمي الفعاليات" },
      desc: { en: "Sanctioning requirements, safety plans, course measurement, and officiating for organizers hosting STF events.",
              ar: "متطلبات الاعتماد، وخطط السلامة، وقياس المسارات، والتحكيم للجهات المنظمة لفعاليات الاتحاد." } },
    { id: "technical-officials",    size: "2.4 MB", updated: "2026-02",
      title: { en: "Technical Officials Handbook", ar: "دليل الحكام الفنيين" },
      desc: { en: "Certification pathway, duties, and positioning for technical officials at national events.",
              ar: "مسار الاعتماد والمهام والتمركز للحكام الفنيين في الفعاليات الوطنية." } },
    { id: "anti-doping-2026",       size: "0.8 MB", updated: "2026-01",
      title: { en: "Anti-Doping Policy", ar: "سياسة مكافحة المنشطات" },
      desc: { en: "Testing procedures, prohibited list references, and athlete whereabouts requirements per SAADC and WADA.",
              ar: "إجراءات الفحص، ومراجع قائمة المواد المحظورة، ومتطلبات أماكن تواجد الرياضيين وفق اللجنة السعودية والوكالة الدولية لمكافحة المنشطات." } }
  ],

  clubs: [
    { name: { en: "Riyadh Tri Club",   ar: "نادي الرياض للترايثلون" }, city: { en: "Riyadh", ar: "الرياض" } },
    { name: { en: "Jeddah Waves",      ar: "أمواج جدة" },              city: { en: "Jeddah", ar: "جدة" } },
    { name: { en: "Eastern Endurance", ar: "تحمّل الشرقية" },          city: { en: "Dammam / Khobar", ar: "الدمام / الخبر" } },
    { name: { en: "Yanbu Open Water",  ar: "ينبع للمياه المفتوحة" },   city: { en: "Yanbu", ar: "ينبع" } },
    { name: { en: "Asir Peaks",        ar: "قمم عسير" },               city: { en: "Abha", ar: "أبها" } },
    { name: { en: "NEOM Multisport",   ar: "نيوم للرياضات المتعددة" }, city: { en: "NEOM / Tabuk", ar: "نيوم / تبوك" } }
  ]
};

/* ---- demo dashboard overlay (Phase B stand-in) ----
   admin.html saves edits to localStorage under "stf-overrides"; they merge
   here so the whole site reflects back-office changes in this browser.
   Production replaces this whole block with the federation dashboard API.

   Overlay shape (all keys optional):
     stats     : { <statKey>: number }
     eventsAdd : [ event, … ]        eventsEdit: { <id>: partialEvent }   eventsDel: [ id, … ]
     docsAdd   : [ doc, … ]          docsDel   : [ id, … ]
     rulesAdd  : [ rule, … ]         rulesDel  : [ id, … ]
     clubsAdd  : [ club, … ]         clubsDel  : [ enName, … ]            */

STF.OVERLAY_KEY = "stf-overrides";
STF.readOverlay = function () {
  try { return JSON.parse(localStorage.getItem(STF.OVERLAY_KEY) || "null") || {}; }
  catch (e) { return {}; }
};

/* pristine copies so the admin can compute effective state and restore removals */
STF._base = JSON.parse(JSON.stringify({
  events: STF.events, documents: STF.documents, rules: STF.rules, clubs: STF.clubs,
  stats: STF.stats
}));

/* pure merge: base snapshot + overlay -> effective lists. Used by the site
   (mutating the globals below) and by admin.html (recomputing on each edit). */
STF.mergeOverlay = function (base, ov) {
  base = JSON.parse(JSON.stringify(base));
  ov = ov || {};
  if (Array.isArray(ov.events)) ov.eventsAdd = (ov.eventsAdd || []).concat(ov.events); /* v1 migration */

  const stats = base.stats;
  if (ov.stats) Object.keys(ov.stats).forEach(k => {
    if (stats[k] && typeof ov.stats[k] === "number") stats[k].value = ov.stats[k];
  });

  let events = base.events;
  if (ov.eventsEdit) events.forEach(e => {
    const p = ov.eventsEdit[e.id];
    if (!p) return;
    if (p.title) e.title = p.title;
    if (p.venue) e.venue = p.venue;
    if (p.distances) e.distances = p.distances;
    ["type", "status", "date", "time", "city"].forEach(k => { if (p[k] != null) e[k] = p[k]; });
  });
  if (Array.isArray(ov.eventsDel)) events = events.filter(e => ov.eventsDel.indexOf(e.id) === -1);
  if (Array.isArray(ov.eventsAdd)) ov.eventsAdd.forEach(e => { if (e && e.id && STF.cities[e.city]) events.push(e); });

  let documents = base.documents;
  if (Array.isArray(ov.docsDel)) documents = documents.filter(d => ov.docsDel.indexOf(d.id) === -1);
  if (Array.isArray(ov.docsAdd)) ov.docsAdd.forEach(d => { if (d && d.id) documents.push(d); });

  let rules = base.rules;
  if (Array.isArray(ov.rulesDel)) rules = rules.filter(r => ov.rulesDel.indexOf(r.id) === -1);
  if (Array.isArray(ov.rulesAdd)) ov.rulesAdd.forEach(r => { if (r && r.id) rules.push(r); });

  let clubs = base.clubs;
  if (Array.isArray(ov.clubsDel)) clubs = clubs.filter(c => ov.clubsDel.indexOf(c.name.en) === -1);
  if (Array.isArray(ov.clubsAdd)) ov.clubsAdd.forEach(c => { if (c && c.name) clubs.push(c); });

  return { events, documents, rules, clubs, stats };
};

(function applyOverlay() {
  const merged = STF.mergeOverlay(STF._base, STF.readOverlay());
  STF.events = merged.events;
  STF.documents = merged.documents;
  STF.rules = merged.rules;
  STF.clubs = merged.clubs;
  STF.stats = merged.stats;
})();

/* today's date is resolved at runtime so past events retire automatically */
STF.today = new Date();
STF.upcoming = STF.events
  .filter(e => new Date(e.date + "T23:59:59") >= STF.today)
  .sort((a, b) => a.date.localeCompare(b.date));
STF.past = STF.events
  .filter(e => new Date(e.date + "T23:59:59") < STF.today)
  .sort((a, b) => b.date.localeCompare(a.date));
