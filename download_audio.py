import os
import time
import urllib.request
import urllib.parse

# List of audio clips to download (filename, text_to_speak)
audio_clips = {
    # Level 1 sentences
    "level1_sentence_0": "The quick brown fox",
    "level1_sentence_1": "Phonetics is very fun",
    "level1_sentence_2": "Read a book today",
    
    # Target and distractor words (Level 1)
    "word_the": "The",
    "word_quick": "quick",
    "word_brown": "brown",
    "word_fox": "fox",
    "word_phonetics": "phonetics",
    "word_is": "is",
    "word_very": "very",
    "word_fun": "fun",
    "word_read": "read",
    "word_a": "a",
    "word_book": "book",
    "word_today": "today",
    "word_slow": "slow",
    "word_dog": "dog",
    "word_cat": "cat",
    "word_jump": "jump",
    "word_apple": "apple",
    "word_word": "word",
    "word_hello": "hello",

    # Level 2 words & syllables
    "level2_fullword_0": "cooperate",
    "level2_slice_reinforce_0_0": "co",
    "level2_slice_reinforce_0_1": "o",
    "level2_slice_reinforce_0_2": "pe",
    "level2_slice_reinforce_0_3": "rate",
    
    "level2_fullword_1": "phonetics",
    "level2_slice_reinforce_1_0": "pho",
    "level2_slice_reinforce_1_1": "ne",
    "level2_slice_reinforce_1_2": "tics",
    
    "level2_fullword_2": "education",
    "level2_slice_reinforce_2_0": "ed",
    "level2_slice_reinforce_2_1": "u",
    "level2_slice_reinforce_2_2": "ca",
    "level2_slice_reinforce_2_3": "tion",
    
    "level2_fullword_3": "dinosaur",
    "level2_slice_reinforce_3_0": "di",
    "level2_slice_reinforce_3_1": "no",
    "level2_slice_reinforce_3_2": "saur",
    
    # Level 3 words & blending elements
    "level3_target_0": "Construct the word: cat",
    "level3_target_1": "Construct the word: string",
    "level3_target_2": "Construct the word: play",
    "level3_target_3": "Construct the word: ship",
    
    "onset_c": "c",
    "onset_str": "str",
    "onset_pl": "pl",
    "onset_sh": "sh",
    
    "rime_at": "at",
    "rime_ing": "ing",
    "rime_ay": "ay",
    "rime_ot": "ot",
    "rime_ip": "ip",
    "rime_ed": "ed",
    
    "word_string": "string",
    "word_play": "play",
    "word_ship": "ship",
    "word_cing": "cing",
    "word_strat": "strat",
    "word_plot": "plot",
    "word_shed": "shed",
    
    # Level 4 elements
    "level4_start_bat": "Start word is: bat",
    "word_bat": "bat",
    "level4_craft_cat": "You crafted: cat",
    "level4_craft_cap": "You crafted: cap",
    "level4_craft_cop": "You crafted: cop",
    "level4_craft_top": "You crafted: top",
    "level4_craft_toy": "You crafted: toy",
    
    "word_cop": "cop",
    "word_top": "top",
    "word_toy": "toy",
    "word_bpt": "bpt",
    "word_cpt": "cpt",
    "word_coy": "coy",
    
    # Introductions & Wins
    "level1_intro": "Listen carefully, then blast the words in the correct order!",
    "level2_intro": "Slice the syllable blocks in order to segment the word!",
    "level3_intro": "Aim and slingshot the onset sound into the matching rime balloon!",
    "level4_intro": "Drag the sound bubbles onto the slots to mutate the word and collect cards!",
    
    "level1_win": "Level One Completed! Excellent Job!",
    "level2_win": "Level Two Completed! Fantastic!",
    "level3_win": "Level Three Completed! Astounding!",
    "level4_win": "Level Four Completed! Master crafter!"
}

# Create output folder
output_dir = "WebAssets/audio"
os.makedirs(output_dir, exist_ok=True)

print(f"Downloading {len(audio_clips)} high-quality voice audio files to {output_dir}...")

headers = {
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36'
}

downloaded = 0
failed = 0

for key, text in audio_clips.items():
    dest_path = os.path.join(output_dir, f"{key}.mp3")
    
    # Skip if already exists
    if os.path.exists(dest_path):
        print(f"Skipping '{key}' (already exists)")
        downloaded += 1
        continue
        
    encoded_text = urllib.parse.quote(text)
    url = f"https://translate.google.com/translate_tts?ie=UTF-8&tl=en&client=tw-ob&q={encoded_text}"
    
    try:
        req = urllib.request.Request(url, headers=headers)
        with urllib.request.urlopen(req) as response:
            with open(dest_path, 'wb') as f:
                f.write(response.read())
        print(f"Successfully downloaded: {key} -> '{text}'")
        downloaded += 1
        # Add a tiny delay to avoid rate limiting
        time.sleep(0.25)
    except Exception as e:
        print(f"Failed to download '{key}' ('{text}'): {e}")
        failed += 1

print(f"\nDownload finished: {downloaded} succeeded, {failed} failed.")
