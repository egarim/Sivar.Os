#!/usr/bin/env python3
"""
Download and re-upload images from WordPress to Azure Blob Storage.
Updates blog.json with new image URLs.

Usage:
    pip install requests azure-storage-blob
    python download_images.py
"""

import json
import os
import re
from pathlib import Path
from urllib.parse import urlparse
from typing import Optional, List

import requests

BLOG_JSON = Path(__file__).parent.parent.parent / "DemoData" / "Blog" / "blog.json"
IMAGES_DIR = Path(__file__).parent.parent.parent / "DemoData" / "Blog" / "images"
WORDPRESS_DOMAIN = "www.jocheojeda.com"

# Set this to your Azure Blob base URL after uploading
BLOB_BASE_URL = "https://your-storage.blob.core.windows.net/blog-images"


def download_image(url: str, save_dir: Path) -> Optional[str]:
    """Download image and return local path."""
    try:
        parsed = urlparse(url)
        filename = os.path.basename(parsed.path)
        
        if not filename:
            return None
        
        local_path = save_dir / filename
        
        if local_path.exists():
            return str(local_path)
        
        response = requests.get(url, timeout=30)
        response.raise_for_status()
        
        with open(local_path, 'wb') as f:
            f.write(response.content)
        
        return str(local_path)
    except Exception as e:
        print(f"   ⚠️ Failed to download {url}: {e}")
        return None


def find_images_in_html(html: str) -> List[str]:
    """Find all image URLs in HTML content."""
    pattern = rf'https?://[^"\s]*{WORDPRESS_DOMAIN}[^"\s]*\.(?:png|jpg|jpeg|gif|webp)'
    return list(set(re.findall(pattern, html, re.IGNORECASE)))


def main():
    print("📷 Downloading WordPress images...")
    
    IMAGES_DIR.mkdir(parents=True, exist_ok=True)
    
    with open(BLOG_JSON, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    posts = data.get("posts", [])
    all_images = set()
    
    # Collect all image URLs
    for post in posts:
        # Cover image
        cover = post.get("coverImageUrl")
        if cover and WORDPRESS_DOMAIN in cover:
            all_images.add(cover)
        
        # Images in content
        content = post.get("blogContent", "")
        content_images = find_images_in_html(content)
        all_images.update(content_images)
    
    print(f"   Found {len(all_images)} unique images")
    
    # Download images
    downloaded = {}
    for url in all_images:
        local = download_image(url, IMAGES_DIR)
        if local:
            filename = os.path.basename(local)
            new_url = f"{BLOB_BASE_URL}/{filename}"
            downloaded[url] = new_url
            print(f"   ✅ {filename}")
    
    # Update URLs in posts
    for post in posts:
        # Update cover image
        cover = post.get("coverImageUrl")
        if cover in downloaded:
            post["coverImageUrl"] = downloaded[cover]
        
        # Update content images
        content = post.get("blogContent", "")
        for old_url, new_url in downloaded.items():
            content = content.replace(old_url, new_url)
        post["blogContent"] = content
    
    # Save updated data
    with open(BLOG_JSON, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    
    print(f"\n✅ Downloaded {len(downloaded)} images to {IMAGES_DIR}")
    print(f"📝 Upload images to Azure Blob and update BLOB_BASE_URL")


if __name__ == "__main__":
    main()
