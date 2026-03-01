#pragma once

#include <stdint.h>
#include <stdbool.h>

// ── Fence Types ─────────────────────────────────────────────
typedef enum {
    FENCE_POLYGON,
    FENCE_CIRCLE,
    FENCE_CORRIDOR
} FenceType;

typedef enum {
    FENCE_ALLOW,
    FENCE_DENY
} FenceRule;

typedef enum {
    FENCE_STATUS_INSIDE,
    FENCE_STATUS_WARNING,   // Within warning distance
    FENCE_STATUS_OUTSIDE,
    FENCE_STATUS_BREACH     // Confirmed breach
} FenceStatus;

// ── Fence Coordinate ────────────────────────────────────────
typedef struct {
    float x;  // Local meters east of reference
    float y;  // Local meters north of reference
} LocalPoint;

// ── Fence Definition (compact, stored in NVS) ───────────────
typedef struct {
    uint16_t   id;
    FenceType  type;
    FenceRule  rule;
    uint8_t    point_count;         // For polygon/corridor
    LocalPoint points[20];          // Polygon vertices or corridor waypoints
    float      radius;              // For circle
    float      width;               // For corridor
    uint8_t    buzz_pattern;        // 0=silent, 1=short, 2=long, 3=continuous
} FenceDefinition;

// ── Evaluation Result ───────────────────────────────────────
typedef struct {
    bool       in_breach;
    bool       in_warning;
    uint16_t   breached_fence_id;
    float      distance_to_nearest_edge;
    FenceRule  breached_fence_rule;
} FenceEvaluation;

// ── Reference Point (for WGS84 → local conversion) ─────────
void geofence_set_reference(double ref_lat, double ref_lng);

// ── Coordinate Conversion ───────────────────────────────────
LocalPoint geofence_to_local(double lat, double lng);

// ── Fence Management ────────────────────────────────────────
void geofence_clear_all();
bool geofence_add(const FenceDefinition* fence);
int  geofence_count();
void geofence_reload();

// ── Evaluation ──────────────────────────────────────────────
FenceEvaluation evaluate_fences(double lat, double lng);

// ── Containment Algorithms (testable independently) ─────────
bool point_in_polygon(const LocalPoint* polygon, int count, float px, float py);
float point_to_polygon_distance(const LocalPoint* polygon, int count, float px, float py);
float point_to_circle_distance(float cx, float cy, float radius, float px, float py);
float point_to_segment_distance(float ax, float ay, float bx, float by, float px, float py);
