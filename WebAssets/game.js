/* =================================================================
   TEACHSMART: PHONETIC CHALLENGE - CORE GAME ENGINE
   ================================================================= */

// STATE MANAGEMENT
let currentScore = 0;
let activeScreen = "main-menu";
let activeLevel = 0;

// Speech synthesis and audio setup
let synthVoices = [];
const audioCtx = new (window.AudioContext || window.webkitAudioContext)();

// Load voices once they are ready
function loadVoices() {
    synthVoices = window.speechSynthesis.getVoices();
    const voiceSelect = document.getElementById("diag-voice-select");
    if (voiceSelect) {
        voiceSelect.innerHTML = "";
        const enVoices = synthVoices.filter(v => v.lang.startsWith("en"));
        enVoices.forEach(voice => {
            const opt = document.createElement("option");
            opt.value = voice.name;
            opt.innerText = `${voice.name} (${voice.lang})`;
            voiceSelect.appendChild(opt);
        });
    }
}
window.speechSynthesis.onvoiceschanged = loadVoices;
loadVoices();

// Audio Synthesizer (Oscillators for zaps, slices, successes, failures)
function playSynthSound(type) {
    if (audioCtx.state === 'suspended') {
        audioCtx.resume();
    }
    const osc = audioCtx.createOscillator();
    const gain = audioCtx.createGain();
    osc.connect(gain);
    gain.connect(audioCtx.destination);
    const now = audioCtx.currentTime;

    switch (type) {
        case 'zap':
            osc.type = 'sawtooth';
            osc.frequency.setValueAtTime(900, now);
            osc.frequency.exponentialRampToValueAtTime(150, now + 0.12);
            gain.gain.setValueAtTime(0.12, now);
            gain.gain.linearRampToValueAtTime(0, now + 0.12);
            osc.start(now);
            osc.stop(now + 0.12);
            break;
        case 'slice':
            osc.type = 'triangle';
            osc.frequency.setValueAtTime(1400, now);
            osc.frequency.linearRampToValueAtTime(400, now + 0.08);
            gain.gain.setValueAtTime(0.08, now);
            gain.gain.linearRampToValueAtTime(0, now + 0.08);
            osc.start(now);
            osc.stop(now + 0.08);
            break;
        case 'success':
            // Cute arpeggio
            const notes = [261.63, 329.63, 392.00, 523.25]; // C4, E4, G4, C5
            notes.forEach((freq, idx) => {
                const o = audioCtx.createOscillator();
                const g = audioCtx.createGain();
                o.connect(g);
                g.connect(audioCtx.destination);
                o.frequency.setValueAtTime(freq, now + idx * 0.07);
                g.gain.setValueAtTime(0.08, now + idx * 0.07);
                g.gain.linearRampToValueAtTime(0, now + idx * 0.07 + 0.18);
                o.start(now + idx * 0.07);
                o.stop(now + idx * 0.07 + 0.18);
            });
            break;
        case 'failure':
            osc.type = 'sawtooth';
            osc.frequency.setValueAtTime(160, now);
            osc.frequency.linearRampToValueAtTime(60, now + 0.35);
            gain.gain.setValueAtTime(0.18, now);
            gain.gain.linearRampToValueAtTime(0, now + 0.35);
            osc.start(now);
            osc.stop(now + 0.35);
            break;
        case 'snap':
            osc.type = 'sine';
            osc.frequency.setValueAtTime(300, now);
            osc.frequency.setValueAtTime(600, now + 0.05);
            gain.gain.setValueAtTime(0.1, now);
            gain.gain.linearRampToValueAtTime(0, now + 0.1);
            osc.start(now);
            osc.stop(now + 0.1);
            break;
    }
}

// Find the best available voice, prioritizing natural neural and Google voices
function getBestVoice() {
    const voices = window.speechSynthesis.getVoices();
    const preferences = [
        v => v.lang.startsWith("en") && v.name.toLowerCase().includes("natural"),
        v => v.lang.startsWith("en") && v.name.toLowerCase().includes("google"),
        v => v.lang.startsWith("en") && v.name.toLowerCase().includes("microsoft"),
        v => v.lang.startsWith("en") && v.name.toLowerCase().includes("apple"),
        v => v.lang.startsWith("en") && v.name.toLowerCase().includes("zira"),
        v => v.lang.startsWith("en") && v.name.toLowerCase().includes("david"),
        v => v.lang.startsWith("en")
    ];

    for (const pref of preferences) {
        const matched = voices.find(pref);
        if (matched) return matched;
    }
    return null;
}

// Speak text using Web Speech API (with SSML stripping and kid-friendly configurations)
function speakText(text) {
    // Strip XML/SSML tags
    let cleanText = text.replace(/<[^>]*>/g, '');
    
    // Web Speech synthesis
    window.speechSynthesis.cancel(); // Stop any current speech
    const utterance = new SpeechSynthesisUtterance(cleanText);
    
    // Choose voice
    const voices = window.speechSynthesis.getVoices();
    const voiceSelect = document.getElementById("diag-voice-select");
    let selectedVoice = null;
    
    if (voiceSelect && voiceSelect.value) {
        selectedVoice = voices.find(v => v.name === voiceSelect.value);
    }
    
    if (!selectedVoice) {
        selectedVoice = getBestVoice();
    }
    
    if (selectedVoice) {
        utterance.voice = selectedVoice;
    }
    utterance.pitch = 1.15; // Warmer, friendlier tone for kids
    utterance.rate = 0.85;  // Clearer, deliberate speed for phonetics
    window.speechSynthesis.speak(utterance);
}

// UPDATE SCORE
function addScore(points, x, y) {
    currentScore += points;
    if (currentScore < 0) currentScore = 0;
    
    // Header update
    const scoreVal = document.getElementById("header-score-val");
    if (scoreVal) {
        scoreVal.innerText = String(currentScore).padStart(4, "0");
    }

    // Spawn floating text
    if (x !== undefined && y !== undefined) {
        spawnFloatingText(points > 0 ? `+${points} PTS` : `${points} PTS`, x, y, points > 0 ? "success" : "failure");
    }
}

