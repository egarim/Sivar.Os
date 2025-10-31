// Client-Side Embedding Generation using Transformers.js
// This module provides browser-based text embedding generation with fallback support

// Import from CDN (no npm install needed)
// Using @xenova/transformers v2.6.0 for browser compatibility
import { pipeline } from 'https://cdn.jsdelivr.net/npm/@xenova/transformers@2.6.0';

let embeddingPipeline = null;
let isInitializing = false;
let initializationError = null;

/**
 * Initialize the embedding model (lazy loading)
 * Model: Xenova/all-MiniLM-L6-v2 
 * - Size: ~25MB (cached in browser after first load)
 * - Dimensions: 384 (matches server-side model)
 * - Speed: ~1-2 seconds per embedding
 */
async function initializeModel() {
    if (embeddingPipeline) {
        return; // Already initialized
    }

    if (isInitializing) {
        // Wait for ongoing initialization
        while (isInitializing) {
            await new Promise(resolve => setTimeout(resolve, 100));
        }
        return;
    }

    if (initializationError) {
        throw initializationError; // Previous init failed
    }

    try {
        isInitializing = true;
        console.log('[ClientEmbeddings] Initializing model: Xenova/all-MiniLM-L6-v2...');
        console.log('[ClientEmbeddings] First load may take 10-30 seconds to download model (~25MB)');
        
        const startTime = performance.now();
        
        embeddingPipeline = await pipeline(
            'feature-extraction', 
            'Xenova/all-MiniLM-L6-v2',
            {
                // Progress callback for model download
                progress_callback: (progress) => {
                    if (progress.status === 'progress') {
                        const percent = ((progress.loaded / progress.total) * 100).toFixed(1);
                        console.log(`[ClientEmbeddings] Downloading model: ${percent}%`);
                    }
                }
            }
        );
        
        const elapsed = ((performance.now() - startTime) / 1000).toFixed(2);
        console.log(`[ClientEmbeddings] Model loaded successfully in ${elapsed}s`);
        console.log('[ClientEmbeddings] Subsequent generations will be instant (model cached)');
        
    } catch (error) {
        console.error('[ClientEmbeddings] Failed to initialize model:', error);
        initializationError = error;
        throw error;
    } finally {
        isInitializing = false;
    }
}

/**
 * Generate embedding for text in the browser
 * Uses WebAssembly for fast inference (no GPU needed)
 * 
 * @param {string} text - Text to embed (max ~512 tokens recommended)
 * @returns {Promise<number[]|null>} - 384-dimensional embedding or null if failed
 * 
 * @example
 * const embedding = await generateEmbedding("Hello world");
 * // Returns: [0.123, -0.456, 0.789, ...] (384 numbers)
 */
export async function generateEmbedding(text) {
    try {
        // Validate input
        if (!text || typeof text !== 'string') {
            console.warn('[ClientEmbeddings] Invalid input: text must be a non-empty string');
            return null;
        }

        const trimmedText = text.trim();
        if (trimmedText.length === 0) {
            console.warn('[ClientEmbeddings] Empty text provided');
            return null;
        }

        // Warn if text is very long (may be truncated)
        if (trimmedText.length > 2000) {
            console.warn('[ClientEmbeddings] Text is long (>2000 chars), may be truncated to 512 tokens');
        }

        // Initialize model (lazy loading on first use)
        await initializeModel();
        
        console.log(`[ClientEmbeddings] Generating embedding for text (${trimmedText.length} chars)...`);
        const startTime = performance.now();
        
        // Generate embedding with pooling and normalization
        const output = await embeddingPipeline(trimmedText, {
            pooling: 'mean',      // Average all token embeddings
            normalize: true       // Normalize to unit vector (for cosine similarity)
        });

        // Convert Float32Array to regular array for JSON serialization
        const embedding = Array.from(output.data);
        
        const elapsed = ((performance.now() - startTime) / 1000).toFixed(3);
        console.log(`[ClientEmbeddings] Embedding generated in ${elapsed}s (${embedding.length} dimensions)`);
        
        // Validate output
        if (embedding.length !== 384) {
            console.error(`[ClientEmbeddings] Unexpected embedding size: ${embedding.length} (expected 384)`);
            return null;
        }

        return embedding;
        
    } catch (error) {
        console.error('[ClientEmbeddings] Failed to generate embedding:', error);
        console.error('[ClientEmbeddings] Stack:', error.stack);
        return null; // Trigger server-side fallback
    }
}

/**
 * Check if client-side embeddings are supported in this browser
 * Requires WebAssembly support (available in all modern browsers)
 * 
 * @returns {Promise<boolean>} - true if supported, false otherwise
 */
export async function isSupported() {
    try {
        // Check for WebAssembly support (required by transformers.js)
        if (typeof WebAssembly === 'undefined') {
            console.warn('[ClientEmbeddings] WebAssembly not supported in this browser');
            return false;
        }

        // Check for required Web APIs
        if (typeof fetch === 'undefined') {
            console.warn('[ClientEmbeddings] Fetch API not supported');
            return false;
        }

        // Additional check: try to instantiate a simple WebAssembly module
        const wasmCode = new Uint8Array([
            0x00, 0x61, 0x73, 0x6d, 0x01, 0x00, 0x00, 0x00
        ]);
        const wasmModule = new WebAssembly.Module(wasmCode);
        const wasmInstance = new WebAssembly.Instance(wasmModule);
        
        if (!wasmInstance) {
            console.warn('[ClientEmbeddings] WebAssembly instantiation failed');
            return false;
        }

        console.log('[ClientEmbeddings] Browser support check: PASSED');
        return true;
        
    } catch (error) {
        console.warn('[ClientEmbeddings] Browser support check failed:', error);
        return false;
    }
}

/**
 * Pre-warm the model (download and initialize without generating embedding)
 * Call this on app startup to avoid delay on first embedding generation
 * 
 * @returns {Promise<boolean>} - true if successful, false otherwise
 */
export async function warmupModel() {
    try {
        console.log('[ClientEmbeddings] Starting model warmup...');
        await initializeModel();
        
        // Generate a dummy embedding to fully initialize
        await embeddingPipeline('warmup', { pooling: 'mean', normalize: true });
        
        console.log('[ClientEmbeddings] Model warmup complete');
        return true;
    } catch (error) {
        console.error('[ClientEmbeddings] Model warmup failed:', error);
        return false;
    }
}

/**
 * Get model info and status
 * 
 * @returns {object} - Model information
 */
export function getModelInfo() {
    return {
        name: 'Xenova/all-MiniLM-L6-v2',
        dimensions: 384,
        initialized: embeddingPipeline !== null,
        isInitializing: isInitializing,
        hasError: initializationError !== null,
        error: initializationError ? initializationError.message : null
    };
}

// Log when module is loaded
console.log('[ClientEmbeddings] Module loaded successfully');
console.log('[ClientEmbeddings] Ready to generate embeddings in the browser');
