/**
 * Profile Context JavaScript Interop
 * Provides device context detection for ProfileContextService
 * 
 * @module context-interop
 * @description Detects timezone, device type, and browser language for profile context
 */

/**
 * Get the device's IANA timezone identifier
 * @returns {string} Timezone like "America/El_Salvador", "America/New_York", or "UTC" as fallback
 */
export function getDeviceTimeZone() {
    try {
        const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
        return timeZone || 'UTC';
    } catch (error) {
        console.warn('[context-interop] Failed to get timezone:', error);
        return 'UTC';
    }
}

/**
 * Get the current local date/time as ISO 8601 string with timezone offset
 * @returns {string} ISO 8601 formatted datetime (e.g., "2025-12-22T10:30:00-06:00")
 */
export function getLocalDateTime() {
    try {
        const now = new Date();
        // Get timezone offset in minutes and convert to hours:minutes format
        const offsetMinutes = now.getTimezoneOffset();
        const offsetHours = Math.floor(Math.abs(offsetMinutes) / 60);
        const offsetMins = Math.abs(offsetMinutes) % 60;
        const offsetSign = offsetMinutes <= 0 ? '+' : '-';
        const offsetStr = `${offsetSign}${String(offsetHours).padStart(2, '0')}:${String(offsetMins).padStart(2, '0')}`;
        
        // Format: YYYY-MM-DDTHH:mm:ss±HH:mm
        const year = now.getFullYear();
        const month = String(now.getMonth() + 1).padStart(2, '0');
        const day = String(now.getDate()).padStart(2, '0');
        const hours = String(now.getHours()).padStart(2, '0');
        const minutes = String(now.getMinutes()).padStart(2, '0');
        const seconds = String(now.getSeconds()).padStart(2, '0');
        
        return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}${offsetStr}`;
    } catch (error) {
        console.warn('[context-interop] Failed to get local datetime:', error);
        return new Date().toISOString();
    }
}

/**
 * Get timezone offset in minutes from UTC
 * Note: JavaScript returns positive for west of UTC, we negate to match .NET convention
 * @returns {number} Offset in minutes (e.g., -360 for CST which is UTC-6)
 */
export function getTimeZoneOffsetMinutes() {
    try {
        // JavaScript getTimezoneOffset() returns positive for west of UTC
        // .NET convention is negative for west of UTC, so we negate
        return -new Date().getTimezoneOffset();
    } catch (error) {
        console.warn('[context-interop] Failed to get timezone offset:', error);
        return 0;
    }
}

/**
 * Detect device type based on screen size and user agent
 * @returns {string} "mobile", "tablet", or "desktop"
 */
export function getDeviceType() {
    try {
        const userAgent = navigator.userAgent.toLowerCase();
        const screenWidth = window.innerWidth;
        const screenHeight = window.innerHeight;

        // Check user agent for specific device indicators
        const isMobileUA = /android|webos|iphone|ipod|blackberry|iemobile|opera mini|mobile/i.test(userAgent);
        const isTabletUA = /ipad|android(?!.*mobile)|tablet|kindle|silk/i.test(userAgent);

        // Screen-based detection as fallback
        const isMobileScreen = screenWidth < 768;
        const isTabletScreen = screenWidth >= 768 && screenWidth < 1024;

        // Touch capability check
        const hasTouch = 'ontouchstart' in window || navigator.maxTouchPoints > 0;

        // Priority: User agent detection, then screen size
        if (isTabletUA) {
            return 'tablet';
        }
        if (isMobileUA) {
            return 'mobile';
        }
        
        // Screen-based fallback for devices that don't identify in UA
        if (isMobileScreen && hasTouch) {
            return 'mobile';
        }
        if (isTabletScreen && hasTouch) {
            return 'tablet';
        }

        return 'desktop';
    } catch (error) {
        console.warn('[context-interop] Failed to detect device type:', error);
        return 'desktop';
    }
}

/**
 * Get browser's preferred language
 * @returns {string} Language code like "es-SV", "en-US", or "en" as fallback
 */
export function getBrowserLanguage() {
    try {
        return navigator.language || navigator.userLanguage || 'en';
    } catch (error) {
        console.warn('[context-interop] Failed to get browser language:', error);
        return 'en';
    }
}

/**
 * Get user agent string
 * @returns {string} Full user agent string
 */
export function getUserAgent() {
    try {
        return navigator.userAgent || '';
    } catch (error) {
        console.warn('[context-interop] Failed to get user agent:', error);
        return '';
    }
}

/**
 * Get complete device context in one call (more efficient than multiple calls)
 * This is the primary function used by ProfileContextService
 * @returns {object} Complete device context object matching DeviceContextJs DTO
 */
export function getDeviceContext() {
    return {
        timeZone: getDeviceTimeZone(),
        localDateTime: getLocalDateTime(),
        timeZoneOffsetMinutes: getTimeZoneOffsetMinutes(),
        deviceType: getDeviceType(),
        language: getBrowserLanguage(),
        userAgent: getUserAgent()
    };
}

/**
 * Check if the browser supports geolocation
 * @returns {boolean} True if geolocation is supported
 */
export function isGeolocationSupported() {
    return 'geolocation' in navigator;
}

/**
 * Add listener for visibility change (to refresh context when user returns to tab)
 * @param {function} callback - Function to call when page becomes visible
 * @returns {function} Cleanup function to remove the listener
 */
export function addVisibilityChangeListener(callback) {
    const handler = () => {
        if (document.visibilityState === 'visible') {
            callback();
        }
    };
    document.addEventListener('visibilitychange', handler);
    
    // Return cleanup function
    return () => document.removeEventListener('visibilitychange', handler);
}

/**
 * Add listener for timezone changes (some browsers support this)
 * @param {function} callback - Function to call when timezone might have changed
 * @returns {function} Cleanup function to remove the listener
 */
export function addTimezoneChangeListener(callback) {
    // Listen for focus events as timezone might change when user was away
    const handler = () => {
        callback();
    };
    window.addEventListener('focus', handler);
    
    // Return cleanup function
    return () => window.removeEventListener('focus', handler);
}
