using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Data.Seeding.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LanguageLearningPlatform.Data.Seeding
{
    /// <summary>
    /// Seeds extra lessons/exercises for Spanish Intermediate, French Beginner,
    /// and German Beginner — giving the platform more content to demonstrate.
    /// Register this in DatabaseSeeder AFTER LessonAndExerciseSeeder.
    /// </summary>
    public class AdditionalLessonSeeder : IEntitySeeder
    {
        public async Task SeedAsync(ApplicationDbContext context)
        {
            // Run only when extra lessons haven't been seeded yet.
            // We use a sentinel title to detect the presence of these records.
            if (await context.Lessons.AnyAsync(l => l.Title == "Future Tense – IR"))
                return;

            var courses = await context.Courses.ToListAsync();

            foreach (var course in courses)
            {
                if (course.Language == "Spanish" && course.Level == "Intermediate")
                    await SeedSpanishIntermediateExtra(context, course);

                if (course.Language == "French" && course.Level == "Beginner")
                    await SeedFrenchBeginnerExtra(context, course);

                if (course.Language == "German" && course.Level == "Beginner")
                    await SeedGermanBeginnerExtra(context, course);
            }

            await context.SaveChangesAsync();
        }

        // ── helpers ──────────────────────────────────────────────────────────
        private static string Opts(params string[] options) =>
            JsonSerializer.Serialize(options);

        private static string Pairs(params (string L, string R)[] pairs) =>
            JsonSerializer.Serialize(pairs.Select(p => new { Left = p.L, Right = p.R, PairId = Guid.NewGuid() }));

        // ═══════════════════════════════════════════════════════════════════════
        // SPANISH INTERMEDIATE – 2 extra lessons
        // ═══════════════════════════════════════════════════════════════════════

        private async Task SeedSpanishIntermediateExtra(ApplicationDbContext context, Course course)
        {
            // ── Lesson 3: Future Tense ─────────────────────────────────────
            var lesson3 = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "Future Tense – IR",
                Description = "Express future plans and predictions using the simple future tense",
                OrderIndex = 3,
                DurationMinutes = 25,
                IsLocked = false,
                Content = @"<h2>El Futuro Simple – Simple Future Tense</h2>
<p>The Spanish simple future is used for predictions, plans, and promises.</p>
<h3>Formation – add endings to the INFINITIVE</h3>
<table>
  <tr><th>Pronoun</th><th>Ending</th><th>Hablar example</th></tr>
  <tr><td>Yo</td><td>-é</td><td>hablaré</td></tr>
  <tr><td>Tú</td><td>-ás</td><td>hablarás</td></tr>
  <tr><td>Él/Ella</td><td>-á</td><td>hablará</td></tr>
  <tr><td>Nosotros</td><td>-emos</td><td>hablaremos</td></tr>
  <tr><td>Ellos</td><td>-án</td><td>hablarán</td></tr>
</table>
<h3>Irregular stems (memorise these!)</h3>
<ul>
  <li>tener → tendr-</li>
  <li>poder → podr-</li>
  <li>saber → sabr-</li>
  <li>hacer → har-</li>
  <li>venir → vendr-</li>
</ul>"
            };
            await context.Lessons.AddAsync(lesson3);

            await context.Exercises.AddRangeAsync(new[]
            {
                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "I will speak", Type = "Translation",
                    Content = "Translate: 'I will speak' (hablar)",
                    CorrectAnswer = "Hablaré", Points = 15, OrderIndex = 1, DifficultyLevel = 2,
                    Hint = "Add -é to the infinitive." },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "She will come",
                    Type = "MultipleChoice", Content = "How do you say 'She will come' (venir)?",
                    CorrectAnswer = "Vendrá",
                    Options = Opts("Venirá","Vendrá","Venirá","Viene"),
                    Points = 15, OrderIndex = 2, DifficultyLevel = 2,
                    Explanation = "Venir has the irregular stem vendr-." },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Future of tener",
                    Type = "FillInBlank", Content = "Nosotros _____ tiempo mañana (tener)",
                    CorrectAnswer = "tendremos", Points = 15, OrderIndex = 3, DifficultyLevel = 2,
                    Hint = "Irregular stem: tendr-" },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "They will work",
                    Type = "Translation", Content = "Translate: 'They will work' (trabajar)",
                    CorrectAnswer = "Trabajarán", Points = 15, OrderIndex = 4, DifficultyLevel = 2 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Future saber",
                    Type = "MultipleChoice", Content = "Complete: 'Tú _____ la verdad' (saber)",
                    CorrectAnswer = "sabrás",
                    Options = Opts("saberás","sabrarás","sabrás","sabes"),
                    Points = 15, OrderIndex = 5, DifficultyLevel = 2 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "We will do",
                    Type = "FillInBlank", Content = "Nosotros _____ los deberes (hacer)",
                    CorrectAnswer = "haremos", Points = 20, OrderIndex = 6, DifficultyLevel = 3,
                    Hint = "Irregular stem: har-" },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Future promise",
                    Type = "Translation", Content = "Translate: 'I will be there' (estar)",
                    CorrectAnswer = "Estaré allí", Points = 20, OrderIndex = 7, DifficultyLevel = 2 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Match future endings",
                    Type = "Matching", Content = "Match each pronoun to the correct future ending.",
                    CorrectAnswer = "matched",
                    Options = Pairs(("Yo","-é"),("Tú","-ás"),("Él/Ella","-á"),("Nosotros","-emos")),
                    Points = 20, OrderIndex = 8, DifficultyLevel = 2 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Say a future plan",
                    Type = "Speaking", Content = "Say this sentence out loud:",
                    CorrectAnswer = "Mañana hablaré con mi profesora y haremos los ejercicios",
                    Points = 25, OrderIndex = 9, DifficultyLevel = 2,
                    Hint = "Tomorrow I will speak with my teacher and we will do the exercises." },
            });

            // ── Lesson 4: Direct & Indirect Object Pronouns ────────────────
            var lesson4 = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "Object Pronouns",
                Description = "Learn to use direct and indirect object pronouns in Spanish",
                OrderIndex = 4,
                DurationMinutes = 30,
                IsLocked = false,
                Content = @"<h2>Pronombres de Objeto – Object Pronouns</h2>
