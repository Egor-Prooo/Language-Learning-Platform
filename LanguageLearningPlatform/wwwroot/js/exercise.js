// Interactive Exercise Handler
class ExerciseHandler {
    constructor() {
        this.currentExercise = null;
        this.attempts = 0;
        this.maxAttempts = 3;
        this.init();
    }

    init() {
        this.bindEventListeners();
    }

    bindEventListeners() {
        // Multiple choice answers
        document.querySelectorAll('.exercise-option').forEach(option => {
            option.addEventListener('click', (e) => this.handleMultipleChoice(e));
        });

        // Translation and fill-in-blank inputs
        document.querySelectorAll('.exercise-input').forEach(input => {
            input.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    this.checkAnswer(e.target);
                }
            });
        });

        // Check answer buttons
        document.querySelectorAll('.btn-check-answer').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const input = e.target.closest('.exercise-item').querySelector('.exercise-input');
                if (input) {
                    this.checkAnswer(input);
                }
            });
        });

        // Skip buttons
        document.querySelectorAll('.btn-skip').forEach(btn => {
            btn.addEventListener('click', () => this.skipExercise());
        });
    }

    handleMultipleChoice(event) {
        const option = event.currentTarget;
        const exerciseItem = option.closest('.exercise-item');
        const options = exerciseItem.querySelectorAll('.exercise-option');

        // Remove previous selection
        options.forEach(opt => opt.classList.remove('selected'));

        // Select current option
        option.classList.add('selected');

        // Enable check button
        const checkBtn = exerciseItem.querySelector('.btn-check-answer');
        if (checkBtn) {
            checkBtn.disabled = false;
        }
    }

    async checkAnswer(input) {
        const exerciseItem = input.closest('.exercise-item');
        const exerciseId = exerciseItem.dataset.exerciseId;
        const correctAnswer = exerciseItem.dataset.correctAnswer;

        let userAnswer;

        // Get answer based on exercise type
        if (exerciseItem.querySelector('.exercise-option.selected')) {
            userAnswer = exerciseItem.querySelector('.exercise-option.selected').dataset.value;
        } else {
            userAnswer = input.value.trim();
        }

        // Check if answer is correct (case-insensitive, trim whitespace)
        const isCorrect = this.compareAnswers(userAnswer, correctAnswer);

        this.attempts++;

        if (isCorrect) {
            this.showFeedback(exerciseItem, true, 'Correct! Well done! 🎉');
            this.animateSuccess(exerciseItem);
            await this.submitResult(exerciseId, userAnswer, true);
        } else {
            if (this.attempts < this.maxAttempts) {
                this.showFeedback(exerciseItem, false, `Not quite. Try again! (${this.maxAttempts - this.attempts} attempts left)`);
                this.animateError(exerciseItem);
            } else {
                this.showFeedback(exerciseItem, false, `The correct answer is: ${correctAnswer}`);
                await this.submitResult(exerciseId, userAnswer, false);
            }
        }
    }

    compareAnswers(userAnswer, correctAnswer) {
        // Normalize both answers
        const normalize = (str) => str.toLowerCase().trim().replace(/[áàä]/g, 'a')
            .replace(/[éèë]/g, 'e')
            .replace(/[íìï]/g, 'i')
            .replace(/[óòö]/g, 'o')
            .replace(/[úùü]/g, 'u')
            .replace(/ñ/g, 'n');

        return normalize(userAnswer) === normalize(correctAnswer);
    }

    showFeedback(exerciseItem, isCorrect, message) {
        let feedbackDiv = exerciseItem.querySelector('.exercise-feedback');

        if (!feedbackDiv) {
            feedbackDiv = document.createElement('div');
            feedbackDiv.className = 'exercise-feedback';
            exerciseItem.querySelector('.exercise-content').appendChild(feedbackDiv);
        }

        feedbackDiv.className = `exercise-feedback ${isCorrect ? 'correct' : 'incorrect'}`;
        feedbackDiv.innerHTML = `
            <i class="fas fa-${isCorrect ? 'check-circle' : 'times-circle'} me-2"></i>
            ${message}
        `;

        // Show explanation if available and answer is correct
        if (isCorrect) {
            const explanation = exerciseItem.dataset.explanation;
            if (explanation) {
                feedbackDiv.innerHTML += `<p class="mt-2 mb-0 small"><strong>Explanation:</strong> ${explanation}</p>`;
            }
        }

        feedbackDiv.style.display = 'block';
    }

    animateSuccess(exerciseItem) {
        exerciseItem.classList.add('exercise-success');
        setTimeout(() => {
            exerciseItem.classList.remove('exercise-success');
        }, 1000);

        // Confetti animation
        this.createConfetti(exerciseItem);
    }

    animateError(exerciseItem) {
        exerciseItem.classList.add('exercise-error');
        setTimeout(() => {
            exerciseItem.classList.remove('exercise-error');
        }, 600);
    }

    createConfetti(element) {
        for (let i = 0; i < 20; i++) {
            const confetti = document.createElement('div');
            confetti.className = 'confetti';
            confetti.style.left = Math.random() * 100 + '%';
            confetti.style.animationDelay = Math.random() * 0.5 + 's';
            confetti.style.backgroundColor = ['#4F46E5', '#10B981', '#F59E0B'][Math.floor(Math.random() * 3)];
            element.appendChild(confetti);

            setTimeout(() => confetti.remove(), 1500);
        }
    }

    async submitResult(exerciseId, userAnswer, isCorrect) {
        try {
            const response = await fetch('/api/exercises/submit', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    exerciseId: exerciseId,
                    userAnswer: userAnswer,
                    isCorrect: isCorrect,
                    attemptsCount: this.attempts
                })
            });

            if (response.ok) {
                const result = await response.json();
                this.updateProgress(result.pointsEarned);
            }
        } catch (error) {
            console.error('Error submitting exercise result:', error);
        }
    }

    updateProgress(pointsEarned) {
        // Update points display
        const pointsDisplay = document.querySelector('.user-points');
        if (pointsDisplay) {
            const currentPoints = parseInt(pointsDisplay.textContent);
            pointsDisplay.textContent = currentPoints + pointsEarned;

            // Animate points
            pointsDisplay.classList.add('points-earned');
            setTimeout(() => pointsDisplay.classList.remove('points-earned'), 600);
        }
    }

    skipExercise() {
        // Move to next exercise or show completion message
        console.log('Exercise skipped');
    }
}

