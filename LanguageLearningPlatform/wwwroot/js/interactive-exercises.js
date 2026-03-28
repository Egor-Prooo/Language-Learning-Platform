// ============================================================
// LingoLearn – Interactive Exercise Handler
// ============================================================

class InteractiveExerciseHandler {
    constructor() {
        this.exercises = [];
        this.completedExercises = new Set();   // exerciseIds answered correctly
        this.skippedExercises = new Map();     // exerciseId -> { element, title } skipped exercises
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
        this.initMatchingExercises();
        this.renderFinishButton();
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
        document.querySelectorAll('.exercise-option').forEach(opt => {
            opt.addEventListener('click', e => this.handleMultipleChoice(e));
        });

        document.querySelectorAll('.exercise-input').forEach(inp => {
            inp.addEventListener('input', e => this.handleInputChange(e));
            inp.addEventListener('keypress', e => {
                if (e.key === 'Enter') { e.preventDefault(); this.triggerCheckFromInput(e.target); }
            });
        });

        document.querySelectorAll('.btn-check-answer').forEach(btn => {
            btn.addEventListener('click', e => this.handleCheckAnswer(e));
        });

        document.querySelectorAll('.btn-hint').forEach(btn => {
            btn.addEventListener('click', e => this.showHint(e));
        });

        document.querySelectorAll('.btn-skip').forEach(btn => {
            btn.addEventListener('click', e => this.skipExercise(e));
        });
    }

    // ── Matching Exercise ─────────────────────────────────────
    initMatchingExercises() {
        document.querySelectorAll('.exercise-item[data-type="Matching"]').forEach(exItem => {
            const exId = exItem.dataset.exerciseId;
            this.setupMatching(exItem, exId);
        });
    }