<h3>Direct Object Pronouns (answer: who/what?)</h3>
<ul>
  <li>me, te, lo/la, nos, los/las</li>
  <li>Veo <strong>a María</strong> → La veo (I see her)</li>
</ul>
<h3>Indirect Object Pronouns (answer: to/for whom?)</h3>
<ul>
  <li>me, te, le, nos, les</li>
  <li>Doy el libro <strong>a Juan</strong> → Le doy el libro (I give him the book)</li>
</ul>
<h3>Position</h3>
<p>Object pronouns go BEFORE the conjugated verb, or attached to the infinitive/gerund.</p>"
            };
            await context.Lessons.AddAsync(lesson4);

            await context.Exercises.AddRangeAsync(new[]
            {
                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson4.Id,
                    Title = "Replace direct object",
                    Type = "MultipleChoice", Content = "Replace 'el libro' with a pronoun: 'Leo el libro'",
                    CorrectAnswer = "Lo leo",
                    Options = Opts("Le leo","Lo leo","La leo","Les leo"),
                    Points = 15, OrderIndex = 1, DifficultyLevel = 2,
                    Explanation = "Libro is masculine singular → lo." },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson4.Id,
                    Title = "Indirect object pronoun",
                    Type = "FillInBlank", Content = "___ doy flores a ella (I give her flowers)",
                    CorrectAnswer = "Le", Points = 15, OrderIndex = 2, DifficultyLevel = 2 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson4.Id,
                    Title = "Feminine direct object",
                    Type = "Translation", Content = "Translate: 'I see her' (ver – ella)",
                    CorrectAnswer = "La veo", Points = 15, OrderIndex = 3, DifficultyLevel = 2 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson4.Id,
                    Title = "Plural direct object",
                    Type = "MultipleChoice", Content = "Replace 'las manzanas': 'Compro las manzanas'",
                    CorrectAnswer = "Las compro",
                    Options = Opts("Los compro","Les compro","Las compro","La compro"),
                    Points = 15, OrderIndex = 4, DifficultyLevel = 2 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson4.Id,
                    Title = "Position with infinitive",
                    Type = "MultipleChoice", Content = "Where does the pronoun go? 'Quiero comprar___lo' or '___lo quiero comprar'?",
                    CorrectAnswer = "Both positions are correct",
                    Options = Opts("Only before the verb","Only attached to infinitive","Both positions are correct","Neither"),
                    Points = 20, OrderIndex = 5, DifficultyLevel = 3,
                    Explanation = "Both 'Lo quiero comprar' and 'Quiero comprarlo' are correct." },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson4.Id,
                    Title = "Match pronouns",
                    Type = "Matching", Content = "Match each pronoun to its grammatical function.",
                    CorrectAnswer = "matched",
                    Options = Pairs(("lo","masculine direct object"),("la","feminine direct object"),
                                    ("le","indirect object singular"),("les","indirect object plural")),
                    Points = 20, OrderIndex = 6, DifficultyLevel = 2 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson4.Id,
                    Title = "Speak with pronouns",
                    Type = "Speaking", Content = "Say this sentence out loud:",
                    CorrectAnswer = "Te doy el libro porque lo necesitas",
                    Points = 25, OrderIndex = 7, DifficultyLevel = 2,
                    Hint = "I give you the book because you need it." },
            });
        }

        // ═══════════════════════════════════════════════════════════════════════
        // FRENCH BEGINNER – 2 extra lessons
        // ═══════════════════════════════════════════════════════════════════════

        private async Task SeedFrenchBeginnerExtra(ApplicationDbContext context, Course course)
        {
            // ── Lesson 2: Numbers & Days ───────────────────────────────────
            var lesson2 = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "Numbers & Days of the Week",
                Description = "Count in French and name the days",
                OrderIndex = 2,
                DurationMinutes = 20,
                IsLocked = false,
                Content = @"<h2>Les Nombres et les Jours</h2>
<h3>Numbers 1–10</h3>
<ul>
  <li>1-un/une, 2-deux, 3-trois, 4-quatre, 5-cinq</li>
  <li>6-six, 7-sept, 8-huit, 9-neuf, 10-dix</li>
</ul>
<h3>Days of the Week</h3>
<ul>
  <li>lundi (Mon), mardi (Tue), mercredi (Wed), jeudi (Thu)</li>
  <li>vendredi (Fri), samedi (Sat), dimanche (Sun)</li>
</ul>
<p><strong>Tip:</strong> French days are not capitalised.</p>"
            };
            await context.Lessons.AddAsync(lesson2);

            await context.Exercises.AddRangeAsync(new[]
            {
                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson2.Id,
                    Title = "Number 7",
                    Type = "MultipleChoice", Content = "What is 7 in French?",
                    CorrectAnswer = "sept", Options = Opts("six","sept","huit","neuf"),
                    Points = 10, OrderIndex = 1, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson2.Id,
                    Title = "Translate: five",
                    Type = "Translation", Content = "How do you say 'five' in French?",
                    CorrectAnswer = "cinq", Points = 10, OrderIndex = 2, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson2.Id,
                    Title = "Monday",
                    Type = "FillInBlank", Content = "Monday in French is _____",
                    CorrectAnswer = "lundi", Points = 10, OrderIndex = 3, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson2.Id,
                    Title = "Weekend days",
                    Type = "MultipleChoice", Content = "Which two days form the weekend?",
                    CorrectAnswer = "samedi et dimanche",
                    Options = Opts("vendredi et samedi","samedi et dimanche","jeudi et vendredi","dimanche et lundi"),
                    Points = 10, OrderIndex = 4, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson2.Id,
                    Title = "Number 3",
                    Type = "Translation", Content = "Translate 'three' into French.",
                    CorrectAnswer = "trois", Points = 10, OrderIndex = 5, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson2.Id,
                    Title = "Wednesday",
                    Type = "MultipleChoice", Content = "How do you say Wednesday?",
                    CorrectAnswer = "mercredi",
                    Options = Opts("mardi","mercredi","jeudi","vendredi"),
                    Points = 10, OrderIndex = 6, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson2.Id,
                    Title = "Match numbers",
                    Type = "Matching", Content = "Match each French number to its digit.",
                    CorrectAnswer = "matched",
                    Options = Pairs(("deux","2"),("cinq","5"),("huit","8"),("dix","10")),
                    Points = 20, OrderIndex = 7, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson2.Id,
                    Title = "Count to five",
                    Type = "Speaking", Content = "Say the numbers one to five in French:",
                    CorrectAnswer = "un deux trois quatre cinq",
                    Points = 20, OrderIndex = 8, DifficultyLevel = 1,
                    Hint = "un, deux, trois, quatre, cinq" },
            });

            // ── Lesson 3: Colours & Adjective Agreement ────────────────────
            var lesson3 = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "Colours & Adjective Agreement",
                Description = "Learn French colours and how adjectives agree with nouns",
                OrderIndex = 3,
                DurationMinutes = 25,
                IsLocked = false,
                Content = @"<h2>Les Couleurs – Colours</h2>