// FLOATING TEXT JUICE SYSTEM
function spawnFloatingText(text, x, y, type) {
    const container = document.getElementById("juice-overlay-container");
    if (!container) return;

    const div = document.createElement("div");
    div.className = `floating-text ${type}`;
    div.style.left = `${x}px`;
    div.style.top = `${y}px`;
    div.innerText = text;
    container.appendChild(div);

    // Remove after animation completes
    setTimeout(() => {
        div.remove();
    }, 1200);
}

// SCREEN SHAKE JUICE SYSTEM
function triggerScreenShake(playfieldId) {
    const playfield = document.getElementById(playfieldId);
    if (!playfield) return;

    playfield.classList.add("screen-shake");
    setTimeout(() => {
        playfield.classList.remove("screen-shake");
    }, 250);
}

// NAVIGATION
function showScreen(screenId) {
    // Hide all screens
    const screens = ["main-menu", "level-1-screen", "level-2-screen", "level-3-screen", "level-4-screen", "diagnostics-screen"];
    screens.forEach(s => {
        const el = document.getElementById(s);
        if (el) el.classList.remove("active-screen");
    });

    // Show targeted screen
    const target = document.getElementById(`${screenId}-screen`) || document.getElementById(screenId);
    if (target) target.classList.add("active-screen");

    // Header bar visibility
    const header = document.getElementById("app-header");
    if (screenId === "main-menu") {
        header.classList.add("hidden");
    } else {
        header.classList.remove("hidden");
    }

    // Stop loops when switching away
    stopAllGameLoops();

    activeScreen = screenId;
    if (screenId === "diagnostics") {
        initDiagnosticsCanvas();
    }
}

document.getElementById("header-back-btn").addEventListener("click", () => {
    showScreen("main-menu");
});

function stopAllGameLoops() {
    stopLevel1();
    stopLevel2();
    stopLevel3();
    stopMicAnalysis();
}


/* =================================================================
   LEVEL 1: WORD BLASTER LOGIC (SPACE INVADERS STYLE)
   ================================================================= */

let l1LoopId = null;
let l1Ships = [];
let l1Sentences = [
    { text: "The quick brown fox", words: ["The", "quick", "brown", "fox"] },
    { text: "Phonetics is very fun", words: ["Phonetics", "is", "very", "fun"] },
    { text: "Read a book today", words: ["Read", "a", "book", "today"] }
];
let l1CurrentIndex = 0;
let l1TargetWordIdx = 0;
let l1Canvas, l1Ctx;
let l1Explosions = [];

function startLevel1() {
    l1Canvas = document.getElementById("l1-canvas");
    l1Ctx = l1Canvas.getContext("2d");
    resizeCanvas(l1Canvas);

    l1CurrentIndex = 0;
    l1TargetWordIdx = 0;
    l1Ships = [];
    l1Explosions = [];

    // Setup events
    document.getElementById("l1-replay-audio").onclick = () => {
        speakText(l1Sentences[l1CurrentIndex].text);
    };

    loadLevel1Round();
    l1LoopId = requestAnimationFrame(level1Loop);
}

function stopLevel1() {
    if (l1LoopId) {
        cancelAnimationFrame(l1LoopId);
        l1LoopId = null;
    }
    // Remove dynamically spawned ship bubbles
    const playfield = document.getElementById("l1-game-playfield");
    if (playfield) {
        const bubbles = playfield.querySelectorAll(".word-ship-bubble");
        bubbles.forEach(b => b.remove());
    }
}

function loadLevel1Round() {
    const round = l1Sentences[l1CurrentIndex];
    document.getElementById("l1-target-sentence").innerText = `"${round.text}"`;
    l1TargetWordIdx = 0;

    // Speak sentence
    speakText(round.text);

    // Remove old ships
    const playfield = document.getElementById("l1-game-playfield");
    const oldBubbles = playfield.querySelectorAll(".word-ship-bubble");
    oldBubbles.forEach(b => b.remove());
    l1Ships = [];

    // Determine distractor words
    const distractors = ["slow", "dog", "cat", "jump", "apple", "word", "hello"];
    
    // Mix target words and distractors
    const spawnWords = [...round.words];
    for (let i = 0; i < 2; i++) {
        spawnWords.push(distractors[Math.floor(Math.random() * distractors.length)]);
    }

    // Shuffle words
    spawnWords.sort(() => Math.random() - 0.5);

    // Spawn ships
    const width = l1Canvas.width;
    spawnWords.forEach((word, idx) => {
        const bubble = document.createElement("div");
        bubble.className = "word-ship-bubble juice-btn";
        bubble.innerText = word;
        
        // Horizontal offset distribution
        const leftPct = 15 + (idx * (70 / (spawnWords.length - 1 || 1)));
        bubble.style.left = `${leftPct}%`;
        
        // Randomize initial vertical position in upper half
        const topY = 80 + Math.random() * 80;
        bubble.style.top = `${topY}px`;
        
        playfield.appendChild(bubble);

        const isTarget = round.words.includes(word);
        const ship = {
            element: bubble,
            word: word,
            isTarget: isTarget,
            xPct: leftPct,
            y: topY,
            speed: 0.2 + Math.random() * 0.3,
            direction: Math.random() > 0.5 ? 1 : -1
        };

        bubble.onclick = (e) => {
            handleShipZap(ship, e.clientX, e.clientY);
        };

        l1Ships.push(ship);
    });
}

