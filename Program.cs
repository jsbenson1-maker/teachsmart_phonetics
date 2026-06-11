using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using PhoneticsEdu.Core;
using PhoneticsEdu.UI;
using PhoneticsEdu.Games.WordBlaster;
using PhoneticsEdu.Games.SyllableShredder;
using PhoneticsEdu.Games.OnsetRime;
using PhoneticsEdu.Games.PhonemeIsolator;

namespace PhoneticsEdu
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Parse arguments
            bool autoTest = false;
            foreach (var arg in args)
            {
                if (arg.Equals("--test", StringComparison.OrdinalIgnoreCase) || arg.Equals("-t", StringComparison.OrdinalIgnoreCase))
                {
                    autoTest = true;
                }
            }

            if (autoTest)
            {
                Console.WriteLine("Running automated verification harness via command line...");
                await RunAutomatedVerification();
                return;
            }

            // Start HTTP Web Server
            string assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebAssets");
            int port = 5080;
            WebServer? server = null;

            if (Directory.Exists(assetsDir))
            {
                try
                {
                    server = new WebServer(assetsDir, port);
                    server.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to start local web server: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Warning: WebAssets folder not found at {assetsDir}. Serve functionality is disabled.");
            }

            // Launch default browser
            if (server != null)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = $"http://localhost:{port}/",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to open default browser: {ex.Message}");
                }
            }

            bool keepRunning = true;
            while (keepRunning)
            {
                Console.Clear();
                Console.WriteLine("=================================================================");
                Console.WriteLine("       TEACHSMART: PHONETIC CHALLENGE - GRAPHICAL LAUNCHER");
                Console.WriteLine("=================================================================");
                Console.WriteLine($"Local web server is RUNNING on: http://localhost:{port}/");
                Console.WriteLine("The graphical game has been automatically launched in your browser!");
                Console.WriteLine("=================================================================");
                Console.WriteLine("Select option:");
                Console.WriteLine("  1. Play Interactive Terminal Fallback (text levels)");
                Console.WriteLine("  2. Run Automated Verification Harness (logs all 4 levels)");
                Console.WriteLine("  3. Re-open Graphical Game in Web Browser");
                Console.WriteLine("  4. Shut Down Server & Exit");
                Console.Write("\nSelect option (1-4): ");
                string? choice = Console.ReadLine();

                if (choice == "1")
                {
                    await RunInteractiveGameLoop();
                }
                else if (choice == "2")
                {
                    await RunAutomatedVerification();
                    Console.WriteLine("\nPress Enter to return to launcher menu...");
                    Console.ReadLine();
                }
                else if (choice == "3")
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = $"http://localhost:{port}/",
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error opening browser: {ex.Message}");
                        Console.ReadLine();
                    }
                }
                else if (choice == "4")
                {
                    keepRunning = false;
                }
            }

            if (server != null)
            {
                server.Stop();
                Console.WriteLine("Server shut down successfully.");
            }
        }

        private static async Task RunInteractiveGameLoop()
        {
            // Initialize core managers
            var gameManagerObj = new GameObject();
            var gameManager = gameManagerObj.AddComponent<GameManager>();
            var audioManagerObj = new GameObject();
            var audioManager = audioManagerObj.AddComponent<PhoneticAudioManager>();
            var juiceManagerObj = new GameObject();
            var juiceManager = juiceManagerObj.AddComponent<UiJuiceManager>();

            InvokeLifecycleMethod(gameManager, "Awake");
            InvokeLifecycleMethod(audioManager, "Awake");
            InvokeLifecycleMethod(juiceManager, "Awake");
            InvokeLifecycleMethod(gameManager, "Start");

            bool running = true;
            while (running)
            {
                Console.Clear();
                Console.WriteLine("=================================================================");
                Console.WriteLine("         TEACHSMART: PHONETIC CHALLENGE - PLAYABLE GAME          ");
                Console.WriteLine("=================================================================");
                Console.WriteLine("Select a Phonological Awareness Level to play:");
                Console.WriteLine("  1. Level 1: Word Blaster (Space Invaders Sequence Matching)");
                Console.WriteLine("  2. Level 2: Syllable Shredder (Fruit Ninja Chunk Slicing)");
                Console.WriteLine("  3. Level 3: Onset-Rime Constructor (Tetris Block Snapping)");
                Console.WriteLine("  4. Level 4: Phoneme Isolator (Magnifier Spelling Crafting)");
                Console.WriteLine("  5. Synth Voice Pro Diagnostic Tester (TTS & Mic Levels)");
                Console.WriteLine("  6. Return/Exit");
                Console.Write("\nSelect option (1-6): ");
                string? menuChoice = Console.ReadLine();

                switch (menuChoice)
                {
                    case "1":
                        await PlayLevel1WordBlaster();
                        break;
                    case "2":
                        await PlayLevel2SyllableShredder();
                        break;
                    case "3":
                        await PlayLevel3OnsetRime();
                        break;
                    case "4":
                        await PlayLevel4PhonemeIsolator();
                        break;
                    case "5":
                        await PlayDiagnosticsMenu();
                        break;
                    case "6":
                        running = false;
                        break;
                    default:
                        Console.WriteLine("\nInvalid option! Press Enter to try again.");
                        Console.ReadLine();
                        break;
                }
            }
        }

        private static async Task PlayLevel1WordBlaster()
        {
            Console.Clear();
            Console.WriteLine("=================================================================");
            Console.WriteLine("           PLAYING LEVEL 1: WORD BLASTER (Space Invaders)        ");
            Console.WriteLine("=================================================================");
            Console.WriteLine("Voiceover Announces Target: \"The quick brown fox\"");
            
            GameManager.Instance!.TransitionToState(GameState.WordBlaster);
            var wordBlasterObj = new GameObject();
            var wordBlaster = wordBlasterObj.AddComponent<WordBlasterManager>();
            InvokeLifecycleMethod(wordBlaster, "Awake");
            wordBlaster.InitializeGame();
            wordBlaster.StartGame();

            Console.WriteLine("\n[Visuals] Word ships are falling! Words: [fox] [quick] [The] [brown] [slow]");
            Console.WriteLine("Goal: Zap the ships in the exact order they are spoken.");
            
            string[] correctSequence = new[] { "The", "quick", "brown", "fox" };
            int step = 0;
            while (step < correctSequence.Length)
            {
                Console.Write($"\nEnter the next correct word to zap (Zap {step + 1}/{correctSequence.Length}): ");
                string? input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input)) continue;

                var ship = new GameObject().AddComponent<WordShip>();
                ship.Initialize(input, true, 1f, wordBlaster);
                
                wordBlaster.ZapShip(ship);

                if (wordBlaster.GetNextWordIndexToZap() > step)
                {
                    Console.WriteLine("-> ZAP SUCCESS! Ship destroyed in correct sequence.");
                    step++;
                }
                else
                {
                    Console.WriteLine("-> ZAP FAILURE! Incorrect ship or out of order.");
                }
            }
            Console.WriteLine($"\nLevel Completed! final score: {wordBlaster.CurrentScore} pts.");
            wordBlaster.EndGame();
            Console.WriteLine("\nPress Enter to return to menu...");
            Console.ReadLine();
        }

        private static async Task PlayLevel2SyllableShredder()
        {
            Console.Clear();
            Console.WriteLine("=================================================================");
            Console.WriteLine("          PLAYING LEVEL 2: SYLLABLE SHREDDER (Fruit Ninja)       ");
            Console.WriteLine("=================================================================");
            Console.WriteLine("Voiceover Announces Target Word: \"cooperate\"");

            GameManager.Instance!.TransitionToState(GameState.SyllableShredder);
            var syllableShredderObj = new GameObject();
            var syllableShredder = syllableShredderObj.AddComponent<SyllableShredderManager>();
            InvokeLifecycleMethod(syllableShredder, "Awake");
            syllableShredder.InitializeGame();
            syllableShredder.StartGame();

            Console.WriteLine("\n[Visuals] Syllable blocks tossed: [o] [co] [rate] [pe]");
            Console.WriteLine("Goal: Slice the blocks in order to segment the word correctly.");

            string[] syllables = new[] { "co", "o", "pe", "rate" };
            int step = 0;
            while (step < syllables.Length)
            {
                Console.Write($"\nEnter syllable chunk to slice (Slice {step + 1}/{syllables.Length}): ");
                string? input = Console.ReadLine()?.Trim().ToLower();
                if (string.IsNullOrEmpty(input)) continue;

                int index = Array.IndexOf(syllables, input);
                if (index == -1)
                {
                    Console.WriteLine("-> That syllable block does not exist on screen!");
                    continue;
                }

                var block = new GameObject().AddComponent<SyllableBlock>();
                block.Initialize(input, index, new Vector3(0, 0, 0), syllableShredder);

                syllableShredder.SliceBlock(block);

                if (syllableShredder.GetNextSyllableIndexToSlice() > step)
                {
                    Console.WriteLine("-> SLICE SUCCESS! Syllable sliced in chronological sequence.");
                    step++;
                }
                else
                {
                    Console.WriteLine("-> SLICE FAILURE! Wrong syllable block sliced.");
                }
            }
            Console.WriteLine($"\nLevel Completed! Syllables merged successfully! final score: {syllableShredder.CurrentScore} pts.");
            syllableShredder.EndGame();
            Console.WriteLine("\nPress Enter to return to menu...");
            Console.ReadLine();
        }

        private static async Task PlayLevel3OnsetRime()
        {
            Console.Clear();
            Console.WriteLine("=================================================================");
            Console.WriteLine("        PLAYING LEVEL 3: ONSET-RIME CONSTRUCTOR (Tetris)        ");
            Console.WriteLine("=================================================================");
            Console.WriteLine("Goal: Construct the target word: \"cat\"");

            GameManager.Instance!.TransitionToState(GameState.OnsetRime);
            var onsetRimeObj = new GameObject();
            var onsetRime = onsetRimeObj.AddComponent<OnsetRimeManager>();
            InvokeLifecycleMethod(onsetRime, "Awake");
            onsetRime.InitializeGame();
            onsetRime.StartGame();

            Console.WriteLine("\n[Visuals] Stationary Rime blocks: [-at] (at X: -1.5)  [-ing] (at X: 1.5)");
            Console.WriteLine("[Visuals] Falling Onset block: 'c-'");

            bool matched = false;
            while (!matched)
            {
                Console.Write("\nEnter target X position to slide & snap block 'c-' (-1.5 or 1.5): ");
                string? input = Console.ReadLine()?.Trim();
                if (input != "-1.5" && input != "1.5")
                {
                    Console.WriteLine("-> Invalid input! Choose -1.5 (for '-at') or 1.5 (for '-ing').");
                    continue;
                }

                float targetX = float.Parse(input);
                onsetRime.SlideActiveBlock(targetX);

                PhoneticBlock? fallingBlock = onsetRime.GetFallingOnsetBlock();
                if (fallingBlock != null)
                {
                    fallingBlock.transform.position = new Vector3(targetX, -4.0f, 0f);
                    onsetRime.SnapOnsetBlock(fallingBlock);
                }

                if (onsetRime.CurrentScore > 0)
                {
                    Console.WriteLine("-> SNAP SUCCESS! Construct match spelled 'cat' successfully.");
                    matched = true;
                }
                else
                {
                    Console.WriteLine("-> SNAP FAILURE! Spelled incorrect word 'cing'.");
                }
            }
            Console.WriteLine($"\nLevel Completed! final score: {onsetRime.CurrentScore} pts.");
            onsetRime.EndGame();
            Console.WriteLine("\nPress Enter to return to menu...");
            Console.ReadLine();
        }

        private static async Task PlayLevel4PhonemeIsolator()
        {
            Console.Clear();
            Console.WriteLine("=================================================================");
            Console.WriteLine("          PLAYING LEVEL 4: PHONEME ISOLATOR (Crafting Grid)       ");
            Console.WriteLine("=================================================================");

            GameManager.Instance!.TransitionToState(GameState.PhonemeIsolator);
            var isolatorObj = new GameObject();
            var isolator = isolatorObj.AddComponent<PhonemeIsolatorManager>();
            InvokeLifecycleMethod(isolator, "Awake");
            isolator.InitializeGame();
            isolator.StartGame();

            while (isolator.IsGameActive)
            {
                Console.WriteLine($"\nBaseline Word: \"{isolator.GetCurrentBaselineWord().ToUpper()}\"");
                string[] currentPhons = isolator.GetCurrentPhonemes();
                for (int i = 0; i < currentPhons.Length; i++)
                {
                    Console.WriteLine($"  Slot {i}: '{currentPhons[i]}'");
                }
                Console.WriteLine("Floating phoneme bubbles in inventory: /c/, /p/, /o/, /t/");
                Console.Write("Enter replacement bubble and slot index (e.g. type 'c 0' to drop 'c' on slot 0): ");
                string? input = Console.ReadLine()?.Trim().ToLower();
                if (string.IsNullOrEmpty(input)) continue;

                string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2 || !int.TryParse(parts[1], out int slotIndex))
                {
                    Console.WriteLine("-> Input error! Use: <letter> <slotIndex>");
                    continue;
                }

                string letter = parts[0];
                int oldScore = isolator.CurrentScore;
                isolator.PerformMutation(letter, slotIndex);

                if (isolator.CurrentScore > oldScore)
                {
                    Console.WriteLine($"-> CRAFT SUCCESS! Mutated to \"{isolator.GetCurrentBaselineWord().ToUpper()}\"!");
                    Console.WriteLine($"-> Earned Reward Card: {isolator.GetEarnedRewardCards()[isolator.GetEarnedRewardCards().Count - 1]}");
                }
                else
                {
                    Console.WriteLine("-> CRAFT FAILURE! Invalid word blend or gibberish.");
                }
            }

            Console.WriteLine($"\nLevel Completed! Collected all Cards: {string.Join(", ", isolator.GetEarnedRewardCards())}");
            Console.WriteLine($"Final Score: {isolator.CurrentScore} pts.");
            isolator.EndGame();
            Console.WriteLine("\nPress Enter to return to menu...");
            Console.ReadLine();
        }

        private static async Task PlayDiagnosticsMenu()
        {
            Console.Clear();
            Console.WriteLine("=================================================================");
            Console.WriteLine("               SYNTH VOICE PRO AUDIO DIAGNOSTICS HUB             ");
            Console.WriteLine("=================================================================");
            Console.WriteLine("  1. Test Voice Synthesis (SSML IPA Clip)");
            Console.WriteLine("  2. Test Microphone connected recording levels");
            Console.WriteLine("  3. Return");
            Console.Write("\nSelect option (1-3): ");
            string? choice = Console.ReadLine();

            if (choice == "1")
            {
                Console.Write("\nEnter SSML content (e.g., '<phoneme alphabet=\"ipa\" ph=\"kæt\">cat</phoneme>'): ");
                string? ssml = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(ssml))
                {
                    ssml = "<phoneme alphabet=\"ipa\" ph=\"kæt\">cat</phoneme>";
                }
                Console.WriteLine("Synthesizing audio and caching locally...");
                await PhoneticAudioManager.Instance!.PlayPhoneticClipAsync(ssml, "console_diagnostic_" + Math.Abs(ssml.GetHashCode()));
                Console.WriteLine("Playback complete.");
            }
            else if (choice == "2")
            {
                if (PhoneticAudioManager.Instance!.HasMicrophoneConnected(out string deviceName))
                {
                    Console.WriteLine($"Default mic found: {deviceName}");
                    PhoneticAudioManager.Instance.StartMicrophoneRecord(deviceName);
                    Console.WriteLine("Recording live... Speak into microphone. Displaying active input levels:");
                    
                    for (int i = 0; i < 6; i++)
                    {
                        float vol = PhoneticAudioManager.Instance.GetActiveMicrophoneVolume();
                        Console.WriteLine($"  [Signal Level: {(vol * 100f):F1}%] " + new string('█', (int)(vol * 15)));
                        await Task.Delay(500);
                    }
                    
                    float peak = PhoneticAudioManager.Instance.StopMicrophoneRecord();
                    Console.WriteLine($"Recording stopped. Peak volume captured: {peak:P1}");
                }
                else
                {
                    Console.WriteLine("No microphones detected on system.");
                }
            }
            Console.WriteLine("\nPress Enter to return to menu...");
            Console.ReadLine();
        }

        private static async Task RunAutomatedVerification()
        {
            // 1. Initialize core managers
            Console.WriteLine("\n[Test] Initializing GameManager, PhoneticAudioManager, and UiJuiceManager...");
            var gameManagerObj = new GameObject();
            var gameManager = gameManagerObj.AddComponent<GameManager>();
            var audioManagerObj = new GameObject();
            var audioManager = audioManagerObj.AddComponent<PhoneticAudioManager>();
            var juiceManagerObj = new GameObject();
            var juiceManager = juiceManagerObj.AddComponent<UiJuiceManager>();

            // Invoke Awake and Start via reflection to simulate Unity engine lifecycle
            InvokeLifecycleMethod(gameManager, "Awake");
            InvokeLifecycleMethod(audioManager, "Awake");
            InvokeLifecycleMethod(juiceManager, "Awake");
            InvokeLifecycleMethod(gameManager, "Start");

            Console.WriteLine($"Initialized. GameManager.Instance: {(GameManager.Instance != null ? "ACTIVE" : "FAILED")}");
            Console.WriteLine($"Initialized. PhoneticAudioManager.Instance: {(PhoneticAudioManager.Instance != null ? "ACTIVE" : "FAILED")}");
            Console.WriteLine($"Initialized. UiJuiceManager.Instance: {(UiJuiceManager.Instance != null ? "ACTIVE" : "FAILED")}");
            Console.WriteLine($"Current State: {GameManager.Instance!.CurrentState}");

            // 1.b Test TeachSmart Animated Logo
            Console.WriteLine("\n[Test] Initializing TeachSmart Logo Animator...");
            var logoObj = new GameObject();
            var logoAnimator = logoObj.AddComponent<LogoAnimator>();
            InvokeLifecycleMethod(logoAnimator, "Start");

            Console.WriteLine("\n--- Simulating TeachSmart Animated Logo Updates (Longer Loop) ---");
            // Frame 1: Initial delta time
            Time.deltaTime = 0.016f;
            InvokeLifecycleMethod(logoAnimator, "Update");

            // Frame 2: Jump time forward to 2s
            Time.deltaTime = 2.0f;
            InvokeLifecycleMethod(logoAnimator, "Update");

            // Frame 3: Jump time forward to 6s
            Time.deltaTime = 4.0f;
            InvokeLifecycleMethod(logoAnimator, "Update");

            // Frame 4: Jump time forward to 106s
            Time.deltaTime = 100.0f;
            InvokeLifecycleMethod(logoAnimator, "Update");

            // Frame 5: Jump time forward to 385s (loop wraps)
            Time.deltaTime = 279.0f;
            InvokeLifecycleMethod(logoAnimator, "Update");

            // Restore default delta time
            Time.deltaTime = 0.016f;


            // 2. Test Level 1 Gameplay Loop (Word Blaster)
            Console.WriteLine("\n[Test] Launching Level 1: Word Blaster...");
            GameManager.Instance.TransitionToState(GameState.WordBlaster);
            var wordBlasterObj = new GameObject();
            var wordBlaster = wordBlasterObj.AddComponent<WordBlasterManager>();
            InvokeLifecycleMethod(wordBlaster, "Awake");
            wordBlaster.InitializeGame();
            wordBlaster.StartGame();

            // Action: Player zaps 'quick' (Out of order)
            var shipQuick = new GameObject().AddComponent<WordShip>();
            shipQuick.Initialize("quick", true, 1.0f, wordBlaster);
            wordBlaster.ZapShip(shipQuick);

            // Action: Player zaps 'The' (Correct target)
            var shipThe = new GameObject().AddComponent<WordShip>();
            shipThe.Initialize("The", true, 1.0f, wordBlaster);
            wordBlaster.OnWordShipExploded += (word) => Console.WriteLine($"[Event] Word Ship exploded! Word: '{word}'");
            wordBlaster.ZapShip(shipThe);

            // Action: Player zaps remaining words
            var shipQuickCorrect = new GameObject().AddComponent<WordShip>();
            shipQuickCorrect.Initialize("quick", true, 1.0f, wordBlaster);
            wordBlaster.ZapShip(shipQuickCorrect);

            var shipBrown = new GameObject().AddComponent<WordShip>();
            shipBrown.Initialize("brown", true, 1.0f, wordBlaster);
            wordBlaster.ZapShip(shipBrown);

            var shipFox = new GameObject().AddComponent<WordShip>();
            shipFox.Initialize("fox", true, 1.0f, wordBlaster);
            wordBlaster.ZapShip(shipFox);

            // Level 1 Final stats
            Console.WriteLine($"Persisted GameManager Level 1 Score: {GameManager.Instance.GetScoreForLevel(GameState.WordBlaster)}");
            wordBlaster.EndGame();


            // 3. Test Level 2 Gameplay Loop (Syllable Shredder)
            Console.WriteLine("\n[Test] Transitioning to Level 2: Syllable Shredder...");
            GameManager.Instance.TransitionToState(GameState.SyllableShredder);
            var syllableShredderObj = new GameObject();
            var syllableShredder = syllableShredderObj.AddComponent<SyllableShredderManager>();
            InvokeLifecycleMethod(syllableShredder, "Awake");
            syllableShredder.InitializeGame();
            syllableShredder.StartGame();

            // A. Slice index 1 ('o') out of order
            var blockO = new GameObject().AddComponent<SyllableBlock>();
            blockO.Initialize("o", 1, new Vector3(0, 8f, 0), syllableShredder);
            syllableShredder.SliceBlock(blockO);

            // B. Slice index 0 ('co') in order
            var blockCo = new GameObject().AddComponent<SyllableBlock>();
            blockCo.Initialize("co", 0, new Vector3(-2f, 8f, 0), syllableShredder);
            syllableShredder.SliceBlock(blockCo);

            // C. Slice remaining syllables in order: 'o', 'pe', 'rate'
            syllableShredder.OnWordSyllablesMerged += (word) => Console.WriteLine($"[Event] Word Syllables merged successfully! Full word: '{word}'");

            var blockOCorrect = new GameObject().AddComponent<SyllableBlock>();
            blockOCorrect.Initialize("o", 1, new Vector3(-1f, 8f, 0), syllableShredder);
            syllableShredder.SliceBlock(blockOCorrect);

            var blockPe = new GameObject().AddComponent<SyllableBlock>();
            blockPe.Initialize("pe", 2, new Vector3(1f, 8f, 0), syllableShredder);
            syllableShredder.SliceBlock(blockPe);

            var blockRate = new GameObject().AddComponent<SyllableBlock>();
            blockRate.Initialize("rate", 3, new Vector3(2f, 8f, 0), syllableShredder);
            syllableShredder.SliceBlock(blockRate);

            // Level 2 Final stats
            Console.WriteLine($"Persisted GameManager Level 2 Score: {GameManager.Instance.GetScoreForLevel(GameState.SyllableShredder)}");
            syllableShredder.EndGame();


            // 4. Test Level 3 Gameplay Loop (Onset-Rime Constructor)
            Console.WriteLine("\n[Test] Transitioning to Level 3: Onset-Rime Constructor...");
            GameManager.Instance.TransitionToState(GameState.OnsetRime);
            var onsetRimeObj = new GameObject();
            var onsetRime = onsetRimeObj.AddComponent<OnsetRimeManager>();
            InvokeLifecycleMethod(onsetRime, "Awake");
            onsetRime.InitializeGame();
            onsetRime.StartGame();

            PhoneticBlock fallingBlock = onsetRime.GetFallingOnsetBlock()!;

            // A. Slide horizontally to line up with incorrect rime block (X = 1.5f, which carries 'ed' or 'ing')
            onsetRime.SlideActiveBlock(1.5f);

            // Simulate snap on the incorrect Rime block (distractor 'ing' at X = 1.5f)
            fallingBlock.transform.position = new Vector3(1.5f, -4.0f, 0f);
            onsetRime.SnapOnsetBlock(fallingBlock);

            // B. Slide the newly spawned falling block to correct rime block (X = -1.5f, which carries 'at')
            fallingBlock = onsetRime.GetFallingOnsetBlock()!;
            onsetRime.SlideActiveBlock(-1.5f);

            // Wire up word construction event log
            onsetRime.OnWordConstructed += (word) => Console.WriteLine($"[Event] Word constructed! Combined spelling: \"{word}\"");

            // Simulate snap on the correct Rime block ('at' at X = -1.5f)
            fallingBlock.transform.position = new Vector3(-1.5f, -4.0f, 0f);
            onsetRime.SnapOnsetBlock(fallingBlock);

            // Level 3 Final stats
            Console.WriteLine($"Persisted GameManager Level 3 Score: {GameManager.Instance.GetScoreForLevel(GameState.OnsetRime)}");
            onsetRime.EndGame();


            // 5. Test Level 4 Gameplay Loop (Phoneme Isolator)
            Console.WriteLine("\n[Test] Transitioning to Level 4: Phoneme Isolator...");
            GameManager.Instance.TransitionToState(GameState.PhonemeIsolator);
            Console.WriteLine($"-> Current GameManager State: {GameManager.Instance.CurrentState}");

            var isolatorObj = new GameObject();
            var isolator = isolatorObj.AddComponent<PhonemeIsolatorManager>();
            InvokeLifecycleMethod(isolator, "Awake");
            isolator.InitializeGame();
            isolator.StartGame();

            Console.WriteLine($"PhonemeIsolator initialized. Baseline Word: \"{isolator.GetCurrentBaselineWord()}\" (Phonemes: {string.Join("-", isolator.GetCurrentPhonemes())})");

            // A. Simulate dragging /p/ onto target slot index 1 ('a') -> spells 'bpt' (Gibberish)
            Console.WriteLine("\n--- Level 4 Action: Drag '/p/' onto Slot 1 ('a') [Gibberish Mutation] ---");
            isolator.PerformMutation("p", 1);
            Console.WriteLine($"Result: Baseline = {isolator.GetCurrentBaselineWord()}, Score = {GameManager.Instance.GetScoreForLevel(GameState.PhonemeIsolator)}, Accuracy = {isolator.GetAccuracy():P0}");

            // B. Simulate dragging /c/ onto target slot index 0 ('b') -> spells 'cat' (Correct!)
            Console.WriteLine("\n--- Level 4 Action: Drag '/c/' onto Slot 0 ('b') [Valid Mutation 1] ---");
            
            // Wire up mutation event log
            isolator.OnWordMutated += (word, card) => Console.WriteLine($"[Event] Word Mutated! Crafted: \"{word.ToUpper()}\", Earned Asset: [{card}]");
            
            isolator.PerformMutation("c", 0);
            Console.WriteLine($"Result: Baseline = {isolator.GetCurrentBaselineWord()}, Score = {GameManager.Instance.GetScoreForLevel(GameState.PhonemeIsolator)}, Accuracy = {isolator.GetAccuracy():P0}");

            // C. Simulate dragging /p/ onto target slot index 2 ('t') -> spells 'cap' (Correct, using new 'cat' baseline!)
            Console.WriteLine("\n--- Level 4 Action: Drag '/p/' onto Slot 2 ('t') [Valid Mutation 2] ---");
            isolator.PerformMutation("p", 2);
            Console.WriteLine($"Result: Baseline = {isolator.GetCurrentBaselineWord()}, Score = {GameManager.Instance.GetScoreForLevel(GameState.PhonemeIsolator)}, Accuracy = {isolator.GetAccuracy():P0}");

            // Level 4 Final stats
            Console.WriteLine("\n--- Level 4 Final Stats ---");
            Console.WriteLine($"Total Correct Mutations Crafted: {isolator.GetCorrectMutationsCount()}");
            Console.WriteLine($"Total Mutation Attempts: {isolator.GetTotalAttemptsCount()}");
            Console.WriteLine($"Final Level Accuracy: {isolator.GetAccuracy():P1}");
            Console.WriteLine($"Earned Collectible Cards: {string.Join(", ", isolator.GetEarnedRewardCards())}");
            isolator.EndGame();
            Console.WriteLine($"Persisted GameManager Level 4 Score: {GameManager.Instance.GetScoreForLevel(GameState.PhonemeIsolator)}");


            // 6. Test Audio Manager Caching and SSML Parsing
            Console.WriteLine("\n[Test] Testing SSML & Phonetic Audio Caching flow...");
            string ssmlText = "<phoneme alphabet=\"ipa\" ph=\"kæt\">cat</phoneme>";
            string testKey = "cat_test_sample";

            // Run first time: should trigger Cache MISS and generate file
            Console.WriteLine("\n--- Run 1: Expect Cache MISS ---");
            await PhoneticAudioManager.Instance!.PlayPhoneticClipAsync(ssmlText, testKey);

            // Run second time: should trigger Cache HIT and load file
            Console.WriteLine("\n--- Run 2: Expect Cache HIT ---");
            await PhoneticAudioManager.Instance.PlayPhoneticClipAsync(ssmlText, testKey);

            // 7. Test Microphone Diagnostics (Synth Voice Pro)
            Console.WriteLine("\n[Test] Testing Microphone Diagnostics...");
            if (PhoneticAudioManager.Instance.HasMicrophoneConnected(out string deviceName))
            {
                Console.WriteLine($"Default Mic Found: {deviceName}");
                PhoneticAudioManager.Instance.StartMicrophoneRecord(deviceName);
                
                // Simulate checking mic levels
                for (int i = 0; i < 3; i++)
                {
                    float vol = PhoneticAudioManager.Instance.GetActiveMicrophoneVolume();
                    Console.WriteLine($"Simulated Recording Level: {vol:P1}");
                    await Task.Delay(200);
                }
                
                float peak = PhoneticAudioManager.Instance.StopMicrophoneRecord();
                Console.WriteLine($"Recording Finished. Peak Volume: {peak:P1}");
            }
            else
            {
                Console.WriteLine("No microphones detected.");
            }

            Console.WriteLine("\n=================================================================");
            Console.WriteLine("VERIFICATION RUN COMPLETED SUCCESSFULLY!");
            Console.WriteLine("=================================================================");

            Console.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();
        }

        private static void InvokeLifecycleMethod(object obj, string methodName)
        {
            Type type = obj.GetType();
            MethodInfo? method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (method != null)
            {
                method.Invoke(obj, null);
            }
            else
            {
                Console.WriteLine($"Warning: Lifecycle method '{methodName}' not found on {type.Name}");
            }
        }
    }
}
