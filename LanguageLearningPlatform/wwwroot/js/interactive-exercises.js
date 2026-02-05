// Enhanced Interactive Exercise Handler - Duolingo Style
class InteractiveExerciseHandler {
    constructor() {
        this.currentExerciseIndex = 0;
        this.exercises = [];
        this.completedExercises = new Set();
        this.totalPoints = 0;
        this.streak = 0;
        this.hearts = 5; // Lives system like Duolingo
        this.init();
    }

    init() {
        this.loadExercises();
        this.bindEventListeners();
        this.initializeProgressBar();
        this.initializeSoundEffects();
    }

    loadExercises() {
        // Get all exercise items from the DOM
        const exerciseElements = document.querySelectorAll('.exercise-item');
        exerciseElements.forEach((element, index) => {
            this.exercises.push({
                id: element.dataset.exerciseId,
                element: element,
                type: element.dataset.type,
                correctAnswer: element.dataset.correctAnswer,
                points: parseInt(element.dataset.points) || 10,
                index: index
            });
        });
    }

    bindEventListeners() {
        // Multiple choice options with animation
        document.querySelectorAll('.exercise-option').forEach(option => {
            option.addEventListener('click', (e) => this.handleMultipleChoice(e));
        });

        // Text input exercises
        document.querySelectorAll('.exercise-input').forEach(input => {
            input.addEventListener('input', (e) => this.handleInputChange(e));
            input.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    this.checkAnswer(e.target);
                }
            });
        });

        // Check answer buttons
        document.querySelectorAll('.btn-check-answer').forEach(btn => {
            btn.addEventListener('click', (e) => this.handleCheckAnswer(e));
        });

        // Hint buttons
        document.querySelectorAll('.btn-hint').forEach(btn => {
            btn.addEventListener('click', (e) => this.showHint(e));
        });

        // Skip buttons
        document.querySelectorAll('.btn-skip').forEach(btn => {
            btn.addEventListener('click', (e) => this.skipExercise(e));
        });

        // Pronunciation buttons
        document.querySelectorAll('.btn-pronunciation').forEach(btn => {
            btn.addEventListener('click', (e) => this.playPronunciation(e));
        });
    }

    handleInputChange(event) {
        const input = event.target;
        const checkBtn = input.closest('.exercise-item').querySelector('.btn-check-answer');

        // Enable/disable check button based on input
        if (checkBtn) {
            checkBtn.disabled = input.value.trim().length === 0;
        }

        // Remove any previous error states
        input.classList.remove('is-invalid');
    }

    handleMultipleChoice(event) {
        const option = event.currentTarget;
        const exerciseItem = option.closest('.exercise-item');
        const options = exerciseItem.querySelectorAll('.exercise-option');

        // Remove previous selections
        options.forEach(opt => {
            opt.classList.remove('selected', 'pulse-animation');
        });

        // Select current option with animation
        option.classList.add('selected', 'pulse-animation');

        // Play selection sound
        this.playSound('select');

        // Enable check button
        const checkBtn = exerciseItem.querySelector('.btn-check-answer');
        if (checkBtn) {
            checkBtn.disabled = false;
        }

        // Remove animation class after animation completes
        setTimeout(() => option.classList.remove('pulse-animation'), 300);
    }

    async handleCheckAnswer(event) {
        const button = event.currentTarget;
        const exerciseItem = button.closest('.exercise-item');
        const exerciseId = exerciseItem.dataset.exerciseId;
        const exerciseType = exerciseItem.dataset.type;

        let userAnswer = '';

        // Get answer based on exercise type
        if (exerciseType === 'MultipleChoice') {
            const selected = exerciseItem.querySelector('.exercise-option.selected');
            if (!selected) return;
            userAnswer = selected.dataset.value;
        } else {
            const input = exerciseItem.querySelector('.exercise-input');
            if (!input) return;
            userAnswer = input.value.trim();
        }

        if (!userAnswer) return;

        // Disable button during check
        button.disabled = true;
        button.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Checking...';

        try {
            await this.submitAnswer(exerciseId, userAnswer, exerciseItem);
        } catch (error) {
            console.error('Error checking answer:', error);
            this.showError(exerciseItem, 'An error occurred. Please try again.');
        } finally {
            button.disabled = false;
            button.innerHTML = '<i class="fas fa-check me-1"></i>Check Answer';
        }
    }

    async submitAnswer(exerciseId, userAnswer, exerciseItem) {
        const startTime = exerciseItem.dataset.startTime || Date.now();
        const timeSpent = Math.floor((Date.now() - startTime) / 1000);

        const response = await fetch('/api/exercises/submit', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                exerciseId: exerciseId,
                userAnswer: userAnswer,
                timeSpentSeconds: timeSpent,
                attemptsCount: parseInt(exerciseItem.dataset.attempts || '1')
            })
        });

        if (!response.ok) {
            throw new Error('Failed to submit answer');
        }

        const result = await response.json();
        this.handleAnswerResult(result, exerciseItem);
    }

    handleAnswerResult(result, exerciseItem) {
        if (result.isCorrect) {
            this.handleCorrectAnswer(result, exerciseItem);
        } else {
            this.handleIncorrectAnswer(result, exerciseItem);
        }
    }

    handleCorrectAnswer(result, exerciseItem) {
        // Play success sound
        this.playSound('correct');

        // Show success feedback
        this.showFeedback(exerciseItem, true, result.feedback, result.explanation);

        // Animate the exercise item
        exerciseItem.classList.add('exercise-success');

        // Create confetti
        this.createConfetti(exerciseItem);

        // Update points with animation
        this.updatePoints(result.pointsEarned, result.totalPoints);

        // Update streak
        this.updateStreak(result.streak);

        // Mark as completed
        this.completedExercises.add(exerciseItem.dataset.exerciseId);

        // Update progress
        this.updateProgress();

        // Disable further interaction
        setTimeout(() => {
            this.disableExercise(exerciseItem);
        }, 2000);

        // Check if level up
        if (result.levelUp) {
            this.showLevelUpModal();
        }
    }

    handleIncorrectAnswer(result, exerciseItem) {
        // Decrease hearts
        this.hearts = Math.max(0, this.hearts - 1);
        this.updateHeartsDisplay();

        // Play error sound
        this.playSound('incorrect');

        // Shake animation
        exerciseItem.classList.add('exercise-error');
        setTimeout(() => exerciseItem.classList.remove('exercise-error'), 600);

        // Show feedback
        this.showFeedback(exerciseItem, false, result.feedback, result.correctAnswer);

        // Increment attempts
        const currentAttempts = parseInt(exerciseItem.dataset.attempts || '1');
        exerciseItem.dataset.attempts = (currentAttempts + 1).toString();

        // If out of hearts, show game over
        if (this.hearts === 0) {
            this.showGameOver();
        }
    }

    showFeedback(exerciseItem, isCorrect, message, extraInfo) {
        let feedbackDiv = exerciseItem.querySelector('.exercise-feedback');

        if (!feedbackDiv) {
            feedbackDiv = document.createElement('div');
            feedbackDiv.className = 'exercise-feedback';
            exerciseItem.querySelector('.exercise-content').appendChild(feedbackDiv);
        }

        const icon = isCorrect ? 'check-circle' : 'times-circle';
        const className = isCorrect ? 'correct' : 'incorrect';

        feedbackDiv.className = `exercise-feedback ${className}`;
        feedbackDiv.innerHTML = `
            <div class="feedback-icon">
                <i class="fas fa-${icon}"></i>
            </div>
            <div class="feedback-content">
                <div class="feedback-message">${message}</div>
                ${extraInfo ? `<div class="feedback-extra">${extraInfo}</div>` : ''}
            </div>
        `;

        feedbackDiv.style.display = 'flex';
        feedbackDiv.classList.add('slide-down-animation');
    }

    createConfetti(element) {
        const colors = ['#4F46E5', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6'];
        const confettiCount = 30;

        for (let i = 0; i < confettiCount; i++) {
            const confetti = document.createElement('div');
            confetti.className = 'confetti';
            confetti.style.left = Math.random() * 100 + '%';
            confetti.style.backgroundColor = colors[Math.floor(Math.random() * colors.length)];
            confetti.style.animationDelay = Math.random() * 0.3 + 's';
            confetti.style.animationDuration = (Math.random() * 1 + 1) + 's';

            element.appendChild(confetti);

            setTimeout(() => confetti.remove(), 2000);
        }
    }

    updatePoints(earned, total) {
        const pointsDisplay = document.getElementById('user-total-points');
        if (pointsDisplay) {
            // Animate points increment
            const startPoints = parseInt(pointsDisplay.textContent) || 0;
            const duration = 1000;
            const startTime = Date.now();

            const animatePoints = () => {
                const now = Date.now();
                const progress = Math.min((now - startTime) / duration, 1);
                const currentPoints = Math.floor(startPoints + (total - startPoints) * progress);

                pointsDisplay.textContent = currentPoints;
                pointsDisplay.classList.add('points-earned');

                if (progress < 1) {
                    requestAnimationFrame(animatePoints);
                } else {
                    setTimeout(() => pointsDisplay.classList.remove('points-earned'), 500);
                }
            };

            animatePoints();
        }

        // Show floating points
        this.showFloatingPoints(earned);
    }

    showFloatingPoints(points) {
        const floatingPoints = document.createElement('div');
        floatingPoints.className = 'floating-points';
        floatingPoints.textContent = `+${points}`;
        floatingPoints.style.position = 'fixed';
        floatingPoints.style.top = '50%';
        floatingPoints.style.left = '50%';
        floatingPoints.style.transform = 'translate(-50%, -50%)';

        document.body.appendChild(floatingPoints);

        setTimeout(() => floatingPoints.remove(), 2000);
    }

    updateStreak(streak) {
        const streakDisplay = document.getElementById('user-streak');
        if (streakDisplay) {
            streakDisplay.textContent = streak;
            if (streak > this.streak) {
                streakDisplay.classList.add('streak-increase');
                setTimeout(() => streakDisplay.classList.remove('streak-increase'), 500);
            }
        }
        this.streak = streak;
    }

    updateHeartsDisplay() {
        const heartsContainer = document.getElementById('hearts-container');
        if (heartsContainer) {
            heartsContainer.innerHTML = '';
            for (let i = 0; i < 5; i++) {
                const heart = document.createElement('i');
                heart.className = i < this.hearts ? 'fas fa-heart' : 'far fa-heart';
                heart.style.color = i < this.hearts ? '#EF4444' : '#E5E7EB';
                heartsContainer.appendChild(heart);
            }
        }
    }

    updateProgress() {
        const completed = this.completedExercises.size;
        const total = this.exercises.length;
        const percentage = (completed / total) * 100;

        const progressBar = document.getElementById('progress-bar');
        const progressText = document.getElementById('completed-count');

        if (progressBar) {
            progressBar.style.width = percentage + '%';
        }

        if (progressText) {
            progressText.textContent = completed;
        }

        // Check if all exercises completed
        if (completed === total) {
            setTimeout(() => this.showCompletionCelebration(), 1000);
        }
    }

    showHint(event) {
        const button = event.currentTarget;
        const exerciseItem = button.closest('.exercise-item');
        const hintDiv = exerciseItem.querySelector('.exercise-hint');

        if (hintDiv) {
            hintDiv.style.display = hintDiv.style.display === 'none' ? 'flex' : 'none';
            button.innerHTML = hintDiv.style.display === 'none'
                ? '<i class="fas fa-lightbulb me-1"></i>Show Hint'
                : '<i class="fas fa-lightbulb me-1"></i>Hide Hint';
        }
    }

    skipExercise(event) {
        const exerciseItem = event.currentTarget.closest('.exercise-item');
        this.disableExercise(exerciseItem);
        this.updateProgress();
    }

    disableExercise(exerciseItem) {
        // Disable all interactive elements
        exerciseItem.querySelectorAll('button, input, .exercise-option').forEach(el => {
            el.disabled = true;
            el.style.pointerEvents = 'none';
        });

        exerciseItem.classList.add('exercise-completed');
    }

    playPronunciation(event) {
        const button = event.currentTarget;
        const text = button.dataset.text;
        const lang = button.dataset.lang || 'es-ES';

        if ('speechSynthesis' in window) {
            const utterance = new SpeechSynthesisUtterance(text);
            utterance.lang = lang;
            utterance.rate = 0.85;
            window.speechSynthesis.speak(utterance);

            // Animate button
            button.classList.add('speaking');
            utterance.onend = () => button.classList.remove('speaking');
        }
    }

    initializeProgressBar() {
        this.updateProgress();
        this.updateHeartsDisplay();
    }

    initializeSoundEffects() {
        // Preload sounds for better performance
        this.sounds = {
            correct: new Audio('/sounds/correct.mp3'),
            incorrect: new Audio('/sounds/incorrect.mp3'),
            select: new Audio('/sounds/select.mp3')
        };

        // Set volume
        Object.values(this.sounds).forEach(sound => {
            sound.volume = 0.3;
        });
    }

    playSound(type) {
        if (this.sounds[type]) {
            this.sounds[type].currentTime = 0;
            this.sounds[type].play().catch(e => console.log('Sound play failed:', e));
        }
    }

    showCompletionCelebration() {
        const modal = document.createElement('div');
        modal.className = 'completion-modal';
        modal.innerHTML = `
            <div class="completion-content">
                <div class="completion-trophy">
                    <i class="fas fa-trophy"></i>
                </div>
                <h2>Lesson Complete! 🎉</h2>
                <p class="completion-message">Amazing work! You've mastered this lesson.</p>
                <div class="completion-stats">
                    <div class="stat">
                        <div class="stat-value">${this.completedExercises.size}</div>
                        <div class="stat-label">Exercises</div>
                    </div>
                    <div class="stat">
                        <div class="stat-value">${this.totalPoints}</div>
                        <div class="stat-label">Points</div>
                    </div>
                    <div class="stat">
                        <div class="stat-value">${this.streak}</div>
                        <div class="stat-label">Day Streak</div>
                    </div>
                </div>
                <div class="completion-actions">
                    <button class="btn btn-enroll" onclick="window.location.href='/courses/mycourses'">
                        Continue Learning
                    </button>
                </div>
            </div>
        `;
        document.body.appendChild(modal);

        // Animate modal in
        setTimeout(() => modal.classList.add('show'), 10);
    }

    showLevelUpModal() {
        // Implement level up celebration
        console.log('Level up!');
    }

    showGameOver() {
        const modal = document.createElement('div');
        modal.className = 'completion-modal';
        modal.innerHTML = `
            <div class="completion-content">
                <div class="game-over-icon">
                    <i class="fas fa-heart-broken"></i>
                </div>
                <h2>Out of Hearts!</h2>
                <p>Don't worry, you can try again or review the material.</p>
                <div class="completion-actions">
                    <button class="btn btn-enroll" onclick="location.reload()">
                        Try Again
                    </button>
                    <button class="btn btn-outline-primary" onclick="window.location.href='/courses/mycourses'">
                        Back to Courses
                    </button>
                </div>
            </div>
        `;
        document.body.appendChild(modal);
        setTimeout(() => modal.classList.add('show'), 10);
    }

    showError(exerciseItem, message) {
        this.showFeedback(exerciseItem, false, message, null);
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    if (document.querySelector('.exercise-item')) {
        window.exerciseHandler = new InteractiveExerciseHandler();
    }
});

// Utility function for hint toggle (called from inline onclick)
function toggleHint(button) {
    if (window.exerciseHandler) {
        window.exerciseHandler.showHint({ currentTarget: button });
    }
}