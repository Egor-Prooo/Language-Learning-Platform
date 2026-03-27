// ============================================================
// LingoLearn – Interactive Exercise Handler
// ============================================================

class InteractiveExerciseHandler {
    constructor() {
        this.exercises = [];
        this.completedExercises = new Set();   // exerciseIds answered correctly
        this.skippedExercises = new Set();     // exerciseIds skipped (not counted as done)
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
        const leftItems  = exItem.querySelectorAll(`#left-column-${exId}  .matching-item`);
        const rightItems = exItem.querySelectorAll(`#right-column-${exId} .matching-item`);
        const hiddenInput = exItem.querySelector(`#matching-answer-${exId}`);
        const checkBtn    = exItem.querySelector('.btn-check-answer');

        // Map: leftEl -> rightEl (user's current pairing)
        let userPairs = new Map();
        // Reverse map for quick lookup: rightEl -> leftEl
        let reverseMap = new Map();

        let selectedLeft  = null;
        let selectedRight = null;

        const totalPairs = leftItems.length;

        function updateCheckButton() {
            if (checkBtn) checkBtn.disabled = userPairs.size !== totalPairs;
        }

        function clearLeftSelection() {
            if (selectedLeft) {
                selectedLeft.classList.remove('selected');
                selectedLeft = null;
            }
        }

        function clearRightSelection() {
            if (selectedRight) {
                selectedRight.classList.remove('selected');
                selectedRight = null;
            }
        }

        // Draw or redraw the SVG connector lines
        function drawConnectors() {
            // Remove old SVG overlay if any
            const old = exItem.querySelector('.matching-connectors-svg');
            if (old) old.remove();

            if (userPairs.size === 0) return;

            const container = exItem.querySelector('.matching-exercise');
            if (!container) return;

            const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
            svg.classList.add('matching-connectors-svg');
            svg.style.cssText = `
                position: absolute;
                top: 0; left: 0;
                width: 100%; height: 100%;
                pointer-events: none;
                overflow: visible;
                z-index: 5;
            `;

            // Make container relative so SVG positions correctly
            if (getComputedStyle(container).position === 'static') {
                container.style.position = 'relative';
            }

            container.appendChild(svg);

            const containerRect = container.getBoundingClientRect();

            userPairs.forEach((rightEl, leftEl) => {
                const lr = leftEl.getBoundingClientRect();
                const rr = rightEl.getBoundingClientRect();

                const x1 = lr.right  - containerRect.left;
                const y1 = lr.top    - containerRect.top  + lr.height / 2;
                const x2 = rr.left   - containerRect.left;
                const y2 = rr.top    - containerRect.top  + rr.height / 2;

                const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
                line.setAttribute('x1', x1);
                line.setAttribute('y1', y1);
                line.setAttribute('x2', x2);
                line.setAttribute('y2', y2);
                line.setAttribute('stroke', '#4F46E5');
                line.setAttribute('stroke-width', '2.5');
                line.setAttribute('stroke-dasharray', '6 3');
                line.setAttribute('stroke-linecap', 'round');
                svg.appendChild(line);
            });
        }

        function recordPair(leftEl, rightEl) {
            // If left was already paired, free the old right
            if (userPairs.has(leftEl)) {
                const oldRight = userPairs.get(leftEl);
                oldRight.classList.remove('paired');
                reverseMap.delete(oldRight);
            }
            // If right was already paired to a different left, free that left
            if (reverseMap.has(rightEl)) {
                const oldLeft = reverseMap.get(rightEl);
                oldLeft.classList.remove('paired');
                userPairs.delete(oldLeft);
            }

            userPairs.set(leftEl, rightEl);
            reverseMap.set(rightEl, leftEl);

            leftEl.classList.add('paired');
            rightEl.classList.add('paired');
        }

        function tryPair() {
            if (!selectedLeft || !selectedRight) return;

            recordPair(selectedLeft, selectedRight);
            drawConnectors();
            updateCheckButton();

            clearLeftSelection();
            clearRightSelection();
        }

        leftItems.forEach(item => {
            item.addEventListener('click', () => {
                if (exItem.classList.contains('exercise-completed')) return;

                if (selectedLeft === item) {
                    // Deselect
                    clearLeftSelection();
                    return;
                }

                clearLeftSelection();
                item.classList.add('selected');
                selectedLeft = item;
                tryPair();
            });
        });

        rightItems.forEach(item => {
            item.addEventListener('click', () => {
                if (exItem.classList.contains('exercise-completed')) return;

                if (selectedRight === item) {
                    // Deselect
                    clearRightSelection();
                    return;
                }

                clearRightSelection();
                item.classList.add('selected');
                selectedRight = item;
                tryPair();
            });
        });

        // Store the userPairs map reference on the element so we can read it at submit time
        exItem._matchingPairs = userPairs;
        exItem._matchingLeft  = leftItems;
        exItem._matchingRight = rightItems;

        // Store drawConnectors so we can call it after reveal
        exItem._drawMatchingConnectors = drawConnectors;

        updateCheckButton();
    }

