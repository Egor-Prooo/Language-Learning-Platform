// ============================================================
// LingoLearn – Interactive Exercise Handler
// Covers: MultipleChoice, FillInBlank, Translation,
//         Matching, Listening, Speaking
// ============================================================

class InteractiveExerciseHandler {
    constructor() {
        this.exercises = [];
        this.completedExercises = new Set();
        this.totalPoints = 0;
        this.streak = 0;
        this.hearts = 5;
        this.init();
    }

    init() {
        this.loadExercises();
        this.bindEventListeners();
        this.initializeProgressBar();
        this.initializeSoundEffects();
    }

    loadExercises() {
        document.querySelectorAll('.exercise-item').forEach((el, i) => {
            this.exercises.push({
                id: el.dataset.exerciseId,
                element: el,
                type: el.dataset.type,
                points: parseInt(el.dataset.points) || 10,
                index: i
            });
        });
    }

    bindEventListeners() {
        // ── Multiple choice ───────────────────────────────
        document.querySelectorAll('.exercise-option').forEach(opt => {
            opt.addEventListener('click', e => this.handleMultipleChoice(e));
        });

        // ── Text inputs (Fill / Translation / Listening fallback) ──
        document.querySelectorAll('.exercise-input').forEach(inp => {
            inp.addEventListener('input', e => this.handleInputChange(e));
            inp.addEventListener('keypress', e => {
                if (e.key === 'Enter') { e.preventDefault(); this.triggerCheckFromInput(e.target); }
            });
        });

        // ── Check answer buttons ──────────────────────────
        document.querySelectorAll('.btn-check-answer').forEach(btn => {
            btn.addEventListener('click', e => this.handleCheckAnswer(e));
        });

        // ── Hint buttons ──────────────────────────────────
        document.querySelectorAll('.btn-hint').forEach(btn => {
            btn.addEventListener('click', e => this.showHint(e));
        });

        // ── Skip buttons ──────────────────────────────────
        document.querySelectorAll('.btn-skip').forEach(btn => {
            btn.addEventListener('click', e => this.skipExercise(e));
        });
    }

    handleInputChange(event) {
        const input = event.target;
        const exItem = input.closest('.exercise-item');
        const checkBtn = exItem?.querySelector('.btn-check-answer');
        if (checkBtn) checkBtn.disabled = input.value.trim().length === 0;
        input.classList.remove('is-invalid');
    }

    triggerCheckFromInput(input) {
        const checkBtn = input.closest('.exercise-item')?.querySelector('.btn-check-answer');
        if (checkBtn && !checkBtn.disabled) checkBtn.click();
    }

    handleMultipleChoice(event) {
        const option = event.currentTarget;
        const exItem = option.closest('.exercise-item');

        exItem.querySelectorAll('.exercise-option').forEach(o => o.classList.remove('selected', 'pulse-animation'));
        option.classList.add('selected', 'pulse-animation');
        this.playSound('select');

        const checkBtn = exItem.querySelector('.btn-check-answer');
        if (checkBtn) checkBtn.disabled = false;
        setTimeout(() => option.classList.remove('pulse-animation'), 300);
    }

    async handleCheckAnswer(event) {
        const btn = event.currentTarget;
        const exItem = btn.closest('.exercise-item');
        const exId = exItem.dataset.exerciseId;
        const exType = exItem.dataset.type;

        let userAnswer = this.getUserAnswer(exItem, exType);
        if (!userAnswer) return;

        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Checking…';

        try {
            await this.submitAnswer(exId, userAnswer, exItem);
        } catch (err) {
            console.error('Answer submit error:', err);
            this.showError(exItem, 'An error occurred. Please try again.');
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="fas fa-check me-1"></i>Check Answer';
        }
    }

