#!/usr/bin/env python3
"""
Generate embeddings for migrated blog posts.
Uses sentence-transformers (same model as Sivar.Os).

Usage:
    pip install sentence-transformers
    python generate_blog_embeddings.py
"""

import json
from pathlib import Path

from bs4 import BeautifulSoup
from sentence_transformers import SentenceTransformer

BLOG_JSON = Path(__file__).parent.parent.parent / "DemoData" / "Blog" / "blog.json"


def format_embedding(embedding) -> str:
    """Format embedding to PostgreSQL vector format."""
    values = ",".join(f"{float(v):.6f}" for v in embedding)
    return f"[{values}]"


def main():
    print("🧠 Generating embeddings for blog posts...")
    
    # Load model (same as Sivar.Os)
    print("   Loading model: all-MiniLM-L6-v2")
    model = SentenceTransformer('all-MiniLM-L6-v2')
    
    # Load blog data
    with open(BLOG_JSON, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    posts = data.get("posts", [])
    print(f"   Found {len(posts)} posts")
    
    for post in posts:
        title = post.get("title", "")
        content = post.get("blogContent", "")
        
        # Extract text from HTML
        text = BeautifulSoup(content, 'html.parser').get_text()
        
        # Combine title + content for embedding
        embed_text = f"{title}. {text}"[:8000]  # Limit length
        
        # Generate embedding
        embedding = model.encode(embed_text, normalize_embeddings=True)
        post["contentEmbedding"] = format_embedding(embedding)
        
        print(f"   ✅ {title[:50]}...")
    
    # Save updated data
    with open(BLOG_JSON, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    
    print(f"\n✅ Added embeddings to {len(posts)} posts")


if __name__ == "__main__":
    main()