    // Build the answer string from the user's pairs map at submission time
    // and reveal correct/incorrect feedback on each item
    getMatchingAnswer(exItem) {
        const userPairs = exItem._matchingPairs;
        if (!userPairs || userPairs.size === 0) return null;

        // We need to determine correctness: each left item has a data-pair-id
        // that must match the right item's data-pair-id
        let allCorrect = true;
        const results = [];

        userPairs.forEach((rightEl, leftEl) => {
            const leftPairId  = leftEl.dataset.pairId;
            const rightPairId = rightEl.dataset.pairId;
            const correct = leftPairId === rightPairId;
            results.push({ leftEl, rightEl, correct });
            if (!correct) allCorrect = false;
        });

        // Reveal feedback colours on the items
        results.forEach(({ leftEl, rightEl, correct }) => {
            const cls = correct ? 'matched' : 'match-error-final';
            leftEl.classList.add(cls);
            rightEl.classList.add(cls);
            leftEl.classList.remove('paired', 'selected');
            rightEl.classList.remove('paired', 'selected');
        });

        // Redraw connectors in green/red
        const container = exItem.querySelector('.matching-exercise');
        if (container) {
            const old = exItem.querySelector('.matching-connectors-svg');
            if (old) old.remove();

            const containerRect = container.getBoundingClientRect();
            const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
            svg.classList.add('matching-connectors-svg');
            svg.style.cssText = `position:absolute;top:0;left:0;width:100%;height:100%;pointer-events:none;overflow:visible;z-index:5;`;
            if (getComputedStyle(container).position === 'static') container.style.position = 'relative';
            container.appendChild(svg);

            results.forEach(({ leftEl, rightEl, correct }) => {
                const lr = leftEl.getBoundingClientRect();
                const rr = rightEl.getBoundingClientRect();
                const x1 = lr.right - containerRect.left;
                const y1 = lr.top   - containerRect.top + lr.height / 2;
                const x2 = rr.left  - containerRect.left;
                const y2 = rr.top   - containerRect.top + rr.height / 2;

                const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
                line.setAttribute('x1', x1); line.setAttribute('y1', y1);
                line.setAttribute('x2', x2); line.setAttribute('y2', y2);
                line.setAttribute('stroke', correct ? '#10B981' : '#EF4444');
                line.setAttribute('stroke-width', '2.5');
                line.setAttribute('stroke-linecap', 'round');
                svg.appendChild(line);
            });
        }

        // Return 'matched' if all correct so the server accepts it,
        // otherwise a special marker so the server knows it's wrong
        return allCorrect ? 'matched' : 'unmatched';
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
        const exId   = exItem.dataset.exerciseId;
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
                // Build visual feedback AND get the answer string
                return this.getMatchingAnswer(exItem);
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
        this.skippedExercises.delete(exItem.dataset.exerciseId);
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

        const extra = result.correctAnswer
            ? `Correct answer: <strong>${result.correctAnswer}</strong>`
            : null;
        this.showFeedback(exItem, false, result.feedback, extra);

        // For matching: re-enable the items so the user can try again
        if (exItem.dataset.type === 'Matching') {
            this.resetMatchingForRetry(exItem);
        }

        exItem.dataset.attempts = (parseInt(exItem.dataset.attempts || '1') + 1).toString();
        if (this.hearts === 0) this.showGameOver();
    }

