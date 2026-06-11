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
    "level1_sentence_3": "The cat sat on the mat",
    "level1_sentence_4": "A happy dog wags its tail",
    "level1_sentence_5": "Birds fly high in the blue sky",
    "level1_sentence_6": "The sun shines bright and warm",
    "level1_sentence_7": "Children love to play games together",
    
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
    "word_sat": "sat",
    "word_on": "on",
    "word_mat": "mat",
    "word_happy": "happy",
    "word_wags": "wags",
    "word_its": "its",
    "word_tail": "tail",
    "word_birds": "birds",
    "word_fly": "fly",
    "word_high": "high",
    "word_in": "in",
    "word_blue": "blue",
    "word_sky": "sky",
    "word_shines": "shines",
    "word_bright": "bright",
    "word_and": "and",
    "word_warm": "warm",
    "word_children": "children",
    "word_love": "love",
    "word_to": "to",
    "word_games": "games",
    "word_together": "together",

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
    
    "level2_fullword_4": "banana",
    "level2_slice_reinforce_4_0": "ba",
    "level2_slice_reinforce_4_1": "na",
    "level2_slice_reinforce_4_2": "na",
    
    "level2_fullword_5": "computer",
    "level2_slice_reinforce_5_0": "com",
    "level2_slice_reinforce_5_1": "pu",
    "level2_slice_reinforce_5_2": "ter",
    
    "level2_fullword_6": "helicopter",
    "level2_slice_reinforce_6_0": "he",
    "level2_slice_reinforce_6_1": "li",
    "level2_slice_reinforce_6_2": "cop",
    "level2_slice_reinforce_6_3": "ter",
    
    "level2_fullword_7": "alligator",
    "level2_slice_reinforce_7_0": "al",
    "level2_slice_reinforce_7_1": "li",
    "level2_slice_reinforce_7_2": "ga",
    "level2_slice_reinforce_7_3": "tor",
    
    "level2_fullword_8": "butterfly",
    "level2_slice_reinforce_8_0": "but",
    "level2_slice_reinforce_8_1": "ter",
    "level2_slice_reinforce_8_2": "fly",
    
    "level2_fullword_9": "adventure",
    "level2_slice_reinforce_9_0": "ad",
    "level2_slice_reinforce_9_1": "ven",
    "level2_slice_reinforce_9_2": "ture",
    
    # Level 3 words & blending elements
    "level3_target_0": "Construct the word: cat",
    "level3_target_1": "Construct the word: string",
    "level3_target_2": "Construct the word: play",
    "level3_target_3": "Construct the word: ship",
    "level3_target_4": "Construct the word: frog",
    "level3_target_5": "Construct the word: glove",
    "level3_target_6": "Construct the word: brick",
    "level3_target_7": "Construct the word: clock",
    "level3_target_8": "Construct the word: spoon",
    "level3_target_9": "Construct the word: nest",
    "level3_target_10": "Construct the word: train",
    "level3_target_11": "Construct the word: beach",
    
    "onset_c": "c",
    "onset_str": "str",
    "onset_pl": "pl",
    "onset_sh": "sh",
    "onset_fr": "fr",
    "onset_gl": "gl",
    "onset_br": "br",
    "onset_cl": "cl",
    "onset_sp": "sp",
    "onset_n": "n",
    "onset_tr": "tr",
    "onset_b": "b",
    "onset_a": "a",
    "onset_t": "t",
    "onset_p": "p",
    "rime_ics": "ics",
    
    "rime_at": "at",
    "rime_ing": "ing",
    "rime_ay": "ay",
    "rime_ot": "ot",
    "rime_ip": "ip",
    "rime_ed": "ed",
    "rime_og": "og",
    "rime_ove": "ove",
    "rime_ick": "ick",
    "rime_ock": "ock",
    "rime_oon": "oon",
    "rime_est": "est",
    "rime_ain": "ain",
    "rime_each": "each",
    
    "word_string": "string",
    "word_play": "play",
    "word_ship": "ship",
    "word_cing": "cing",
    "word_strat": "strat",
    "word_plot": "plot",
    "word_shed": "shed",
    
    "word_frog": "frog",
    "word_glove": "glove",
    "word_brick": "brick",
    "word_clock": "clock",
    "word_spoon": "spoon",
    "word_nest": "nest",
    "word_train": "train",
    "word_beach": "beach",
    
    "word_frove": "frove",
    "word_glog": "glog",
    "word_brock": "brock",
    "word_click": "click",
    "word_spest": "spest",
    "word_noon": "noon",
    "word_treach": "treach",
    "word_bain": "bain",
    
    # Level 4 elements (Chain 1: bat/cat/cap/cop/top/toy)
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

    # Level 4 elements (Chain 2: pig/pin/pen/pan/fan/fin)
    "level4_start_pig": "Start word is: pig",
    "word_pig": "pig",
    "level4_craft_pin": "You crafted: pin",
    "level4_craft_pen": "You crafted: pen",
    "level4_craft_pan": "You crafted: pan",
    "level4_craft_fan": "You crafted: fan",
    "level4_craft_fin": "You crafted: fin",
    
    "word_pin": "pin",
    "word_pen": "pen",
    "word_pan": "pan",
    "word_fan": "fan",
    "word_fin": "fin",
    "word_fig": "fig",
    "word_peg": "peg",
    "word_pag": "pag",
    "word_fag": "fag",
    
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
        # print(f"Skipping '{key}' (already exists)")
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
