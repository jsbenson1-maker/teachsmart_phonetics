/* =================================================================
   TEACHSMART: PHONETIC CHALLENGE - CORE GAME ENGINE
   ================================================================= */

// STATE MANAGEMENT
let currentScore = 0;
let activeScreen = "main-menu";
let activeLevel = 0;

// Helper to shuffle array (Fisher-Yates)
function shuffleArray(array) {
    for (let i = array.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [array[i], array[j]] = [array[j], array[i]];
    }
    return array;
}

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

// Speak text using pre-recorded high-quality MP3 clips when available, falling back to kid-friendly TTS
function speakText(text, clipKey) {
    if (clipKey) {
        const audio = new Audio(`audio/${clipKey}.mp3`);
        audio.oncanplaythrough = () => {
            audio.play().catch(() => {
                fallbackSpeak(text);
            });
        };
        audio.onerror = () => {
            fallbackSpeak(text);
        };
    } else {
        fallbackSpeak(text);
    }
}

// Speak text using Web Speech API (with SSML stripping and kid-friendly configurations)
function fallbackSpeak(text) {
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
    stopLevel4();
    stopMicAnalysis();
}

function stopLevel4() {
    const bodyBubbles = document.querySelectorAll("body > .phoneme-bubble");
    bodyBubbles.forEach(b => b.remove());
}


/* =================================================================
   LEVEL 1: WORD BLASTER LOGIC (SPACE INVADERS STYLE)
   ================================================================= */

let l1LoopId = null;
let l2LoopId = null;
let l3LoopId = null;
let l1SentencesDatabase = [
    { text: "The quick brown fox", words: ["The", "quick", "brown", "fox"] },
    { text: "Phonetics is very fun", words: ["Phonetics", "is", "very", "fun"] },
    { text: "Read a book today", words: ["Read", "a", "book", "today"] },
    { text: "The cat sat on the mat", words: ["The", "cat", "sat", "on", "the", "mat"] },
    { text: "A happy dog wags its tail", words: ["A", "happy", "dog", "wags", "its", "tail"] },
    { text: "Birds fly high in the blue sky", words: ["Birds", "fly", "high", "in", "the", "blue", "sky"] },
    { text: "The sun shines bright and warm", words: ["The", "sun", "shines", "bright", "and", "warm"] },
    { text: "Children love to play games together", words: ["Children", "love", "to", "play", "games", "together"] }
];
let l1Sentences = [];
let l1CurrentIndex = 0;
let l1TargetWordIdx = 0;
let l1Canvas, l1Ctx;
let l1Explosions = [];

