#include "geofence.h"
#include "config.h"
#include <math.h>

#ifndef M_PI
#define M_PI 3.14159265358979323846
#endif

#define DEG_TO_RAD (M_PI / 180.0)
#define METERS_PER_DEG_LAT 111320.0

// ── Reference Point ─────────────────────────────────────────
static double ref_lat = 0;
static double ref_lng = 0;
static double cos_ref_lat = 1.0;

// ── Fence Storage ───────────────────────────────────────────
static FenceDefinition fences[GEOFENCE_MAX_FENCES];
static int fence_count = 0;

// ── Breach State (for hysteresis) ───────────────────────────
static int consecutive_outside[GEOFENCE_MAX_FENCES] = {0};
#define BREACH_CONFIRM_COUNT 3
#define BREACH_CONFIRM_DIST  1.0f

void geofence_set_reference(double lat, double lng) {
    ref_lat = lat;
    ref_lng = lng;
    cos_ref_lat = cos(lat * DEG_TO_RAD);
}

LocalPoint geofence_to_local(double lat, double lng) {
    LocalPoint p;
    p.y = (float)((lat - ref_lat) * METERS_PER_DEG_LAT);
    p.x = (float)((lng - ref_lng) * METERS_PER_DEG_LAT * cos_ref_lat);
    return p;
}

void geofence_clear_all() {
    fence_count = 0;
    for (int i = 0; i < GEOFENCE_MAX_FENCES; i++) {
        consecutive_outside[i] = 0;
    }
}

bool geofence_add(const FenceDefinition* fence) {
    if (fence_count >= GEOFENCE_MAX_FENCES) return false;
    fences[fence_count] = *fence;
    fence_count++;
    return true;
}

int geofence_count() {
    return fence_count;
}

void geofence_reload() {
    // Placeholder — load from NVS/SPIFFS via storage module
    // Called after fence sync from API
}

// ── Ray-Casting Polygon Containment ─────────────────────────
bool point_in_polygon(const LocalPoint* polygon, int count, float px, float py) {
    bool inside = false;
    for (int i = 0, j = count - 1; i < count; j = i++) {
        float xi = polygon[i].x, yi = polygon[i].y;
        float xj = polygon[j].x, yj = polygon[j].y;

        if (((yi > py) != (yj > py)) &&
            (px < (xj - xi) * (py - yi) / (yj - yi) + xi)) {
            inside = !inside;
        }
    }
    return inside;
}

// ── Distance from Point to Line Segment ─────────────────────
float point_to_segment_distance(float ax, float ay, float bx, float by,
                                float px, float py) {
    float dx = bx - ax;
    float dy = by - ay;
    float len_sq = dx * dx + dy * dy;

    if (len_sq < 1e-8f) {
        // Degenerate segment (point)
        float ex = px - ax;
        float ey = py - ay;
        return sqrtf(ex * ex + ey * ey);
    }

    float t = ((px - ax) * dx + (py - ay) * dy) / len_sq;
    if (t < 0.0f) t = 0.0f;
    if (t > 1.0f) t = 1.0f;

    float closest_x = ax + t * dx;
    float closest_y = ay + t * dy;
    float ex = px - closest_x;
    float ey = py - closest_y;
    return sqrtf(ex * ex + ey * ey);
}

// ── Distance from Point to Polygon Boundary ─────────────────
float point_to_polygon_distance(const LocalPoint* polygon, int count,
                                float px, float py) {
    float min_dist = 1e9f;
    for (int i = 0, j = count - 1; i < count; j = i++) {
        float d = point_to_segment_distance(
            polygon[j].x, polygon[j].y,
            polygon[i].x, polygon[i].y,
            px, py
        );
        if (d < min_dist) min_dist = d;
    }
    return min_dist;
}

// ── Distance from Point to Circle Boundary ──────────────────
float point_to_circle_distance(float cx, float cy, float radius,
                               float px, float py) {
    float dx = px - cx;
    float dy = py - cy;
    float dist_to_center = sqrtf(dx * dx + dy * dy);
    return fabsf(dist_to_center - radius);
}