    // ── Get the user's answer depending on exercise type ──
    getUserAnswer(exItem, exType) {
        switch (exType) {
            case 'MultipleChoice': {
                const sel = exItem.querySelector('.exercise-option.selected');
                return sel ? sel.dataset.value : null;
            }
            case 'Listening': {
                // Try the dedicated listening input first
                const liInput = exItem.querySelector('[id^="listeningInput-"]');
                if (liInput && liInput.value.trim()) return liInput.value.trim();
                // Fallback to any exercise-input
                const inp = exItem.querySelector('.exercise-input');
                return inp ? inp.value.trim() : null;
            }
            case 'Speaking': {
                // Hidden answer set by speech recogniser or typed fallback
                const hidden = exItem.querySelector('[id^="speakingAnswer-"]');
                if (hidden && hidden.value.trim()) return hidden.value.trim();
                const fallback = exItem.querySelector('[id^="speakingFallback-"]');
                return fallback ? fallback.value.trim() : null;
            }
            case 'Matching': {
                const hidden = exItem.querySelector('[id^="matching-answer-"]');
                return hidden ? hidden.value.trim() : null;
            }
            default: {
                const inp = exItem.querySelector('.exercise-input');
                return inp ? inp.value.trim() : null;
            }
        }
    }

    async submitAnswer(exId, userAnswer, exItem) {
        const startTime = exItem.dataset.startTime ? parseInt(exItem.dataset.startTime) : Date.now();
        const timeSpent = Math.max(0, Math.floor((Date.now() - startTime) / 1000));

        const payload = {
            exerciseId: exId,
            userAnswer: userAnswer,
            timeSpentSeconds: timeSpent,
            attemptsCount: parseInt(exItem.dataset.attempts || '1')
        };

        const response = await fetch('/api/exercises/submit', {
            method: 'POST',
            credentials: 'include',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const text = await response.text();
            throw new Error(`Server ${response.status}: ${text}`);
        }

        const result = await response.json();
        this.handleAnswerResult(result, exItem);
    }

    handleAnswerResult(result, exItem) {
        if (result.isCorrect) this.handleCorrectAnswer(result, exItem);
        else this.handleIncorrectAnswer(result, exItem);
    }

    handleCorrectAnswer(result, exItem) {
        this.playSound('correct');
        this.showFeedback(exItem, true, result.feedback, result.explanation);
        exItem.classList.add('exercise-success');
        this.createConfetti(exItem);
        this.updatePoints(result.pointsEarned, result.totalPoints);
        this.updateStreak(result.streak);
        this.completedExercises.add(exItem.dataset.exerciseId);
        this.updateProgress();
        setTimeout(() => this.disableExercise(exItem), 2200);
        if (result.levelUp) this.showLevelUpModal();
    }

    handleIncorrectAnswer(result, exItem) {
        this.hearts = Math.max(0, this.hearts - 1);
        this.updateHeartsDisplay();
        this.playSound('incorrect');
        exItem.classList.add('exercise-error');
        setTimeout(() => exItem.classList.remove('exercise-error'), 600);
        this.showFeedback(exItem, false, result.feedback, result.correctAnswer
            ? `Correct answer: <strong>${result.correctAnswer}</strong>` : null);
        exItem.dataset.attempts = (parseInt(exItem.dataset.attempts || '1') + 1).toString();
        if (this.hearts === 0) this.showGameOver();
    }

    showFeedback(exItem, isCorrect, message, extraInfo) {
        let div = exItem.querySelector('.exercise-feedback');
        if (!div) {
            div = document.createElement('div');
            div.className = 'exercise-feedback';
            exItem.querySelector('.exercise-content').appendChild(div);
        }
        const icon = isCorrect ? 'check-circle' : 'times-circle';
        const klass = isCorrect ? 'correct' : 'incorrect';
        div.className = `exercise-feedback ${klass}`;
        div.innerHTML = `
            <div class="feedback-icon"><i class="fas fa-${icon}"></i></div>
            <div class="feedback-content">
                <div class="feedback-message">${message}</div>
                ${extraInfo ? `<div class="feedback-extra">${extraInfo}</div>` : ''}
            </div>`;
        div.style.display = 'flex';
        div.classList.add('slide-down-animation');
    }

    createConfetti(element) {
        const colors = ['#4F46E5', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#F4623A'];
        for (let i = 0; i < 28; i++) {
            const c = document.createElement('div');
            c.className = 'confetti';
            c.style.left = Math.random() * 100 + '%';
            c.style.backgroundColor = colors[Math.floor(Math.random() * colors.length)];
            c.style.animationDelay = Math.random() * 0.35 + 's';
            c.style.animationDuration = (Math.random() * 1 + 1) + 's';
            element.appendChild(c);
            setTimeout(() => c.remove(), 2200);
        }
    }

    updatePoints(earned, total) {
        const el = document.getElementById('user-total-points');
        if (!el) return;
        const start = parseInt(el.textContent) || 0;
        const duration = 900;
        const began = Date.now();
        const tick = () => {
            const t = Math.min((Date.now() - began) / duration, 1);
            el.textContent = Math.floor(start + (total - start) * t);
            el.classList.add('points-earned');
            if (t < 1) requestAnimationFrame(tick);
            else setTimeout(() => el.classList.remove('points-earned'), 400);
        };
        tick();
        this.showFloatingPoints(earned);
    }

    showFloatingPoints(points) {
        if (!points) return;
        const el = document.createElement('div');
        el.className = 'floating-points';
        el.textContent = `+${points}`;
        el.style.cssText = 'position:fixed;top:50%;left:50%;transform:translate(-50%,-50%);';
        document.body.appendChild(el);
        setTimeout(() => el.remove(), 2000);
    }

    updateStreak(streak) {
        const el = document.getElementById('user-streak');
        if (el) {
            el.textContent = streak;
            if (streak > this.streak) {
                el.classList.add('streak-increase');
                setTimeout(() => el.classList.remove('streak-increase'), 500);
            }
        }
        this.streak = streak;
    }

    updateHeartsDisplay() {
        const c = document.getElementById('hearts-container');
        if (!c) return;
        c.innerHTML = '';
        for (let i = 0; i < 5; i++) {
            const h = document.createElement('i');
            h.className = i < this.hearts ? 'fas fa-heart' : 'far fa-heart';
            h.style.color = i < this.hearts ? '#EF4444' : '#D1D5DB';
            h.style.fontSize = '1.4rem';
            h.style.transition = 'all 0.3s ease';
            c.appendChild(h);
        }
    }

    updateProgress() {
        const done = this.completedExercises.size;
        const total = this.exercises.length;
        const pct = total > 0 ? (done / total) * 100 : 0;

        const bar = document.getElementById('progress-bar');
        const text = document.getElementById('completed-count');
        if (bar) bar.style.width = pct + '%';
        if (text) text.textContent = done;

        if (done === total && total > 0) {
            setTimeout(() => this.showCompletionCelebration(), 1000);
        }
    }

    showHint(event) {
        const btn = event.currentTarget;
        const exItem = btn.closest('.exercise-item');
        const hintDiv = exItem.querySelector('.exercise-hint');
        if (!hintDiv) return;
        const showing = hintDiv.style.display === 'flex';
        hintDiv.style.display = showing ? 'none' : 'flex';
        btn.innerHTML = showing
            ? '<i class="fas fa-lightbulb me-1"></i>Hint'
            : '<i class="fas fa-lightbulb me-1"></i>Hide Hint';
    }

    skipExercise(event) {
        const exItem = event.currentTarget.closest('.exercise-item');
        this.disableExercise(exItem);
        this.updateProgress();
    }

    disableExercise(exItem) {
        exItem.querySelectorAll('button, input, .exercise-option').forEach(el => {
            el.disabled = true;
            el.style.pointerEvents = 'none';
        });
        exItem.classList.add('exercise-completed');
    }

    initializeProgressBar() {
        this.updateProgress();
        this.updateHeartsDisplay();
    }

    initializeSoundEffects() {
        this.sounds = {};
        try {
            this.sounds = {
                correct: new Audio('/sounds/correct.mp3'),
                incorrect: new Audio('/sounds/incorrect.mp3'),
                select: new Audio('/sounds/select.mp3')
            };
            Object.values(this.sounds).forEach(s => { s.volume = 0.3; });
        } catch (e) { /* sounds optional */ }
    }

    playSound(type) {
        try {
            const s = this.sounds[type];
            if (s) { s.currentTime = 0; s.play().catch(() => { }); }
        } catch (e) { }
    }

    showCompletionCelebration() {
        const totalPoints = parseInt(document.getElementById('user-total-points')?.textContent || 0);
        const modal = document.createElement('div');
        modal.className = 'completion-modal';
        modal.innerHTML = `
            <div class="completion-content">
                <div class="completion-trophy"><i class="fas fa-trophy"></i></div>
                <h2>Lesson Complete! 🎉</h2>
                <p class="completion-message">Outstanding work! You've finished all exercises.</p>
                <div class="completion-stats">
                    <div class="stat">
                        <div class="stat-value">${this.completedExercises.size}</div>
                        <div class="stat-label">Exercises</div>
                    </div>
                    <div class="stat">
                        <div class="stat-value">${totalPoints}</div>
                        <div class="stat-label">Points</div>
                    </div>
                    <div class="stat">
                        <div class="stat-value">${this.streak}</div>
                        <div class="stat-label">Day Streak</div>
                    </div>
                </div>
                <div class="completion-actions">
                    <button class="btn btn-enroll" onclick="window.location.href='/courses/mycourses'">
                        <i class="fas fa-arrow-right me-2"></i>Continue Learning
                    </button>
                </div>
            </div>`;
        document.body.appendChild(modal);
        setTimeout(() => modal.classList.add('show'), 10);
    }

    showGameOver() {
        const modal = document.createElement('div');
        modal.className = 'completion-modal';
        modal.innerHTML = `
            <div class="completion-content">
                <div class="game-over-icon"><i class="fas fa-heart-broken"></i></div>
                <h2>Out of Hearts!</h2>
                <p>Don't worry — review the material and try again.</p>
                <div class="completion-actions">
                    <button class="btn btn-enroll" onclick="location.reload()">
                        <i class="fas fa-redo me-2"></i>Try Again
                    </button>
                    <button class="btn btn-outline-primary" onclick="window.location.href='/courses/mycourses'">
                        Back to Courses
                    </button>
                </div>
            </div>`;
        document.body.appendChild(modal);
        setTimeout(() => modal.classList.add('show'), 10);
    }

    showLevelUpModal() {
        const modal = document.createElement('div');
        modal.className = 'completion-modal';
        modal.innerHTML = `
            <div class="completion-content">
                <div style="font-size:4.5rem;margin-bottom:1rem;">⬆️</div>
                <h2>Level Up!</h2>
                <p class="completion-message">You've reached a new level. Keep it up!</p>
                <div class="completion-actions">
                    <button class="btn btn-enroll" onclick="this.closest('.completion-modal').remove()">
                        Continue
                    </button>
                </div>
            </div>`;
        document.body.appendChild(modal);
        setTimeout(() => modal.classList.add('show'), 10);
        setTimeout(() => { modal.classList.remove('show'); setTimeout(() => modal.remove(), 300); }, 3500);
    }

    showError(exItem, message) {
        this.showFeedback(exItem, false, message, null);
    }
}

// ── Init ──────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    if (document.querySelector('.exercise-item')) {
        window.exerciseHandler = new InteractiveExerciseHandler();
    }
});

// Utility: hint toggle called from inline onclick in the view
function toggleHint(button) {
    if (window.exerciseHandler) {
        window.exerciseHandler.showHint({ currentTarget: button });
    }
}