function startLevel1() {
    l1Canvas = document.getElementById("l1-canvas");
    l1Ctx = l1Canvas.getContext("2d");
    resizeCanvas(l1Canvas);

    // Randomize the level sequence
    l1Sentences = shuffleArray([...l1SentencesDatabase]).slice(0, 3);

    l1CurrentIndex = 0;
    l1TargetWordIdx = 0;
    l1Ships = [];
    l1Explosions = [];

    // Setup events
    document.getElementById("l1-replay-audio").onclick = () => {
        speakText(l1Sentences[l1CurrentIndex].text, "level1_sentence_" + l1CurrentIndex);
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

    // Speak sentence (pre-recorded audio)
    speakText(round.text, "level1_sentence_" + l1CurrentIndex);

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

        // Speak zapped word (pre-recorded audio)
        speakText(ship.word, "word_" + ship.word.toLowerCase());

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
                speakText("Level One Completed! Excellent Job!", "level1_win");
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
   ================================================================= */const l2WordsDatabase = [
    { word: "cooperate", syllables: ["co", "o", "pe", "rate"] },
    { word: "phonetics", syllables: ["pho", "ne", "tics"] },
    { word: "education", syllables: ["ed", "u", "ca", "tion"] },
    { word: "dinosaur", syllables: ["di", "no", "saur"] },
    { word: "banana", syllables: ["ba", "na", "na"] },
    { word: "computer", syllables: ["com", "pu", "ter"] },
    { word: "helicopter", syllables: ["he", "li", "cop", "ter"] },
    { word: "alligator", syllables: ["al", "li", "ga", "tor"] },
    { word: "butterfly", syllables: ["but", "ter", "fly"] },
    { word: "adventure", syllables: ["ad", "ven", "ture"] }
];
let l2Words = [];
let l2CurrentIndex = 0;
let l2TargetSyllableIdx = 0;
let l2Blocks = [];
let l2Explosions = [];
let mousePath = [];

function startLevel2() {
    l2Canvas = document.getElementById("l2-canvas");
    l2Ctx = l2Canvas.getContext("2d");
    resizeCanvas(l2Canvas);

    // Randomize the level sequence
    l2Words = shuffleArray([...l2WordsDatabase]).slice(0, 3);

    l2CurrentIndex = 0;
    l2TargetSyllableIdx = 0;
    l2Blocks = [];
    l2Explosions = [];
    mousePath = [];

    // Replay button
    document.getElementById("l2-replay-audio").onclick = () => {
        speakText(l2Words[l2CurrentIndex].word, "level2_fullword_" + l2CurrentIndex);
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

    // Speak initial full word (pre-recorded audio)
    speakText(round.word, "level2_fullword_" + l2CurrentIndex);

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

        // Pronounce syllable (pre-recorded audio)
        speakText(block.syllable, "level2_slice_reinforce_" + l2CurrentIndex + "_" + block.index);

        l2TargetSyllableIdx++;

        if (l2TargetSyllableIdx >= round.syllables.length) {
            // MERGE SUCCESS
            setTimeout(() => {
                playSynthSound('success');
                speakText(round.word, "level2_fullword_" + l2CurrentIndex);
                addScore(250, window.innerWidth / 2, window.innerHeight / 2 - 100);
                spawnFloatingText("MERGED!", window.innerWidth / 2, window.innerHeight / 2 - 100, "merge");
                triggerScreenShake("l2-game-playfield");
                createCelebration();

                l2CurrentIndex++;
                if (l2CurrentIndex >= l2Words.length) {
                    speakText("Level Two Completed! Fantastic!", "level2_win");
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

/* =================================================================
   LEVEL 3: ONSET-RIME SLINGSHOT LOGIC (AIM & FIRE PHYSICS)
   ================================================================= */const l3WordsDatabase = [
    { target: "cat", onset: "c", rime: "at", distractor: "ing", correctIdx: 0 },
    { target: "string", onset: "str", rime: "ing", distractor: "at", correctIdx: 1 },
    { target: "play", onset: "pl", rime: "ay", distractor: "ot", correctIdx: 0 },
    { target: "ship", onset: "sh", rime: "ip", distractor: "ed", correctIdx: 0 },
    { target: "frog", onset: "fr", rime: "og", distractor: "ove", correctIdx: 0 },
    { target: "glove", onset: "gl", rime: "ove", distractor: "og", correctIdx: 1 },
    { target: "brick", onset: "br", rime: "ick", distractor: "ock", correctIdx: 0 },
    { target: "clock", onset: "cl", rime: "ock", distractor: "ick", correctIdx: 1 },
    { target: "spoon", onset: "sp", rime: "oon", distractor: "est", correctIdx: 0 },
    { target: "nest", onset: "n", rime: "est", distractor: "oon", correctIdx: 1 },
    { target: "train", onset: "tr", rime: "ain", distractor: "each", correctIdx: 0 },
    { target: "beach", onset: "b", rime: "each", distractor: "ain", correctIdx: 1 }
];
let l3Words = [];
let l3CurrentIndex = 0;
let l3IsChecking = false;

// Slingshot State
let slingshot = { x: 0, y: 0, radius: 30 };
let projectile = { x: 0, y: 0, z: 0, vx: 0, vy: 0, vz: 0, radius: 25, text: "" };
let balloons = [];
let gravity = 0.15; // Lighter gravity for perspective flight
let maxPull = 110;
let isAiming = false;
let isFlying = false;

function startLevel3() {
    l3Canvas = document.getElementById("l3-canvas");
    l3Ctx = l3Canvas.getContext("2d");
    resizeCanvas(l3Canvas);

    // Randomize the level sequence
    l3Words = shuffleArray([...l3WordsDatabase]).slice(0, 3);

    l3CurrentIndex = 0;
    l3IsChecking = false;
    isAiming = false;
    isFlying = false;

    // Bind pointer events directly to the playfield to allow slingshot dragging
    const playfield = document.getElementById("l3-game-playfield");
    playfield.style.touchAction = "none"; // prevent scrolling while aiming
    
    playfield.onpointerdown = handleL3PointerDown;
    playfield.onpointermove = handleL3PointerMove;
    playfield.onpointerup = handleL3PointerUp;

    loadLevel3Round();
    l3LoopId = requestAnimationFrame(level3Loop);
}

function stopLevel3() {
    if (l3LoopId) {
        cancelAnimationFrame(l3LoopId);
        l3LoopId = null;
    }
    const playfield = document.getElementById("l3-game-playfield");
    if (playfield) {
        playfield.onpointerdown = null;
        playfield.onpointermove = null;
        playfield.onpointerup = null;
    }
}

function loadLevel3Round() {
    const round = l3Words[l3CurrentIndex];
    document.getElementById("l3-target-word").innerText = round.target;
    
    // Announce target (pre-recorded audio)
    const audioIdx = l3WordsDatabase.findIndex(w => w.target === round.target);
    speakText(`Construct the word: ${round.target}`, `level3_target_${audioIdx !== -1 ? audioIdx : 0}`);

    const w = l3Canvas.width;
    const h = l3Canvas.height;

    // Position slingshot (front center)
    slingshot.x = w / 2;
    slingshot.y = h - 90;
    
    projectile.text = round.onset + "-";
    projectile.x = slingshot.x;
    projectile.y = slingshot.y;
    projectile.z = 0;
    projectile.vx = 0;
    projectile.vy = 0;
    projectile.vz = 0;
    
    isAiming = false;
    isFlying = false;
    l3IsChecking = false;

    // Clear old balloons and spawn new ones
    balloons = [];
    const rimes = [
        { text: "-" + round.rime, isCorrect: true, key: "rime_" + round.rime },
        { text: "-" + round.distractor, isCorrect: false, key: "rime_" + round.distractor }
    ];
    // Shuffle rimes
    rimes.sort(() => Math.random() - 0.5);

    rimes.forEach((rime, idx) => {
        // Position on the left and right in the background plane (z = 1.0)
        const bx = w * (0.28 + idx * 0.44);
        const byCenter = h * 0.32; // higher up, near the horizon
        balloons.push({
            text: rime.text,
            isCorrect: rime.isCorrect,
            key: rime.key,
            x: bx,
            y: byCenter,
            floatCenter: byCenter,
            floatOffset: Math.random() * Math.PI,
            radius: 35,
            pop: false,
            color: rime.isCorrect ? 'hsl(195, 100%, 45%)' : 'hsl(330, 95%, 60%)'
        });
    });
}

function handleL3PointerDown(e) {
    if (isFlying || l3IsChecking) return;
    
    const playfieldRect = document.getElementById("l3-game-playfield").getBoundingClientRect();
    const mx = e.clientX - playfieldRect.left;
    const my = e.clientY - playfieldRect.top;

    // Check distance to projectile slingshot rest position
    const dx = mx - slingshot.x;
    const dy = my - slingshot.y;
    const dist = Math.sqrt(dx*dx + dy*dy);

    if (dist < 60) {
        isAiming = true;
        playSynthSound('snap');
    }
}

function handleL3PointerMove(e) {
    if (!isAiming) return;

    const playfieldRect = document.getElementById("l3-game-playfield").getBoundingClientRect();
    const mx = e.clientX - playfieldRect.left;
    const my = e.clientY - playfieldRect.top;

    // Calculate pull offset
    let dx = mx - slingshot.x;
    let dy = my - slingshot.y;

    // Only allow pulling down (towards yourself)
    if (dy < 0) dy = 0;

    const dist = Math.sqrt(dx*dx + dy*dy);

    if (dist > maxPull) {
        dx = (dx / dist) * maxPull;
        dy = (dy / dist) * maxPull;
    }

    projectile.x = slingshot.x + dx;
    projectile.y = slingshot.y + dy;
}

function handleL3PointerUp(e) {
    if (!isAiming) return;
    isAiming = false;

    // Fire!
    const dx = slingshot.x - projectile.x;
    const dy = slingshot.y - projectile.y;
    const pullDist = Math.sqrt(dx*dx + dy*dy);

    if (pullDist > 15) {
        // Set velocities relative to pull
        // vx shoots opposite to horizontal pull, vy shoots upward, vz shoots forward
        projectile.vx = -dx * 0.08;
        projectile.vy = -dy * 0.08 - 2.5; 
        projectile.vz = dy * 0.0003 + 0.012; 
        
        projectile.z = 0;
        isFlying = true;
        playSynthSound('zap');
    } else {
        // Snap back if pull was too small
        projectile.x = slingshot.x;
        projectile.y = slingshot.y;
        projectile.z = 0;
    }
}

function level3Loop() {
    if (!l3LoopId) return;

    l3Ctx.clearRect(0, 0, l3Canvas.width, l3Canvas.height);
    const w = l3Canvas.width;
    const h = l3Canvas.height;

    // Update slingshot base coordinates on window resize
    slingshot.x = w / 2;
    slingshot.y = h - 90;

    // Horizon line height
    const horizonY = h * 0.45;

    // Render beautiful 3D background grids/horizon
    draw3DBackground(l3Ctx, w, h, horizonY);

    // Update balloons floating animation (at depth z = 1.0)
    const time = Date.now() * 0.0025;
    balloons.forEach(b => {
        if (!b.pop) {
            b.y = b.floatCenter + Math.sin(time + b.floatOffset) * 12;
            // Draw balloon (far away, scale is small, let's say 0.35)
            drawBalloon3D(l3Ctx, b, 0.35);
        }
    });

    // Update projectile flight
    if (isFlying) {
        projectile.x += projectile.vx;
        projectile.y += projectile.vy;
        projectile.vy += gravity;
        projectile.z += projectile.vz;

        // Check collision at depth z >= 1.0
        if (projectile.z >= 1.0) {
            // Check collision in projected coordinates
            const scale = 1 - 1.0 * 0.72; // 0.28
            const projX = w / 2 + (projectile.x - w / 2) * scale;
            const projY = horizonY + (projectile.y - horizonY) * scale;

            let hit = false;
            balloons.forEach(b => {
                if (!b.pop) {
                    const dx = projX - b.x;
                    const dy = projY - b.y;
                    const dist = Math.sqrt(dx*dx + dy*dy);
                    // Balloon radius is 35 * 0.35 = 12.25, proj block radius is about 15
                    if (dist < 45) {
                        b.pop = true;
                        isFlying = false;
                        hit = true;
                        checkSlingshotMatch(b);
                    }
                }
            });

            if (!hit) {
                // Fly past horizon or fall down
                if (projectile.y > h + 50 || projectile.z > 1.3) {
                    resetSlingshot(300);
                }
            }
        }
    }

    // Render slingshot forks (in 3D, behind the projectile if pulled, or in front)
    drawSlingshot3D(l3Ctx, w, h, slingshot);

    // Draw elastic bands if aiming
    if (isAiming) {
        drawSlingshotBands3D(l3Ctx, slingshot, projectile);
    }

    // Draw projectile block with perspective scale
    if (!l3IsChecking || isFlying) {
        let blockScale = 1.0;
        let projX = projectile.x;
        let projY = projectile.y;

        if (isAiming) {
            // Scale up slightly as pulled back (closer to camera)
            const dy = projectile.y - slingshot.y;
            blockScale = 1.0 + dy * 0.0035;
        } else if (isFlying) {
            // Interpolate scale down towards the horizon
            blockScale = 1.0 - projectile.z * 0.72; // shrinks to 0.28 at z = 1.0
            projX = w / 2 + (projectile.x - w / 2) * blockScale;
            projY = horizonY + (projectile.y - horizonY) * blockScale;
        } else {
            // Rest position
            projectile.x = slingshot.x;
            projectile.y = slingshot.y;
            projectile.z = 0;
        }

        const blockW = 85 * blockScale;
        const blockH = 45 * blockScale;

        drawConstructorBlock(l3Ctx, {
            x: projX - blockW / 2,
            y: projY - blockH / 2,
            w: blockW,
            h: blockH,
            text: projectile.text,
            scale: blockScale
        }, '#5a3bb6', '#ffff00');
    }

    // Trajectory dots preview
    if (isAiming) {
        drawTrajectory3D(l3Ctx, w, h, horizonY, slingshot, projectile);
    }

    // Particles/explosions update
    updateExplosions(l3Ctx);

    l3LoopId = requestAnimationFrame(level3Loop);
}

function draw3DBackground(ctx, w, h, horizonY) {
    ctx.save();
    // Horizon gradient glow
    const grad = ctx.createLinearGradient(0, horizonY - 100, 0, h);
    grad.addColorStop(0, 'rgba(10, 8, 24, 1.0)');
    grad.addColorStop(0.3, 'rgba(30, 20, 60, 1.0)');
    grad.addColorStop(1.0, 'rgba(10, 8, 20, 1.0)');
    ctx.fillStyle = grad;
    ctx.fillRect(0, 0, w, h);

    // Draw horizon line
    ctx.strokeStyle = 'rgba(0, 191, 255, 0.25)';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(0, horizonY);
    ctx.lineTo(w, horizonY);
    ctx.stroke();

    // Perspective grid lines
    ctx.strokeStyle = 'rgba(138, 43, 226, 0.15)';
    ctx.lineWidth = 2;
    const gridLines = 16;
    for (let i = 0; i <= gridLines; i++) {
        const xOffset = (w / gridLines) * i;
        ctx.beginPath();
        ctx.moveTo(w / 2, horizonY);
        ctx.lineTo(xOffset, h);
        ctx.stroke();
    }

    // Transverse horizontal grid lines
    for (let j = 0; j < 8; j++) {
        const z = j / 8;
        const y = horizonY + (h - horizonY) * Math.pow(z, 2);
        ctx.beginPath();
        ctx.moveTo(0, y);
        ctx.lineTo(w, y);
        ctx.stroke();
    }
    ctx.restore();
}

function drawSlingshot3D(ctx, w, h, slingshot) {
    ctx.save();
    ctx.strokeStyle = '#6f4420'; 
    ctx.lineWidth = 14;
    ctx.lineCap = 'round';
    
    const leftForkX = slingshot.x - 45;
    const rightForkX = slingshot.x + 45;
    const forkY = slingshot.y - 30;

    // Draw left post
    ctx.beginPath();
    ctx.moveTo(slingshot.x - 12, h);
    ctx.lineTo(slingshot.x - 12, slingshot.y + 20);
    ctx.lineTo(leftForkX, forkY);
    ctx.stroke();

    // Draw right post
    ctx.beginPath();
    ctx.moveTo(slingshot.x + 12, h);
    ctx.lineTo(slingshot.x + 12, slingshot.y + 20);
    ctx.lineTo(rightForkX, forkY);
    ctx.stroke();

    // Draw wood caps
    ctx.fillStyle = '#8a5a36';
    ctx.beginPath();
    ctx.arc(leftForkX, forkY, 7, 0, Math.PI * 2);
    ctx.arc(rightForkX, forkY, 7, 0, Math.PI * 2);
    ctx.fill();
    
    ctx.restore();
}

function drawSlingshotBands3D(ctx, slingshot, projectile) {
    ctx.save();
    ctx.strokeStyle = 'rgba(230, 126, 34, 0.85)';
    ctx.lineWidth = 6;
    ctx.lineCap = 'round';

    const leftForkX = slingshot.x - 45;
    const rightForkX = slingshot.x + 45;
    const forkY = slingshot.y - 30;

    ctx.beginPath();
    ctx.moveTo(leftForkX, forkY);
    ctx.lineTo(projectile.x, projectile.y);
    ctx.stroke();

    ctx.beginPath();
    ctx.moveTo(rightForkX, forkY);
    ctx.lineTo(projectile.x, projectile.y);
    ctx.stroke();

    ctx.restore();
}

function drawTrajectory3D(ctx, w, h, horizonY, slingshot, projectile) {
    ctx.save();
    ctx.fillStyle = 'rgba(255, 215, 0, 0.6)';
    
    const dx = slingshot.x - projectile.x;
    const dy = slingshot.y - projectile.y;

    let tx = projectile.x;
    let ty = projectile.y;
    let tz = 0;

    let tvx = -dx * 0.08;
    let tvy = -dy * 0.08 - 2.5;
    let tvz = dy * 0.0003 + 0.012;

    for (let i = 0; i < 25; i++) {
        tx += tvx;
        ty += tvy;
        tvy += gravity;
        tz += tvz;

        if (tz > 1.1) break;

        const scale = 1 - tz * 0.72;
        const projX = w / 2 + (tx - w / 2) * scale;
        const projY = horizonY + (ty - horizonY) * scale;

        ctx.beginPath();
        ctx.arc(projX, projY, 4 * scale, 0, Math.PI * 2);
        ctx.fill();
    }
    ctx.restore();
}

function drawBalloon3D(ctx, b, scale) {
    ctx.save();
    const r = b.radius * scale;
    
    ctx.shadowBlur = 15;
    ctx.shadowColor = b.color;

    // Draw balloon circle body
    ctx.fillStyle = b.color;
    ctx.beginPath();
    ctx.arc(b.x, b.y, r, 0, Math.PI * 2);
    ctx.fill();

    // Knot
    ctx.beginPath();
    ctx.moveTo(b.x, b.y + r);
    ctx.lineTo(b.x - 4, b.y + r + 5);
    ctx.lineTo(b.x + 4, b.y + r + 5);
    ctx.closePath();
    ctx.fill();

    // String
    ctx.strokeStyle = 'rgba(255,255,255,0.25)';
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.moveTo(b.x, b.y + r + 5);
    ctx.quadraticCurveTo(b.x - 3, b.y + r + 15, b.x, b.y + r + 25);
    ctx.stroke();

    // Text
    ctx.fillStyle = "#ffffff";
    ctx.font = `bold ${Math.floor(22 * scale)}px Outfit`;
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillText(b.text, b.x, b.y);

    ctx.restore();
}

function drawConstructorBlock(ctx, block, fillStyle, strokeStyle) {
    ctx.save();
    const scale = block.scale || 1.0;
    ctx.fillStyle = fillStyle || '#5a3bb6';
    ctx.strokeStyle = strokeStyle || '#ffff00';
    ctx.lineWidth = 3 * scale;
    ctx.shadowBlur = 10 * scale;
    ctx.shadowColor = ctx.strokeStyle;
    
    const x = block.x;
    const y = block.y;
    const w = block.w;
    const h = block.h;
    const r = 8 * scale;
    
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + w - r, y);
    ctx.quadraticCurveTo(x + w, y, x + w, y + r);
    ctx.lineTo(x + w, y + h - r);
    ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
    ctx.lineTo(x + r, y + h);
    ctx.quadraticCurveTo(x, y + h, x, y + h - r);
    ctx.lineTo(x, y + r);
    ctx.quadraticCurveTo(x, y, x + r, y);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
    
    // Draw text inside
    ctx.fillStyle = '#ffffff';
    ctx.font = `bold ${Math.floor(18 * scale)}px Outfit`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(block.text, x + w/2, y + h/2);
    ctx.restore();
}

function checkSlingshotMatch(balloon) {
    l3IsChecking = true;
    const round = l3Words[l3CurrentIndex];
    const combinedWord = projectile.text.replace('-', '') + balloon.text.replace('-', '');

    const playfieldRect = document.getElementById("l3-game-playfield").getBoundingClientRect();
    // Balloons are drawn in back coordinates which match canvas coordinates
    const scale = 1 - 1.0 * 0.72; // 0.28
    const horizonY = l3Canvas.height * 0.45;
    const projX = l3Canvas.width / 2 + (projectile.x - l3Canvas.width / 2) * scale;
    const projY = horizonY + (projectile.y - horizonY) * scale;

    const screenX = projX + playfieldRect.left;
    const screenY = projY + playfieldRect.top;

    if (combinedWord === round.target) {
        // SUCCESS
        playSynthSound('success');
        speakText(round.target, "word_" + round.target); // pre-recorded audio

        addScore(200, screenX, screenY);
        createExplosion(projX, projY, '#00ff00');
        createCelebration();
        triggerScreenShake("l3-game-playfield");

        l3CurrentIndex++;
        if (l3CurrentIndex >= l3Words.length) {
            speakText("Level Three Completed! Astounding!", "level3_win");
            setTimeout(() => showScreen("main-menu"), 2500);
        } else {
            setTimeout(loadLevel3Round, 2000);
        }
    } else {
        // FAILURE
        playSynthSound('failure');
        speakText(combinedWord, "word_" + combinedWord); // pre-recorded failure blend audio

        addScore(-50, screenX, screenY);
        spawnFloatingText("TRY AGAIN!", screenX, screenY - 30, "failure");
        triggerScreenShake("l3-game-playfield");
        createExplosion(projX, projY, '#ff0000');

        resetSlingshot(1500);
    }
}

function resetSlingshot(delay) {
    setTimeout(() => {
        projectile.x = slingshot.x;
        projectile.y = slingshot.y;
        projectile.z = 0;
        projectile.vx = 0;
        projectile.vy = 0;
        projectile.vz = 0;
        isFlying = false;
        isAiming = false;
        l3IsChecking = false;
        // Un-pop balloons if failed
        balloons.forEach(b => {
            if (!b.isCorrect) b.pop = false;
        });
    }, delay);
}


/* =================================================================
   LEVEL 4: PHONEME ISOLATOR LOGIC (CRAFTING GRID / DRAG AND DROP)
   ================================================================= */

let l4BaselineWord = "bat";
let l4Phonemes = ["b", "a", "t"];
let l4Inventory = ["c", "p", "o", "t"];
let l4EarnedCards = new Set();

// Rewards dictionary
let l4CardRewards = {};

const l4Configs = [
    {
        baselineWord: "bat",
        phonemes: ["b", "a", "t"],
        inventory: ["c", "p", "o", "t"],
        targetCards: {
            "cat": { name: "Curious Cat Card", art: "🐱", rarity: "Common" },
            "cap": { name: "Captain Cap Card", art: "🧢", rarity: "Uncommon" },
            "cop": { name: "Cool Cop Card", art: "👮", rarity: "Rare" },
            "top": { name: "Spinning Top Card", art: "🌪️", rarity: "Uncommon" },
            "toy": { name: "Shiny Toy Card", art: "🧸", rarity: "Rare" }
        },
        startPrompt: "Start word is: bat. Drag sound bubbles to mutate the word!",
        clipKey: "level4_start_bat"
    },
    {
        baselineWord: "pig",
        phonemes: ["p", "i", "g"],
        inventory: ["n", "e", "a", "f"],
        targetCards: {
            "pin": { name: "Safety Pin Card", art: "🧷", rarity: "Common" },
            "pen": { name: "Ink Pen Card", art: "🖋️", rarity: "Uncommon" },
            "pan": { name: "Frying Pan Card", art: "🍳", rarity: "Common" },
            "fan": { name: "Electric Fan Card", art: "🪭", rarity: "Rare" },
            "fin": { name: "Shark Fin Card", art: "🦈", rarity: "Rare" }
        },
        startPrompt: "Start word is: pig. Drag sound bubbles to mutate the word!",
        clipKey: "level4_start_pig"
    }
];

function startLevel4() {
    l4EarnedCards.clear();

    // Randomly select a configuration for Level 4
    const config = l4Configs[Math.floor(Math.random() * l4Configs.length)];
    l4BaselineWord = config.baselineWord;
    l4Phonemes = [...config.phonemes];
    l4Inventory = [...config.inventory];
    l4CardRewards = config.targetCards;

    // Populate locks on cards
    renderCardCabinet();

    loadLevel4State();
    speakText(config.startPrompt, config.clipKey);
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

            // Calculate document-relative coordinates of the click
            const pageX = e.pageX !== undefined ? e.pageX : (e.clientX + window.scrollX);
            const pageY = e.pageY !== undefined ? e.pageY : (e.clientY + window.scrollY);

            // Get viewport-relative rect of the bubble and convert to document-relative
            const rect = bubble.getBoundingClientRect();
            const bubblePageX = rect.left + window.scrollX;
            const bubblePageY = rect.top + window.scrollY;

            // Calculate offset of the click from the top-left corner of the bubble
            const shiftX = pageX - bubblePageX;
            const shiftY = pageY - bubblePageY;

            const originalParent = bubble.parentElement;
            const origPosition = bubble.style.position;
            const origZIndex = bubble.style.zIndex;
            const origLeft = bubble.style.left;
            const origTop = bubble.style.top;
            const origTransition = bubble.style.transition;

            // Append directly to document.body to escape containing-blocks of backdrop-filters
            document.body.appendChild(bubble);

            bubble.style.position = "absolute";
            bubble.style.zIndex = "1000";
            bubble.style.transition = "none";

            moveAt(pageX, pageY);

            function moveAt(currPageX, currPageY) {
                bubble.style.left = `${currPageX - shiftX}px`;
                bubble.style.top = `${currPageY - shiftY}px`;
            }

            let hoveredSlot = null;

            function onPointerMove(moveEvent) {
                const movePageX = moveEvent.pageX !== undefined ? moveEvent.pageX : (moveEvent.clientX + window.scrollX);
                const movePageY = moveEvent.pageY !== undefined ? moveEvent.pageY : (moveEvent.clientY + window.scrollY);
                moveAt(movePageX, movePageY);

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

                if (slot) {
                    slot.classList.remove('hovered');
                    const slotIdx = parseInt(slot.dataset.index);
                    
                    // Restore original styles
                    bubble.style.transform = "";
                    bubble.style.boxShadow = "";
                    bubble.style.position = origPosition;
                    bubble.style.zIndex = origZIndex;
                    bubble.style.left = origLeft;
                    bubble.style.top = origTop;
                    bubble.style.transition = origTransition;

                    originalParent.appendChild(bubble); // Put it back into normal DOM flow
                    handlePhonemeDrop(letter, slotIdx);
                } else {
                    // Create placeholder to find absolute screen landing target
                    const placeholder = document.createElement("div");
                    placeholder.style.width = "60px";
                    placeholder.style.height = "60px";
                    placeholder.style.visibility = "hidden";
                    originalParent.appendChild(placeholder);
                    
                    const placeholderRect = placeholder.getBoundingClientRect();
                    const placeholderPageX = placeholderRect.left + window.scrollX;
                    const placeholderPageY = placeholderRect.top + window.scrollY;
                    
                    // Animate back to placeholder
                    bubble.style.transition = "all 0.25s ease-out";
                    bubble.style.left = `${placeholderPageX}px`;
                    bubble.style.top = `${placeholderPageY}px`;
                    
                    setTimeout(() => {
                        placeholder.remove();
                        
                        // Restore original styles
                        bubble.style.transform = "";
                        bubble.style.boxShadow = "";
                        bubble.style.position = origPosition;
                        bubble.style.zIndex = origZIndex;
                        bubble.style.left = origLeft;
                        bubble.style.top = origTop;
                        bubble.style.transition = origTransition;
                        
                        originalParent.appendChild(bubble);
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
        speakText(`You crafted: ${candidateWord}`, "level4_craft_" + candidateWord);

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
                speakText("Level Four Completed! Master crafter!", "level4_win");
                createCelebration();
                setTimeout(() => showScreen("main-menu"), 2500);
            }, 1500);
        }
    } else {
        // INVALID MUTATION / GIBBERISH
        playSynthSound('failure');
        speakText(candidateWord, "word_" + candidateWord); // Speak the failed blend (e.g. "bpt")

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
let ambientBubbles = [];
let ambientPopParticles = [];

function initAmbientBackground() {
    const canvas = document.getElementById("ambient-canvas");
    resizeCanvas(canvas);
    
    // Add event listener for mouse push force
    let mouseX = -9999;
    let mouseY = -9999;
    window.addEventListener("mousemove", (e) => {
        const rect = canvas.getBoundingClientRect();
        mouseX = e.clientX - rect.left;
        mouseY = e.clientY - rect.top;
    });

    // Add event listener for bubble pop click
    window.addEventListener("pointerdown", (e) => {
        if (activeScreen !== "main-menu") return;
        const rect = canvas.getBoundingClientRect();
        const mx = e.clientX - rect.left;
        const my = e.clientY - rect.top;

        for (let i = 0; i < ambientBubbles.length; i++) {
            const b = ambientBubbles[i];
            if (b.pop) continue;
            
            const dx = mx - b.x;
            const dy = my - b.y;
            const dist = Math.sqrt(dx*dx + dy*dy);
            
            if (dist < b.r) {
                b.pop = true;
                playSynthSound('slice'); // Pop sound

                // Split into cartoon gravity particles
                const letters = b.text.replace(/\//g, '').split('');
                for (let j = 0; j < 8; j++) {
                    const l = letters[j % letters.length] || '★';
                    ambientPopParticles.push({
                        x: b.x,
                        y: b.y,
                        vx: (Math.random() - 0.5) * 7,
                        vy: -3 - Math.random() * 5,
                        text: l,
                        color: b.color.replace('0.45', '0.9'),
                        alpha: 1.0,
                        size: 14 + Math.random() * 10,
                        rotation: Math.random() * Math.PI * 2,
                        rotSpeed: (Math.random() - 0.5) * 0.2
                    });
                }

                // Respawn bubble at bottom after 1s
                setTimeout(() => {
                    b.x = Math.random() * canvas.width;
                    b.y = canvas.height + b.r + 50;
                    b.vx = (Math.random() - 0.5) * 1.5;
                    b.vy = -(0.5 + Math.random() * 1.2);
                    b.pop = false;
                }, 1200);

                break; // pop only one bubble per click
            }
        }
    });

    window.addEventListener("resize", () => {
        resizeCanvas(canvas);
        const l1 = document.getElementById("l1-canvas"); if (l1 && activeScreen === 'level-1') resizeCanvas(l1);
        const l2 = document.getElementById("l2-canvas"); if (l2 && activeScreen === 'level-2') resizeCanvas(l2);
        const l3 = document.getElementById("l3-canvas"); if (l3 && activeScreen === 'level-3') resizeCanvas(l3);
        initDiagnosticsCanvas();
    });

    const ctx = canvas.getContext("2d");
    
    // Spawn ambient dots (stars)
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

    const bubbleSounds = ["/a/", "/t/", "/sh/", "/co/", "/ing/", "/ba/", "/na/", "/pho/", "/ics/", "/ed/", "/c/", "/p/"];
    function spawnMenuBubbles() {
        ambientBubbles = [];
        for (let i = 0; i < 10; i++) {
            ambientBubbles.push({
                x: Math.random() * canvas.width,
                y: canvas.height + 40 + Math.random() * 180,
                vx: (Math.random() - 0.5) * 1.5,
                vy: -(0.5 + Math.random() * 1.2),
                r: 32 + Math.random() * 16,
                text: bubbleSounds[i % bubbleSounds.length],
                color: `hsla(${200 + Math.random() * 140}, 85%, 60%, 0.45)`,
                pop: false
            });
        }
    }

    function ambientLoop() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        
        // Stars update
        ambientStars.forEach(star => {
            star.y -= star.speed;
            if (star.y < 0) {
                star.y = canvas.height;
                star.x = Math.random() * canvas.width;
            }

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

        // Main Menu Interactive Bubbles
        if (activeScreen === "main-menu") {
            if (ambientBubbles.length === 0) {
                spawnMenuBubbles();
            }

            // Update & Draw Bubbles
            ambientBubbles.forEach(b => {
                if (b.pop) return;

                // Move
                b.x += b.vx;
                b.y += b.vy;

                // Gentle cursor push force
                const dx = b.x - mouseX;
                const dy = b.y - mouseY;
                const dist = Math.sqrt(dx*dx + dy*dy);
                if (dist < b.r + 60) {
                    const force = (1 - dist / (b.r + 60)) * 0.4;
                    b.vx += (dx / dist) * force;
                    b.vy += (dy / dist) * force;
                }

                // Bounce off left/right
                if (b.x < b.r) { b.x = b.r; b.vx *= -0.8; }
                if (b.x > canvas.width - b.r) { b.x = canvas.width - b.r; b.vx *= -0.8; }

                // Reset at top to float back up from bottom
                if (b.y < -b.r) {
                    b.y = canvas.height + b.r + Math.random() * 50;
                    b.x = Math.random() * canvas.width;
                    b.vx = (Math.random() - 0.5) * 1.5;
                    b.vy = -(0.5 + Math.random() * 1.2);
                }

                // Speed limits
                const speed = Math.sqrt(b.vx*b.vx + b.vy*b.vy);
                if (speed > 2.5) {
                    b.vx = (b.vx / speed) * 2.5;
                    b.vy = (b.vy / speed) * 2.5;
                }

                // Render shiny radial gradient bubble
                ctx.save();
                const bubbleGrad = ctx.createRadialGradient(
                    b.x - b.r * 0.3, b.y - b.r * 0.3, b.r * 0.1,
                    b.x, b.y, b.r
                );
                bubbleGrad.addColorStop(0, 'rgba(255, 255, 255, 0.55)');
                bubbleGrad.addColorStop(0.3, b.color);
                bubbleGrad.addColorStop(1, b.color.replace('0.45', '0.15'));
                
                ctx.fillStyle = bubbleGrad;
                ctx.strokeStyle = b.color.replace('0.45', '0.85');
                ctx.lineWidth = 2.5;
                
                ctx.beginPath();
                ctx.arc(b.x, b.y, b.r, 0, Math.PI * 2);
                ctx.fill();
                ctx.stroke();

                // Highlight gloss arc
                ctx.strokeStyle = 'rgba(255,255,255,0.45)';
                ctx.lineWidth = 2.5;
                ctx.beginPath();
                ctx.arc(b.x - b.r*0.1, b.y - b.r*0.1, b.r * 0.7, Math.PI * 1.05, Math.PI * 1.45);
                ctx.stroke();

                // Text
                ctx.fillStyle = '#ffffff';
                ctx.font = 'bold 15px Outfit';
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.fillText(b.text, b.x, b.y);
                ctx.restore();
            });

            // Update & Draw Cartoon Pop Particles
            ambientPopParticles.forEach((p, idx) => {
                p.x += p.vx;
                p.y += p.vy;
                p.vy += 0.2; // Gravity
                p.alpha -= 0.025;
                p.rotation += p.rotSpeed;

                if (p.alpha <= 0) {
                    ambientPopParticles.splice(idx, 1);
                    return;
                }

                ctx.save();
                ctx.globalAlpha = p.alpha;
                ctx.fillStyle = p.color;
                ctx.font = `bold ${p.size}px Outfit`;
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.translate(p.x, p.y);
                ctx.rotate(p.rotation);
                ctx.fillText(p.text, 0, 0);
                ctx.restore();
            });
        } else {
            // Free memory when not on menu
            if (ambientBubbles.length > 0) ambientBubbles = [];
            if (ambientPopParticles.length > 0) ambientPopParticles = [];
        }

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
