/**
 * Client-Side Sentiment Analysis using Transformers.js
 * Analyzes text emotions in English and Spanish using local AI models
 * 
 * Models Used:
 * - lxyuan/distilbert-base-multilingual-cased-sentiments-student (ES/EN sentiment)
 * - SamLowe/roberta-base-go_emotions (28 emotions → 5 categories)
 * 
 * @author Jose Manuel Ojeda Melgar (Joche Ojeda)
 * @date October 31, 2025
 */

import { pipeline, env } from 'https://cdn.jsdelivr.net/npm/@xenova/transformers@2.6.0';

// Configure Transformers.js for browser
env.allowLocalModels = false;
env.useBrowserCache = true;
env.allowRemoteModels = true;

/**
 * Sentiment Analyzer Class
 * Handles initialization and sentiment analysis with caching
 */
class SentimentAnalyzer {
    constructor() {
        this.sentimentClassifier = null;
        this.emotionClassifier = null;
        this.isInitialized = false;
        this.isInitializing = false;
        this.initPromise = null;
        
        // Emotion category mapping (28 go_emotions → 5 categories)
        this.emotionMapping = {
            // Joy category
            'joy': 'Joy',
            'excitement': 'Joy',
            'love': 'Joy',
            'gratitude': 'Joy',
            'amusement': 'Joy',
            'admiration': 'Joy',
            'approval': 'Joy',
            'caring': 'Joy',
            'optimism': 'Joy',
            'pride': 'Joy',
            'relief': 'Joy',
            
            // Sadness category
            'sadness': 'Sadness',
            'grief': 'Sadness',
            'disappointment': 'Sadness',
            'remorse': 'Sadness',
            
            // Anger category
            'anger': 'Anger',
            'annoyance': 'Anger',
            'disapproval': 'Anger',
            
            // Fear category
            'fear': 'Fear',
            'nervousness': 'Fear',
            'embarrassment': 'Fear',
            
            // Neutral category
            'neutral': 'Neutral',
            'realization': 'Neutral',
            'surprise': 'Neutral',
            'curiosity': 'Neutral',
            'confusion': 'Neutral',
            'desire': 'Neutral'
        };
        
        // Thresholds
        this.ANGER_THRESHOLD = 0.6;
        this.HIGH_ANGER_THRESHOLD = 0.75;
    }
    
    /**
     * Initialize both sentiment models
     * @returns {Promise<void>}
     */
    async initialize() {
        if (this.isInitialized) {
            return;
        }
        
        if (this.isInitializing) {
            return this.initPromise;
        }
        
        this.isInitializing = true;
        
        this.initPromise = (async () => {
            try {
                console.log('[SentimentAnalyzer] Initializing models...');
                
                // Load multilingual sentiment model (EN/ES) from local files
                // Models are bundled with the app for faster loading and offline support
                this.sentimentClassifier = await pipeline(
                    'text-classification',
                    '/models/sentiment/', // Local path to quantized model
                    { quantized: true, local_files_only: true }
                );
                
                console.log('[SentimentAnalyzer] Sentiment model loaded');
                
                // Load emotion detection model from local files
                this.emotionClassifier = await pipeline(
                    'text-classification',
                    '/models/emotion/', // Local path to quantized model
                    { topk: 5, quantized: true, local_files_only: true } // Get top 5 emotions
                );
                
                console.log('[SentimentAnalyzer] Emotion model loaded');
                
                this.isInitialized = true;
                this.isInitializing = false;
                
                console.log('[SentimentAnalyzer] ✅ All models ready');
            } catch (error) {
                console.error('[SentimentAnalyzer] ❌ Initialization failed:', error);
                this.isInitializing = false;
                throw error;
            }
        })();
        
        return this.initPromise;
    }
    
