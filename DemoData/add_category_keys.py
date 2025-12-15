#!/usr/bin/env python3
"""
Script to add categoryKeys to demo data JSON files for multilingual search.
CategoryKeys are normalized English keys that enable searching in any language.
"""

import json
import os
from pathlib import Path

# Category mapping based on tags and content keywords
TAG_TO_CATEGORY = {
    # Restaurant types
    "restaurant": ["restaurant"],
    "salvadoran": ["restaurant"],
    "tipico": ["restaurant"],
    "pupusas": ["restaurant"],
    "mexican": ["restaurant"],
    "tacos": ["restaurant"],
    "burritos": ["restaurant"],
    "american": ["restaurant"],
    "burgers": ["restaurant"],
    "italian": ["restaurant"],
    "pasta": ["restaurant"],
    "asian": ["restaurant"],
    "sushi": ["restaurant"],
    "japanese": ["restaurant"],
    "chinese": ["restaurant"],
    "thai": ["restaurant"],
    "ramen": ["restaurant"],
    "noodles": ["restaurant"],
    "seafood": ["restaurant"],
    "mariscos": ["restaurant"],
    "ceviche": ["restaurant"],
    "steakhouse": ["restaurant"],
    "vegetarian": ["restaurant"],
    "vegan": ["restaurant"],
    
    # Pizza (gets both pizza and restaurant)
    "pizza": ["pizza", "restaurant"],
    
    # Cafe/Bakery
    "cafe": ["cafe"],
    "coffee": ["cafe"],
    "bakery": ["bakery"],
    
    # Fast food
    "fast-food": ["fast_food"],
    "fast_food": ["fast_food"],
    "chicken": ["fast_food"],
    "hot-dogs": ["fast_food"],
    
    # Government
    "government": ["government_office"],
    "dui": ["dui_office", "government_office"],
    "passport": ["passport_office", "government_office"],
    "migration": ["passport_office", "government_office"],
    
    # Financial
    "bank": ["bank"],
    "banking": ["bank"],
    "atm": ["atm"],
    "money-transfer": ["bank"],
    
    # Healthcare
    "pharmacy": ["pharmacy"],
    "hospital": ["hospital"],
    "medical": ["hospital"],
    "healthcare": ["hospital"],
    
    # Tourism
    "tourism": ["tourist_attraction"],
    "tourist": ["tourist_attraction"],
    "beach": ["beach"],
    "playa": ["beach"],
    "museum": ["museum"],
    "park": ["park"],
    "parque": ["park"],
    "volcano": ["tourist_attraction"],
    "archaeological": ["tourist_attraction"],
    "ruins": ["tourist_attraction"],
    
    # Shopping
    "mall": ["shopping"],
    "shopping": ["shopping"],
    "supermarket": ["supermarket"],
    
    # Services
    "gym": ["gym"],
    "fitness": ["gym"],
    "church": ["church"],
    "lawyer": ["lawyer"],
    "notary": ["lawyer"],
    "mechanic": ["mechanic"],
    "automotive": ["mechanic"],
    "beauty": ["beauty_salon"],
    "salon": ["beauty_salon"],
    "hotel": ["hotel"],
    "hostel": ["hotel"],
    
    # Entertainment
    "cinema": ["entertainment"],
    "theater": ["entertainment"],
    "teatro": ["entertainment"],
    "bar": ["bar"],
    "nightlife": ["bar"],
    "sports-bar": ["bar"],
    
    # Education
    "school": ["school"],
    "university": ["university"],
    "education": ["school"],
}

