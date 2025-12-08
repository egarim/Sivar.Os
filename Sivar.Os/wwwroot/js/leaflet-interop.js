/**
 * Leaflet.js Interop for Blazor
 * Provides map functionality for location-based posts
 * 
 * @author Sivar.Os Team
 * @date December 2025
 */

// Store map instances by ID for cleanup and updates
window._leafletMaps = window._leafletMaps || {};

/**
 * Initialize a new Leaflet map instance
 * @param {string} mapId - Unique ID of the map container element
 * @param {number} lat - Initial latitude
 * @param {number} lng - Initial longitude
 * @param {number} zoom - Initial zoom level (1-18)
 * @param {object} options - Additional options
 * @param {object} dotNetRef - .NET object reference for callbacks (optional)
 */
window.initializeMap = function (mapId, lat, lng, zoom = 13, options = {}, dotNetRef = null) {
    try {
        // Check if map already exists
        if (window._leafletMaps[mapId]) {
            console.log(`[Leaflet] Map ${mapId} already exists, destroying first`);
            window.destroyMap(mapId);
        }

        const container = document.getElementById(mapId);
        if (!container) {
            console.error(`[Leaflet] Container not found: ${mapId}`);
            return false;
        }

        // Create map centered on location
        const map = L.map(mapId, {
            center: [lat, lng],
            zoom: zoom,
            scrollWheelZoom: options.scrollWheelZoom !== false,
            dragging: options.dragging !== false,
            zoomControl: options.zoomControl !== false
        });

        // Add OpenStreetMap tiles (FREE, no API key needed)
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
            maxZoom: 19
        }).addTo(map);

        // Store map instance
        window._leafletMaps[mapId] = {
            map: map,
            marker: null,
            markers: [],
            circle: null,
            dotNetRef: dotNetRef
        };

        console.log(`[Leaflet] Map initialized: ${mapId} at (${lat}, ${lng})`);
        return true;
    } catch (error) {
        console.error(`[Leaflet] Error initializing map ${mapId}:`, error);
        return false;
    }
};

/**
 * Add a single marker to the map
 * @param {string} mapId - Map ID
 * @param {number} lat - Marker latitude
 * @param {number} lng - Marker longitude
 * @param {object} options - Marker options (title, popup, draggable, iconType)
 */
window.addMapMarker = function (mapId, lat, lng, options = {}) {
    try {
        const instance = window._leafletMaps[mapId];
        if (!instance) {
            console.error(`[Leaflet] Map not found: ${mapId}`);
            return false;
        }

        // Remove existing single marker if present
        if (instance.marker) {
            instance.map.removeLayer(instance.marker);
        }

        // Create marker icon based on type
        const icon = getMarkerIcon(options.iconType);

        // Create marker
        const marker = L.marker([lat, lng], {
            draggable: options.draggable === true,
            title: options.title || '',
            icon: icon
        }).addTo(instance.map);

        // Add popup if provided
        if (options.popup) {
            marker.bindPopup(options.popup);
            if (options.openPopup) {
                marker.openPopup();
            }
        }

        // Handle marker drag events
        if (options.draggable && instance.dotNetRef) {
            marker.on('dragend', function (e) {
                const pos = marker.getLatLng();
                instance.dotNetRef.invokeMethodAsync('OnMarkerDragEnd', pos.lat, pos.lng);
            });
        }

        instance.marker = marker;
        console.log(`[Leaflet] Marker added to ${mapId} at (${lat}, ${lng})`);
        return true;
    } catch (error) {
        console.error(`[Leaflet] Error adding marker to ${mapId}:`, error);
        return false;
    }
};

/**
 * Update the position of the main marker
 * @param {string} mapId - Map ID
 * @param {number} lat - New latitude
 * @param {number} lng - New longitude
 * @param {boolean} panTo - Whether to pan the map to the new position
 */