function handleShipZap(ship, clickX, clickY) {
    const round = l1Sentences[l1CurrentIndex];
    const expectedWord = round.words[l1TargetWordIdx];

    if (ship.word === expectedWord) {
        // Success
        playSynthSound('zap');
        addScore(100, clickX, clickY);
        triggerScreenShake("l1-game-playfield");

        // Spawn explosion particles
        const playfieldRect = document.getElementById("l1-game-playfield").getBoundingClientRect();
        const localX = clickX - playfieldRect.left;
        const localY = clickY - playfieldRect.top;
        createExplosion(localX, localY, '#00ff00');

        // Remove ship
        ship.element.remove();
        l1Ships = l1Ships.filter(s => s !== ship);

        // Advance sequence
        l1TargetWordIdx++;

        if (l1TargetWordIdx >= round.words.length) {
            // Celebratory particles
            createCelebration();
            // Next round
            l1CurrentIndex++;
            if (l1CurrentIndex >= l1Sentences.length) {
                // Completed level
                speakText("Level One Completed! Excellent Job!");
                setTimeout(() => {
                    showScreen("main-menu");
                }, 2000);
            } else {
                setTimeout(loadLevel1Round, 1500);
            }
        }
    } else {
        // Wrong ship
        playSynthSound('failure');
        addScore(-50, clickX, clickY);
        spawnFloatingText("WRONG ORDER!", clickX, clickY - 30, "failure");
        triggerScreenShake("l1-game-playfield");
        
        const playfieldRect = document.getElementById("l1-game-playfield").getBoundingClientRect();
        const localX = clickX - playfieldRect.left;
        const localY = clickY - playfieldRect.top;
        createExplosion(localX, localY, '#ff0000');
    }
}

function level1Loop() {
    if (!l1LoopId) return;

    l1Ctx.clearRect(0, 0, l1Canvas.width, l1Canvas.height);

    // Update ships horizontal drift and vertical descent
    const playfieldHeight = l1Canvas.height;
    l1Ships.forEach(ship => {
        // Slow vertical descent
        ship.y += ship.speed;
        
        // Loop back up if it falls off bottom
        if (ship.y > playfieldHeight - 40) {
            ship.y = 50;
        }

        // Apply visual drift
        ship.element.style.top = `${ship.y}px`;
    });

    // Render particles & explosions
    updateExplosions(l1Ctx);

    l1LoopId = requestAnimationFrame(level1Loop);
}

function createExplosion(x, y, color) {
    for (let i = 0; i < 20; i++) {
        l1Explosions.push({
            x: x,
            y: y,
            vx: (Math.random() - 0.5) * 8,
            vy: (Math.random() - 0.5) * 8,
            radius: 3 + Math.random() * 4,
            color: color,
            alpha: 1,
            decay: 0.02 + Math.random() * 0.02
        });
    }
}

function updateExplosions(ctx) {
    l1Explosions.forEach((p, idx) => {
        p.x += p.vx;
        p.y += p.vy;
        p.vy += 0.1; // gravity
        p.alpha -= p.decay;

        if (p.alpha <= 0) {
            l1Explosions.splice(idx, 1);
            return;
        }

        ctx.save();
        ctx.globalAlpha = p.alpha;
        ctx.fillStyle = p.color;
        ctx.beginPath();
        ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();
    });
}

function createCelebration() {
    const container = document.getElementById("juice-overlay-container");
    const count = 50;
    const colors = ["#ff00ff", "#00ffff", "#ffff00", "#ff0000", "#00ff00"];

    for (let i = 0; i < count; i++) {
        const div = document.createElement("div");
        div.className = "floating-text";
        div.style.left = `${Math.random() * 100}%`;
        div.style.top = `${100 + Math.random() * 100}px`;
        div.style.color = colors[Math.floor(Math.random() * colors.length)];
        div.style.fontSize = `${10 + Math.random() * 20}px`;
        div.innerText = "★";
        container.appendChild(div);

        setTimeout(() => div.remove(), 1200);
    }
}


/* =================================================================
   LEVEL 2: SYLLABLE SHREDDER LOGIC (FRUIT NINJA STYLE)
   ================================================================= */

let l2LoopId = null;
let l2Canvas, l2Ctx;
let l2Words = [
    { word: "cooperate", syllables: ["co", "o", "pe", "rate"] },
    { word: "phonetics", syllables: ["pho", "ne", "tics"] },
    { word: "education", syllables: ["ed", "u", "ca", "tion"] }
];
let l2CurrentIndex = 0;
let l2TargetSyllableIdx = 0;
let l2Blocks = [];
let l2Explosions = [];
let mousePath = [];
let isMouseDown = false;

function startLevel2() {
    l2Canvas = document.getElementById("l2-canvas");
    l2Ctx = l2Canvas.getContext("2d");
    resizeCanvas(l2Canvas);

    l2CurrentIndex = 0;
    l2TargetSyllableIdx = 0;
    l2Blocks = [];
    l2Explosions = [];
    mousePath = [];

    // Replay button
    document.getElementById("l2-replay-audio").onclick = () => {
        speakText(l2Words[l2CurrentIndex].word);
    };

    // Slice input handlers
    const playfield = document.getElementById("l2-game-playfield");
    
    const handleStart = (e) => {
        isMouseDown = true;
        addMousePoint(e, playfield);
    };

    const handleMove = (e) => {
        if (!isMouseDown) return;
        addMousePoint(e, playfield);
        checkSlices();
    };

    const handleEnd = () => {
        isMouseDown = false;
        mousePath = [];
    };

    playfield.onmousedown = handleStart;
    playfield.onmousemove = handleMove;
    window.onmouseup = handleEnd;

    playfield.ontouchstart = (e) => handleStart(e.touches[0]);
    playfield.ontouchmove = (e) => handleMove(e.touches[0]);
    window.ontouchend = handleEnd;

    loadLevel2Round();
    l2LoopId = requestAnimationFrame(level2Loop);
}

function stopLevel2() {
    if (l2LoopId) {
        cancelAnimationFrame(l2LoopId);
        l2LoopId = null;
    }
    const playfield = document.getElementById("l2-game-playfield");
    if (playfield) {
        const bubbles = playfield.querySelectorAll(".syllable-block-bubble");
        bubbles.forEach(b => b.remove());
        playfield.onmousedown = null;
        playfield.onmousemove = null;
        playfield.ontouchstart = null;
        playfield.ontouchmove = null;
    }
    window.onmouseup = null;
    window.ontouchend = null;
}

function addMousePoint(e, container) {
    const rect = container.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    mousePath.push({ x, y, time: Date.now() });

    // Limit length of path
    if (mousePath.length > 25) {
        mousePath.shift();
    }
}

