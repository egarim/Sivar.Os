#!/usr/bin/env python3
"""
Re-embed existing posts with OpenAI's text-embedding-3-small model.

This script:
1. Connects to PostgreSQL database
2. Fetches all posts with Content
3. Generates new 384-dimension embeddings using OpenAI
4. Updates the ContentEmbedding column in the database

Usage:
    pip install openai psycopg2-binary python-dotenv
    python reembed_with_openai.py

Environment variables (or modify the config below):
    OPENAI_API_KEY - Your OpenAI API key
    DB_HOST - PostgreSQL host (default: localhost)
    DB_PORT - PostgreSQL port (default: 5432)
    DB_NAME - Database name (default: XafSivarOs)
    DB_USER - Database user (default: postgres)
    DB_PASSWORD - Database password
"""

import os
import sys
import time
from typing import List, Tuple, Optional
import json
from pathlib import Path

try:
    from openai import OpenAI
    import psycopg2
    from psycopg2.extras import execute_values
except ImportError as e:
    print(f"Missing dependency: {e}")
    print("Install with: pip install openai psycopg2-binary")
    sys.exit(1)

# =============================================================================
# CONFIGURATION - Reads from Sivar.Os appsettings.json
# =============================================================================

def load_config_from_appsettings():
    """Load configuration from Sivar.Os/appsettings.json"""
    script_dir = Path(__file__).parent
    appsettings_path = script_dir.parent / "Sivar.Os" / "appsettings.json"
    
    if not appsettings_path.exists():
        print(f"ERROR: appsettings.json not found at {appsettings_path}")
        sys.exit(1)
    
    # Read and parse JSON (handling comments by removing them)
    with open(appsettings_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Remove single-line comments (// ...)
    import re
    lines = content.split('\n')
    cleaned_lines = []
    for line in lines:
        # Remove // comments but be careful with URLs (http://)
        if '//' in line:
            # Find // that's not part of a URL (http:// or https://)
            idx = 0
            while True:
                pos = line.find('//', idx)
                if pos == -1:
                    break
                # Check if it's preceded by : (URL pattern)
                if pos > 0 and line[pos-1] == ':':
                    idx = pos + 2
                    continue
                # It's a comment, remove from here
                line = line[:pos]
                break
        cleaned_lines.append(line)
    
    content = '\n'.join(cleaned_lines)
    
    try:
        return json.loads(content)
    except json.JSONDecodeError as e:
        print(f"JSON parse error: {e}")
        print("Trying alternative parsing...")
        # Try with json5 style - more lenient
        # Remove trailing commas before } or ]
        content = re.sub(r',\s*([}\]])', r'\1', content)
        return json.loads(content)

def parse_connection_string(conn_str: str) -> dict:
    """Parse PostgreSQL connection string to dict."""
    config = {}
    for part in conn_str.split(';'):
        if '=' in part:
            key, value = part.split('=', 1)
            key = key.strip().lower()
            value = value.strip()
            if key == 'host':
                config['host'] = value
            elif key == 'port':
                config['port'] = int(value)
            elif key == 'database':
                config['dbname'] = value
            elif key == 'username':
                config['user'] = value
            elif key == 'password':
                config['password'] = value
    return config

# Load configuration
_config = load_config_from_appsettings()

# OpenAI API Key from ChatService.OpenAI.ApiKey
OPENAI_API_KEY = _config.get("ChatService", {}).get("OpenAI", {}).get("ApiKey", "")

# Database connection from ConnectionStrings.DefaultConnection
_conn_str = _config.get("ConnectionStrings", {}).get("DefaultConnection", "")
DB_CONFIG = parse_connection_string(_conn_str)

# Embedding configuration
EMBEDDING_MODEL = "text-embedding-3-small"
EMBEDDING_DIMENSIONS = 384  # Matryoshka dimensions to match existing all-minilm embeddings
BATCH_SIZE = 100  # Number of texts to embed per API call (max 2048)
MAX_TEXT_LENGTH = 8000  # Truncate longer texts

# Table configuration
TABLE_NAME = "Sivar_Posts"
ID_COLUMN = "Id"
CONTENT_COLUMN = "Content"
EMBEDDING_COLUMN = "ContentEmbedding"

# =============================================================================
# MAIN LOGIC
# =============================================================================

def get_openai_client() -> OpenAI:
    """Create OpenAI client."""
    return OpenAI(api_key=OPENAI_API_KEY)

def get_db_connection():
    """Create database connection."""
    return psycopg2.connect(**DB_CONFIG)

def fetch_posts_to_embed(conn) -> List[Tuple[str, str]]:
    """Fetch all posts that have content."""
    with conn.cursor() as cur:
        cur.execute(f"""
            SELECT "{ID_COLUMN}", "{CONTENT_COLUMN}"
            FROM "{TABLE_NAME}"
            WHERE "{CONTENT_COLUMN}" IS NOT NULL 
            AND "{CONTENT_COLUMN}" != ''
            ORDER BY "{ID_COLUMN}"
        """)
        return cur.fetchall()

def generate_embeddings(client: OpenAI, texts: List[str]) -> List[List[float]]:
    """Generate embeddings for a batch of texts using OpenAI."""
    # Truncate texts that are too long
    processed_texts = [
        text[:MAX_TEXT_LENGTH] if len(text) > MAX_TEXT_LENGTH else text
        for text in texts
    ]
    
    response = client.embeddings.create(
        model=EMBEDDING_MODEL,
        input=processed_texts,
        dimensions=EMBEDDING_DIMENSIONS  # Matryoshka embedding truncation
    )
    
    return [item.embedding for item in response.data]

def format_embedding_for_pgvector(embedding: List[float]) -> str:
    """Format embedding list as PostgreSQL vector string."""
    return "[" + ",".join(f"{x:.8f}" for x in embedding) + "]"

def update_embeddings_batch(conn, updates: List[Tuple[str, str]]):
    """Update embeddings in the database."""
    with conn.cursor() as cur:
        for post_id, embedding_str in updates:
            cur.execute(f"""
                UPDATE "{TABLE_NAME}"
                SET "{EMBEDDING_COLUMN}" = %s::vector
                WHERE "{ID_COLUMN}" = %s
            """, (embedding_str, post_id))
    conn.commit()

def main():
    print("=" * 60)
    print("Re-embedding Posts with OpenAI text-embedding-3-small")
    print(f"Target dimensions: {EMBEDDING_DIMENSIONS}")
    print("=" * 60)
    
    # Validate API key
    if not OPENAI_API_KEY or OPENAI_API_KEY.startswith("sk-your"):
        print("ERROR: Please set OPENAI_API_KEY environment variable or update the script")
        sys.exit(1)
    
    # Initialize clients
    print("\n[1/4] Connecting to services...")
    openai_client = get_openai_client()
    conn = get_db_connection()
    print(f"  ✓ Connected to database: {DB_CONFIG['host']}:{DB_CONFIG['port']}/{DB_CONFIG['dbname']}")
    
    # Fetch posts
    print("\n[2/4] Fetching posts from database...")
    posts = fetch_posts_to_embed(conn)
    total_posts = len(posts)
    print(f"  ✓ Found {total_posts} posts with content")
    
    if total_posts == 0:
        print("\n  No posts to process. Exiting.")
        conn.close()
        return
    
    # Process in batches
    print(f"\n[3/4] Generating embeddings (batch size: {BATCH_SIZE})...")
    processed = 0
    errors = 0
    start_time = time.time()
    
    for i in range(0, total_posts, BATCH_SIZE):
        batch = posts[i:i + BATCH_SIZE]
        batch_ids = [p[0] for p in batch]
        batch_texts = [p[1] for p in batch]
        
        try:
            # Generate embeddings
            embeddings = generate_embeddings(openai_client, batch_texts)
            
            # Format for PostgreSQL
            updates = [
                (post_id, format_embedding_for_pgvector(embedding))
                for post_id, embedding in zip(batch_ids, embeddings)
            ]
            
            # Update database
            update_embeddings_batch(conn, updates)
            
            processed += len(batch)
            elapsed = time.time() - start_time
            rate = processed / elapsed if elapsed > 0 else 0
            eta = (total_posts - processed) / rate if rate > 0 else 0
            
            print(f"  Progress: {processed}/{total_posts} ({processed*100//total_posts}%) - "
                  f"{rate:.1f} posts/sec - ETA: {eta:.0f}s")
            
            # Small delay to respect rate limits
            time.sleep(0.1)
            
        except Exception as e:
            errors += 1
            print(f"  ERROR processing batch {i//BATCH_SIZE + 1}: {e}")
            if errors >= 5:
                print("  Too many errors, stopping.")
                break
    
    # Summary
    elapsed = time.time() - start_time
    print(f"\n[4/4] Complete!")
    print("=" * 60)
    print(f"  Posts processed: {processed}/{total_posts}")
    print(f"  Errors: {errors}")
    print(f"  Time elapsed: {elapsed:.1f}s")
    print(f"  Embedding model: {EMBEDDING_MODEL}")
    print(f"  Dimensions: {EMBEDDING_DIMENSIONS}")
    print("=" * 60)
    
    conn.close()

if __name__ == "__main__":
    main()