window.updateMapMarker = function (mapId, lat, lng, panTo = true) {
    try {
        const instance = window._leafletMaps[mapId];
        if (!instance) {
            console.error(`[Leaflet] Map not found: ${mapId}`);
            return false;
        }

        if (instance.marker) {
            instance.marker.setLatLng([lat, lng]);
        } else {
            // Create marker if it doesn't exist
            window.addMapMarker(mapId, lat, lng, {});
        }

        if (panTo) {
            instance.map.setView([lat, lng], instance.map.getZoom());
        }

        return true;
    } catch (error) {
        console.error(`[Leaflet] Error updating marker on ${mapId}:`, error);
        return false;
    }
};

/**
 * Display multiple locations with markers
 * @param {string} mapId - Map ID
 * @param {Array} locations - Array of location objects
 *   Each: { lat, lng, title, description, iconType, id }
 * @param {boolean} fitBounds - Whether to auto-zoom to fit all markers
 */
window.displayLocations = function (mapId, locations, fitBounds = true) {
    try {
        const instance = window._leafletMaps[mapId];
        if (!instance) {
            console.error(`[Leaflet] Map not found: ${mapId}`);
            return false;
        }

        // Clear existing markers
        instance.markers.forEach(m => instance.map.removeLayer(m));
        instance.markers = [];

        if (!locations || locations.length === 0) {
            console.log(`[Leaflet] No locations to display on ${mapId}`);
            return true;
        }

        // Add markers for each location
        locations.forEach(loc => {
            const icon = getMarkerIcon(loc.iconType);
            
            const marker = L.marker([loc.lat, loc.lng], {
                title: loc.title || '',
                icon: icon
            }).addTo(instance.map);

            // Create popup content
            const popupContent = createPopupContent(loc);
            marker.bindPopup(popupContent);

            // Handle click callback
            if (instance.dotNetRef && loc.id) {
                marker.on('click', function () {
                    instance.dotNetRef.invokeMethodAsync('OnMarkerClick', loc.id);
                });
            }

            instance.markers.push(marker);
        });

        // Fit map to show all markers
        if (fitBounds && instance.markers.length > 0) {
            const group = L.featureGroup(instance.markers);
            instance.map.fitBounds(group.getBounds().pad(0.1));
        }

        console.log(`[Leaflet] Displayed ${locations.length} locations on ${mapId}`);
        return true;
    } catch (error) {
        console.error(`[Leaflet] Error displaying locations on ${mapId}:`, error);
        return false;
    }
};

/**
 * Enable click-to-select mode for picking locations
 * @param {string} mapId - Map ID
 */
window.enableLocationPicking = function (mapId) {
    try {
        const instance = window._leafletMaps[mapId];
        if (!instance) {
            console.error(`[Leaflet] Map not found: ${mapId}`);
            return false;
        }

        // Remove existing click handler
        instance.map.off('click');

        // Add click handler
        instance.map.on('click', function (e) {
            const { lat, lng } = e.latlng;

            // Update or create marker
            window.updateMapMarker(mapId, lat, lng, false);

            // Callback to Blazor
            if (instance.dotNetRef) {
                instance.dotNetRef.invokeMethodAsync('OnMapLocationSelected', lat, lng);
            }
        });

        console.log(`[Leaflet] Location picking enabled on ${mapId}`);
        return true;
    } catch (error) {
        console.error(`[Leaflet] Error enabling location picking on ${mapId}:`, error);
        return false;
    }
};

/**
 * Add a radius circle around a point
 * @param {string} mapId - Map ID
 * @param {number} lat - Center latitude
 * @param {number} lng - Center longitude
 * @param {number} radiusKm - Radius in kilometers
 * @param {string} color - Circle color (default: blue)
 */
