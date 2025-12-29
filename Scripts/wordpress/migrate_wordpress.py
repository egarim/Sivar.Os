#!/usr/bin/env python3
"""
WordPress to Sivar.Os Blog Migration Script
============================================

Uses WordPress REST API to export posts and format for Sivar.Os seeding.

Usage:
    pip install requests sentence-transformers beautifulsoup4
    python migrate_wordpress.py

Output:
    DemoData/Blog/blog.json
"""

import json
import os
import re
import uuid
from datetime import datetime
from pathlib import Path
from typing import Optional, List

import requests
from bs4 import BeautifulSoup

# Configuration
WORDPRESS_URL = "https://www.jocheojeda.com"
PROFILE_ID = "b1000000-0000-0000-0000-000000000001"
OUTPUT_DIR = Path(__file__).parent.parent.parent / "DemoData" / "Blog"


def fetch_all_posts(base_url: str, per_page: int = 100) -> list:
    """Fetch all posts from WordPress REST API."""
    posts = []
    page = 1
    
    while True:
        url = f"{base_url}/wp-json/wp/v2/posts"
        params = {"per_page": per_page, "page": page, "_embed": True}
        
        print(f"📥 Fetching page {page}...")
        response = requests.get(url, params=params, timeout=30)
        
        if response.status_code == 400:
            break  # No more pages
        
        response.raise_for_status()
        batch = response.json()
        
        if not batch:
            break
            
        posts.extend(batch)
        page += 1
        
        # Check if we've reached the last page
        total_pages = int(response.headers.get("X-WP-TotalPages", 1))
        if page > total_pages:
            break
    
    print(f"✅ Fetched {len(posts)} posts")
    return posts


def generate_slug(title: str) -> str:
    """Generate URL-friendly slug from title."""
    slug = title.lower()
    slug = re.sub(r'[^a-z0-9\s-]', '', slug)
    slug = re.sub(r'[\s]+', '-', slug)
    slug = re.sub(r'-+', '-', slug)
    return slug.strip('-')[:200]


def calculate_read_time(content: str) -> int:
    """Estimate read time in minutes (200 words/min)."""
    text = BeautifulSoup(content, 'html.parser').get_text()
    words = len(text.split())
    return max(1, round(words / 200))


def extract_cover_image(wp_post: dict) -> Optional[str]:
    """Extract featured image URL from embedded data."""
    try:
        embedded = wp_post.get("_embedded", {})
        featured = embedded.get("wp:featuredmedia", [])
        if featured and len(featured) > 0:
            return featured[0].get("source_url")
    except (KeyError, IndexError):
        pass
    return None


def extract_tags(wp_post: dict) -> List[str]:
    """Extract tag names from embedded data."""
    tags = []
    try:
        embedded = wp_post.get("_embedded", {})
        terms = embedded.get("wp:term", [])
        for term_group in terms:
            for term in term_group:
                if term.get("taxonomy") == "post_tag":
                    tags.append(term.get("name", "").lower())
    except (KeyError, TypeError):
        pass
    return tags


def convert_post(wp_post: dict, index: int) -> dict:
    """Convert WordPress post to Sivar.Os format."""
    # Generate deterministic GUID from WordPress post ID
    wp_id = wp_post.get("id", index)
    post_id = str(uuid.uuid5(uuid.NAMESPACE_URL, f"wordpress-{wp_id}"))
    
    title = wp_post.get("title", {}).get("rendered", "Untitled")
    content_html = wp_post.get("content", {}).get("rendered", "")
    excerpt = wp_post.get("excerpt", {}).get("rendered", "")
    
    # Clean up excerpt (remove HTML)
    excerpt_text = BeautifulSoup(excerpt, 'html.parser').get_text().strip()
    
    # Parse date
    date_str = wp_post.get("date", "")
    try:
        published_at = datetime.fromisoformat(date_str.replace("Z", "+00:00"))
    except ValueError:
        published_at = datetime.utcnow()
    
    return {
        "id": post_id,
        "profileId": PROFILE_ID,
        "postType": 7,  # Blog
        "title": title,
        "slug": wp_post.get("slug") or generate_slug(title),
        "content": excerpt_text[:500] if excerpt_text else title,
        "blogContent": content_html,
        "coverImageUrl": extract_cover_image(wp_post),
        "tags": extract_tags(wp_post),
        "readTimeMinutes": calculate_read_time(content_html),
        "publishedAt": published_at.isoformat(),
        "wordpressId": wp_id,
        "wordpressUrl": wp_post.get("link", "")
    }


def main():
    print("🚀 WordPress to Sivar.Os Migration")
    print(f"   Source: {WORDPRESS_URL}")
    print(f"   Output: {OUTPUT_DIR}")
    print()
    
    # Fetch posts
    wp_posts = fetch_all_posts(WORDPRESS_URL)
    
    # Convert to Sivar.Os format
    print("\n🔄 Converting posts...")
    sivar_posts = []
    for i, wp_post in enumerate(wp_posts):
        post = convert_post(wp_post, i)
        sivar_posts.append(post)
        print(f"   ✅ {post['title'][:50]}...")
    
    # Create output structure
    output = {
        "metadata": {
            "category": "Blog",
            "description": f"Migrated from {WORDPRESS_URL}",
            "migratedAt": datetime.utcnow().isoformat(),
            "profileTypeId": "11111111-1111-1111-1111-111111111111",
            "postType": 7,
            "totalPosts": len(sivar_posts)
        },
        "profiles": [
            {
                "id": PROFILE_ID,
                "displayName": "Jose Ojeda",
                "handle": "joche",
                "bio": "Software developer, DevExpress MVP, XAF enthusiast",
                "categoryKeys": ["blog", "developer", "xaf", "devexpress"]
            }
        ],
        "posts": sivar_posts
    }
    
    # Save output
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    output_file = OUTPUT_DIR / "blog.json"
    
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(output, f, indent=2, ensure_ascii=False)
    
    print(f"\n✅ Saved {len(sivar_posts)} posts to {output_file}")
    print("\n📝 Next steps:")
    print("   1. Run: python generate_blog_embeddings.py")
    print("   2. Run: python download_images.py (optional)")
    print("   3. Restart Sivar.Os to seed blog posts")


if __name__ == "__main__":
    main()