function loadLevel2Round() {
    const round = l2Words[l2CurrentIndex];
    document.getElementById("l2-target-word").innerText = round.word;
    l2TargetSyllableIdx = 0;

    // Speak initial full word
    speakText(round.word);

    // Clear old bubbles
    const playfield = document.getElementById("l2-game-playfield");
    const oldBubbles = playfield.querySelectorAll(".syllable-block-bubble");
    oldBubbles.forEach(b => b.remove());
    l2Blocks = [];

    // Toss blocks up
    tossSyllables(round.syllables);
}

function tossSyllables(syllables) {
    const playfield = document.getElementById("l2-game-playfield");
    const w = l2Canvas.width;
    const h = l2Canvas.height;

    syllables.forEach((syll, idx) => {
        // Spawn block element
        const bubble = document.createElement("div");
        bubble.className = "syllable-block-bubble juice-btn";
        bubble.innerText = syll;
        
        playfield.appendChild(bubble);

        // Toss properties
        // Distribute starting X across bottom screen
        const startX = w * (0.2 + (idx * (0.6 / (syllables.length - 1 || 1))));
        const startY = h + 50;

        // Custom upward velocity and horizontal push
        const vy = -(h * 0.021 + Math.random() * 3);
        const vx = (Math.random() - 0.5) * 2;

        const block = {
            element: bubble,
            syllable: syll,
            index: idx,
            x: startX,
            y: startY,
            vx: vx,
            vy: vy,
            sliced: false
        };

        l2Blocks.push(block);
    });
}

function checkSlices() {
    if (mousePath.length < 2) return;
    const p1 = mousePath[mousePath.length - 2];
    const p2 = mousePath[mousePath.length - 1];

    l2Blocks.forEach(block => {
        if (block.sliced) return;

        // Simple bounding box checks on the DOM elements
        const rect = block.element.getBoundingClientRect();
        const playfieldRect = document.getElementById("l2-game-playfield").getBoundingClientRect();
        
        const bx = rect.left + rect.width/2 - playfieldRect.left;
        const by = rect.top + rect.height/2 - playfieldRect.top;
        const radius = rect.width / 2;

        // Check distance from segment (p1, p2) to center of block
        if (distToSegment({ x: bx, y: by }, p1, p2) < radius) {
            sliceBlock(block, p2.x, p2.y);
        }
    });
}

function sliceBlock(block, sliceX, sliceY) {
    const round = l2Words[l2CurrentIndex];

    if (block.index === l2TargetSyllableIdx) {
        // CORRECT SLICE IN SEQUENCE
        block.sliced = true;
        block.element.classList.add("sliced");
        playSynthSound('slice');
        
        // Convert local coordinates back to screen coordinate for floating text
        const playfieldRect = document.getElementById("l2-game-playfield").getBoundingClientRect();
        const screenX = sliceX + playfieldRect.left;
        const screenY = sliceY + playfieldRect.top;

        addScore(150, screenX, screenY);
        createExplosion(sliceX, sliceY, '#00ffff');

        // Pronounce syllable
        speakText(block.syllable);

        l2TargetSyllableIdx++;

        if (l2TargetSyllableIdx >= round.syllables.length) {
            // MERGE SUCCESS
            setTimeout(() => {
                playSynthSound('success');
                speakText(round.word);
                addScore(250, window.innerWidth / 2, window.innerHeight / 2 - 100);
                spawnFloatingText("MERGED!", window.innerWidth / 2, window.innerHeight / 2 - 100, "merge");
                triggerScreenShake("l2-game-playfield");
                createCelebration();

                l2CurrentIndex++;
                if (l2CurrentIndex >= l2Words.length) {
                    speakText("Level Two Completed! Fantastic!");
                    setTimeout(() => showScreen("main-menu"), 2500);
                } else {
                    setTimeout(loadLevel2Round, 2000);
                }
            }, 500);
        }
    } else {
        // INCORRECT SLICE IN SEQUENCE
        // Play failure sound, but don't slice it
        playSynthSound('failure');
        
        const playfieldRect = document.getElementById("l2-game-playfield").getBoundingClientRect();
        const screenX = sliceX + playfieldRect.left;
        const screenY = sliceY + playfieldRect.top;

        addScore(-50, screenX, screenY);
        spawnFloatingText("WRONG CHUNK!", screenX, screenY - 30, "failure");
        triggerScreenShake("l2-game-playfield");
        createExplosion(sliceX, sliceY, '#ff0000');
    }
}

function level2Loop() {
    if (!l2LoopId) return;

    l2Ctx.clearRect(0, 0, l2Canvas.width, l2Canvas.height);

    const h = l2Canvas.height;

    // Update block positions
    l2Blocks.forEach(block => {
        if (!block.sliced) {
            block.x += block.vx;
            block.y += block.vy;
            block.vy += 0.3; // Gravity pull

            // Update DOM element positions
            block.element.style.left = `${block.x}px`;
            block.element.style.top = `${block.y}px`;

            // If it falls off screen, toss it back up
            if (block.y > h + 100 && block.vy > 0) {
                // reset toss
                block.x = l2Canvas.width * (0.2 + block.index * (0.6 / (l2Blocks.length - 1 || 1)));
                block.y = h + 50;
                block.vy = -(h * 0.021 + Math.random() * 3);
                block.vx = (Math.random() - 0.5) * 2;
            }
        } else {
            // Sliced block falls off or disappears
            block.element.style.display = 'none';
        }
    });

    // Render Slice Trail
    renderSliceTrail(l2Ctx);

    // Update explosions
    updateExplosions(l2Ctx);

    l2LoopId = requestAnimationFrame(level2Loop);
}

function renderSliceTrail(ctx) {
    if (mousePath.length < 2) return;

    ctx.save();
    ctx.strokeStyle = "rgba(0, 255, 255, 0.8)";
    ctx.lineWidth = 4;
    ctx.lineCap = "round";
    ctx.shadowBlur = 10;
    ctx.shadowColor = "cyan";
    
    ctx.beginPath();
    ctx.moveTo(mousePath[0].x, mousePath[0].y);
    for (let i = 1; i < mousePath.length; i++) {
        ctx.lineTo(mousePath[i].x, mousePath[i].y);
    }
    ctx.stroke();
    ctx.restore();

    // Fade out points based on age
    const now = Date.now();
    mousePath = mousePath.filter(p => now - p.time < 300);
}