window.addRadiusCircle = function (mapId, lat, lng, radiusKm, color = '#2196F3') {
    try {
        const instance = window._leafletMaps[mapId];
        if (!instance) {
            console.error(`[Leaflet] Map not found: ${mapId}`);
            return false;
        }

        // Remove existing circle
        if (instance.circle) {
            instance.map.removeLayer(instance.circle);
        }

        // Add new circle (radius in meters)
        instance.circle = L.circle([lat, lng], {
            color: color,
            fillColor: color,
            fillOpacity: 0.1,
            radius: radiusKm * 1000
        }).addTo(instance.map);

        return true;
    } catch (error) {
        console.error(`[Leaflet] Error adding circle to ${mapId}:`, error);
        return false;
    }
};

/**
 * Set map view (center and zoom)
 * @param {string} mapId - Map ID
 * @param {number} lat - Center latitude
 * @param {number} lng - Center longitude
 * @param {number} zoom - Zoom level (optional)
 */
window.setMapView = function (mapId, lat, lng, zoom = null) {
    try {
        const instance = window._leafletMaps[mapId];
        if (!instance) {
            console.error(`[Leaflet] Map not found: ${mapId}`);
            return false;
        }

        const zoomLevel = zoom || instance.map.getZoom();
        instance.map.setView([lat, lng], zoomLevel);
        return true;
    } catch (error) {
        console.error(`[Leaflet] Error setting view on ${mapId}:`, error);
        return false;
    }
};

/**
 * Get current map center
 * @param {string} mapId - Map ID
 * @returns {object} { lat, lng } or null
 */
window.getMapCenter = function (mapId) {
    try {
        const instance = window._leafletMaps[mapId];
        if (!instance) {
            return null;
        }

        const center = instance.map.getCenter();
        return { lat: center.lat, lng: center.lng };
    } catch (error) {
        console.error(`[Leaflet] Error getting center of ${mapId}:`, error);
        return null;
    }
};

/**
 * Invalidate map size (call after container resize)
 * @param {string} mapId - Map ID
 */
window.invalidateMapSize = function (mapId) {
    try {
        const instance = window._leafletMaps[mapId];
        if (instance) {
            instance.map.invalidateSize();
            return true;
        }
        return false;
    } catch (error) {
        console.error(`[Leaflet] Error invalidating size of ${mapId}:`, error);
        return false;
    }
};

/**
 * Destroy a map instance and clean up resources
 * @param {string} mapId - Map ID
 */
window.destroyMap = function (mapId) {
    try {
        const instance = window._leafletMaps[mapId];
        if (instance) {
            // Remove all markers
            if (instance.marker) {
                instance.map.removeLayer(instance.marker);
            }
            instance.markers.forEach(m => instance.map.removeLayer(m));
            
            // Remove circle
            if (instance.circle) {
                instance.map.removeLayer(instance.circle);
            }

            // Destroy map
            instance.map.remove();

            // Clean up reference
            delete window._leafletMaps[mapId];

            console.log(`[Leaflet] Map destroyed: ${mapId}`);
        }
        return true;
    } catch (error) {
        console.error(`[Leaflet] Error destroying map ${mapId}:`, error);
        return false;
    }
};

// ============ Helper Functions ============

/**
 * Get marker icon based on type
 * @param {string} iconType - Type of icon (MainOffice, CustomerBranch, etc.)
 * @returns {L.Icon} Leaflet icon
 */
function getMarkerIcon(iconType) {
    // Default marker colors by business location type
    const iconColors = {
        'MainOffice': '#1976D2',        // Blue
        'CustomerBranch': '#388E3C',     // Green
        'AdministrativeOffice': '#757575', // Gray
        'RetailStore': '#F57C00',        // Orange
        'Warehouse': '#795548',          // Brown
        'ServiceCenter': '#D32F2F',      // Red
        'Default': '#1976D2'             // Blue
    };

    const color = iconColors[iconType] || iconColors['Default'];

    // Use default Leaflet marker with color tint via CSS filter
    // For production, consider using custom SVG markers
    return L.icon({
        iconUrl: `https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-${getColorName(color)}.png`,
        shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
        iconSize: [25, 41],
        iconAnchor: [12, 41],
        popupAnchor: [1, -34],
        shadowSize: [41, 41]
    });
}

