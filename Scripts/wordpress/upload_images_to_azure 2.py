#!/usr/bin/env python3
"""
Upload WordPress images to Azure Blob Storage and update blog.json URLs.
"""

import json
import os
from pathlib import Path
from typing import Dict, List
from urllib.parse import quote

# Azure Blob Storage configuration
BLOB_ENDPOINT = "https://sivarstorage.blob.core.windows.net"
SAS_TOKEN = "sp=racwdl&st=2025-12-07T17:11:36Z&se=2026-01-08T01:26:36Z&spr=https&sv=2024-11-04&sr=c&sig=%2F4EiRJWJWJ0YgaTe5nHzMP3zYjcRFOnYkPd%2FNxzlGeg%3D"
CONTAINER_NAME = "images"
BLOG_FOLDER = "blog"  # Subfolder within container for blog images

# Paths
SCRIPT_DIR = Path(__file__).parent
PROJECT_ROOT = SCRIPT_DIR.parent.parent
IMAGES_DIR = PROJECT_ROOT / "DemoData" / "Blog" / "images"
BLOG_JSON = PROJECT_ROOT / "DemoData" / "Blog" / "blog.json"

def upload_images() -> Dict[str, str]:
    """Upload all images to Azure Blob Storage and return URL mapping."""
    try:
        from azure.storage.blob import BlobServiceClient, ContentSettings
    except ImportError:
        print("❌ azure-storage-blob not installed. Installing...")
        import subprocess
        subprocess.check_call(["pip3", "install", "azure-storage-blob"])
        from azure.storage.blob import BlobServiceClient, ContentSettings
    
    # Build connection string
    connection_string = f"BlobEndpoint={BLOB_ENDPOINT};SharedAccessSignature={SAS_TOKEN}"
    
    blob_service = BlobServiceClient.from_connection_string(connection_string)
    container_client = blob_service.get_container_client(CONTAINER_NAME)
    
    url_mapping: Dict[str, str] = {}
    
    if not IMAGES_DIR.exists():
        print(f"❌ Images directory not found: {IMAGES_DIR}")
        return url_mapping
    
    images = list(IMAGES_DIR.glob("*"))
    print(f"📤 Uploading {len(images)} images to Azure Blob Storage...")
    
    for img_path in images:
        if not img_path.is_file():
            continue
            
        filename = img_path.name
        blob_name = f"{BLOG_FOLDER}/{filename}"
        
        # Determine content type
        ext = img_path.suffix.lower()
        content_types = {
            ".jpg": "image/jpeg",
            ".jpeg": "image/jpeg",
            ".png": "image/png",
            ".gif": "image/gif",
            ".webp": "image/webp",
            ".svg": "image/svg+xml",
        }
        content_type = content_types.get(ext, "application/octet-stream")
        
        try:
            blob_client = container_client.get_blob_client(blob_name)
            
            with open(img_path, "rb") as data:
                blob_client.upload_blob(
                    data,
                    overwrite=True,
                    content_settings=ContentSettings(content_type=content_type)
                )
            
            # Build public URL
            public_url = f"{BLOB_ENDPOINT}/{CONTAINER_NAME}/{blob_name}"
            url_mapping[filename] = public_url
            print(f"   ✅ {filename}")
            
        except Exception as e:
            print(f"   ❌ {filename}: {e}")
    
    return url_mapping

def update_blog_json(url_mapping: Dict[str, str]) -> None:
    """Update blog.json with new Azure Blob URLs."""
    if not BLOG_JSON.exists():
        print(f"❌ blog.json not found: {BLOG_JSON}")
        return
    
    with open(BLOG_JSON, "r", encoding="utf-8") as f:
        data = json.load(f)
    
    # Handle both flat list and structured format
    if isinstance(data, dict) and "posts" in data:
        posts = data["posts"]
    else:
        posts = data
    
    print(f"\n🔄 Updating {len(posts)} posts with Azure Blob URLs...")
    
    updated_count = 0
    for post in posts:
        # Update cover image
        if post.get("coverImageUrl"):
            old_url = post["coverImageUrl"]
            # Extract filename from WordPress URL
            filename = old_url.split("/")[-1].split("?")[0]
            if filename in url_mapping:
                post["coverImageUrl"] = url_mapping[filename]
                updated_count += 1
        
        # Update images in content
        if post.get("blogContent"):
            content = post["blogContent"]
            for filename, new_url in url_mapping.items():
                # Replace various WordPress URL patterns
                patterns = [
                    f"https://www.jocheojeda.com/wp-content/uploads/",
                    f"https://jocheojeda.com/wp-content/uploads/",
                ]
                for pattern in patterns:
                    # Match the pattern followed by any path ending with the filename
                    old_patterns = [
                        f'src="{pattern}',
                        f"src='{pattern}",
                    ]
                    for old_pat in old_patterns:
                        if old_pat in content and filename in content:
                            # This is a simple approach - find the img with this filename
                            import re
                            # Match img src with this filename
                            regex = rf'(src=["\'])https?://[^"\']*{re.escape(filename)}(["\'])'
                            content = re.sub(regex, rf'\1{new_url}\2', content)
            
            post["blogContent"] = content
    
    # Save updated JSON (preserve structure)
    with open(BLOG_JSON, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    
    print(f"✅ Updated {updated_count} cover images")
    print(f"✅ Saved updated blog.json")

def main():
    print("🚀 Azure Blob Storage Image Migration")
    print(f"   Endpoint: {BLOB_ENDPOINT}")
    print(f"   Container: {CONTAINER_NAME}/{BLOG_FOLDER}")
    print()
    
    # Step 1: Upload images
    url_mapping = upload_images()
    
    if not url_mapping:
        print("❌ No images uploaded, aborting.")
        return
    
    print(f"\n✅ Uploaded {len(url_mapping)} images")
    
    # Step 2: Update blog.json
    update_blog_json(url_mapping)
    
    print("\n🎉 Migration complete!")
    print(f"   Images available at: {BLOB_ENDPOINT}/{CONTAINER_NAME}/{BLOG_FOLDER}/")

if __name__ == "__main__":
    main()
