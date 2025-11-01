// Browser Permissions Management
// iOS-style permission handling for Geolocation and future permissions

export function isGeolocationSupported() {
    return 'geolocation' in navigator;
}

export async function getLocationPermissionStatus() {
    if (!isGeolocationSupported()) {
        return 'denied';
    }

    try {
        // Check if Permissions API is supported
        if (navigator.permissions && navigator.permissions.query) {
            const result = await navigator.permissions.query({ name: 'geolocation' });
            return result.state; // 'granted', 'denied', or 'prompt'
        }
        
        // Fallback: assume prompt if we can't check
        return 'prompt';
    } catch (error) {
        console.warn('Permissions API not supported:', error);
        return 'prompt';
    }
}

export function requestLocation() {
    return new Promise((resolve, reject) => {
        if (!isGeolocationSupported()) {
            reject(new Error('Geolocation is not supported by this browser'));
            return;
        }

        const options = {
            enableHighAccuracy: true,
            timeout: 10000,
            maximumAge: 0
        };

        navigator.geolocation.getCurrentPosition(
            (position) => {
                resolve({
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude,
                    accuracy: position.coords.accuracy,
                    altitude: position.coords.altitude,
                    altitudeAccuracy: position.coords.altitudeAccuracy,
                    heading: position.coords.heading,
                    speed: position.coords.speed,
                    timestamp: position.timestamp
                });
            },
            (error) => {
                console.error('Geolocation error:', error);
                reject(error);
            },
            options
        );
    });
}

export function getCurrentPosition() {
    return requestLocation(); // Same implementation for now
}

// Future: Camera permission
export async function requestCameraPermission() {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ video: true });
        stream.getTracks().forEach(track => track.stop()); // Stop immediately, we just wanted permission
        return 'granted';
    } catch (error) {
        console.error('Camera permission denied:', error);
        return 'denied';
    }
}

// Future: Microphone permission
export async function requestMicrophonePermission() {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        stream.getTracks().forEach(track => track.stop());
        return 'granted';
    } catch (error) {
        console.error('Microphone permission denied:', error);
        return 'denied';
    }
}

// Future: Notifications permission
export async function requestNotificationPermission() {
    if (!('Notification' in window)) {
        return 'denied';
    }

    try {
        const permission = await Notification.requestPermission();
        return permission; // 'granted', 'denied', or 'default'
    } catch (error) {
        console.error('Notification permission error:', error);
        return 'denied';
    }
}

// Future: Clipboard read permission
export async function requestClipboardReadPermission() {
    try {
        if (navigator.permissions && navigator.permissions.query) {
            const result = await navigator.permissions.query({ name: 'clipboard-read' });
            return result.state;
        }
        return 'prompt';
    } catch (error) {
        console.error('Clipboard read permission error:', error);
        return 'denied';
    }
}
