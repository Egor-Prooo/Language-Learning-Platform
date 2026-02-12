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

        #region Spanish Courses

        private async Task SeedSpanishCourse(ApplicationDbContext context, Course course)
        {
            if (course.Level == "Beginner")
            {
                await SeedSpanishBeginnerLessons(context, course);
            }
            else if (course.Level == "Intermediate")
            {
                await SeedSpanishIntermediateLessons(context, course);
            }
            else if (course.Level == "Advanced")
            {
                await SeedSpanishAdvancedLessons(context, course);
            }
        }

        private async Task SeedSpanishBeginnerLessons(ApplicationDbContext context, Course course)
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
                "
            };
            await context.Lessons.AddAsync(lesson1);

            var lesson1Exercises = new List<Exercise>
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
                    Title = "Good morning greeting",
                    Type = "MultipleChoice",
                    Content = "Which greeting would you use in the morning?",
                    CorrectAnswer = "Buenos días",
                    Options = JsonSerializer.Serialize(new[] { "Buenas noches", "Buenos días", "Buenas tardes", "Adiós" }),
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
                    Title = "Complete: My name is...",
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
                    Title = "How are you? (informal)",
                    Type = "MultipleChoice",
                    Content = "Select the informal way to ask 'How are you?'",
                    CorrectAnswer = "¿Cómo estás?",
                    Options = JsonSerializer.Serialize(new[] { "¿Cómo está usted?", "¿Cómo estás?", "¿Qué tal?", "Mucho gusto" }),
                    Points = 15,
                    OrderIndex = 4,
                    DifficultyLevel = 1,
                    Explanation = "¿Cómo estás? is the informal version. ¿Cómo está usted? is formal."
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Translate: Good afternoon",
                    Type = "Translation",
                    Content = "How do you say 'Good afternoon' in Spanish?",
                    CorrectAnswer = "Buenas tardes",
                    Points = 10,
                    OrderIndex = 5,
                    DifficultyLevel = 1,
                    Hint = "It's used from noon until evening",
                    Explanation = "Buenas tardes is used from approximately noon until evening (around 8 PM)."
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Nice to meet you",
                    Type = "MultipleChoice",
                    Content = "Which phrase means 'Nice to meet you'?",
                    CorrectAnswer = "Mucho gusto",
                    Options = JsonSerializer.Serialize(new[] { "Mucho gusto", "Buenos días", "Por favor", "De nada" }),
                    Points = 10,
                    OrderIndex = 6,
                    DifficultyLevel = 1,
                    Explanation = "Mucho gusto literally means 'much pleasure' and is used when meeting someone."
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Response to greeting",
                    Type = "MultipleChoice",
                    Content = "How would you respond to '¿Cómo estás?'",
                    CorrectAnswer = "Bien, gracias",
                    Options = JsonSerializer.Serialize(new[] { "Hola", "Bien, gracias", "Me llamo Juan", "Buenas noches" }),
                    Points = 15,
                    OrderIndex = 7,
                    DifficultyLevel = 1,
                    Explanation = "Bien, gracias means 'Good, thank you' - the standard response to asking how someone is."
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Complete: And you?",
                    Type = "FillInBlank",
                    Content = "Complete: '¿Y _____?' (And you? - informal)",
                    CorrectAnswer = "tú",
                    Points = 15,
                    OrderIndex = 8,
                    DifficultyLevel = 1,
                    Hint = "It's the informal word for 'you'",
                    Explanation = "Tú is the informal 'you' in Spanish, used with friends and family."
                }
            };
            await context.Exercises.AddRangeAsync(lesson1Exercises);

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
                "
            };
            await context.Lessons.AddAsync(lesson2);

            var lesson2Exercises = new List<Exercise>
            {
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson2.Id,
                    Title = "Number 5",
                    Type = "MultipleChoice",
                    Content = "What is the Spanish word for 5?",
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
                    Title = "Translate: twelve",
                    Type = "Translation",
                    Content = "How do you say 'twelve' in Spanish?",
                    CorrectAnswer = "doce",
                    Points = 10,
                    OrderIndex = 2,
                    DifficultyLevel = 1,
                    Hint = "It starts with 'd' and sounds similar to English"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson2.Id,
                    Title = "Number 17",
                    Type = "MultipleChoice",
                    Content = "Select the correct spelling for 17",
                    CorrectAnswer = "diecisiete",
                    Options = JsonSerializer.Serialize(new[] { "diez y siete", "diecisiete", "diesisiete", "diezeisiete" }),
                    Points = 15,
                    OrderIndex = 3,
                    DifficultyLevel = 2,
                    Explanation = "16-19 are written as one word: dieciséis, diecisiete, dieciocho, diecinueve"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson2.Id,
                    Title = "Translate: three",
                    Type = "Translation",
                    Content = "How do you say 'three' in Spanish?",
                    CorrectAnswer = "tres",
                    Points = 10,
                    OrderIndex = 4,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson2.Id,
                    Title = "Complete: Number 8",
                    Type = "FillInBlank",
                    Content = "The number 8 in Spanish is: _____",
                    CorrectAnswer = "ocho",
                    Points = 10,
                    OrderIndex = 5,
                    DifficultyLevel = 1,
                    Hint = "It starts with 'o'"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson2.Id,
                    Title = "Number 15",
                    Type = "MultipleChoice",
                    Content = "What is 15 in Spanish?",
                    CorrectAnswer = "quince",
                    Options = JsonSerializer.Serialize(new[] { "catorce", "quince", "dieciséis", "trece" }),
                    Points = 10,
                    OrderIndex = 6,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson2.Id,
                    Title = "Translate: twenty",
                    Type = "Translation",
                    Content = "How do you say 'twenty' in Spanish?",
                    CorrectAnswer = "veinte",
                    Points = 10,
                    OrderIndex = 7,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson2.Id,
                    Title = "Number sequence",
                    Type = "FillInBlank",
                    Content = "Complete the sequence: uno, dos, _____, cuatro",
                    CorrectAnswer = "tres",
                    Points = 15,
                    OrderIndex = 8,
                    DifficultyLevel = 1,
                    Hint = "What comes between 2 and 4?"
                }
            };
            await context.Exercises.AddRangeAsync(lesson2Exercises);

            // Lesson 3: Essential Verbs
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
                "
            };
            await context.Lessons.AddAsync(lesson3);

            var lesson3Exercises = new List<Exercise>
            {
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson3.Id,
                    Title = "I am (permanent)",
                    Type = "MultipleChoice",
                    Content = "How do you say 'I am a teacher' (permanent characteristic)?",
                    CorrectAnswer = "Soy profesor",
                    Options = JsonSerializer.Serialize(new[] { "Estoy profesor", "Soy profesor", "Eres profesor", "Es profesor" }),
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
                    Title = "Location verb",
                    Type = "FillInBlank",
                    Content = "Complete: ¿Dónde _____? (Where are you?)",
                    CorrectAnswer = "estás",
                    Points = 15,
                    OrderIndex = 2,
                    DifficultyLevel = 2,
                    Hint = "Location always uses ESTAR",
                    Explanation = "Location requires ESTAR: ¿Dónde estás?"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson3.Id,
                    Title = "I have",
                    Type = "Translation",
                    Content = "Translate: 'I have'",
                    CorrectAnswer = "Tengo",
                    Points = 10,
                    OrderIndex = 3,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson3.Id,
                    Title = "Temporary state",
                    Type = "MultipleChoice",
                    Content = "Which verb do you use for temporary states? 'I am tired'",
                    CorrectAnswer = "Estoy cansado",
                    Options = JsonSerializer.Serialize(new[] { "Soy cansado", "Estoy cansado", "Tengo cansado", "Es cansado" }),
                    Points = 15,
                    OrderIndex = 4,
                    DifficultyLevel = 2,
                    Explanation = "Temporary states like emotions and conditions use ESTAR"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson3.Id,
                    Title = "We are",
                    Type = "FillInBlank",
                    Content = "Complete: Nosotros _____ estudiantes (We are students - permanent)",
                    CorrectAnswer = "somos",
                    Points = 15,
                    OrderIndex = 5,
                    DifficultyLevel = 2,
                    Hint = "Use SER for permanent characteristics"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson3.Id,
                    Title = "You have",
                    Type = "MultipleChoice",
                    Content = "How do you say 'You have' (informal)?",
                    CorrectAnswer = "Tienes",
                    Options = JsonSerializer.Serialize(new[] { "Tengo", "Tienes", "Tiene", "Tienen" }),
                    Points = 10,
                    OrderIndex = 6,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson3.Id,
                    Title = "She is (location)",
                    Type = "Translation",
                    Content = "Translate: 'She is in the house'",
                    CorrectAnswer = "Ella está en la casa",
                    Points = 20,
                    OrderIndex = 7,
                    DifficultyLevel = 2,
                    Hint = "Location uses ESTAR"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson3.Id,
                    Title = "They are",
                    Type = "FillInBlank",
                    Content = "Complete: Ellos _____ en Madrid (They are in Madrid)",
                    CorrectAnswer = "están",
                    Points = 15,
                    OrderIndex = 8,
                    DifficultyLevel = 2
                }
            };
            await context.Exercises.AddRangeAsync(lesson3Exercises);
        }

        private async Task SeedSpanishIntermediateLessons(ApplicationDbContext context, Course course)
        {
            // Lesson 1: Past Tense (Preterite)
            var lesson1 = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "Past Tense - Preterite",
                Description = "Master the Spanish preterite tense for completed actions",
                OrderIndex = 1,
                DurationMinutes = 30,
                IsLocked = false,
                Content = @"
                    <h2>El Pretérito - The Preterite Tense</h2>
                    <p>Use the preterite to talk about completed actions in the past.</p>
                    
                    <h3>Regular -AR Verbs (Hablar - to speak)</h3>
                    <ul>
                        <li>Yo hablé - I spoke</li>
                        <li>Tú hablaste - You spoke</li>
                        <li>Él/Ella habló - He/She spoke</li>
                        <li>Nosotros hablamos - We spoke</li>
                        <li>Ellos hablaron - They spoke</li>
                    </ul>

                    <h3>Regular -ER/-IR Verbs (Comer - to eat)</h3>
                    <ul>
                        <li>Yo comí - I ate</li>
                        <li>Tú comiste - You ate</li>
                        <li>Él/Ella comió - He/She ate</li>
                        <li>Nosotros comimos - We ate</li>
                        <li>Ellos comieron - They ate</li>
                    </ul>
                "
            };
            await context.Lessons.AddAsync(lesson1);

            var lesson1Exercises = new List<Exercise>
            {
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "I spoke",
                    Type = "Translation",
                    Content = "Translate: 'I spoke'",
                    CorrectAnswer = "Hablé",
                    Points = 15,
                    OrderIndex = 1,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "You ate",
                    Type = "MultipleChoice",
                    Content = "How do you say 'You ate' (informal)?",
                    CorrectAnswer = "Comiste",
                    Options = JsonSerializer.Serialize(new[] { "Comes", "Comiste", "Comió", "Comí" }),
                    Points = 15,
                    OrderIndex = 2,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Complete: She spoke",
                    Type = "FillInBlank",
                    Content = "Ella _____ con María (She spoke with María)",
                    CorrectAnswer = "habló",
                    Points = 15,
                    OrderIndex = 3,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "We lived",
                    Type = "MultipleChoice",
                    Content = "How do you say 'We lived' (vivir)?",
                    CorrectAnswer = "Vivimos",
                    Options = JsonSerializer.Serialize(new[] { "Vivimos", "Vivieron", "Vivió", "Viviste" }),
                    Points = 15,
                    OrderIndex = 4,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "They worked",
                    Type = "Translation",
                    Content = "Translate: 'They worked' (trabajar)",
                    CorrectAnswer = "Trabajaron",
                    Points = 15,
                    OrderIndex = 5,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "I ate",
                    Type = "FillInBlank",
                    Content = "Yo _____ pizza ayer (I ate pizza yesterday)",
                    CorrectAnswer = "comí",
                    Points = 15,
                    OrderIndex = 6,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "He studied",
                    Type = "MultipleChoice",
                    Content = "How do you say 'He studied' (estudiar)?",
                    CorrectAnswer = "Estudió",
                    Options = JsonSerializer.Serialize(new[] { "Estudia", "Estudió", "Estudió", "Estudiaste" }),
                    Points = 15,
                    OrderIndex = 7,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Complete: You wrote",
                    Type = "FillInBlank",
                    Content = "Tú _____ una carta (You wrote a letter - escribir)",
                    CorrectAnswer = "escribiste",
                    Points = 20,
                    OrderIndex = 8,
                    DifficultyLevel = 3
                }
            };
            await context.Exercises.AddRangeAsync(lesson1Exercises);

            // Lesson 2: Reflexive Verbs
            var lesson2 = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "Reflexive Verbs",
                Description = "Learn how to use reflexive verbs in Spanish",
                OrderIndex = 2,
                DurationMinutes = 25,
                IsLocked = false,
                Content = @"
                    <h2>Verbos Reflexivos - Reflexive Verbs</h2>
                    <p>Reflexive verbs indicate that the subject performs an action on itself.</p>
                    
                    <h3>Levantarse (to get up)</h3>
                    <ul>
                        <li>Me levanto - I get up</li>
                        <li>Te levantas - You get up</li>
                        <li>Se levanta - He/She gets up</li>
                        <li>Nos levantamos - We get up</li>
                        <li>Se levantan - They get up</li>
                    </ul>

                    <h3>Common Reflexive Verbs</h3>
                    <ul>
                        <li>Ducharse - to shower</li>
                        <li>Vestirse - to get dressed</li>
                        <li>Acostarse - to go to bed</li>
                        <li>Llamarse - to be called/named</li>
                    </ul>
                "
            };
            await context.Lessons.AddAsync(lesson2);

            var lesson2Exercises = new List<Exercise>
            {
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson2.Id,
                    Title = "I get up",
                    Type = "Translation",
                    Content = "Translate: 'I get up'",
                    CorrectAnswer = "Me levanto",
                    Points = 15,
                    OrderIndex = 1,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson2.Id,
                    Title = "Reflexive pronoun",
                    Type = "MultipleChoice",
                    Content = "Which pronoun goes with 'Tú' in reflexive verbs?",
                    CorrectAnswer = "te",
                    Options = JsonSerializer.Serialize(new[] { "me", "te", "se", "nos" }),
                    Points = 15,
                    OrderIndex = 2,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson2.Id,
                    Title = "She showers",
                    Type = "FillInBlank",
                    Content = "Ella _____ ducha por la mañana (She showers in the morning)",
                    CorrectAnswer = "se",
                    Points = 15,
                    OrderIndex = 3,
                    DifficultyLevel = 2,
                    Hint = "Use the reflexive pronoun for 'she/he'"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson2.Id,
                    Title = "We get dressed",
                    Type = "Translation",
                    Content = "Translate: 'We get dressed' (vestirse)",
                    CorrectAnswer = "Nos vestimos",
                    Points = 20,
                    OrderIndex = 4,
                    DifficultyLevel = 3
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson2.Id,
                    Title = "My name is",
                    Type = "MultipleChoice",
                    Content = "Complete: '_____ llamo Carlos' (My name is Carlos)",
                    CorrectAnswer = "Me",
                    Options = JsonSerializer.Serialize(new[] { "Me", "Te", "Se", "Nos" }),
                    Points = 10,
                    OrderIndex = 5,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson2.Id,
                    Title = "They go to bed",
                    Type = "FillInBlank",
                    Content = "Ellos _____ acuestan tarde (They go to bed late)",
                    CorrectAnswer = "se",
                    Points = 15,
                    OrderIndex = 6,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson2.Id,
                    Title = "You wash yourself",
                    Type = "Translation",
                    Content = "Translate: 'You wash yourself' (informal - lavarse)",
                    CorrectAnswer = "Te lavas",
                    Points = 15,
                    OrderIndex = 7,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson2.Id,
                    Title = "Reflexive identification",
                    Type = "MultipleChoice",
                    Content = "Which sentence uses a reflexive verb correctly?",
                    CorrectAnswer = "Me despierto a las 7",
                    Options = JsonSerializer.Serialize(new[] { "Yo despierto a las 7", "Me despierto a las 7", "Despierto me a las 7", "A las 7 despierto" }),
                    Points = 15,
                    OrderIndex = 8,
                    DifficultyLevel = 2
                }
            };
            await context.Exercises.AddRangeAsync(lesson2Exercises);
        }

        private async Task SeedSpanishAdvancedLessons(ApplicationDbContext context, Course course)
        {
            // Lesson 1: Subjunctive Mood
            var lesson1 = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "The Subjunctive Mood",
                Description = "Master the Spanish subjunctive for wishes, doubts, and emotions",
                OrderIndex = 1,
                DurationMinutes = 35,
                IsLocked = false,
                Content = @"
                    <h2>El Subjuntivo - The Subjunctive Mood</h2>
                    <p>The subjunctive is used to express wishes, doubts, emotions, and uncertain situations.</p>
                    
                    <h3>Present Subjunctive Formation</h3>
                    <p>Regular verbs: Take the yo form, drop the -o, add opposite endings</p>
                    
                    <h3>Hablar (to speak)</h3>
                    <ul>
                        <li>hable, hables, hable, hablemos, hablen</li>
                    </ul>

                    <h3>Comer (to eat)</h3>
                    <ul>
                        <li>coma, comas, coma, comamos, coman</li>
                    </ul>

                    <h3>Common Triggers</h3>
                    <ul>
                        <li>Querer que - to want that</li>
                        <li>Esperar que - to hope that</li>
                        <li>Es importante que - it's important that</li>
                        <li>Dudar que - to doubt that</li>
                    </ul>
                "
            };
            await context.Lessons.AddAsync(lesson1);

            var lesson1Exercises = new List<Exercise>
            {
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Subjunctive trigger",
                    Type = "MultipleChoice",
                    Content = "Complete: 'Quiero que tú _____ español' (I want you to speak Spanish)",
                    CorrectAnswer = "hables",
                    Options = JsonSerializer.Serialize(new[] { "hablas", "hables", "habla", "hable" }),
                    Points = 20,
                    OrderIndex = 1,
                    DifficultyLevel = 3,
                    Explanation = "After 'querer que' we use the subjunctive mood"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "It's important that",
                    Type = "FillInBlank",
                    Content = "Es importante que ellos _____ (estudiar) - It's important that they study",
                    CorrectAnswer = "estudien",
                    Points = 20,
                    OrderIndex = 2,
                    DifficultyLevel = 3,
                    Hint = "Use the subjunctive form of estudiar"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "I hope that",
                    Type = "Translation",
                    Content = "Translate: 'I hope that she comes' (venir - irregular: venga)",
                    CorrectAnswer = "Espero que ella venga",
                    Points = 25,
                    OrderIndex = 3,
                    DifficultyLevel = 3
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Doubt expression",
                    Type = "MultipleChoice",
                    Content = "Complete: 'Dudo que él _____ la verdad' (I doubt he knows the truth - saber)",
                    CorrectAnswer = "sepa",
                    Options = JsonSerializer.Serialize(new[] { "sabe", "sepa", "saben", "sepas" }),
                    Points = 20,
                    OrderIndex = 4,
                    DifficultyLevel = 3
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Emotion trigger",
                    Type = "FillInBlank",
                    Content = "Me alegro de que tú _____ aquí (I'm glad you're here - estar)",
                    CorrectAnswer = "estés",
                    Points = 20,
                    OrderIndex = 5,
                    DifficultyLevel = 3
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Subjunctive vs Indicative",
                    Type = "MultipleChoice",
                    Content = "Which sentence requires the subjunctive?",
                    CorrectAnswer = "Es posible que llueva",
                    Options = JsonSerializer.Serialize(new[] { "Sé que él viene", "Es posible que llueva", "Creo que ella está", "Es verdad que trabaja" }),
                    Points = 20,
                    OrderIndex = 6,
                    DifficultyLevel = 3,
                    Explanation = "Expressions of possibility require subjunctive"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Negative command",
                    Type = "Translation",
                    Content = "Translate: 'Don't speak!' (informal - hablar)",
                    CorrectAnswer = "No hables",
                    Points = 20,
                    OrderIndex = 7,
                    DifficultyLevel = 3,
                    Hint = "Negative commands use subjunctive"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Complex subjunctive",
                    Type = "FillInBlank",
                    Content = "No creo que ellos _____ mañana (I don't think they'll come tomorrow - venir: vengan)",
                    CorrectAnswer = "vengan",
                    Points = 25,
                    OrderIndex = 8,
                    DifficultyLevel = 3
                }
            };
            await context.Exercises.AddRangeAsync(lesson1Exercises);
        }

        #endregion

        #region French Courses

        private async Task SeedFrenchCourse(ApplicationDbContext context, Course course)
        {
            if (course.Level == "Beginner")
            {
                await SeedFrenchBeginnerLessons(context, course);
            }
            else if (course.Level == "Intermediate")
            {
                await SeedFrenchIntermediateLessons(context, course);
            }
            else if (course.Level == "Advanced")
            {
                await SeedFrenchAdvancedLessons(context, course);
            }
        }

        private async Task SeedFrenchBeginnerLessons(ApplicationDbContext context, Course course)
        {
            var lesson1 = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "French Greetings & Basics",
                Description = "Master French greetings and basic phrases",
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

                    <h3>Introductions</h3>
                    <ul>
                        <li><strong>Je m'appelle...</strong> - My name is...</li>
                        <li><strong>Enchanté(e)</strong> - Nice to meet you</li>
                    </ul>
                "
            };
            await context.Lessons.AddAsync(lesson1);

            var lesson1Exercises = new List<Exercise>
            {
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Basic greeting",
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
                    Title = "My name is",
                    Type = "FillInBlank",
                    Content = "Complete: Je _____ Pierre (My name is Pierre)",
                    CorrectAnswer = "m'appelle",
                    Points = 15,
                    OrderIndex = 2,
                    DifficultyLevel = 1,
                    Hint = "It's a reflexive verb meaning 'I call myself'"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Translate: Goodbye",
                    Type = "Translation",
                    Content = "How do you say 'Goodbye' in French?",
                    CorrectAnswer = "Au revoir",
                    Points = 10,
                    OrderIndex = 3,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Informal greeting",
                    Type = "MultipleChoice",
                    Content = "Which is the informal way to say 'Hi'?",
                    CorrectAnswer = "Salut",
                    Options = JsonSerializer.Serialize(new[] { "Bonjour", "Bonsoir", "Salut", "Au revoir" }),
                    Points = 10,
                    OrderIndex = 4,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "How are you (informal)?",
                    Type = "Translation",
                    Content = "Translate: 'How are you?' (informal)",
                    CorrectAnswer = "Comment ça va",
                    Points = 15,
                    OrderIndex = 5,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Good evening",
                    Type = "FillInBlank",
                    Content = "_____ is used to greet someone in the evening",
                    CorrectAnswer = "Bonsoir",
                    Points = 10,
                    OrderIndex = 6,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Nice to meet you",
                    Type = "MultipleChoice",
                    Content = "How do you say 'Nice to meet you' in French?",
                    CorrectAnswer = "Enchanté",
                    Options = JsonSerializer.Serialize(new[] { "Bonjour", "Enchanté", "Merci", "Au revoir" }),
                    Points = 10,
                    OrderIndex = 7,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Complete introduction",
                    Type = "FillInBlank",
                    Content = "Bonjour, je _____ Marie (Hello, my name is Marie)",
                    CorrectAnswer = "m'appelle",
                    Points = 15,
                    OrderIndex = 8,
                    DifficultyLevel = 1
                }
            };
            await context.Exercises.AddRangeAsync(lesson1Exercises);
        }

        private async Task SeedFrenchIntermediateLessons(ApplicationDbContext context, Course course)
        {
            var lesson1 = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "Passé Composé",
                Description = "Learn the French compound past tense",
                OrderIndex = 1,
                DurationMinutes = 30,
                IsLocked = false,
                Content = @"
                    <h2>Le Passé Composé</h2>
                    <p>The passé composé is used to talk about completed actions in the past.</p>
                    
                    <h3>Formation: avoir/être + past participle</h3>
                    
                    <h3>With Avoir (most verbs)</h3>
                    <ul>
                        <li>J'ai parlé - I spoke</li>
                        <li>Tu as mangé - You ate</li>
                        <li>Il a fini - He finished</li>
                    </ul>

                    <h3>With Être (DR & MRS VANDERTRAMP verbs)</h3>
                    <ul>
                        <li>Je suis allé(e) - I went</li>
                        <li>Tu es venu(e) - You came</li>
                        <li>Elle est partie - She left</li>
                    </ul>
                "
            };
            await context.Lessons.AddAsync(lesson1);

            var lesson1Exercises = new List<Exercise>
            {
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "I ate",
                    Type = "Translation",
                    Content = "Translate: 'I ate' (manger)",
                    CorrectAnswer = "J'ai mangé",
                    Points = 15,
                    OrderIndex = 1,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Auxiliary verb",
                    Type = "MultipleChoice",
                    Content = "Which auxiliary verb is used with 'aller' (to go)?",
                    CorrectAnswer = "être",
                    Options = JsonSerializer.Serialize(new[] { "avoir", "être", "faire", "aller" }),
                    Points = 15,
                    OrderIndex = 2,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "She went",
                    Type = "FillInBlank",
                    Content = "Elle _____ allée à Paris (She went to Paris)",
                    CorrectAnswer = "est",
                    Points = 15,
                    OrderIndex = 3,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "We finished",
                    Type = "Translation",
                    Content = "Translate: 'We finished' (finir)",
                    CorrectAnswer = "Nous avons fini",
                    Points = 20,
                    OrderIndex = 4,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Past participle",
                    Type = "MultipleChoice",
                    Content = "What is the past participle of 'faire' (to do)?",
                    CorrectAnswer = "fait",
                    Options = JsonSerializer.Serialize(new[] { "faisi", "fait", "fai", "faisé" }),
                    Points = 15,
                    OrderIndex = 5,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "They arrived",
                    Type = "FillInBlank",
                    Content = "Ils _____ arrivés hier (They arrived yesterday)",
                    CorrectAnswer = "sont",
                    Points = 15,
                    OrderIndex = 6,
                    DifficultyLevel = 2,
                    Hint = "Arriver uses être"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "You spoke",
                    Type = "Translation",
                    Content = "Translate: 'You spoke' (informal - parler)",
                    CorrectAnswer = "Tu as parlé",
                    Points = 15,
                    OrderIndex = 7,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Agreement check",
                    Type = "MultipleChoice",
                    Content = "Complete: 'Marie est _____ au marché' (Marie went to the market)",
                    CorrectAnswer = "allée",
                    Options = JsonSerializer.Serialize(new[] { "allé", "allée", "allés", "allées" }),
                    Points = 20,
                    OrderIndex = 8,
                    DifficultyLevel = 3,
                    Explanation = "With être, the past participle agrees with the subject"
                }
            };
            await context.Exercises.AddRangeAsync(lesson1Exercises);
        }

        private async Task SeedFrenchAdvancedLessons(ApplicationDbContext context, Course course)
        {
            var lesson1 = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "French Subjunctive",
                Description = "Master the French subjunctive mood",
                OrderIndex = 1,
                DurationMinutes = 35,
                IsLocked = false,
                Content = @"
                    <h2>Le Subjonctif</h2>
                    <p>The subjunctive expresses wishes, emotions, doubts, and uncertainty.</p>
                    
                    <h3>Formation</h3>
                    <p>Take the 'ils' form of present tense, drop -ent, add subjunctive endings</p>
                    
                    <h3>Endings: -e, -es, -e, -ions, -iez, -ent</h3>
                    
                    <h3>Common Triggers</h3>
                    <ul>
                        <li>Il faut que - It's necessary that</li>
                        <li>Je veux que - I want that</li>
                        <li>Il est important que - It's important that</li>
                        <li>Bien que - Although</li>
                    </ul>
                "
            };
            await context.Lessons.AddAsync(lesson1);

            var lesson1Exercises = new List<Exercise>
            {
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Subjunctive necessity",
                    Type = "MultipleChoice",
                    Content = "Complete: 'Il faut que tu _____ tes devoirs' (You must do your homework - faire)",
                    CorrectAnswer = "fasses",
                    Options = JsonSerializer.Serialize(new[] { "fais", "fasses", "fait", "font" }),
                    Points = 20,
                    OrderIndex = 1,
                    DifficultyLevel = 3
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Wish expression",
                    Type = "FillInBlank",
                    Content = "Je veux que vous _____ (partir) - I want you to leave",
                    CorrectAnswer = "partiez",
                    Points = 20,
                    OrderIndex = 2,
                    DifficultyLevel = 3
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Irregular subjunctive",
                    Type = "MultipleChoice",
                    Content = "What is the subjunctive form of 'avoir' for 'je'?",
                    CorrectAnswer = "aie",
                    Options = JsonSerializer.Serialize(new[] { "ai", "aie", "aye", "ave" }),
                    Points = 20,
                    OrderIndex = 3,
                    DifficultyLevel = 3
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Doubt expression",
                    Type = "Translation",
                    Content = "Translate: 'I doubt that he knows' (savoir - sache)",
                    CorrectAnswer = "Je doute qu'il sache",
                    Points = 25,
                    OrderIndex = 4,
                    DifficultyLevel = 3
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Emotion trigger",
                    Type = "FillInBlank",
                    Content = "Je suis content que tu _____ là (I'm happy you're here - être)",
                    CorrectAnswer = "sois",
                    Points = 20,
                    OrderIndex = 5,
                    DifficultyLevel = 3
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Subjunctive recognition",
                    Type = "MultipleChoice",
                    Content = "Which sentence requires the subjunctive?",
                    CorrectAnswer = "Il est possible qu'il vienne",
                    Options = JsonSerializer.Serialize(new[] { "Je sais qu'il vient", "Il est possible qu'il vienne", "Je pense qu'il vient", "Il est clair qu'il vient" }),
                    Points = 20,
                    OrderIndex = 6,
                    DifficultyLevel = 3
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Complex subjunctive",
                    Type = "Translation",
                    Content = "Translate: 'It's important that we understand' (comprendre - comprenions)",
                    CorrectAnswer = "Il est important que nous comprenions",
                    Points = 25,
                    OrderIndex = 7,
                    DifficultyLevel = 3
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Subjunctive with bien que",
                    Type = "FillInBlank",
                    Content = "Bien qu'il _____ malade, il travaille (Although he is sick, he works - être)",
                    CorrectAnswer = "soit",
                    Points = 20,
                    OrderIndex = 8,
                    DifficultyLevel = 3
                }
            };
            await context.Exercises.AddRangeAsync(lesson1Exercises);
        }

        #endregion

        #region German Courses

        private async Task SeedGermanCourse(ApplicationDbContext context, Course course)
        {
            if (course.Level == "Beginner")
            {
                await SeedGermanBeginnerLessons(context, course);
            }
            else if (course.Level == "Intermediate")
            {
                await SeedGermanIntermediateLessons(context, course);
            }
            else if (course.Level == "Advanced")
            {
                await SeedGermanAdvancedLessons(context, course);
            }
        }

        private async Task SeedGermanBeginnerLessons(ApplicationDbContext context, Course course)
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
                    </ul>
                "
            };
            await context.Lessons.AddAsync(lesson1);

            var lesson1Exercises = new List<Exercise>
            {
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Good morning",
                    Type = "Translation",
                    Content = "How do you say 'Good morning' in German?",
                    CorrectAnswer = "Guten Morgen",
                    Points = 10,
                    OrderIndex = 1,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Informal hello",
                    Type = "MultipleChoice",
                    Content = "Which is an informal greeting?",
                    CorrectAnswer = "Hallo",
                    Options = JsonSerializer.Serialize(new[] { "Guten Tag", "Hallo", "Auf Wiedersehen", "Guten Morgen" }),
                    Points = 10,
                    OrderIndex = 2,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Goodbye (informal)",
                    Type = "FillInBlank",
                    Content = "The informal way to say goodbye is _____",
                    CorrectAnswer = "Tschüss",
                    Points = 10,
                    OrderIndex = 3,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Formal goodbye",
                    Type = "Translation",
                    Content = "Translate: 'Goodbye' (formal)",
                    CorrectAnswer = "Auf Wiedersehen",
                    Points = 10,
                    OrderIndex = 4,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Good evening",
                    Type = "MultipleChoice",
                    Content = "How do you say 'Good evening'?",
                    CorrectAnswer = "Guten Abend",
                    Options = JsonSerializer.Serialize(new[] { "Guten Morgen", "Guten Tag", "Guten Abend", "Gute Nacht" }),
                    Points = 10,
                    OrderIndex = 5,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "How are you (informal)?",
                    Type = "FillInBlank",
                    Content = "Complete: Wie _____? (How are you? - informal)",
                    CorrectAnswer = "geht's",
                    Points = 15,
                    OrderIndex = 6,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Formal question",
                    Type = "Translation",
                    Content = "Translate: 'How are you?' (formal)",
                    CorrectAnswer = "Wie geht es Ihnen",
                    Points = 15,
                    OrderIndex = 7,
                    DifficultyLevel = 1
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Appropriate greeting",
                    Type = "MultipleChoice",
                    Content = "What would you say when meeting someone in the afternoon?",
                    CorrectAnswer = "Guten Tag",
                    Options = JsonSerializer.Serialize(new[] { "Guten Morgen", "Guten Tag", "Guten Abend", "Gute Nacht" }),
                    Points = 10,
                    OrderIndex = 8,
                    DifficultyLevel = 1
                }
            };
            await context.Exercises.AddRangeAsync(lesson1Exercises);
        }

        private async Task SeedGermanIntermediateLessons(ApplicationDbContext context, Course course)
        {
            var lesson1 = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "German Cases - Nominative & Accusative",
                Description = "Understand German grammatical cases",
                OrderIndex = 1,
                DurationMinutes = 30,
                IsLocked = false,
                Content = @"
                    <h2>Deutsche Fälle - German Cases</h2>
                    
                    <h3>Nominative Case (Subject)</h3>
                    <p>Used for the subject of the sentence</p>
                    <ul>
                        <li>der Mann (the man)</li>
                        <li>die Frau (the woman)</li>
                        <li>das Kind (the child)</li>
                    </ul>

                    <h3>Accusative Case (Direct Object)</h3>
                    <p>Used for the direct object - der becomes den for masculine nouns</p>
                    <ul>
                        <li>Ich sehe den Mann (I see the man)</li>
                        <li>Ich sehe die Frau (I see the woman)</li>
                        <li>Ich sehe das Kind (I see the child)</li>
                    </ul>
                "
            };
            await context.Lessons.AddAsync(lesson1);

            var lesson1Exercises = new List<Exercise>
            {
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Nominative article",
                    Type = "MultipleChoice",
                    Content = "What is the nominative article for 'Mann' (man)?",
                    CorrectAnswer = "der",
                    Options = JsonSerializer.Serialize(new[] { "der", "den", "dem", "des" }),
                    Points = 15,
                    OrderIndex = 1,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Accusative transformation",
                    Type = "FillInBlank",
                    Content = "Ich sehe _____ Mann (I see the man)",
                    CorrectAnswer = "den",
                    Points = 15,
                    OrderIndex = 2,
                    DifficultyLevel = 2,
                    Hint = "Masculine articles change in accusative"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Case identification",
                    Type = "MultipleChoice",
                    Content = "In 'Der Mann sieht die Frau', which case is 'die Frau'?",
                    CorrectAnswer = "Accusative",
                    Options = JsonSerializer.Serialize(new[] { "Nominative", "Accusative", "Dative", "Genitive" }),
                    Points = 15,
                    OrderIndex = 3,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Translate with accusative",
                    Type = "Translation",
                    Content = "Translate: 'I have the book' (das Buch)",
                    CorrectAnswer = "Ich habe das Buch",
                    Points = 20,
                    OrderIndex = 4,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Feminine accusative",
                    Type = "MultipleChoice",
                    Content = "Complete: 'Ich liebe _____ Frau' (I love the woman)",
                    CorrectAnswer = "die",
                    Options = JsonSerializer.Serialize(new[] { "der", "die", "den", "das" }),
                    Points = 15,
                    OrderIndex = 5,
                    DifficultyLevel = 2,
                    Explanation = "Feminine articles don't change in accusative"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Neuter accusative",
                    Type = "FillInBlank",
                    Content = "Wir kaufen _____ Auto (We buy the car - das Auto)",
                    CorrectAnswer = "das",
                    Points = 15,
                    OrderIndex = 6,
                    DifficultyLevel = 2
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Subject identification",
                    Type = "MultipleChoice",
                    Content = "In 'Den Mann sieht die Frau', who is doing the seeing?",
                    CorrectAnswer = "die Frau",
                    Options = JsonSerializer.Serialize(new[] { "der Mann", "den Mann", "die Frau", "unclear" }),
                    Points = 20,
                    OrderIndex = 7,
                    DifficultyLevel = 3,
                    Explanation = "Die Frau is nominative (subject), den Mann is accusative (object)"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Complete sentence",
                    Type = "FillInBlank",
                    Content = "Der Lehrer fragt _____ Schüler (The teacher asks the student - der Schüler)",
                    CorrectAnswer = "den",
                    Points = 15,
                    OrderIndex = 8,
                    DifficultyLevel = 2
                }
            };
            await context.Exercises.AddRangeAsync(lesson1Exercises);
        }

        private async Task SeedGermanAdvancedLessons(ApplicationDbContext context, Course course)
        {
            var lesson1 = new Lesson
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "Subjunctive Mood (Konjunktiv II)",
                Description = "Master the German subjunctive for hypothetical situations",
                OrderIndex = 1,
                DurationMinutes = 35,
                IsLocked = false,
                Content = @"
                    <h2>Konjunktiv II - Subjunctive Mood</h2>
                    <p>Used for hypothetical situations, polite requests, and wishes</p>
                    
                    <h3>Common Forms</h3>
                    <ul>
                        <li><strong>sein</strong> → wäre (would be)</li>
                        <li><strong>haben</strong> → hätte (would have)</li>
                        <li><strong>werden</strong> → würde (would)</li>
                        <li><strong>können</strong> → könnte (could)</li>
                    </ul>

                    <h3>Usage</h3>
                    <ul>
                        <li>Wenn ich reich wäre... (If I were rich...)</li>
                        <li>Ich hätte gerne... (I would like...)</li>
                        <li>Könnten Sie mir helfen? (Could you help me?)</li>
                    </ul>
                "
            };
            await context.Lessons.AddAsync(lesson1);

            var lesson1Exercises = new List<Exercise>
            {
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Would be",
                    Type = "Translation",
                    Content = "Translate: 'I would be' (sein)",
                    CorrectAnswer = "Ich wäre",
                    Points = 20,
                    OrderIndex = 1,
                    DifficultyLevel = 3
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Polite request",
                    Type = "MultipleChoice",
                    Content = "Complete: '_____ Sie mir helfen?' (Could you help me?)",
                    CorrectAnswer = "Könnten",
                    Options = JsonSerializer.Serialize(new[] { "Können", "Könnten", "Könnte", "Konnten" }),
                    Points = 20,
                    OrderIndex = 2,
                    DifficultyLevel = 3
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Would have",
                    Type = "FillInBlank",
                    Content = "Ich _____ gerne einen Kaffee (I would like a coffee - haben)",
                    CorrectAnswer = "hätte",
                    Points = 20,
                    OrderIndex = 3,
                    DifficultyLevel = 3
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Hypothetical situation",
                    Type = "Translation",
                    Content = "Translate: 'If I were rich' (reich)",
                    CorrectAnswer = "Wenn ich reich wäre",
                    Points = 25,
                    OrderIndex = 4,
                    DifficultyLevel = 3
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Subjunctive form",
                    Type = "MultipleChoice",
                    Content = "What is the Konjunktiv II form of 'gehen' (to go) for 'er'?",
                    CorrectAnswer = "ginge",
                    Options = JsonSerializer.Serialize(new[] { "geht", "ging", "ginge", "würde gehen" }),
                    Points = 20,
                    OrderIndex = 5,
                    DifficultyLevel = 3
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Wish expression",
                    Type = "FillInBlank",
                    Content = "Ich wünschte, ich _____ mehr Zeit (I wish I had more time - haben)",
                    CorrectAnswer = "hätte",
                    Points = 20,
                    OrderIndex = 6,
                    DifficultyLevel = 3
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Alternative form",
                    Type = "MultipleChoice",
                    Content = "Which is an alternative to 'Er käme'?",
                    CorrectAnswer = "Er würde kommen",
                    Options = JsonSerializer.Serialize(new[] { "Er kommt", "Er würde kommen", "Er kam", "Er ist gekommen" }),
                    Points = 20,
                    OrderIndex = 7,
                    DifficultyLevel = 3,
                    Explanation = "Würde + infinitive is commonly used instead of conjugated Konjunktiv II"
                },
                new Exercise
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    LessonId = lesson1.Id,
                    Title = "Complex conditional",
                    Type = "Translation",
                    Content = "Translate: 'If I could, I would help' (können, helfen)",
                    CorrectAnswer = "Wenn ich könnte, würde ich helfen",
                    Points = 25,
                    OrderIndex = 8,
                    DifficultyLevel = 3
                }
            };
            await context.Exercises.AddRangeAsync(lesson1Exercises);
        }

        #endregion

        #region Japanese, Italian, Chinese Courses
        // Note: For brevity, I'll create one beginner lesson each for Japanese, Italian, and Chinese
        // You can expand these following the same pattern

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
                        <h2>ひらがな - Hiragana</h2>
                        
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
                            <li>か (ka), き (ki), く (ku), け (ke), こ (ko)</li>
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
                        Title = "Hiragana 'a'",
                        Type = "MultipleChoice",
                        Content = "Which hiragana represents the sound 'a'?",
                        CorrectAnswer = "あ",
                        Options = JsonSerializer.Serialize(new[] { "あ", "い", "う", "え" }),
                        Points = 10,
                        OrderIndex = 1,
                        DifficultyLevel = 1
                    },
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson1.Id,
                        Title = "Translate: hello",
                        Type = "Translation",
                        Content = "How do you say 'hello' in Japanese?",
                        CorrectAnswer = "こんにちは",
                        Points = 10,
                        OrderIndex = 2,
                        DifficultyLevel = 1
                    },
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson1.Id,
                        Title = "Sound 'ka'",
                        Type = "FillInBlank",
                        Content = "The hiragana for 'ka' is _____",
                        CorrectAnswer = "か",
                        Points = 10,
                        OrderIndex = 3,
                        DifficultyLevel = 1
                    }
                };
                await context.Exercises.AddRangeAsync(exercises);
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
                            <li><strong>Come stai?</strong> - How are you? (informal)</li>
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
                        Title = "Informal hello",
                        Type = "Translation",
                        Content = "How do you say 'Hello' informally in Italian?",
                        CorrectAnswer = "Ciao",
                        Points = 10,
                        OrderIndex = 1,
                        DifficultyLevel = 1
                    },
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson1.Id,
                        Title = "Good morning",
                        Type = "MultipleChoice",
                        Content = "Which greeting is used in the morning?",
                        CorrectAnswer = "Buongiorno",
                        Options = JsonSerializer.Serialize(new[] { "Ciao", "Buongiorno", "Buonasera", "Arrivederci" }),
                        Points = 10,
                        OrderIndex = 2,
                        DifficultyLevel = 1
                    },
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson1.Id,
                        Title = "How are you?",
                        Type = "FillInBlank",
                        Content = "Come _____? (How are you? - informal)",
                        CorrectAnswer = "stai",
                        Points = 10,
                        OrderIndex = 3,
                        DifficultyLevel = 1
                    }
                };
                await context.Exercises.AddRangeAsync(exercises);
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

                var exercises = new List<Exercise>
                {
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson1.Id,
                        Title = "Hello",
                        Type = "Translation",
                        Content = "How do you say 'Hello' in Mandarin (pinyin)?",
                        CorrectAnswer = "nǐ hǎo",
                        Points = 10,
                        OrderIndex = 1,
                        DifficultyLevel = 1
                    },
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson1.Id,
                        Title = "Thank you",
                        Type = "MultipleChoice",
                        Content = "Which is 'Thank you' in Chinese?",
                        CorrectAnswer = "谢谢",
                        Options = JsonSerializer.Serialize(new[] { "你好", "谢谢", "再见", "早上好" }),
                        Points = 10,
                        OrderIndex = 2,
                        DifficultyLevel = 1
                    },
                    new Exercise
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        LessonId = lesson1.Id,
                        Title = "Pinyin completion",
                        Type = "FillInBlank",
                        Content = "Complete: zài _____ (Goodbye)",
                        CorrectAnswer = "jiàn",
                        Points = 10,
                        OrderIndex = 3,
                        DifficultyLevel = 1
                    }
                };
                await context.Exercises.AddRangeAsync(exercises);
            }
        }

        #endregion
    }
}