<ul>
  <li><strong>rouge</strong> – red</li>
  <li><strong>bleu/bleue</strong> – blue</li>
  <li><strong>vert/verte</strong> – green</li>
  <li><strong>jaune</strong> – yellow</li>
  <li><strong>noir/noire</strong> – black</li>
  <li><strong>blanc/blanche</strong> – white</li>
</ul>
<h3>Adjective Agreement Rule</h3>
<p>Adjectives FOLLOW the noun and agree in gender and number:</p>
<ul>
  <li>un chat <strong>noir</strong> – a black cat (masc)</li>
  <li>une robe <strong>noire</strong> – a black dress (fem → add -e)</li>
  <li>des chats <strong>noirs</strong> – black cats (plural → add -s)</li>
</ul>"
            };
            await context.Lessons.AddAsync(lesson3);

            await context.Exercises.AddRangeAsync(new[]
            {
                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Red",
                    Type = "Translation", Content = "How do you say 'red' in French?",
                    CorrectAnswer = "rouge", Points = 10, OrderIndex = 1, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Feminine black",
                    Type = "MultipleChoice", Content = "Complete: 'une voiture _____' (a black car – fem)",
                    CorrectAnswer = "noire",
                    Options = Opts("noir","noire","noirs","noires"),
                    Points = 15, OrderIndex = 2, DifficultyLevel = 2,
                    Explanation = "Voiture is feminine, so the adjective adds -e." },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Adjective position",
                    Type = "MultipleChoice", Content = "Where do most colour adjectives go in French?",
                    CorrectAnswer = "After the noun",
                    Options = Opts("Before the noun","After the noun","Either position","At the end of the sentence"),
                    Points = 10, OrderIndex = 3, DifficultyLevel = 1,
                    Explanation = "Most French adjectives, including colours, follow the noun." },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Green (feminine)",
                    Type = "FillInBlank", Content = "une pomme _____ (a green apple)",
                    CorrectAnswer = "verte", Points = 15, OrderIndex = 4, DifficultyLevel = 2,
                    Hint = "Vert becomes verte in the feminine." },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "White (masculine)",
                    Type = "MultipleChoice", Content = "Which form of 'white' goes with 'un mur' (a wall)?",
                    CorrectAnswer = "blanc",
                    Options = Opts("blanche","blanc","blancs","blanches"),
                    Points = 15, OrderIndex = 5, DifficultyLevel = 2 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Translate blue dress",
                    Type = "Translation", Content = "Translate: 'a blue dress' (robe = fem)",
                    CorrectAnswer = "une robe bleue", Points = 20, OrderIndex = 6, DifficultyLevel = 2 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Match colours",
                    Type = "Matching", Content = "Match each French colour to its English meaning.",
                    CorrectAnswer = "matched",
                    Options = Pairs(("rouge","red"),("jaune","yellow"),("vert","green"),("bleu","blue")),
                    Points = 20, OrderIndex = 7, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Describe an object",
                    Type = "Speaking", Content = "Say this phrase out loud:",
                    CorrectAnswer = "J'ai une voiture rouge et un vélo bleu",
                    Points = 20, OrderIndex = 8, DifficultyLevel = 2,
                    Hint = "I have a red car and a blue bike." },
            });
        }

        // ═══════════════════════════════════════════════════════════════════════
        // GERMAN BEGINNER – 2 extra lessons
        // ═══════════════════════════════════════════════════════════════════════

        private async Task SeedGermanBeginnerExtra(ApplicationDbContext context, Course course)
        {
            // ── Lesson 2: Numbers & Colours ────────────────────────────────
            var lesson2 = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "Numbers 1–20 & Colours",
                Description = "Count in German and learn basic colour vocabulary",
                OrderIndex = 2,
                DurationMinutes = 20,
                IsLocked = false,
                Content = @"<h2>Zahlen und Farben</h2>
<h3>Numbers 1–10</h3>
<ul>
  <li>1-ein, 2-zwei, 3-drei, 4-vier, 5-fünf</li>
  <li>6-sechs, 7-sieben, 8-acht, 9-neun, 10-zehn</li>
</ul>
<h3>Numbers 11–20</h3>
<ul>
  <li>11-elf, 12-zwölf, 13-dreizehn, 14-vierzehn, 15-fünfzehn</li>
  <li>16-sechzehn, 17-siebzehn, 18-achtzehn, 19-neunzehn, 20-zwanzig</li>
</ul>
<h3>Common Colours (Farben)</h3>
<ul>
  <li>rot – red, blau – blue, grün – green</li>
  <li>gelb – yellow, schwarz – black, weiß – white</li>
</ul>"
            };
            await context.Lessons.AddAsync(lesson2);

            await context.Exercises.AddRangeAsync(new[]
            {
                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson2.Id,
                    Title = "Number 5",
                    Type = "Translation", Content = "How do you say 'five' in German?",
                    CorrectAnswer = "fünf", Points = 10, OrderIndex = 1, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson2.Id,
                    Title = "Number 12",
                    Type = "MultipleChoice", Content = "What is 12 in German?",
                    CorrectAnswer = "zwölf",
                    Options = Opts("zwanzig","elf","zwölf","dreizehn"),
                    Points = 10, OrderIndex = 2, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson2.Id,
                    Title = "Red",
                    Type = "Translation", Content = "Translate 'red' into German.",
                    CorrectAnswer = "rot", Points = 10, OrderIndex = 3, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson2.Id,
                    Title = "Black or white?",
                    Type = "MultipleChoice", Content = "Which word means 'black'?",
                    CorrectAnswer = "schwarz",
                    Options = Opts("weiß","grün","schwarz","blau"),
                    Points = 10, OrderIndex = 4, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson2.Id,
                    Title = "Complete: 19",
                    Type = "FillInBlank", Content = "The German word for 19 is _____",
                    CorrectAnswer = "neunzehn", Points = 15, OrderIndex = 5, DifficultyLevel = 2,
                    Hint = "neun (9) + zehn (10)" },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson2.Id,
                    Title = "Yellow",
                    Type = "Translation", Content = "How do you say 'yellow' in German?",
                    CorrectAnswer = "gelb", Points = 10, OrderIndex = 6, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson2.Id,
                    Title = "Match numbers",
                    Type = "Matching", Content = "Match each digit to its German word.",
                    CorrectAnswer = "matched",
                    Options = Pairs(("3","drei"),("8","acht"),("15","fünfzehn"),("20","zwanzig")),
                    Points = 20, OrderIndex = 7, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson2.Id,
                    Title = "Count to five",
                    Type = "Speaking", Content = "Count aloud from one to five in German:",
                    CorrectAnswer = "ein zwei drei vier fünf",
                    Points = 20, OrderIndex = 8, DifficultyLevel = 1,
                    Hint = "ein, zwei, drei, vier, fünf" },
            });

            // ── Lesson 3: Family Vocabulary & Possessives ──────────────────
            var lesson3 = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "Family & Possessive Articles",
                Description = "Learn German family words and how to express possession",
                OrderIndex = 3,
                DurationMinutes = 25,
                IsLocked = false,
                Content = @"<h2>Familie und Possessivartikel</h2>