    // Reset matching items to allow another attempt after a wrong answer
    resetMatchingForRetry(exItem) {
        const exId = exItem.dataset.exerciseId;

        // Clear visual state
        exItem.querySelectorAll('.matching-item').forEach(el => {
            el.classList.remove('matched', 'match-error-final', 'paired', 'selected');
            el.style.pointerEvents = '';
        });

        // Remove connector lines
        const old = exItem.querySelector('.matching-connectors-svg');
        if (old) old.remove();

        // Reset the pairs maps
        if (exItem._matchingPairs) exItem._matchingPairs.clear();

        // Re-disable the check button until all pairs are made again
        const checkBtn = exItem.querySelector('.btn-check-answer');
        if (checkBtn) checkBtn.disabled = true;
    }

    showFeedback(exItem, isCorrect, message, extraInfo) {
        let div = exItem.querySelector('.exercise-feedback');
        if (!div) {
            div = document.createElement('div');
            div.className = 'exercise-feedback';
            exItem.querySelector('.exercise-content').appendChild(div);
        }
        const icon  = isCorrect ? 'check-circle' : 'times-circle';
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
        const start    = parseInt(el.textContent) || 0;
        const duration = 900;
        const began    = Date.now();
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
            h.className  = i < this.hearts ? 'fas fa-heart' : 'far fa-heart';
            h.style.color = i < this.hearts ? '#EF4444' : '#D1D5DB';
            h.style.fontSize = '1.4rem';
            c.appendChild(h);
        }
    }

    updateProgress() {
        const done  = this.completedExercises.size;
        const total = this.exercises.length;
        const pct   = total > 0 ? (done / total) * 100 : 0;

        const bar  = document.getElementById('progress-bar');
        const text = document.getElementById('completed-count');
        if (bar)  bar.style.width = pct + '%';
        if (text) text.textContent = done;
    }

    skipExercise(event) {
        const btn    = event.currentTarget;
        const exItem = btn.closest('.exercise-item');
        const exId   = exItem.dataset.exerciseId;

        if (this.completedExercises.has(exId)) return;

        this.skippedExercises.add(exId);

        let indicator = exItem.querySelector('.skipped-indicator');
        if (!indicator) {
            indicator = document.createElement('div');
            indicator.className = 'skipped-indicator';
            indicator.innerHTML = '<i class="fas fa-forward me-1 text-secondary"></i><span class="text-secondary small">Skipped – you can still answer this</span>';
            indicator.style.cssText = 'padding:0.5rem 0;margin-top:0.5rem;';
            exItem.querySelector('.exercise-actions').after(indicator);
        }

        this.updateFinishButton();
    }

    renderFinishButton() {
        const container = document.getElementById('finish-lesson-container');
        if (!container) return;
        this.updateFinishButton();
    }

    updateFinishButton() {
        const container = document.getElementById('finish-lesson-container');
        if (!container) return;

        const total      = this.exercises.length;
        const done       = this.completedExercises.size;
        const hasSkipped = this.skippedExercises.size > 0;
        const canFinish  = done === total && total > 0;
        const allAttempted = (done + this.skippedExercises.size) === total;

        container.innerHTML = '';

        if (canFinish) {
            container.innerHTML = `
                <div class="finish-lesson-panel success">
                    <i class="fas fa-check-circle fa-2x text-success mb-2"></i>
                    <h4 class="fw-bold mb-1">All exercises completed!</h4>
                    <p class="text-secondary mb-3">Great work! You can now finish this lesson.</p>
                    <button class="btn btn-success btn-lg px-5" onclick="finishLesson()">
                        <i class="fas fa-flag-checkered me-2"></i>Finish Lesson
                    </button>
                </div>`;
        } else if (hasSkipped) {
            container.innerHTML = `
                <div class="finish-lesson-panel warning">
                    <i class="fas fa-exclamation-triangle fa-2x text-warning mb-2"></i>
                    <h4 class="fw-bold mb-1">Skipped exercises detected</h4>
                    <p class="text-secondary mb-1">You have <strong>${this.skippedExercises.size}</strong> skipped exercise(s). Answer all exercises correctly to complete the lesson.</p>
                    <p class="text-secondary mb-0 small">Scroll up to complete the remaining exercises.</p>
                </div>`;
        } else if (allAttempted && done < total) {
            container.innerHTML = `
                <div class="finish-lesson-panel info">
                    <i class="fas fa-info-circle fa-2x text-primary mb-2"></i>
                    <p class="text-secondary mb-0">Answer all <strong>${total - done}</strong> remaining exercise(s) correctly to finish the lesson.</p>
                </div>`;
        }
    }

    showHint(event) {
        const btn     = event.currentTarget;
        const exItem  = btn.closest('.exercise-item');
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
                correct:   new Audio('/sounds/correct.mp3'),
                incorrect: new Audio('/sounds/incorrect.mp3'),
                select:    new Audio('/sounds/select.mp3')
            };
            Object.values(this.sounds).forEach(s => { s.volume = 0.3; });
        } catch (e) { }
    }

    playSound(type) {
        try {
            const s = this.sounds[type];
            if (s) { s.currentTime = 0; s.play().catch(() => {}); }
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
    const lessonId = document.getElementById('finish-lesson-container')?.dataset.lessonId;
    if (!lessonId) return;

    const btn = document.querySelector('#finish-lesson-container .btn-success');
    if (btn) { btn.disabled = true; btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Finishing…'; }

    try {
        const resp = await fetch(`/api/lessons/${lessonId}/complete`, {
            method: 'POST',
            credentials: 'include',
            headers: { 'Content-Type': 'application/json' }
        });
        const data = await resp.json();

        if (data.success) {
            const modal = document.createElement('div');
            modal.className = 'completion-modal';
            modal.innerHTML = `
                <div class="completion-content">
                    <div class="completion-trophy"><i class="fas fa-trophy"></i></div>
                    <h2>Lesson Complete! 🎉</h2>
                    <p class="completion-message">You've successfully finished this lesson.</p>
                    <div class="completion-stats">
                        <div class="stat"><div class="stat-value">${window.exerciseHandler?.completedExercises?.size || 0}</div><div class="stat-label">Exercises</div></div>
                        <div class="stat"><div class="stat-value">${document.getElementById('user-total-points')?.textContent || 0}</div><div class="stat-label">Points</div></div>
                    </div>
                    <div class="completion-actions">
                        <a href="${data.nextLessonUrl || '/courses/mycourses'}" class="btn btn-enroll">
                            <i class="fas fa-arrow-right me-2"></i>${data.nextLessonUrl ? 'Next Lesson' : 'My Courses'}
                        </a>
                    </div>
                </div>`;
            document.body.appendChild(modal);
            setTimeout(() => modal.classList.add('show'), 10);
        } else {
            alert(data.message || 'Could not finish lesson. Make sure all exercises are answered correctly.');
            if (btn) { btn.disabled = false; btn.innerHTML = '<i class="fas fa-flag-checkered me-2"></i>Finish Lesson'; }
        }
    } catch (err) {
        console.error(err);
        if (btn) { btn.disabled = false; btn.innerHTML = '<i class="fas fa-flag-checkered me-2"></i>Finish Lesson'; }
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