// ── Evaluate Single Fence ───────────────────────────────────
static FenceStatus evaluate_single_fence(const FenceDefinition* fence,
                                         float px, float py,
                                         float* distance_out) {
    bool inside = false;
    float distance = 0;

    switch (fence->type) {
        case FENCE_POLYGON:
            inside = point_in_polygon(fence->points, fence->point_count, px, py);
            distance = point_to_polygon_distance(fence->points, fence->point_count, px, py);
            break;

        case FENCE_CIRCLE: {
            float cx = fence->points[0].x;
            float cy = fence->points[0].y;
            float dx = px - cx;
            float dy = py - cy;
            float dist_to_center = sqrtf(dx * dx + dy * dy);
            inside = (dist_to_center <= fence->radius);
            distance = fabsf(dist_to_center - fence->radius);
            break;
        }

        case FENCE_CORRIDOR: {
            // Check if point is within width of any corridor segment
            float min_dist = 1e9f;
            for (int i = 0; i < fence->point_count - 1; i++) {
                float d = point_to_segment_distance(
                    fence->points[i].x, fence->points[i].y,
                    fence->points[i+1].x, fence->points[i+1].y,
                    px, py
                );
                if (d < min_dist) min_dist = d;
            }
            inside = (min_dist <= fence->width / 2.0f);
            distance = fabsf(min_dist - fence->width / 2.0f);
            break;
        }
    }

    if (distance_out) *distance_out = distance;

    // For ALLOW fences: inside = good, outside = bad
    // For DENY fences: inside = bad, outside = good
    bool in_allowed_zone;
    if (fence->rule == FENCE_ALLOW) {
        in_allowed_zone = inside;
    } else {
        in_allowed_zone = !inside;
    }

    if (in_allowed_zone) {
        if (distance < GEOFENCE_WARNING_DIST_M) {
            return FENCE_STATUS_WARNING;
        }
        return FENCE_STATUS_INSIDE;
    } else {
        // Outside allowed zone (or inside deny zone)
        if (distance < GEOFENCE_HYSTERESIS_M) {
            return FENCE_STATUS_WARNING;
        }
        return FENCE_STATUS_OUTSIDE;
    }
}

// ── Evaluate All Fences ─────────────────────────────────────
// Deny fences override allow fences
FenceEvaluation evaluate_fences(double lat, double lng) {
    FenceEvaluation result = {false, false, 0, 1e9f, FENCE_ALLOW};

    if (fence_count == 0) return result;

    LocalPoint p = geofence_to_local(lat, lng);

    // First pass: check deny fences (they override everything)
    for (int i = 0; i < fence_count; i++) {
        if (fences[i].rule != FENCE_DENY) continue;

        float distance;
        FenceStatus status = evaluate_single_fence(&fences[i], p.x, p.y, &distance);

        if (status == FENCE_STATUS_OUTSIDE) {
            // We're inside a deny zone
            consecutive_outside[i]++;
            if (consecutive_outside[i] >= BREACH_CONFIRM_COUNT) {
                result.in_breach = true;
                result.breached_fence_id = fences[i].id;
                result.breached_fence_rule = FENCE_DENY;
                result.distance_to_nearest_edge = distance;
                return result;
            }
        } else {
            consecutive_outside[i] = 0;
        }

        if (status == FENCE_STATUS_WARNING) {
            result.in_warning = true;
        }
        if (distance < result.distance_to_nearest_edge) {
            result.distance_to_nearest_edge = distance;
        }
    }

    // Second pass: check allow fences
    for (int i = 0; i < fence_count; i++) {
        if (fences[i].rule != FENCE_ALLOW) continue;

        float distance;
        FenceStatus status = evaluate_single_fence(&fences[i], p.x, p.y, &distance);

        if (status == FENCE_STATUS_OUTSIDE) {
            consecutive_outside[i]++;
            if (consecutive_outside[i] >= BREACH_CONFIRM_COUNT) {
                result.in_breach = true;
                result.breached_fence_id = fences[i].id;
                result.breached_fence_rule = FENCE_ALLOW;
                result.distance_to_nearest_edge = distance;
                return result;
            }
        } else {
            consecutive_outside[i] = 0;
        }

        if (status == FENCE_STATUS_WARNING) {
            result.in_warning = true;
        }
        if (distance < result.distance_to_nearest_edge) {
            result.distance_to_nearest_edge = distance;
        }
    }

    return result;
}