# Content keywords to category mapping (for posts without matching tags)
CONTENT_TO_CATEGORY = {
    "pizza": ["pizza", "restaurant"],
    "pizzería": ["pizza", "restaurant"],
    "pizzeria": ["pizza", "restaurant"],
    "pupusa": ["restaurant"],
    "taco": ["restaurant"],
    "burrito": ["restaurant"],
    "hamburguesa": ["restaurant"],
    "burger": ["restaurant"],
    "café": ["cafe"],
    "coffee": ["cafe"],
    "bank": ["bank"],
    "banco": ["bank"],
    "farmacia": ["pharmacy"],
    "pharmacy": ["pharmacy"],
    "hospital": ["hospital"],
    "médico": ["hospital"],
    "doctor": ["hospital"],
    "dui": ["dui_office", "government_office"],
    "pasaporte": ["passport_office", "government_office"],
    "passport": ["passport_office", "government_office"],
    "migración": ["passport_office", "government_office"],
    "playa": ["beach"],
    "beach": ["beach"],
    "museo": ["museum"],
    "museum": ["museum"],
    "volcán": ["tourist_attraction"],
    "volcano": ["tourist_attraction"],
}


def get_category_keys(item: dict, is_post: bool = True) -> list[str]:
    """Extract category keys from tags and content."""
    categories = set()
    
    # Get tags
    tags = item.get("tags", []) or []
    
    # Map tags to categories
    for tag in tags:
        tag_lower = tag.lower()
        if tag_lower in TAG_TO_CATEGORY:
            categories.update(TAG_TO_CATEGORY[tag_lower])
    
    # Also check content for keywords
    content = ""
    if is_post:
        content = (item.get("content", "") or "").lower()
        title = (item.get("title", "") or "").lower()
        content = f"{title} {content}"
    else:
        content = (item.get("bio", "") or "").lower()
        display_name = (item.get("displayName", "") or "").lower()
        content = f"{display_name} {content}"
    
    for keyword, cats in CONTENT_TO_CATEGORY.items():
        if keyword.lower() in content:
            categories.update(cats)
    
    # Check category field for profiles (services.json uses this)
    category = (item.get("category", "") or "").lower()
    if category:
        if "bank" in category:
            categories.add("bank")
        elif "pharmacy" in category:
            categories.add("pharmacy")
        elif "hospital" in category:
            categories.add("hospital")
        elif "medical" in category:
            categories.add("hospital")
        elif "telecom" in category:
            categories.add("telecom")
        elif "notary" in category:
            categories.add("lawyer")
    
    return sorted(list(categories))


def process_json_file(file_path: Path) -> bool:
    """Process a single JSON file and add categoryKeys."""
    print(f"Processing: {file_path}")
    
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            data = json.load(f)
    except Exception as e:
        print(f"  Error reading file: {e}")
        return False
    
    modified = False
    
    # Process profiles
    profiles = data.get("profiles", [])
    for profile in profiles:
        if "categoryKeys" not in profile:
            category_keys = get_category_keys(profile, is_post=False)
            if category_keys:
                profile["categoryKeys"] = category_keys
                modified = True
    
    # Process posts
    posts = data.get("posts", [])
    for post in posts:
        if "categoryKeys" not in post:
            category_keys = get_category_keys(post, is_post=True)
            if category_keys:
                post["categoryKeys"] = category_keys
                modified = True
    
    if modified:
        try:
            with open(file_path, 'w', encoding='utf-8') as f:
                json.dump(data, f, indent=2, ensure_ascii=False)
            print(f"  ✅ Updated {len(profiles)} profiles and {len(posts)} posts")
            return True
        except Exception as e:
            print(f"  Error writing file: {e}")
            return False
    else:
        print(f"  ℹ️ No changes needed")
        return True


def main():
    """Main entry point."""
    demo_data_dir = Path(__file__).parent
    
    # Find all JSON files in subdirectories
    json_files = []
    for subdir in ["Restaurants", "Government", "Services", "Tourism", "Entertainment"]:
        subdir_path = demo_data_dir / subdir
        if subdir_path.exists():
            for json_file in subdir_path.glob("*.json"):
                json_files.append(json_file)
    
    print(f"Found {len(json_files)} JSON files to process\n")
    
    success_count = 0
    for json_file in json_files:
        if process_json_file(json_file):
            success_count += 1
    
    print(f"\n✅ Successfully processed {success_count}/{len(json_files)} files")


if __name__ == "__main__":
    main()
