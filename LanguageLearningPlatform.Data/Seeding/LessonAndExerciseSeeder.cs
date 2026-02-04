using LanguageLearningPlatform.Data.Models;
using LanguageLearningPlatform.Data.Seeding.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LanguageLearningPlatform.Data.Seeding
{
    public class LessonAndExerciseSeeder : IEntitySeeder
    {
        public async Task SeedAsync(ApplicationDbContext context)
        {
            if (await context.Lessons.AnyAsync() || await context.Exercises.AnyAsync())
                return;

            var courses = await context.Courses.ToListAsync();

            foreach (var course in courses)
            {
                await SeedLessonsAndExercisesForCourse(context, course);
            }

            await context.SaveChangesAsync();
        }

        private async Task SeedLessonsAndExercisesForCourse(ApplicationDbContext context, Course course)
        {
            if (course.Language == "Spanish")
            {
                await SeedSpanishCourse(context, course);
            }
            else if (course.Language == "French")
            {
                await SeedFrenchCourse(context, course);
            }
            else if (course.Language == "German")
            {
                await SeedGermanCourse(context, course);
            }
            else if (course.Language == "Japanese")
            {
                await SeedJapaneseCourse(context, course);
            }
            else if (course.Language == "Italian")
            {
                await SeedItalianCourse(context, course);
            }
            else if (course.Language == "Chinese")
            {
                await SeedChineseCourse(context, course);
            }
        }

        private async Task SeedSpanishCourse(ApplicationDbContext context, Course course)
        {
            if (course.Level == "Beginner")
            {
                // Lesson 1: Basic Greetings
                var lesson1 = new Lesson
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    Title = "Basic Greetings",
                    Description = "Learn essential Spanish greetings and introductions",
                    OrderIndex = 1,
                    DurationMinutes = 15,
                    IsLocked = false,
                    Content = @"
                        <h2>¡Hola! Welcome to Spanish Greetings</h2>
                        <p>Let's start your Spanish journey by learning the most common greetings!</p>
                        
                        <h3>Essential Greetings</h3>
                        <ul>
                            <li><strong>Hola</strong> - Hello</li>
                            <li><strong>Buenos días</strong> - Good morning</li>
                            <li><strong>Buenas tardes</strong> - Good afternoon</li>
                            <li><strong>Buenas noches</strong> - Good evening/night</li>
                            <li><strong>¿Cómo estás?</strong> - How are you? (informal)</li>
                            <li><strong>¿Cómo está usted?</strong> - How are you? (formal)</li>
                        </ul>

                        <h3>Common Responses</h3>
                        <ul>
                            <li><strong>Bien, gracias</strong> - Good, thank you</li>
                            <li><strong>Muy bien</strong> - Very good</li>
                            <li><strong>Más o menos</strong> - So-so</li>
                            <li><strong>¿Y tú?</strong> - And you?</li>
                        </ul>

                        <h3>Introductions</h3>
                        <ul>
                            <li><strong>Me llamo...</strong> - My name is...</li>
                            <li><strong>Mucho gusto</strong> - Nice to meet you</li>
                            <li><strong>Encantado/Encantada</strong> - Pleased to meet you</li>
                        </ul>

                        <p><em>Now let's practice with some exercises!</em></p>
                    "
                };
                await context.Lessons.AddAsync(lesson1);

                // Exercises for Lesson 1
                var exercises = new List<Exercise>
                {
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson1.Id,
                        Title = "Translate: Hello",
                        Type = "Translation",
                        Content = "How do you say 'Hello' in Spanish?",
                        CorrectAnswer = "Hola",
                        Points = 10,
                        OrderIndex = 1,
                        DifficultyLevel = 1,
                        Hint = "It's one of the most common Spanish words!",
                        Explanation = "¡Hola! is the most common greeting in Spanish, used at any time of day."
                    },
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson1.Id,
                        Title = "Multiple Choice: Good morning",
                        Type = "MultipleChoice",
                        Content = "Which greeting would you use in the morning?",
                        CorrectAnswer = "Buenos días",
                        Options = JsonSerializer.Serialize(new[] { "Buenas noches", "Buenos días", "Buenas tardes", "Hola" }),
                        Points = 10,
                        OrderIndex = 2,
                        DifficultyLevel = 1,
                        Explanation = "Buenos días is used to greet someone in the morning until around noon."
                    },
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson1.Id,
                        Title = "Fill in the blank: Introduction",
                        Type = "FillInBlank",
                        Content = "Complete the sentence: '_____ llamo María' (My name is María)",
                        CorrectAnswer = "Me",
                        Points = 15,
                        OrderIndex = 3,
                        DifficultyLevel = 1,
                        Hint = "Think about the reflexive pronoun for 'I'",
                        Explanation = "'Me llamo' literally means 'I call myself' and is the standard way to introduce your name."
                    },
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson1.Id,
                        Title = "Match: How are you?",
                        Type = "MultipleChoice",
                        Content = "Select the informal way to ask 'How are you?'",
                        CorrectAnswer = "¿Cómo estás?",
                        Options = JsonSerializer.Serialize(new[] { "¿Cómo está usted?", "¿Cómo estás?", "¿Qué tal?", "Both B and C" }),
                        Points = 15,
                        OrderIndex = 4,
                        DifficultyLevel = 2,
                        Explanation = "¿Cómo estás? is the informal version. ¿Cómo está usted? is formal."
                    }
                };

                await context.Exercises.AddRangeAsync(exercises);

                // Lesson 2: Numbers 1-20
                var lesson2 = new Lesson
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    Title = "Numbers 1-20",
                    Description = "Master counting from 1 to 20 in Spanish",
                    OrderIndex = 2,
                    DurationMinutes = 20,
                    IsLocked = false,
                    Content = @"
                        <h2>Los Números - Numbers in Spanish</h2>
                        <p>Counting is essential in any language. Let's learn numbers 1-20!</p>
                        
                        <h3>Numbers 1-10</h3>
                        <ul>
                            <li>1 - uno</li>
                            <li>2 - dos</li>
                            <li>3 - tres</li>
                            <li>4 - cuatro</li>
                            <li>5 - cinco</li>
                            <li>6 - seis</li>
                            <li>7 - siete</li>
                            <li>8 - ocho</li>
                            <li>9 - nueve</li>
                            <li>10 - diez</li>
                        </ul>

                        <h3>Numbers 11-20</h3>
                        <ul>
                            <li>11 - once</li>
                            <li>12 - doce</li>
                            <li>13 - trece</li>
                            <li>14 - catorce</li>
                            <li>15 - quince</li>
                            <li>16 - dieciséis</li>
                            <li>17 - diecisiete</li>
                            <li>18 - dieciocho</li>
                            <li>19 - diecinueve</li>
                            <li>20 - veinte</li>
                        </ul>

                        <h3>Quick Tips</h3>
                        <p>Notice that 16-19 are combinations: diez y seis → dieciséis (ten and six)</p>
                    "
                };
                await context.Lessons.AddAsync(lesson2);

                var numberExercises = new List<Exercise>
                {
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson2.Id,
                        Title = "Number Recognition: 5",
                        Type = "MultipleChoice",
                        Content = "What is the Spanish word for the number 5?",
                        CorrectAnswer = "cinco",
                        Options = JsonSerializer.Serialize(new[] { "cuatro", "cinco", "seis", "siete" }),
                        Points = 10,
                        OrderIndex = 1,
                        DifficultyLevel = 1
                    },
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson2.Id,
                        Title = "Translation: Twelve",
                        Type = "Translation",
                        Content = "Translate: twelve",
                        CorrectAnswer = "doce",
                        Points = 10,
                        OrderIndex = 2,
                        DifficultyLevel = 1,
                        Hint = "It starts with 'd' and sounds a bit like the English word"
                    },
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson2.Id,
                        Title = "Number Challenge: 17",
                        Type = "MultipleChoice",
                        Content = "Select the correct spelling for 17",
                        CorrectAnswer = "diecisiete",
                        Options = JsonSerializer.Serialize(new[] { "diez y siete", "diecisiete", "diesisiete", "diecisiete" }),
                        Points = 15,
                        OrderIndex = 3,
                        DifficultyLevel = 2,
                        Explanation = "16-19 are written as one word: dieciséis, diecisiete, dieciocho, diecinueve"
                    }
                };

                await context.Exercises.AddRangeAsync(numberExercises);

                // Lesson 3: Common Verbs
                var lesson3 = new Lesson
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    Title = "Essential Verbs - Present Tense",
                    Description = "Learn the most common Spanish verbs in present tense",
                    OrderIndex = 3,
                    DurationMinutes = 25,
                    IsLocked = false,
                    Content = @"
                        <h2>Verbos Esenciales - Essential Verbs</h2>
                        
                        <h3>Ser (to be - permanent)</h3>
                        <ul>
                            <li>Yo soy - I am</li>
                            <li>Tú eres - You are (informal)</li>
                            <li>Él/Ella es - He/She is</li>
                            <li>Nosotros somos - We are</li>
                            <li>Ellos son - They are</li>
                        </ul>

                        <h3>Estar (to be - temporary/location)</h3>
                        <ul>
                            <li>Yo estoy - I am</li>
                            <li>Tú estás - You are</li>
                            <li>Él/Ella está - He/She is</li>
                            <li>Nosotros estamos - We are</li>
                            <li>Ellos están - They are</li>
                        </ul>

                        <h3>Tener (to have)</h3>
                        <ul>
                            <li>Yo tengo - I have</li>
                            <li>Tú tienes - You have</li>
                            <li>Él/Ella tiene - He/She has</li>
                        </ul>

                        <h3>When to use SER vs ESTAR</h3>
                        <p><strong>SER:</strong> Permanent characteristics, identity, origin</p>
                        <p>Example: Soy estudiante (I am a student)</p>
                        
                        <p><strong>ESTAR:</strong> Temporary states, location, emotions</p>
                        <p>Example: Estoy cansado (I am tired)</p>
                    "
                };
                await context.Lessons.AddAsync(lesson3);

                var verbExercises = new List<Exercise>
                {
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson3.Id,
                        Title = "Verb Conjugation: I am (permanent)",
                        Type = "MultipleChoice",
                        Content = "How do you say 'I am a teacher' (permanent characteristic)?",
                        CorrectAnswer = "Soy profesor",
                        Options = JsonSerializer.Serialize(new[] { "Estoy profesor", "Soy profesor", "Eres profesor", "Está profesor" }),
                        Points = 15,
                        OrderIndex = 1,
                        DifficultyLevel = 2,
                        Explanation = "Use SER for professions and permanent identities"
                    },
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson3.Id,
                        Title = "Location: Where are you?",
                        Type = "FillInBlank",
                        Content = "Complete: ¿Dónde _____? (Where are you?)",
                        CorrectAnswer = "estás",
                        Points = 15,
                        OrderIndex = 2,
                        DifficultyLevel = 2,
                        Hint = "Location always uses ESTAR",
                        Explanation = "Location requires ESTAR: ¿Dónde estás?"
                    }
                };

                await context.Exercises.AddRangeAsync(verbExercises);
            }
        }

        private async Task SeedFrenchCourse(ApplicationDbContext context, Course course)
        {
            if (course.Level == "Beginner")
            {
                var lesson1 = new Lesson
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    Title = "French Greetings & Pronunciation",
                    Description = "Master French greetings and basic pronunciation",
                    OrderIndex = 1,
                    DurationMinutes = 15,
                    IsLocked = false,
                    Content = @"
                        <h2>Bonjour! French Greetings</h2>
                        
                        <h3>Basic Greetings</h3>
                        <ul>
                            <li><strong>Bonjour</strong> - Hello/Good day</li>
                            <li><strong>Bonsoir</strong> - Good evening</li>
                            <li><strong>Salut</strong> - Hi (informal)</li>
                            <li><strong>Au revoir</strong> - Goodbye</li>
                            <li><strong>Comment allez-vous?</strong> - How are you? (formal)</li>
                            <li><strong>Comment ça va?</strong> - How are you? (informal)</li>
                        </ul>

                        <h3>Responses</h3>
                        <ul>
                            <li><strong>Ça va bien</strong> - I'm fine</li>
                            <li><strong>Très bien, merci</strong> - Very well, thank you</li>
                            <li><strong>Comme ci, comme ça</strong> - So-so</li>
                        </ul>

                        <h3>Introductions</h3>
                        <ul>
                            <li><strong>Je m'appelle...</strong> - My name is...</li>
                            <li><strong>Enchanté(e)</strong> - Nice to meet you</li>
                        </ul>
                    "
                };
                await context.Lessons.AddAsync(lesson1);

                var exercises = new List<Exercise>
                {
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson1.Id,
                        Title = "Basic Greeting",
                        Type = "MultipleChoice",
                        Content = "How do you greet someone in the morning in French?",
                        CorrectAnswer = "Bonjour",
                        Options = JsonSerializer.Serialize(new[] { "Bonsoir", "Bonjour", "Salut", "Bonne nuit" }),
                        Points = 10,
                        OrderIndex = 1,
                        DifficultyLevel = 1
                    },
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson1.Id,
                        Title = "Introduction",
                        Type = "FillInBlank",
                        Content = "Complete: Je _____ Pierre (My name is Pierre)",
                        CorrectAnswer = "m'appelle",
                        Points = 15,
                        OrderIndex = 2,
                        DifficultyLevel = 1,
                        Hint = "It's a reflexive verb meaning 'I call myself'"
                    }
                };
                await context.Exercises.AddRangeAsync(exercises);
            }
        }

        private async Task SeedGermanCourse(ApplicationDbContext context, Course course)
        {
            if (course.Level == "Beginner")
            {
                var lesson1 = new Lesson
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    Title = "German Basics & Greetings",
                    Description = "Learn essential German greetings",
                    OrderIndex = 1,
                    DurationMinutes = 15,
                    IsLocked = false,
                    Content = @"
                        <h2>Guten Tag! German Greetings</h2>
                        
                        <h3>Common Greetings</h3>
                        <ul>
                            <li><strong>Guten Morgen</strong> - Good morning</li>
                            <li><strong>Guten Tag</strong> - Good day/Hello</li>
                            <li><strong>Guten Abend</strong> - Good evening</li>
                            <li><strong>Hallo</strong> - Hello (informal)</li>
                            <li><strong>Auf Wiedersehen</strong> - Goodbye</li>
                            <li><strong>Tschüss</strong> - Bye (informal)</li>
                        </ul>

                        <h3>How are you?</h3>
                        <ul>
                            <li><strong>Wie geht es Ihnen?</strong> - How are you? (formal)</li>
                            <li><strong>Wie geht's?</strong> - How are you? (informal)</li>
                            <li><strong>Gut, danke</strong> - Good, thank you</li>
                        </ul>
                    "
                };
                await context.Lessons.AddAsync(lesson1);
            }
        }

        private async Task SeedJapaneseCourse(ApplicationDbContext context, Course course)
        {
            if (course.Level == "Beginner")
            {
                var lesson1 = new Lesson
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    Title = "Hiragana Basics",
                    Description = "Learn the Japanese Hiragana writing system",
                    OrderIndex = 1,
                    DurationMinutes = 25,
                    IsLocked = false,
                    Content = @"
                        <h2>ひらがな - Hiragana Introduction</h2>
                        
                        <h3>Basic Vowels (あ行)</h3>
                        <ul>
                            <li>あ (a) - like 'ah'</li>
                            <li>い (i) - like 'ee'</li>
                            <li>う (u) - like 'oo'</li>
                            <li>え (e) - like 'eh'</li>
                            <li>お (o) - like 'oh'</li>
                        </ul>

                        <h3>K-Row (か行)</h3>
                        <ul>
                            <li>か (ka)</li>
                            <li>き (ki)</li>
                            <li>く (ku)</li>
                            <li>け (ke)</li>
                            <li>こ (ko)</li>
                        </ul>

                        <h3>Common Greetings</h3>
                        <ul>
                            <li>こんにちは (konnichiwa) - Hello</li>
                            <li>ありがとう (arigatou) - Thank you</li>
                        </ul>
                    "
                };
                await context.Lessons.AddAsync(lesson1);
            }
        }

        private async Task SeedItalianCourse(ApplicationDbContext context, Course course)
        {
            if (course.Level == "Beginner")
            {
                var lesson1 = new Lesson
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    Title = "Italian Greetings",
                    Description = "Learn to greet people in Italian",
                    OrderIndex = 1,
                    DurationMinutes = 15,
                    IsLocked = false,
                    Content = @"
                        <h2>Ciao! Italian Greetings</h2>
                        
                        <h3>Basic Greetings</h3>
                        <ul>
                            <li><strong>Ciao</strong> - Hello/Bye (informal)</li>
                            <li><strong>Buongiorno</strong> - Good morning</li>
                            <li><strong>Buonasera</strong> - Good evening</li>
                            <li><strong>Arrivederci</strong> - Goodbye (formal)</li>
                            <li><strong>Come stai?</strong> - How are you? (informal)</li>
                            <li><strong>Come sta?</strong> - How are you? (formal)</li>
                        </ul>

                        <h3>Introductions</h3>
                        <ul>
                            <li><strong>Mi chiamo...</strong> - My name is...</li>
                            <li><strong>Piacere</strong> - Nice to meet you</li>
                        </ul>
                    "
                };
                await context.Lessons.AddAsync(lesson1);
            }
        }

        private async Task SeedChineseCourse(ApplicationDbContext context, Course course)
        {
            if (course.Level == "Beginner")
            {
                var lesson1 = new Lesson
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    Title = "Pinyin & Basic Greetings",
                    Description = "Master Mandarin pronunciation and greetings",
                    OrderIndex = 1,
                    DurationMinutes = 20,
                    IsLocked = false,
                    Content = @"
                        <h2>你好! Mandarin Basics</h2>
                        
                        <h3>The Four Tones</h3>
                        <p>Mandarin is a tonal language with 4 main tones:</p>
                        <ul>
                            <li>1st tone (ā) - high, level</li>
                            <li>2nd tone (á) - rising</li>
                            <li>3rd tone (ǎ) - falling then rising</li>
                            <li>4th tone (à) - sharp falling</li>
                        </ul>

                        <h3>Basic Greetings</h3>
                        <ul>
                            <li><strong>你好 (nǐ hǎo)</strong> - Hello</li>
                            <li><strong>早上好 (zǎo shang hǎo)</strong> - Good morning</li>
                            <li><strong>谢谢 (xiè xie)</strong> - Thank you</li>
                            <li><strong>再见 (zài jiàn)</strong> - Goodbye</li>
                        </ul>
                    "
                };
                await context.Lessons.AddAsync(lesson1);
            }
        }
    }
}