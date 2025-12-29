# WordPress Migration Scripts

Migrate blog posts from WordPress (jocheojeda.com) to Sivar.Os.

## Prerequisites

```bash
pip install requests sentence-transformers beautifulsoup4
```

## Usage

### Step 1: Export from WordPress

```bash
cd Scripts/wordpress_migration
python migrate_wordpress.py
```

This fetches all posts via WordPress REST API and creates:
- `DemoData/Blog/blog.json`

### Step 2: Generate Embeddings

```bash
python generate_blog_embeddings.py
```

Adds `contentEmbedding` field to each post for semantic search.

### Step 3: Download Images (Optional)

```bash
python download_images.py
```

Downloads images locally. Then upload to Azure Blob and update `BLOB_BASE_URL`.

### Step 4: Seed to Database

Restart Sivar.Os - the Updater.cs will automatically seed from `blog.json`.

## Alternative: Direct Database Export

If you have SSH access to WordPress server, you can export directly:

```bash
# On WordPress server
wp post list --format=json > posts.json
```

## Output Format

```json
{
  "posts": [
    {
      "id": "uuid",
      "title": "Post Title",
      "slug": "post-title",
      "blogContent": "<p>HTML content</p>",
      "coverImageUrl": "https://...",
      "tags": ["tag1", "tag2"],
      "readTimeMinutes": 5,
      "publishedAt": "2025-12-23T10:00:00Z",
      "contentEmbedding": "[0.123,0.456,...]"
    }
  ]
}
```