// Distance helper
function distToSegment(p, v, w) {
    const l2 = Math.pow(v.x - w.x, 2) + Math.pow(v.y - w.y, 2);
    if (l2 === 0) return Math.sqrt(Math.pow(p.x - v.x, 2) + Math.pow(p.y - v.y, 2));
    let t = ((p.x - v.x) * (w.x - v.x) + (p.y - v.y) * (w.y - v.y)) / l2;
    t = Math.max(0, Math.min(1, t));
    return Math.sqrt(
        Math.pow(p.x - (v.x + t * (w.x - v.x)), 2) +
        Math.pow(p.y - (v.y + t * (w.y - v.y)), 2)
    );
}


/* =================================================================
   LEVEL 3: ONSET-RIME CONSTRUCTOR LOGIC (TETRIS STYLE)
   ================================================================= */

let l3LoopId = null;
let l3Canvas, l3Ctx;
let l3Words = [
    { target: "cat", onset: "c-", rime: "-at", distractor: "-ing", correctX: 0.25 },
    { target: "string", onset: "str-", rime: "-ing", distractor: "-at", correctX: 0.75 },
    { target: "play", onset: "pl-", rime: "-ay", distractor: "-ot", correctX: 0.25 },
    { target: "ship", onset: "sh-", rime: "-ip", distractor: "-ed", correctX: 0.25 }
];
let l3CurrentIndex = 0;
let l3IsChecking = false; // Double-trigger checking lock

// Block states
let fallingBlock = { text: "c-", x: 0, y: 0, w: 90, h: 50, xPct: 0.25 };
let rimeLeft = { text: "-at", x: 0, y: 0, w: 100, h: 50 };
let rimeRight = { text: "-ing", x: 0, y: 0, w: 100, h: 50 };
let dropSpeed = 1.0;

function startLevel3() {
    l3Canvas = document.getElementById("l3-canvas");
    l3Ctx = l3Canvas.getContext("2d");
    resizeCanvas(l3Canvas);

    l3CurrentIndex = 0;
    l3IsChecking = false;
    
    // Bind Controls
    document.getElementById("l3-btn-left").onclick = () => moveFallingBlock(-1);
    document.getElementById("l3-btn-right").onclick = () => moveFallingBlock(1);
    document.getElementById("l3-btn-drop").onclick = dropFallingBlock;

    window.onkeydown = (e) => {
        if (activeScreen !== "level-3-screen") return;
        if (e.key === "ArrowLeft") moveFallingBlock(-1);
        if (e.key === "ArrowRight") moveFallingBlock(1);
        if (e.key === "Space" || e.key === " " || e.key === "ArrowDown") dropFallingBlock();
    };

    loadLevel3Round();
    l3LoopId = requestAnimationFrame(level3Loop);
}

function stopLevel3() {
    if (l3LoopId) {
        cancelAnimationFrame(l3LoopId);
        l3LoopId = null;
    }
    window.onkeydown = null;
}

function loadLevel3Round() {
    const round = l3Words[l3CurrentIndex];
    document.getElementById("l3-target-word").innerText = round.target;
    
    // Voiceover instruction
    speakText(`Construct the word: ${round.target}`);

    // Left is correct for index 0, 2, 3...
    const leftIsCorrect = round.correctX === 0.25;

    // Spawn block data
    rimeLeft.text = leftIsCorrect ? round.rime : round.distractor;
    rimeRight.text = leftIsCorrect ? round.distractor : round.rime;

    // Reset falling block with normalized coordinate
    fallingBlock.text = round.onset;
    fallingBlock.xPct = 0.25; // Default starts on left
    fallingBlock.y = 20;
    dropSpeed = 0.8;
    l3IsChecking = false; // Reset lock
}

function moveFallingBlock(dir) {
    if (l3IsChecking) return;
    playSynthSound('snap');

    if (dir < 0) {
        fallingBlock.xPct = 0.25;
    } else {
        fallingBlock.xPct = 0.75;
    }
}

function dropFallingBlock() {
    if (l3IsChecking) return;
    dropSpeed = 16.0;
}

function level3Loop() {
    if (!l3LoopId) return;

    l3Ctx.clearRect(0, 0, l3Canvas.width, l3Canvas.height);
    const w = l3Canvas.width;
    const h = l3Canvas.height;

    // Dynamically update rime positions based on current canvas dimension (handles resize/delayed rendering)
    rimeLeft.x = w * 0.25 - rimeLeft.w / 2;
    rimeLeft.y = h - 140;
    rimeRight.x = w * 0.75 - rimeRight.w / 2;
    rimeRight.y = h - 140;

    // Dynamically calculate falling block absolute position
    fallingBlock.x = w * fallingBlock.xPct - fallingBlock.w / 2;

    // Draw lines/slots indicating landing zones
    drawLandingZones(l3Ctx);

    // Update falling block if not landing/checking
    if (!l3IsChecking) {
        fallingBlock.y += dropSpeed;

        // Check collision landing
        const landingY = h - 190;
        if (fallingBlock.y >= landingY) {
            fallingBlock.y = landingY;
            l3IsChecking = true; // Lock execution
            checkMatchLanding();
        }
    }

    // Render rime blocks
    drawConstructorBlock(l3Ctx, rimeLeft, '#1e1c3e', '#00ffff');
    drawConstructorBlock(l3Ctx, rimeRight, '#1e1c3e', '#ff00ff');

    // Render falling block
    drawConstructorBlock(l3Ctx, fallingBlock, '#5a3bb6', '#ffff00');

    // Explosions / juice
    updateExplosions(l3Ctx);

    l3LoopId = requestAnimationFrame(level3Loop);
}

function drawLandingZones(ctx) {
    const w = l3Canvas.width;
    const h = l3Canvas.height;

    ctx.save();
    ctx.setLineDash([6, 6]);
    ctx.strokeStyle = "rgba(255,255,255,0.15)";
    ctx.lineWidth = 2;

    // Left slot guide line
    ctx.beginPath();
    ctx.moveTo(w * 0.25, 0);
    ctx.lineTo(w * 0.25, h);
    ctx.stroke();

    // Right slot guide line
    ctx.beginPath();
    ctx.moveTo(w * 0.75, 0);
    ctx.lineTo(w * 0.75, h);
    ctx.stroke();

    ctx.restore();
}

