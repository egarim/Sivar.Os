#!/usr/bin/env python3
"""
Fix Cover Images Script
Re-fetches WordPress posts with _embed to get featured images and updates the database.
"""

import requests
import psycopg2
import hashlib
import os
from azure.storage.blob import BlobServiceClient, ContentSettings

# Configuration
WORDPRESS_API = "https://www.jocheojeda.com/wp-json/wp/v2/posts"
DB_CONNECTION = "postgresql://postgres:postgres@localhost/XafSivarOs"

# Azure Blob Storage configuration
AZURE_CONNECTION_STRING = os.environ.get("AZURE_STORAGE_CONNECTION_STRING", 
    "DefaultEndpointsProtocol=https;AccountName=sivarstorage;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net")
AZURE_CONTAINER = "images"
AZURE_BLOB_PREFIX = "blog/"

def get_slug_from_url(url):
    """Extract slug from WordPress URL."""
    parts = url.rstrip('/').split('/')
    return parts[-1] if parts else None

def generate_uuid_from_slug(slug):
    """Generate a deterministic UUID v5 from slug."""
    import uuid
    namespace = uuid.UUID('6ba7b810-9dad-11d1-80b4-00c04fd430c8')
    return str(uuid.uuid5(namespace, slug))

def fetch_all_posts_with_embed():
    """Fetch all WordPress posts with embedded featured media."""
    all_posts = []
    page = 1
    per_page = 100
    
    while True:
        print(f"Fetching page {page}...")
        response = requests.get(
            WORDPRESS_API,
            params={
                'per_page': per_page,
                'page': page,
                '_embed': ''  # Empty string to trigger embed
            }
        )
        
        if response.status_code != 200:
            print(f"Error fetching page {page}: {response.status_code}")
            break
            
        posts = response.json()
        if not posts:
            break
        
        # Debug first post
        if page == 1 and posts:
            first = posts[0]
            print(f"  First post title: {first.get('title', {}).get('rendered', 'N/A')[:40]}")
            print(f"  Has _embedded: {'_embedded' in first}")
            if '_embedded' in first:
                print(f"  _embedded keys: {list(first['_embedded'].keys())}")
            
        all_posts.extend(posts)
        
        # Check if there are more pages
        total_pages = int(response.headers.get('X-WP-TotalPages', 1))
        if page >= total_pages:
            break
            
        page += 1
    
    return all_posts

def download_and_upload_image(image_url, blob_service_client):
    """Download image from WordPress and upload to Azure Blob Storage."""
    try:
        # Generate a unique filename based on the original URL
        filename = image_url.split('/')[-1]
        blob_name = f"{AZURE_BLOB_PREFIX}{filename}"
        
        # Check if already exists in Azure
        container_client = blob_service_client.get_container_client(AZURE_CONTAINER)
        blob_client = container_client.get_blob_client(blob_name)
        
        if blob_client.exists():
            print(f"  Image already exists in Azure: {blob_name}")
            return blob_client.url
        
        # Download image
        print(f"  Downloading: {image_url}")
        response = requests.get(image_url, timeout=30)
        if response.status_code != 200:
            print(f"  Failed to download image: {response.status_code}")
            return None
        
        # Determine content type
        content_type = response.headers.get('Content-Type', 'image/png')
        
        # Upload to Azure
        print(f"  Uploading to Azure: {blob_name}")
        blob_client.upload_blob(
            response.content,
            content_settings=ContentSettings(content_type=content_type),
            overwrite=True
        )
        
        return blob_client.url
        
    except Exception as e:
        print(f"  Error processing image {image_url}: {e}")
        return None

def main():
    print("Fetching WordPress posts with embedded featured media...")
    posts = fetch_all_posts_with_embed()
    print(f"Fetched {len(posts)} posts")
    
    # Initialize Azure Blob Service (optional - will skip upload if not configured)
    blob_service_client = None
    try:
        if "YOUR_KEY" not in AZURE_CONNECTION_STRING:
            blob_service_client = BlobServiceClient.from_connection_string(AZURE_CONNECTION_STRING)
            print("Azure Blob Storage connected")
    except Exception as e:
        print(f"Azure Blob Storage not available: {e}")
        print("Will store WordPress URLs directly (may have CORS issues)")
    
    # Connect to database
    conn = psycopg2.connect(DB_CONNECTION)
    cursor = conn.cursor()
    
    updated_count = 0
    skipped_count = 0
    no_image_count = 0
    
    for post in posts:
        slug = post.get('slug')
        if not slug:
            continue
            
        # Get featured image URL from embedded data
        embedded = post.get('_embedded', {})
        featured_media = embedded.get('wp:featuredmedia', [])
        
        if not featured_media:
            no_image_count += 1
            continue
            
        wp_image_url = featured_media[0].get('source_url')
        if not wp_image_url:
            no_image_count += 1
            continue
        
        # Find post by slug instead of generated ID
        cursor.execute("""
            SELECT "Id", "CoverImageUrl" FROM "Sivar_Posts" 
            WHERE "Slug" = %s AND "PostType" = 7
        """, (slug,))
        
        result = cursor.fetchone()
        if not result:
            continue
        
        post_id = result[0]
        current_cover = result[1]
        
        # Skip if already has an Azure cover image
        if current_cover and 'sivarstorage.blob.core.windows.net' in current_cover:
            skipped_count += 1
            continue
        
        # Determine final image URL
        if blob_service_client:
            # Upload to Azure and get new URL
            azure_url = download_and_upload_image(wp_image_url, blob_service_client)
            final_url = azure_url if azure_url else wp_image_url
        else:
            # Use WordPress URL directly
            final_url = wp_image_url
        
        # Update database
        print(f"Updating: {slug[:50]}")
        print(f"  URL: {final_url[:80]}...")
        
        cursor.execute("""
            UPDATE "Sivar_Posts" 
            SET "CoverImageUrl" = %s
            WHERE "Id" = %s AND "PostType" = 7
        """, (final_url, post_id))
        
        updated_count += 1
    
    conn.commit()
    cursor.close()
    conn.close()
    
    print(f"\n=== Summary ===")
    print(f"Total posts: {len(posts)}")
    print(f"Updated: {updated_count}")
    print(f"Skipped (already have Azure URL): {skipped_count}")
    print(f"No featured image in WordPress: {no_image_count}")

if __name__ == "__main__":
    main()