/**
 * Map hex color to leaflet-color-markers color name
 */
function getColorName(hexColor) {
    const colorMap = {
        '#1976D2': 'blue',
        '#388E3C': 'green',
        '#757575': 'grey',
        '#F57C00': 'orange',
        '#795548': 'orange', // No brown, use orange
        '#D32F2F': 'red'
    };
    return colorMap[hexColor] || 'blue';
}

/**
 * Create HTML popup content for a location marker
 * @param {object} loc - Location object
 * @returns {string} HTML content
 */
function createPopupContent(loc) {
    let html = '<div class="leaflet-popup-content-inner">';
    
    if (loc.title) {
        html += `<strong>${escapeHtml(loc.title)}</strong>`;
    }
    
    if (loc.description) {
        html += `<br><span>${escapeHtml(loc.description)}</span>`;
    }
    
    if (loc.address) {
        html += `<br><small>📍 ${escapeHtml(loc.address)}</small>`;
    }
    
    if (loc.distance !== undefined && loc.distance !== null) {
        html += `<br><small>📏 ${loc.distance.toFixed(2)} km away</small>`;
    }
    
    // Add "Get Directions" link
    if (loc.lat && loc.lng) {
        const directionsUrl = `https://www.google.com/maps/dir/?api=1&destination=${loc.lat},${loc.lng}`;
        html += `<br><a href="${directionsUrl}" target="_blank" rel="noopener" class="directions-link">🧭 Get Directions</a>`;
    }
    
    html += '</div>';
    return html;
}

/**
 * Escape HTML to prevent XSS
 */
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ============ Leaflet Interop Namespace (for Blazor) ============

