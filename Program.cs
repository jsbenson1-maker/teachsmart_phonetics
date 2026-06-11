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
            Console.WriteLine("=================================================================");
            Console.WriteLine("TEACHSMART: PHONETIC CHALLENGE - COMPLETE 4-LEVEL VERIFICATION HARNESS");
            Console.WriteLine("=================================================================");

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