    setupMatching(exItem, exId) {
        const leftItems = exItem.querySelectorAll(`#left-column-${exId} .matching-item`);
        const rightItems = exItem.querySelectorAll(`#right-column-${exId} .matching-item`);
        const hiddenInput = exItem.querySelector(`#matching-answer-${exId}`);
        const checkBtn = exItem.querySelector('.btn-check-answer');

        let selectedLeft = null;
        let selectedRight = null;
        let matchedPairIds = new Set();

        function tryMatch() {
            if (!selectedLeft || !selectedRight) return;

            const leftPairId = selectedLeft.dataset.pairId;
            const rightPairId = selectedRight.dataset.pairId;

            if (leftPairId === rightPairId) {
                selectedLeft.classList.add('matched');
                selectedRight.classList.add('matched');
                selectedLeft.style.pointerEvents = 'none';
                selectedRight.style.pointerEvents = 'none';
                matchedPairIds.add(leftPairId);
            } else {
                [selectedLeft, selectedRight].forEach(el => {
                    el.classList.add('match-error');
                    setTimeout(() => el.classList.remove('match-error'), 600);
                });
            }

            selectedLeft.classList.remove('selected');
            selectedRight.classList.remove('selected');
            selectedLeft = null;
            selectedRight = null;

            const totalPairs = exItem.querySelectorAll(`#left-column-${exId} .matching-item`).length;

            if (matchedPairIds.size === totalPairs) {
                hiddenInput.value = 'matched';
                if (checkBtn) checkBtn.disabled = false;
            }
        }

        leftItems.forEach(item => {
            item.addEventListener('click', () => {
                if (item.classList.contains('matched')) return;
                leftItems.forEach(i => i.classList.remove('selected'));
                item.classList.add('selected');
                selectedLeft = item;
                tryMatch();
            });
        });

        rightItems.forEach(item => {
            item.addEventListener('click', () => {
                if (item.classList.contains('matched')) return;
                rightItems.forEach(i => i.classList.remove('selected'));
                item.classList.add('selected');
                selectedRight = item;
                tryMatch();
            });
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
        if (exItem.classList.contains('exercise-completed')) return;

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
            if (!exItem.classList.contains('exercise-completed')) {
                btn.disabled = false;
                btn.innerHTML = '<i class="fas fa-check me-1"></i>Check Answer';
            }
        }
    }

    getUserAnswer(exItem, exType) {
        switch (exType) {
            case 'MultipleChoice': {
                const sel = exItem.querySelector('.exercise-option.selected');
                return sel ? sel.dataset.value : null;
            }
            case 'Listening': {
                const liInput = exItem.querySelector('[id^="listeningInput-"]');
                if (liInput && liInput.value.trim()) return liInput.value.trim();
                const inp = exItem.querySelector('.exercise-input');
                return inp ? inp.value.trim() : null;
            }
            case 'Speaking': {
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
        return result;
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

        const exId = exItem.dataset.exerciseId;
        this.completedExercises.add(exId);
        // If it was previously skipped and now answered correctly, remove from skipped
        if (this.skippedExercises.has(exId)) {
            this.skippedExercises.delete(exId);
            const indicator = exItem.querySelector('.skipped-indicator');
            if (indicator) indicator.remove();
        }

        this.updateProgress();
        this.disableExercise(exItem);
        this.updateFinishButton();
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
        const colors = ['#4F46E5', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6'];
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
        el.style.cssText = 'position:fixed;top:50%;left:50%;transform:translate(-50%,-50%);z-index:9999;';
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
    }

    // ── Skip: marks as skipped, counts toward the finish unlock ──
    skipExercise(event) {
        const btn = event.currentTarget;
        const exItem = btn.closest('.exercise-item');
        const exId = exItem.dataset.exerciseId;

        if (this.completedExercises.has(exId)) return; // already answered correctly

        // Collect a readable title for the review modal later
        const titleEl = exItem.querySelector('.exercise-title');
        const title = titleEl ? titleEl.textContent.trim() : `Exercise ${exItem.dataset.exerciseId}`;

        this.skippedExercises.set(exId, { element: exItem, title });

        // Visual: dim the exercise and show a "skipped" banner
        let indicator = exItem.querySelector('.skipped-indicator');
        if (!indicator) {
            indicator = document.createElement('div');
            indicator.className = 'skipped-indicator';
            indicator.style.cssText = [
                'display:flex;align-items:center;gap:.5rem;',
                'padding:.6rem 1rem;margin-top:1rem;',
                'background:#FEF3C7;border:1.5px solid #FCD34D;border-radius:12px;',
                'font-size:.85rem;font-weight:600;color:#92400E;'
            ].join('');
            indicator.innerHTML = `
                <i class="fas fa-forward-step" style="color:#D97706;"></i>
                Skipped — correct answer will be shown when you finish.
                <button type="button" class="btn-undo-skip ms-auto"
                    style="background:none;border:none;color:#92400E;font-weight:700;cursor:pointer;font-size:.8rem;padding:0;"
                    title="Undo skip">
                    <i class="fas fa-undo me-1"></i>Try again
                </button>`;
            exItem.querySelector('.exercise-actions').insertAdjacentElement('afterend', indicator);

            // Undo skip handler
            indicator.querySelector('.btn-undo-skip').addEventListener('click', () => {
                this.skippedExercises.delete(exId);
                indicator.remove();
                this.updateProgress();
                this.updateFinishButton();
            });
        }

        this.updateProgress();
        this.updateFinishButton();
    }

    // ── Finish Lesson Button ──────────────────────────────────
    renderFinishButton() {
        const container = document.getElementById('finish-lesson-container');
        if (!container) return;
        this.updateFinishButton();
    }

    updateFinishButton() {
        const container = document.getElementById('finish-lesson-container');
        if (!container) return;

        const total = this.exercises.length;
        const done = this.completedExercises.size;
        const skipped = this.skippedExercises.size;
        const remaining = total - done - skipped;
        const allAttempted = remaining <= 0 && total > 0;

        container.innerHTML = '';

        if (allAttempted) {
            // All exercises have been answered or skipped — show the finish button
            const skippedNote = skipped > 0
                ? `<p class="mb-3" style="font-size:.9rem;color:#6B7280;">
                       <i class="fas fa-info-circle me-1 text-primary"></i>
                       You skipped <strong>${skipped}</strong> exercise${skipped > 1 ? 's' : ''}.
                       The correct answers will be shown after you finish.
                   </p>`
                : '';

            container.innerHTML = `
                <div class="finish-lesson-panel" style="
                    background:white;border-radius:20px;
                    box-shadow:0 4px 20px rgba(0,0,0,.08);
                    border:2px solid ${skipped > 0 ? '#FCD34D' : '#10B981'};
                    padding:2rem;text-align:center;margin-top:2rem;">
                    <i class="fas fa-${skipped > 0 ? 'flag-checkered' : 'check-circle'} fa-2x mb-3"
                       style="color:${skipped > 0 ? '#D97706' : '#10B981'};display:block;"></i>
                    <h4 class="fw-bold mb-1">
                        ${skipped > 0 ? 'Ready to hand in?' : 'All exercises complete!'}
                    </h4>
                    ${skippedNote}
                    <button id="btn-finish-lesson" class="btn btn-lg px-5 fw-bold"
                        style="background:${skipped > 0 ? 'linear-gradient(135deg,#F59E0B,#D97706)' : 'linear-gradient(135deg,#10B981,#059669)'};
                               color:white;border:none;border-radius:14px;
                               box-shadow:0 4px 16px rgba(0,0,0,.15);">
                        <i class="fas fa-flag-checkered me-2"></i>Hand In Assignment
                    </button>
                </div>`;

            document.getElementById('btn-finish-lesson')
                .addEventListener('click', () => finishLesson());

        } else if (done > 0 || skipped > 0) {
            // Partially through — show a status hint
            container.innerHTML = `
                <div style="text-align:center;padding:1.5rem;color:#6B7280;font-size:.9rem;">
                    <i class="fas fa-hourglass-half me-2 text-primary"></i>
                    <strong>${remaining}</strong> exercise${remaining > 1 ? 's' : ''} remaining before you can hand in.
                </div>`;
        }
    }

    // ── Submit a skipped exercise with a blank answer to register it as attempted ──
    async submitSkippedAnswer(exId, info) {
        const exItem = info.element;
        const startTime = exItem.dataset.startTime ? parseInt(exItem.dataset.startTime) : Date.now();

        const payload = {
            exerciseId: exId,
            userAnswer: '',           // blank → will be marked incorrect by the server
            timeSpentSeconds: 0,
            attemptsCount: parseInt(exItem.dataset.attempts || '1')
        };

        try {
            const response = await fetch('/api/exercises/submit', {
                method: 'POST',
                credentials: 'include',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (!response.ok) return null;
            const result = await response.json();
            return {
                title: info.title,
                correctAnswer: result.correctAnswer || '—',
                explanation: result.explanation || null
            };
        } catch (e) {
            console.error('Error submitting skipped exercise', exId, e);
            return null;
        }
    }

    // ── Modal showing correct answers for all skipped exercises ──
    showReviewModal(reviews, courseUrl) {
        const rows = reviews.map(r => `
            <div style="
                background:#F9FAFB;border:1.5px solid #E5E7EB;border-radius:12px;
                padding:1rem 1.25rem;margin-bottom:.75rem;text-align:left;">
                <div style="font-weight:700;font-size:.9rem;color:#1F2937;margin-bottom:.4rem;">
                    <i class="fas fa-forward-step me-2" style="color:#D97706;"></i>${r.title}
                </div>
                <div style="font-size:.85rem;color:#374151;">
                    <span style="font-weight:600;color:#059669;">Correct answer:</span>
                    <span style="margin-left:.4rem;font-weight:700;">${r.correctAnswer}</span>
                </div>
                ${r.explanation ? `<div style="font-size:.78rem;color:#6B7280;margin-top:.3rem;"><i class="fas fa-lightbulb me-1 text-warning"></i>${r.explanation}</div>` : ''}
            </div>`).join('');

        const modal = document.createElement('div');
        modal.className = 'completion-modal';
        modal.style.cssText = 'overflow-y:auto;';
        modal.innerHTML = `
            <div class="completion-content" style="max-width:560px;width:90%;max-height:85vh;overflow-y:auto;padding:2.5rem;">
                <div style="font-size:3rem;margin-bottom:.75rem;">📋</div>
                <h2 style="font-size:1.6rem;margin-bottom:.4rem;">Assignment Handed In!</h2>
                <p style="color:#6B7280;margin-bottom:1.5rem;font-size:.95rem;">
                    Great effort! Here are the correct answers for the exercises you skipped.
                </p>
                <div style="margin-bottom:1.75rem;">${rows}</div>
                <div class="completion-actions" style="flex-direction:column;gap:.75rem;">
                    <a href="${courseUrl}" class="btn btn-enroll btn-lg"
                       style="border-radius:14px;font-weight:700;">
                        <i class="fas fa-arrow-left me-2"></i>Back to Course
                    </a>
                </div>
            </div>`;
        document.body.appendChild(modal);
        setTimeout(() => modal.classList.add('show'), 10);
    }

    // ── Pure-completion modal (no skips) ─────────────────────
    showCompletionModal(courseUrl) {
        const modal = document.createElement('div');
        modal.className = 'completion-modal';
        modal.innerHTML = `
            <div class="completion-content">
                <div class="completion-trophy"><i class="fas fa-trophy"></i></div>
                <h2>Lesson Complete! 🎉</h2>
                <p class="completion-message">You answered every exercise correctly. Outstanding!</p>
                <div class="completion-stats">
                    <div class="stat">
                        <div class="stat-value">${this.completedExercises.size}</div>
                        <div class="stat-label">Exercises</div>
                    </div>
                    <div class="stat">
                        <div class="stat-value">${document.getElementById('user-total-points')?.textContent || 0}</div>
                        <div class="stat-label">Total Points</div>
                    </div>
                </div>
                <div class="completion-actions">
                    <a href="${courseUrl}" class="btn btn-enroll">
                        <i class="fas fa-arrow-left me-2"></i>Back to Course
                    </a>
                </div>
            </div>`;
        document.body.appendChild(modal);
        setTimeout(() => modal.classList.add('show'), 10);
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

    disableExercise(exItem) {
        exItem.querySelectorAll('button, input, .exercise-option, .matching-item').forEach(el => {
            el.disabled = true;
            el.style.pointerEvents = 'none';
        });
        exItem.classList.add('exercise-completed');
    }

    initializeProgressBar() {
        this.updateProgress();
        this.updateHeartsDisplay();
        this.updateFinishButton();
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
        } catch (e) { }
    }

    playSound(type) {
        try {
            const s = this.sounds[type];
            if (s) { s.currentTime = 0; s.play().catch(() => { }); }
        } catch (e) { }
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
                    <button class="btn btn-enroll" onclick="this.closest('.completion-modal').remove()">Continue</button>
                </div>
            </div>`;
        document.body.appendChild(modal);
        setTimeout(() => modal.classList.add('show'), 10);
        setTimeout(() => { modal.classList.remove('show'); setTimeout(() => modal.remove(), 300); }, 3500);
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
                    <button class="btn btn-enroll" onclick="location.reload()"><i class="fas fa-redo me-2"></i>Try Again</button>
                    <button class="btn btn-outline-primary" onclick="window.location.href='/courses/mycourses'">Back to Courses</button>
                </div>
            </div>`;
        document.body.appendChild(modal);
        setTimeout(() => modal.classList.add('show'), 10);
    }

    showError(exItem, message) {
        this.showFeedback(exItem, false, message, null);
    }
}

// ── Finish lesson global function ─────────────────────────────
async function finishLesson() {
    const container = document.getElementById('finish-lesson-container');
    const lessonId = container?.dataset.lessonId;
    const courseId = container?.dataset.courseId;
    const courseUrl = courseId
        ? `/Courses/Details/${courseId}`
        : '/Courses/MyCourses';

    const btn = document.getElementById('btn-finish-lesson');
    if (btn) {
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Submitting…';
    }

    const handler = window.exerciseHandler;
    if (!handler) return;

    // 1. Submit every skipped exercise with a blank answer so the server registers
    //    all exercises as attempted (required by TryCompleteLessonAsync).
    const reviews = [];
    for (const [exId, info] of handler.skippedExercises) {
        const review = await handler.submitSkippedAnswer(exId, info);
        if (review) reviews.push(review);
    }

    // 2. Show the appropriate completion UI.
    if (reviews.length > 0) {
        // There were skipped exercises — show the review modal with correct answers.
        handler.showReviewModal(reviews, courseUrl);
    } else {
        // All exercises were answered correctly — show the standard completion modal.
        handler.showCompletionModal(courseUrl);
    }
}

// ── Init ──────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    if (document.querySelector('.exercise-item')) {
        window.exerciseHandler = new InteractiveExerciseHandler();
    }
});

function toggleHint(button) {
    if (window.exerciseHandler) {
        window.exerciseHandler.showHint({ currentTarget: button });
    }
}