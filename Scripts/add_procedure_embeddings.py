#!/usr/bin/env python3
"""
Add embeddings to procedure posts in government.json
Uses OpenAI text-embedding-3-small with 384 dimensions
"""

import json
import sys
from pathlib import Path

try:
    from openai import OpenAI
except ImportError:
    print("Missing openai package. Install with: pip install openai")
    sys.exit(1)

# Load config from appsettings.json
def load_api_key():
    script_dir = Path(__file__).parent
    appsettings_path = script_dir.parent / "Sivar.Os" / "appsettings.json"
    
    with open(appsettings_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Remove comments
    import re
    lines = content.split('\n')
    cleaned_lines = []
    for line in lines:
        if '//' in line:
            idx = 0
            while True:
                pos = line.find('//', idx)
                if pos == -1:
                    break
                if pos > 0 and line[pos-1] == ':':
                    idx = pos + 2
                    continue
                line = line[:pos]
                break
        cleaned_lines.append(line)
    
    content = '\n'.join(cleaned_lines)
    content = re.sub(r',\s*([}\]])', r'\1', content)
    
    config = json.loads(content)
    return config.get("ChatService", {}).get("OpenAI", {}).get("ApiKey", "")

def generate_embedding(client: OpenAI, text: str) -> str:
    """Generate 384-dim embedding and format for pgvector."""
    response = client.embeddings.create(
        model="text-embedding-3-small",
        input=text[:8000],  # Truncate if too long
        dimensions=384
    )
    
    embedding = response.data[0].embedding
    return "[" + ",".join(f"{x:.6f}" for x in embedding) + "]"

def main():
    print("Loading OpenAI API key...")
    api_key = load_api_key()
    if not api_key:
        print("ERROR: No API key found")
        sys.exit(1)
    
    client = OpenAI(api_key=api_key)
    
    # Load government.json
    json_path = Path(__file__).parent.parent / "DemoData" / "Government" / "government.json"
    print(f"Loading {json_path}...")
    
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    # Find posts without contentEmbedding (the new procedure posts)
    posts_updated = 0
    for post in data['posts']:
        if 'contentEmbedding' not in post or not post.get('contentEmbedding'):
            title = post.get('title', 'Unknown')
            content = post.get('content', '')
            
            print(f"Generating embedding for: {title}")
            embedding = generate_embedding(client, content)
            post['contentEmbedding'] = embedding
            posts_updated += 1
    
    # Save updated JSON
    print(f"\nUpdated {posts_updated} posts with embeddings")
    print(f"Saving to {json_path}...")
    
    with open(json_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    
    print("Done!")

if __name__ == "__main__":
    main()