function drawConstructorBlock(ctx, block, fill, stroke) {
    ctx.save();
    
    // Glow effect
    ctx.shadowBlur = 8;
    ctx.shadowColor = stroke;

    // Background
    ctx.fillStyle = fill;
    ctx.strokeStyle = stroke;
    ctx.lineWidth = 3;
    
    // Draw rounded rect
    const r = 10;
    const x = block.x;
    const y = block.y;
    const w = block.w;
    const h = block.h;

    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + w - r, y);
    ctx.quadraticCurveTo(x + w, y, x + w, y + r);
    ctx.lineTo(x + w, y + h - r);
    ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
    ctx.lineTo(x + r, y + h - r);
    ctx.quadraticCurveTo(x, y + h, x, y + h - r);
    ctx.lineTo(x, y + r);
    ctx.quadraticCurveTo(x, y, x + r, y);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    // Inner highlight
    ctx.strokeStyle = "rgba(255,255,255,0.15)";
    ctx.lineWidth = 1;
    ctx.stroke();

    // Text label
    ctx.fillStyle = "#ffffff";
    ctx.font = "bold 20px Outfit";
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.shadowBlur = 0; // disable shadow for clean text
    ctx.fillText(block.text, block.x + block.w / 2, block.y + block.h / 2);

    ctx.restore();
}

function checkMatchLanding() {
    const round = l3Words[l3CurrentIndex];
    
    // Check lanes using normalized coordinates (independent of canvas size)
    const onLeft = fallingBlock.xPct === 0.25;
    const selectedRime = onLeft ? rimeLeft.text : rimeRight.text;
    const combinedWord = fallingBlock.text.replace('-', '') + selectedRime.replace('-', '');

    if (combinedWord === round.target) {
        // SUCCESS
        playSynthSound('success');
        speakText(round.target);
        
        // Local coordinates for popup
        const playfieldRect = document.getElementById("l3-game-playfield").getBoundingClientRect();
        const screenX = fallingBlock.x + fallingBlock.w / 2 + playfieldRect.left;
        const screenY = fallingBlock.y + playfieldRect.top;

        addScore(200, screenX, screenY);
        createExplosion(fallingBlock.x + fallingBlock.w / 2, fallingBlock.y + 25, '#00ff00');
        createCelebration();
        triggerScreenShake("l3-game-playfield");

        l3CurrentIndex++;
        if (l3CurrentIndex >= l3Words.length) {
            speakText("Level Three Completed! Astounding!");
            setTimeout(() => showScreen("main-menu"), 2500);
        } else {
            setTimeout(loadLevel3Round, 1800);
        }
    } else {
        // FAILURE
        playSynthSound('failure');
        speakText(combinedWord); // Say incorrect blend (e.g. "cing")

        const playfieldRect = document.getElementById("l3-game-playfield").getBoundingClientRect();
        const screenX = fallingBlock.x + fallingBlock.w / 2 + playfieldRect.left;
        const screenY = fallingBlock.y + playfieldRect.top;

        addScore(-50, screenX, screenY);
        spawnFloatingText("TRY AGAIN!", screenX, screenY - 30, "failure");
        triggerScreenShake("l3-game-playfield");
        createExplosion(fallingBlock.x + fallingBlock.w / 2, fallingBlock.y + 25, '#ff0000');

        // Respawn block after delay and unlock
        setTimeout(() => {
            fallingBlock.y = 20;
            dropSpeed = 0.8;
            l3IsChecking = false; // Unlock for next try
        }, 1200);
    }
}


/* =================================================================
   LEVEL 4: PHONEME ISOLATOR LOGIC (CRAFTING GRID / DRAG AND DROP)
   ================================================================= */

let l4BaselineWord = "bat";
let l4Phonemes = ["b", "a", "t"];
let l4Inventory = ["c", "p", "o", "t"];
let l4EarnedCards = new Set();

// Rewards dictionary
const l4CardRewards = {
    "cat": { name: "Curious Cat Card", art: "🐱", rarity: "Common" },
    "cap": { name: "Captain Cap Card", art: "🧢", rarity: "Uncommon" },
    "cop": { name: "Cool Cop Card", art: "👮", rarity: "Rare" },
    "top": { name: "Spinning Top Card", art: "🌪️", rarity: "Uncommon" },
    "toy": { name: "Shiny Toy Card", art: "🧸", rarity: "Rare" }
};

function startLevel4() {
    l4BaselineWord = "bat";
    l4Phonemes = ["b", "a", "t"];
    l4EarnedCards.clear();

    // Populate locks on cards
    renderCardCabinet();

    loadLevel4State();
    speakText(`Start word is: bat. Drag sound bubbles to mutate the word!`);
}