    /**
     * Analyze text sentiment and emotions
     * @param {string} text - Text to analyze
     * @param {string} language - Language code (en or es)
     * @returns {Promise<Object>} Analysis result
     */
    async analyze(text, language = 'en') {
        if (!text || text.trim().length === 0) {
            throw new Error('Text cannot be empty');
        }
        
        // Initialize if not already done
        if (!this.isInitialized) {
            await this.initialize();
        }
        
        try {
            console.log(`[SentimentAnalyzer] Analyzing ${language} text (${text.length} chars)`);
            
            // Step 1: Get sentiment polarity
            const sentimentResult = await this.sentimentClassifier(text);
            const sentiment = sentimentResult[0];
            
            // Convert to polarity score (-1 to +1)
            const polarity = this.calculatePolarity(sentiment.label, sentiment.score);
            
            // Step 2: Get detailed emotions
            const emotionResults = await this.emotionClassifier(text);
            
            // Step 3: Map to 5 categories and aggregate
            const emotionScores = this.aggregateEmotions(emotionResults);
            
            // Step 4: Determine primary emotion
            const primaryEmotion = this.getPrimaryEmotion(emotionScores);
            
            // Step 5: Check for anger/moderation flags
            const hasAnger = emotionScores.Anger >= this.ANGER_THRESHOLD;
            const needsReview = emotionScores.Anger >= this.HIGH_ANGER_THRESHOLD;
            
            const result = {
                primaryEmotion: primaryEmotion.name,
                emotionScore: primaryEmotion.score,
                sentimentPolarity: polarity,
                emotionScores: {
                    joy: emotionScores.Joy,
                    sadness: emotionScores.Sadness,
                    anger: emotionScores.Anger,
                    fear: emotionScores.Fear,
                    neutral: emotionScores.Neutral
                },
                hasAnger: hasAnger,
                needsReview: needsReview,
                language: language,
                analysisSource: 'client',
                analyzedAt: new Date().toISOString()
            };
            
            console.log('[SentimentAnalyzer] ✅ Analysis complete:', result);
            return result;
            
        } catch (error) {
            console.error('[SentimentAnalyzer] ❌ Analysis failed:', error);
            throw error;
        }
    }
    
    /**
     * Convert sentiment label to polarity score
     * @private
     */
    calculatePolarity(label, score) {
        // Labels: positive, negative, neutral
        if (label === 'positive') {
            return score; // 0 to 1
        } else if (label === 'negative') {
            return -score; // -1 to 0
        } else {
            return 0; // neutral
        }
    }
    
    /**
     * Aggregate 28 emotions into 5 categories
     * @private
     */
    aggregateEmotions(emotionResults) {
        const scores = {
            Joy: 0,
            Sadness: 0,
            Anger: 0,
            Fear: 0,
            Neutral: 0
        };
        
        const counts = {
            Joy: 0,
            Sadness: 0,
            Anger: 0,
            Fear: 0,
            Neutral: 0
        };
        
        // Aggregate scores by category
        for (const emotion of emotionResults) {
            const category = this.emotionMapping[emotion.label];
            if (category) {
                scores[category] += emotion.score;
                counts[category]++;
            }
        }
        
        // Average scores
        for (const category in scores) {
            if (counts[category] > 0) {
                scores[category] = scores[category] / counts[category];
            }
        }
        
        // Normalize to sum to 1.0
        const total = Object.values(scores).reduce((a, b) => a + b, 0);
        if (total > 0) {
            for (const category in scores) {
                scores[category] = scores[category] / total;
            }
        }
        
        return scores;
    }
    
    /**
     * Get primary emotion with highest score
     * @private
     */
    getPrimaryEmotion(emotionScores) {
        let maxScore = 0;
        let primaryEmotion = 'Neutral';
        
        for (const [emotion, score] of Object.entries(emotionScores)) {
            if (score > maxScore) {
                maxScore = score;
                primaryEmotion = emotion;
            }
        }
        
        return { name: primaryEmotion, score: maxScore };
    }
    
    /**
     * Check if sentiment analysis is supported
     * @returns {boolean}
     */
    isSupported() {
        return typeof Worker !== 'undefined' && typeof WebAssembly !== 'undefined';
    }
}

// Create singleton instance
const sentimentAnalyzer = new SentimentAnalyzer();

// Export for ES6 modules
export default sentimentAnalyzer;

// Global namespace for Blazor JS Interop
window.SentimentAnalyzer = {
    /**
     * Analyze text sentiment (called from Blazor)
     * @param {string} text - Text to analyze
     * @param {string} language - Language code (en or es)
     * @returns {Promise<Object>} Analysis result
     */
    analyze: async (text, language) => {
        try {
            return await sentimentAnalyzer.analyze(text, language);
        } catch (error) {
            console.error('[SentimentAnalyzer] Error:', error);
            return {
                primaryEmotion: 'Neutral',
                emotionScore: 0.5,
                sentimentPolarity: 0,
                emotionScores: {
                    joy: 0.2,
                    sadness: 0.2,
                    anger: 0.2,
                    fear: 0.2,
                    neutral: 0.2
                },
                hasAnger: false,
                needsReview: false,
                language: language,
                analysisSource: 'client-error',
                analyzedAt: new Date().toISOString(),
                error: error.message
            };
        }
    },
    
    /**
     * Check if browser supports sentiment analysis
     * @returns {boolean}
     */
    isSupported: () => {
        return sentimentAnalyzer.isSupported();
    },
    
    /**
     * Pre-initialize models (optional)
     * @returns {Promise<void>}
     */
    initialize: async () => {
        return await sentimentAnalyzer.initialize();
    }
};

console.log('[SentimentAnalyzer] Module loaded. Use window.SentimentAnalyzer.analyze(text, language)');
