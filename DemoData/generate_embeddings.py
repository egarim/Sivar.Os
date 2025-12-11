#!/usr/bin/env python3
"""
Demo Data Embedding Generator for Sivar.Os
==========================================

This script generates 384-dimensional embeddings using the sentence-transformers
model 'all-MiniLM-L6-v2' - the same model used in the Sivar.Os application.

It reads all JSON files in the DemoData folder, generates embeddings for each
post's content, and updates the JSON files with the contentEmbedding field.

Requirements:
    pip install sentence-transformers

Usage:
    python generate_embeddings.py

The script will:
1. Find all *.json files in subdirectories
2. For each post, generate an embedding from the content field
3. Add a 'contentEmbedding' field with the vector as a string array
4. Save the updated JSON file
"""

import json
import os
from pathlib import Path
from sentence_transformers import SentenceTransformer
import numpy as np


def format_embedding_for_postgres(embedding: np.ndarray) -> str:
    """
    Format embedding array to PostgreSQL vector format.
    Example: "[0.123,0.456,0.789,...]"
    """
    values = ",".join(f"{float(v):.6f}" for v in embedding)
    return f"[{values}]"


def generate_embeddings_for_file(model: SentenceTransformer, file_path: Path) -> dict:
    """
    Generate embeddings for all posts in a demo data JSON file.
    
    Args:
        model: The sentence transformer model
        file_path: Path to the JSON file
        
    Returns:
        Statistics about the processing
    """
    print(f"\n📄 Processing: {file_path}")
    
    # Read the JSON file
    with open(file_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    posts = data.get('posts', [])
    if not posts:
        print(f"   ⚠️ No posts found in file")
        return {'file': str(file_path), 'posts_processed': 0, 'skipped': 0}
    
    processed = 0
    skipped = 0
    
    for post in posts:
        content = post.get('content', '')
        title = post.get('title', 'Unknown')
        
        if not content:
            print(f"   ⚠️ Skipping post '{title}' - no content")
            skipped += 1
            continue
        
        # Combine title and content for better embedding
        # This matches what the application does
        text_to_embed = f"{title}. {content}"
        
        # Generate embedding
        embedding = model.encode(text_to_embed, normalize_embeddings=True)
        
        # Format for PostgreSQL
        embedding_str = format_embedding_for_postgres(embedding)
        
        # Add to post
        post['contentEmbedding'] = embedding_str
        
        processed += 1
        if processed % 10 == 0:
            print(f"   ✅ Processed {processed}/{len(posts)} posts...")
    
    # Write the updated JSON back
    with open(file_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    
    print(f"   ✅ Completed: {processed} posts embedded, {skipped} skipped")
    
    return {
        'file': str(file_path),
        'posts_processed': processed,
        'skipped': skipped
    }


def main():
    """Main entry point for the embedding generator."""
    print("=" * 60)
    print("🚀 Sivar.Os Demo Data Embedding Generator")
    print("=" * 60)
    print("\nThis will generate 384-dimensional embeddings for all posts")
    print("using the sentence-transformers/all-MiniLM-L6-v2 model.\n")
    
    # Find the script's directory (DemoData folder)
    script_dir = Path(__file__).parent.resolve()
    print(f"📁 Working directory: {script_dir}")
    
    # Find all JSON files in subdirectories
    json_files = list(script_dir.glob("*/*.json"))
    
    if not json_files:
        print("\n❌ No JSON files found in subdirectories!")
        print("   Expected structure: DemoData/CategoryName/*.json")
        return
    
    print(f"\n📋 Found {len(json_files)} JSON file(s):")
    for f in json_files:
        print(f"   - {f.relative_to(script_dir)}")
    
    # Load the model
    print("\n🔄 Loading sentence-transformers model...")
    print("   Model: all-MiniLM-L6-v2")
    print("   Dimensions: 384")
    
    model = SentenceTransformer('all-MiniLM-L6-v2')
    print("   ✅ Model loaded successfully!")
    
    # Process each file
    results = []
    for json_file in json_files:
        result = generate_embeddings_for_file(model, json_file)
        results.append(result)
    
    # Print summary
    print("\n" + "=" * 60)
    print("📊 Summary")
    print("=" * 60)
    
    total_processed = sum(r['posts_processed'] for r in results)
    total_skipped = sum(r['skipped'] for r in results)
    
    for result in results:
        file_name = Path(result['file']).name
        print(f"   {file_name}: {result['posts_processed']} posts embedded")
    
    print(f"\n   Total: {total_processed} posts embedded, {total_skipped} skipped")
    print("\n✅ Done! JSON files have been updated with contentEmbedding fields.")
    print("   You can now run the XAF Updater to seed the database.")


if __name__ == "__main__":
    main()