function loadLevel4State() {
    // Render letters slots inside glass
    const slotsContainer = document.getElementById("l4-spelling-slots");
    slotsContainer.innerHTML = "";

    l4Phonemes.forEach((phon, idx) => {
        const slot = document.createElement("div");
        slot.className = "letter-slot filled";
        slot.dataset.index = idx;
        slot.innerText = phon;
        slotsContainer.appendChild(slot);
    });

    // Render inventory sound bubbles
    const bubblesContainer = document.getElementById("l4-bubbles-container");
    bubblesContainer.innerHTML = "";

    l4Inventory.forEach(letter => {
        const bubble = document.createElement("div");
        bubble.className = "phoneme-bubble juice-btn";
        bubble.innerText = `/${letter}/`;
        bubble.style.touchAction = "none"; // Required for pointer dragging without page scrolls

        // Pointer-based fluid drag & drop (works on desktops & touchscreens natively)
        bubble.onpointerdown = (e) => {
            e.preventDefault();
            bubble.releasePointerCapture(e.pointerId);

            playSynthSound('snap');
            bubble.style.transform = "scale(1.15)";
            bubble.style.boxShadow = "0 8px 20px var(--accent-glow)";

            const rect = bubble.getBoundingClientRect();
            const shiftX = e.clientX - rect.left;
            const shiftY = e.clientY - rect.top;

            const origPosition = bubble.style.position;
            const origZIndex = bubble.style.zIndex;
            const origLeft = bubble.style.left;
            const origTop = bubble.style.top;
            const origTransition = bubble.style.transition;

            bubble.style.position = "fixed";
            bubble.style.zIndex = "1000";
            bubble.style.transition = "none";

            moveAt(e.clientX, e.clientY);

            function moveAt(clientX, clientY) {
                bubble.style.left = `${clientX - shiftX}px`;
                bubble.style.top = `${clientY - shiftY}px`;
            }

            let hoveredSlot = null;

            function onPointerMove(moveEvent) {
                moveAt(moveEvent.clientX, moveEvent.clientY);

                // Find slot underneath the drag element
                bubble.style.pointerEvents = 'none';
                const elemBelow = document.elementFromPoint(moveEvent.clientX, moveEvent.clientY);
                bubble.style.pointerEvents = 'auto';

                if (!elemBelow) return;

                const slot = elemBelow.closest('.letter-slot');
                if (slot) {
                    if (hoveredSlot !== slot) {
                        if (hoveredSlot) hoveredSlot.classList.remove('hovered');
                        hoveredSlot = slot;
                        hoveredSlot.classList.add('hovered');
                    }
                } else {
                    if (hoveredSlot) {
                        hoveredSlot.classList.remove('hovered');
                        hoveredSlot = null;
                    }
                }
            }

            document.addEventListener('pointermove', onPointerMove);

            bubble.onpointerup = (upEvent) => {
                document.removeEventListener('pointermove', onPointerMove);
                bubble.onpointerup = null;

                bubble.style.pointerEvents = 'none';
                const elemBelow = document.elementFromPoint(upEvent.clientX, upEvent.clientY);
                bubble.style.pointerEvents = 'auto';

                const slot = elemBelow ? elemBelow.closest('.letter-slot') : null;

                // Restore default styles
                bubble.style.transform = "";
                bubble.style.boxShadow = "";
                bubble.style.position = origPosition;
                bubble.style.zIndex = origZIndex;
                bubble.style.left = origLeft;
                bubble.style.top = origTop;
                bubble.style.transition = origTransition;

                if (slot) {
                    slot.classList.remove('hovered');
                    const slotIdx = parseInt(slot.dataset.index);
                    handlePhonemeDrop(letter, slotIdx);
                } else {
                    // Slide back animation
                    bubble.style.transition = "all 0.25s ease-out";
                    setTimeout(() => {
                        bubble.style.transition = origTransition;
                    }, 250);
                }
            };
        };

        bubblesContainer.appendChild(bubble);
    });
}

function handlePhonemeDrop(phoneme, slotIdx) {
    // Create candidate word
    const candidateArray = [...l4Phonemes];
    candidateArray[slotIdx] = phoneme;
    const candidateWord = candidateArray.join("");

    if (candidateWord === l4BaselineWord) return; // No mutation

    // Check validity
    if (l4CardRewards[candidateWord]) {
        // VALID MUTATION SUCCESS
        l4BaselineWord = candidateWord;
        l4Phonemes = candidateArray;
        
        playSynthSound('success');
        speakText(`You crafted: ${candidateWord}`);

        // Award card
        l4EarnedCards.add(candidateWord);
        renderCardCabinet();

        // Screen popup
        const glassRect = document.querySelector(".magnifier-glass").getBoundingClientRect();
        const popupX = glassRect.left + glassRect.width/2;
        const popupY = glassRect.top + glassRect.height/2 - 40;

        addScore(300, popupX, popupY);
        triggerScreenShake("l4-magnifier-container");

        // Reload slots
        loadLevel4State();

        // Check level completion: if collected 3 cards, win!
        if (l4EarnedCards.size >= 3) {
            setTimeout(() => {
                speakText("Level Four Completed! Master crafter!");
                createCelebration();
                setTimeout(() => showScreen("main-menu"), 2500);
            }, 1500);
        }
    } else {
        // INVALID MUTATION / GIBBERISH
        playSynthSound('failure');
        speakText(candidateWord); // Speak the failed blend (e.g. "bpt")

        const glassRect = document.querySelector(".magnifier-glass").getBoundingClientRect();
        const popupX = glassRect.left + glassRect.width/2;
        const popupY = glassRect.top + glassRect.height/2 - 40;

        addScore(-50, popupX, popupY);
        spawnFloatingText("WRONG BLEND", popupX, popupY - 30, "failure");
        
        // Wobble slots
        const slots = document.querySelectorAll(".letter-slot");
        slots.forEach(slot => {
            slot.style.borderColor = 'red';
            setTimeout(() => {
                slot.style.borderColor = '';
            }, 300);
        });
    }
}

function renderCardCabinet() {
    const cabinet = document.getElementById("l4-card-cabinet");
    cabinet.innerHTML = "";

    Object.keys(l4CardRewards).forEach(word => {
        const cardDef = l4CardRewards[word];
        const unlocked = l4EarnedCards.has(word);

        const card = document.createElement("div");
        card.className = `collectible-card ${unlocked ? 'unlocked' : 'card-locked'}`;

        if (unlocked) {
            card.innerHTML = `
                <div class="card-art">${cardDef.art}</div>
                <div class="card-name">${cardDef.name}</div>
                <div class="card-meta">${cardDef.rarity}</div>
            `;
        } else {
            card.innerHTML = `
                <div class="card-art">🔒</div>
                <div class="card-name">Locked Card</div>
                <div class="card-meta">Mutate to: ???</div>
            `;
        }

        cabinet.appendChild(card);
    });
}


/* =================================================================
   🎙️ DIAGNOSTICS & MICROPHONE STREAM ANALYZER
   ================================================================= */

let micStream = null;
let micAudioCtx = null;
let micAnalyser = null;
let micLoopId = null;

// TTS Test
document.getElementById("diag-play-tts").onclick = () => {
    const input = document.getElementById("diag-tts-input").value;
    speakText(input);
};