window.leafletInterop = {
    /**
     * Initialize a map with a .NET reference for callbacks
     */
    initializeMap: function (mapId, lat, lng, zoom = 13) {
        return window.initializeMap(mapId, lat, lng, zoom, {}, null);
    },
    
    /**
     * Add a marker (simplified for LocationPicker)
     */
    addMapMarker: function (mapId, lat, lng, popupText, draggable = false) {
        return window.addMapMarker(mapId, lat, lng, {
            popup: popupText,
            draggable: draggable,
            openPopup: true
        });
    },
    
    /**
     * Enable location picking with .NET callback
     */
    enableLocationPicking: function (mapId, dotNetRef) {
        const instance = window._leafletMaps[mapId];
        if (instance) {
            instance.dotNetRef = dotNetRef;
            
            // Enable marker dragging callbacks
            if (instance.marker) {
                instance.marker.options.draggable = true;
                if (instance.marker.dragging) {
                    instance.marker.dragging.enable();
                }
                instance.marker.on('dragend', function () {
                    const pos = instance.marker.getLatLng();
                    dotNetRef.invokeMethodAsync('OnMarkerDragged', pos.lat, pos.lng);
                });
            }
        }
        return window.enableLocationPicking(mapId);
    },
    
    /**
     * Set map center and zoom
     */
    setMapCenter: function (mapId, lat, lng, zoom) {
        return window.setMapView(mapId, lat, lng, zoom);
    },
    
    /**
     * Clear all markers from the map
     */
    clearMarkers: function (mapId) {
        try {
            const instance = window._leafletMaps[mapId];
            if (!instance) return false;
            
            if (instance.marker) {
                instance.map.removeLayer(instance.marker);
                instance.marker = null;
            }
            
            instance.markers.forEach(m => instance.map.removeLayer(m));
            instance.markers = [];
            
            return true;
        } catch (error) {
            console.error(`[Leaflet] Error clearing markers on ${mapId}:`, error);
            return false;
        }
    },
    
    /**
     * Dispose/destroy the map
     */
    disposeMap: function (mapId) {
        return window.destroyMap(mapId);
    },
    
    /**
     * Search for an address using Nominatim (OpenStreetMap)
     * @param {string} query - Search query
     * @returns {Promise<Array>} Array of search results
     */
    searchAddress: async function (query) {
        try {
            const url = `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&addressdetails=1&limit=5`;
            
            const response = await fetch(url, {
                headers: {
                    'Accept': 'application/json',
                    'User-Agent': 'SivarOs/1.0'
                }
            });
            
            if (!response.ok) {
                throw new Error(`Search failed: ${response.status}`);
            }
            
            const data = await response.json();
            
            return data.map(item => ({
                lat: parseFloat(item.lat),
                lon: parseFloat(item.lon),
                displayName: item.display_name,
                city: item.address?.city || item.address?.town || item.address?.village || item.address?.municipality,
                state: item.address?.state || item.address?.region,
                country: item.address?.country
            }));
        } catch (error) {
            console.error('[Leaflet] Address search error:', error);
            return [];
        }
    },
    
    /**
     * Reverse geocode coordinates to get address
     * @param {number} lat - Latitude
     * @param {number} lng - Longitude
     * @returns {Promise<object>} Geocode result
     */
    reverseGeocode: async function (lat, lng) {
        try {
            const url = `https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&addressdetails=1`;
            
            const response = await fetch(url, {
                headers: {
                    'Accept': 'application/json',
                    'User-Agent': 'SivarOs/1.0'
                }
            });
            
            if (!response.ok) {
                throw new Error(`Reverse geocode failed: ${response.status}`);
            }
            
            const data = await response.json();
            
            return {
                displayName: data.display_name,
                city: data.address?.city || data.address?.town || data.address?.village || data.address?.municipality,
                town: data.address?.town,
                village: data.address?.village,
                state: data.address?.state || data.address?.region,
                country: data.address?.country
            };
        } catch (error) {
            console.error('[Leaflet] Reverse geocode error:', error);
            return null;
        }
    },
    
    /**
     * Display multiple locations (used by profile locations view)
     */
    displayLocations: function (mapId, locations, fitBounds = true) {
        return window.displayLocations(mapId, locations, fitBounds);
    },
    
    /**
     * Invalidate map size - call this when the map container has resized
     * Leaflet needs this to recalculate its dimensions
     */
    invalidateSize: function (mapId) {
        try {
            const instance = window._leafletMaps[mapId];
            if (!instance || !instance.map) {
                console.warn(`[Leaflet] Map not found for invalidateSize: ${mapId}`);
                return false;
            }
            
            // Small delay to ensure DOM has updated
            setTimeout(() => {
                instance.map.invalidateSize({ animate: false, pan: false });
                
                // Re-center on marker if exists
                if (instance.marker) {
                    const pos = instance.marker.getLatLng();
                    instance.map.setView(pos, instance.map.getZoom(), { animate: false });
                }
                
                console.log(`[Leaflet] Invalidated size for ${mapId}`);
            }, 150);
            
            return true;
        } catch (error) {
            console.error(`[Leaflet] Error invalidating size on ${mapId}:`, error);
            return false;
        }
    },
    
    /**
     * Invalidate size for all maps on the page
     */
    invalidateAllMaps: function () {
        try {
            const mapIds = Object.keys(window._leafletMaps);
            mapIds.forEach(mapId => {
                this.invalidateSize(mapId);
            });
            console.log(`[Leaflet] Invalidated ${mapIds.length} maps`);
            return true;
        } catch (error) {
            console.error('[Leaflet] Error invalidating all maps:', error);
            return false;
        }
    }
};

// Auto-invalidate maps on window resize
window.addEventListener('resize', function () {
    if (window.leafletInterop && typeof window.leafletInterop.invalidateAllMaps === 'function') {
        // Debounce resize events
        clearTimeout(window._leafletResizeTimeout);
        window._leafletResizeTimeout = setTimeout(() => {
            window.leafletInterop.invalidateAllMaps();
        }, 200);
    }
});

console.log('[Leaflet Interop] Loaded successfully');