// Speech synthesis for pronunciation
class PronunciationHelper {
    constructor() {
        this.synth = window.speechSynthesis;
        this.voices = [];
        this.loadVoices();
    }

    loadVoices() {
        this.voices = this.synth.getVoices();
        if (this.voices.length === 0) {
            this.synth.addEventListener('voiceschanged', () => {
                this.voices = this.synth.getVoices();
            });
        }
    }

    speak(text, language = 'es-ES') {
        if (!this.synth) return;

        const utterance = new SpeechSynthesisUtterance(text);

        // Find appropriate voice for language
        const voice = this.voices.find(v => v.lang.startsWith(language.split('-')[0]));
        if (voice) {
            utterance.voice = voice;
        }

        utterance.lang = language;
        utterance.rate = 0.9; // Slightly slower for learning

        this.synth.speak(utterance);
    }
}

// Progress tracker
class ProgressTracker {
    constructor() {
        this.completedExercises = 0;
        this.totalExercises = document.querySelectorAll('.exercise-item').length;
        this.updateProgressBar();
    }

    markComplete() {
        this.completedExercises++;
        this.updateProgressBar();

        if (this.completedExercises === this.totalExercises) {
            this.showCompletionCelebration();
        }
    }

    updateProgressBar() {
        const progressBar = document.querySelector('.lesson-progress-bar');
        if (progressBar) {
            const percentage = (this.completedExercises / this.totalExercises) * 100;
            progressBar.style.width = percentage + '%';

            const progressText = document.querySelector('.lesson-progress-text');
            if (progressText) {
                progressText.textContent = `${this.completedExercises}/${this.totalExercises} exercises completed`;
            }
        }
    }

    showCompletionCelebration() {
        // Show completion modal or message
        const modal = document.createElement('div');
        modal.className = 'completion-modal';
        modal.innerHTML = `
            <div class="completion-content">
                <i class="fas fa-trophy" style="font-size: 4rem; color: var(--accent-color);"></i>
                <h2>Lesson Complete!</h2>
                <p>Great job! You've completed all exercises.</p>
                <button class="btn btn-enroll" onclick="window.location.href='/courses/mycourses'">
                    Continue Learning
                </button>
            </div>
        `;
        document.body.appendChild(modal);
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    const exerciseHandler = new ExerciseHandler();
    const pronunciationHelper = new PronunciationHelper();
    const progressTracker = new ProgressTracker();

    // Add pronunciation buttons
    document.querySelectorAll('.pronunciation-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            const text = e.target.dataset.text;
            const lang = e.target.dataset.lang || 'es-ES';
            pronunciationHelper.speak(text, lang);
        });
    });
});