// Microphone trigger
document.getElementById("diag-toggle-mic").onclick = function() {
    if (micStream) {
        stopMicAnalysis();
        this.className = "diag-action-btn juice-btn mic-off";
        this.innerText = "🎤 Start Microphone Recording";
    } else {
        startMicAnalysis();
        this.className = "diag-action-btn juice-btn mic-on";
        this.innerText = "🛑 Stop Microphone Recording";
    }
};

async function startMicAnalysis() {
    const statusText = document.getElementById("diag-mic-status");
    
    try {
        micStream = await navigator.mediaDevices.getUserMedia({ audio: true });
        
        // Create audio graph
        micAudioCtx = new (window.AudioContext || window.webkitAudioContext)();
        const source = micAudioCtx.createMediaStreamSource(micStream);
        
        micAnalyser = micAudioCtx.createAnalyser();
        micAnalyser.fftSize = 256;
        source.connect(micAnalyser);

        statusText.innerText = "RECORDING ACTIVE";
        statusText.style.color = "var(--success)";

        micLoopId = requestAnimationFrame(micAnalysisLoop);
    } catch (err) {
        statusText.innerText = "ERROR ACCESSING MIC";
        statusText.style.color = "var(--warning)";
        console.error(err);
    }
}

function stopMicAnalysis() {
    const statusText = document.getElementById("diag-mic-status");
    statusText.innerText = "OFFLINE";
    statusText.style.color = "var(--warning)";

    if (micLoopId) {
        cancelAnimationFrame(micLoopId);
        micLoopId = null;
    }

    if (micStream) {
        micStream.getTracks().forEach(track => track.stop());
        micStream = null;
    }

    if (micAudioCtx) {
        micAudioCtx.close();
        micAudioCtx = null;
    }
    
    micAnalyser = null;

    // Clear UI
    document.getElementById("diag-mic-progress").style.width = "0%";
    document.getElementById("diag-mic-vol-text").innerText = "0.0%";
}

function micAnalysisLoop() {
    if (!micStream || !micAnalyser) return;

    const dataArray = new Uint8Array(micAnalyser.frequencyBinCount);
    micAnalyser.getByteFrequencyData(dataArray);

    // Calculate average volume / level
    let sum = 0;
    for (let i = 0; i < dataArray.length; i++) {
        sum += dataArray[i];
    }
    const average = sum / dataArray.length;
    const volumePct = Math.min(100, (average / 180) * 100);

    // Update progress bar
    document.getElementById("diag-mic-progress").style.width = `${volumePct}%`;
    document.getElementById("diag-mic-vol-text").innerText = `${volumePct.toFixed(1)}%`;

    // Draw waveform visualizer canvas
    const canvas = document.getElementById("diag-visualizer-canvas");
    const ctx = canvas.getContext("2d");
    const w = canvas.width;
    const h = canvas.height;
    ctx.clearRect(0, 0, w, h);

    // Draw bars
    ctx.fillStyle = "rgba(0, 255, 255, 0.7)";
    const barWidth = w / dataArray.length;
    for (let i = 0; i < dataArray.length; i++) {
        const val = dataArray[i];
        const barHeight = (val / 255) * h;
        ctx.fillRect(i * barWidth, h - barHeight, barWidth - 1, barHeight);
    }

    micLoopId = requestAnimationFrame(micAnalysisLoop);
}

function initDiagnosticsCanvas() {
    const canvas = document.getElementById("diag-visualizer-canvas");
    resizeCanvas(canvas);
}


/* =================================================================
   GLOBAL INITIALIZATIONS & BOOTSTRAP
   ================================================================= */

function resizeCanvas(canvas) {
    if (!canvas) return;
    const parent = canvas.parentElement;
    canvas.width = parent.clientWidth;
    canvas.height = parent.clientHeight;
}

// Background ambient loop
let ambientLoopId = null;
const ambientStars = [];

function initAmbientBackground() {
    const canvas = document.getElementById("ambient-canvas");
    resizeCanvas(canvas);
    window.addEventListener("resize", () => {
        resizeCanvas(canvas);
        const l1 = document.getElementById("l1-canvas"); if (l1 && activeScreen === 'level-1') resizeCanvas(l1);
        const l2 = document.getElementById("l2-canvas"); if (l2 && activeScreen === 'level-2') resizeCanvas(l2);
        const l3 = document.getElementById("l3-canvas"); if (l3 && activeScreen === 'level-3') resizeCanvas(l3);
        initDiagnosticsCanvas();
    });

    const ctx = canvas.getContext("2d");
    
    // Spawn ambient dots
    for (let i = 0; i < 30; i++) {
        ambientStars.push({
            x: Math.random() * canvas.width,
            y: Math.random() * canvas.height,
            r: 1 + Math.random() * 2,
            speed: 0.1 + Math.random() * 0.2,
            alpha: 0.1 + Math.random() * 0.4,
            fadeDir: Math.random() > 0.5 ? 1 : -1
        });
    }

    function ambientLoop() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        
        ambientStars.forEach(star => {
            star.y -= star.speed;
            if (star.y < 0) {
                star.y = canvas.height;
                star.x = Math.random() * canvas.width;
            }

            // Pulse opacity
            star.alpha += 0.005 * star.fadeDir;
            if (star.alpha > 0.6 || star.alpha < 0.1) {
                star.fadeDir *= -1;
            }

            ctx.save();
            ctx.fillStyle = `rgba(255, 255, 255, ${star.alpha})`;
            ctx.beginPath();
            ctx.arc(star.x, star.y, star.r, 0, Math.PI * 2);
            ctx.fill();
            ctx.restore();
        });

        ambientLoopId = requestAnimationFrame(ambientLoop);
    }

    ambientLoop();
}

initAmbientBackground();

// LEVEL SWITCH LAUNCHER
function startGame(level) {
    activeLevel = level;
    
    if (level === 1) {
        showScreen("level-1");
        startLevel1();
    } else if (level === 2) {
        showScreen("level-2");
        startLevel2();
    } else if (level === 3) {
        showScreen("level-3");
        startLevel3();
    } else if (level === 4) {
        showScreen("level-4");
        startLevel4();
    }
}
