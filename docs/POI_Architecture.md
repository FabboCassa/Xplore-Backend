# POI Storage & Retrieval Architecture

## Core Decision
To prevent explosive database growth and reduce infrastructure costs, **the backend will NOT permanently store Points of Interest (POIs, museums, artworks, etc.) in a relational or document database.**

Instead, we will use a **Stateless Proxy Backend + Client-Side Caching** architecture.

## 1. Backend Roles (Stateless Proxy)
- Receives map boundary queries (Lat/Lon/Radius or Bounding Box) from the mobile App.
- Acts as a proxy to fetch data in real-time from **OpenStreetMap (Overpass API)** which allows commercial use for free.
- Normalizes and merges the external JSON data into our standard `MapPin` / `Museum` DTO format.
- Returns the compiled list to the client.
- **Zero POI storage:** The backend DB only stores Users, Authentication, User Progress/Achievements, and core app metadata. It does not store the worldwide map of museums.

## 2. Frontend / App Roles (Client-Side Caching)
- Receives the POIs from the Backend.
- Displays them on the MapLibre map.
- Saves the fetched POIs into the device's **local database (SQLDelight)** for offline capability and fast re-rendering.
- **Auto-Expiry:** The App will run a cleanup routine to delete any cached POI that is older than **30 days** to free up user storage.
- **Manual Cleanup:** A "Clear Map Cache" button will be added to the App Settings, allowing users to manually delete downloaded map data without affecting their login, visited places, or achievements.

## 3. Future Enhancements
- If OSM is not enough, a secondary free/cheap API fallback can be added *on the backend*.
- Visited/Saved places by the user *will* be stored permanently on the backend (as a relation between `UserId` and a basic `POI_ID`), but the global map catalog will remain dynamic.