<h3>Family Vocabulary</h3>
<ul>
  <li>die Mutter – mother, der Vater – father</li>
  <li>die Schwester – sister, der Bruder – brother</li>
  <li>die Großmutter – grandmother, der Großvater – grandfather</li>
</ul>
<h3>Possessive Articles (my, your, his/her)</h3>
<table>
  <tr><th></th><th>Masculine</th><th>Feminine</th><th>Neuter</th></tr>
  <tr><td>my</td><td>mein</td><td>meine</td><td>mein</td></tr>
  <tr><td>your (inf.)</td><td>dein</td><td>deine</td><td>dein</td></tr>
  <tr><td>his</td><td>sein</td><td>seine</td><td>sein</td></tr>
  <tr><td>her</td><td>ihr</td><td>ihre</td><td>ihr</td></tr>
</table>
<p>Example: <strong>mein Vater</strong> (my father – masc) / <strong>meine Mutter</strong> (my mother – fem)</p>"
            };
            await context.Lessons.AddAsync(lesson3);

            await context.Exercises.AddRangeAsync(new[]
            {
                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "My mother",
                    Type = "MultipleChoice", Content = "How do you say 'my mother'?",
                    CorrectAnswer = "meine Mutter",
                    Options = Opts("mein Mutter","meine Mutter","meinen Mutter","meiner Mutter"),
                    Points = 15, OrderIndex = 1, DifficultyLevel = 2,
                    Explanation = "Mutter is feminine, so we use meine." },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Translate: father",
                    Type = "Translation", Content = "How do you say 'father' in German?",
                    CorrectAnswer = "Vater", Points = 10, OrderIndex = 2, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "His brother",
                    Type = "FillInBlank", Content = "_____ Bruder ist groß. (His brother is tall.)",
                    CorrectAnswer = "Sein", Points = 15, OrderIndex = 3, DifficultyLevel = 2,
                    Hint = "Bruder is masculine." },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Her sister",
                    Type = "MultipleChoice", Content = "How do you say 'her sister'?",
                    CorrectAnswer = "ihre Schwester",
                    Options = Opts("ihr Schwester","ihre Schwester","ihren Schwester","ihrem Schwester"),
                    Points = 15, OrderIndex = 4, DifficultyLevel = 2 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Grandmother",
                    Type = "Translation", Content = "Translate: 'grandmother'",
                    CorrectAnswer = "Großmutter", Points = 10, OrderIndex = 5, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Your father (informal)",
                    Type = "FillInBlank", Content = "Wie alt ist _____ Vater? (How old is your father?)",
                    CorrectAnswer = "dein", Points = 15, OrderIndex = 6, DifficultyLevel = 2,
                    Hint = "Vater is masculine → dein." },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Match family words",
                    Type = "Matching", Content = "Match each German family word to its English meaning.",
                    CorrectAnswer = "matched",
                    Options = Pairs(("die Mutter","mother"),("der Vater","father"),
                                    ("der Bruder","brother"),("die Schwester","sister")),
                    Points = 20, OrderIndex = 7, DifficultyLevel = 1 },

                new Exercise { Id = Guid.NewGuid(), CourseId = course.Id, LessonId = lesson3.Id,
                    Title = "Introduce your family",
                    Type = "Speaking", Content = "Say this sentence out loud:",
                    CorrectAnswer = "Meine Mutter heißt Anna und mein Vater heißt Klaus",
                    Points = 25, OrderIndex = 8, DifficultyLevel = 2,
                    Hint = "My mother is called Anna and my father is called Klaus." },
            });
        }
    }
}