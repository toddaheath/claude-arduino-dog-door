#include <unity.h>
#include <math.h>

// Include the testable geofence functions
extern bool point_in_polygon(const struct LocalPointS* polygon, int count, float px, float py);
extern float point_to_polygon_distance(const struct LocalPointS* polygon, int count, float px, float py);
extern float point_to_circle_distance(float cx, float cy, float radius, float px, float py);
extern float point_to_segment_distance(float ax, float ay, float bx, float by, float px, float py);

// Redefine LocalPoint for native test (matches geofence.h)
typedef struct LocalPointS {
    float x;
    float y;
} LocalPoint;

// ── Point in Polygon Tests ──────────────────────────────────

// Simple 10x10 square: (0,0) (10,0) (10,10) (0,10)
static LocalPoint square[] = {
    {0, 0}, {10, 0}, {10, 10}, {0, 10}
};

void test_point_inside_square(void) {
    TEST_ASSERT_TRUE(point_in_polygon(square, 4, 5.0f, 5.0f));
}

void test_point_outside_square(void) {
    TEST_ASSERT_FALSE(point_in_polygon(square, 4, -1.0f, 5.0f));
}

void test_point_outside_square_far(void) {
    TEST_ASSERT_FALSE(point_in_polygon(square, 4, 15.0f, 5.0f));
}

void test_point_just_inside_square(void) {
    TEST_ASSERT_TRUE(point_in_polygon(square, 4, 0.1f, 0.1f));
}

void test_point_at_origin_corner(void) {
    // Vertex behavior is implementation-defined; just ensure no crash
    point_in_polygon(square, 4, 0.0f, 0.0f);
}

// L-shaped polygon
static LocalPoint l_shape[] = {
    {0, 0}, {10, 0}, {10, 5}, {5, 5}, {5, 10}, {0, 10}
};

void test_point_inside_l_shape_lower(void) {
    TEST_ASSERT_TRUE(point_in_polygon(l_shape, 6, 7.0f, 2.0f));
}

void test_point_inside_l_shape_upper(void) {
    TEST_ASSERT_TRUE(point_in_polygon(l_shape, 6, 2.0f, 7.0f));
}

void test_point_outside_l_shape_notch(void) {
    TEST_ASSERT_FALSE(point_in_polygon(l_shape, 6, 7.0f, 7.0f));
}

// Triangle
static LocalPoint triangle[] = {
    {0, 0}, {10, 0}, {5, 10}
};

void test_point_inside_triangle(void) {
    TEST_ASSERT_TRUE(point_in_polygon(triangle, 3, 5.0f, 3.0f));
}

void test_point_outside_triangle(void) {
    TEST_ASSERT_FALSE(point_in_polygon(triangle, 3, 1.0f, 8.0f));
}

// ── Point to Segment Distance Tests ─────────────────────────

void test_distance_to_horizontal_segment(void) {
    // Segment from (0,0) to (10,0), point at (5,3)
    float d = point_to_segment_distance(0, 0, 10, 0, 5, 3);
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 3.0f, d);
}

void test_distance_to_segment_endpoint(void) {
    // Segment from (0,0) to (10,0), point at (-3,4) — closest to (0,0)
    float d = point_to_segment_distance(0, 0, 10, 0, -3, 4);
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 5.0f, d);
}

void test_distance_to_segment_perpendicular(void) {
    // Segment from (0,0) to (0,10), point at (5,5)
    float d = point_to_segment_distance(0, 0, 0, 10, 5, 5);
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 5.0f, d);
}

void test_distance_to_segment_on_segment(void) {
    // Point on the segment
    float d = point_to_segment_distance(0, 0, 10, 0, 5, 0);
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 0.0f, d);
}

void test_distance_to_degenerate_segment(void) {
    // Zero-length segment (a point)
    float d = point_to_segment_distance(5, 5, 5, 5, 8, 9);
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 5.0f, d);
}

// ── Point to Polygon Distance Tests ─────────────────────────

void test_polygon_distance_from_inside(void) {
    // Point at center of 10x10 square — closest edge is 5m away
    float d = point_to_polygon_distance(square, 4, 5.0f, 5.0f);
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 5.0f, d);
}

void test_polygon_distance_from_near_edge(void) {
    // Point 1m inside the left edge
    float d = point_to_polygon_distance(square, 4, 1.0f, 5.0f);
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 1.0f, d);
}

void test_polygon_distance_from_outside(void) {
    // Point 3m outside the right edge
    float d = point_to_polygon_distance(square, 4, 13.0f, 5.0f);
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 3.0f, d);
}

// ── Circle Distance Tests ───────────────────────────────────

void test_circle_distance_from_center(void) {
    // Circle at (0,0) radius 10, point at center
    float d = point_to_circle_distance(0, 0, 10, 0, 0);
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 10.0f, d);
}

void test_circle_distance_from_inside(void) {
    // Circle at (0,0) radius 10, point at (3,0)
    float d = point_to_circle_distance(0, 0, 10, 3, 0);
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 7.0f, d);
}

void test_circle_distance_from_outside(void) {
    // Circle at (0,0) radius 10, point at (13,0)
    float d = point_to_circle_distance(0, 0, 10, 13, 0);
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 3.0f, d);
}

void test_circle_distance_on_boundary(void) {
    float d = point_to_circle_distance(0, 0, 10, 10, 0);
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 0.0f, d);
}

// ── Coordinate Conversion Tests ─────────────────────────────
// These test the equirectangular projection math

extern void geofence_set_reference(double ref_lat, double ref_lng);
extern LocalPoint geofence_to_local(double lat, double lng);

void test_to_local_same_point_is_origin(void) {
    geofence_set_reference(40.0, -105.0);
    LocalPoint p = geofence_to_local(40.0, -105.0);
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 0.0f, p.x);
    TEST_ASSERT_FLOAT_WITHIN(0.01f, 0.0f, p.y);
}

void test_to_local_north_is_positive_y(void) {
    geofence_set_reference(40.0, -105.0);
    LocalPoint p = geofence_to_local(40.001, -105.0);  // ~111m north
    TEST_ASSERT_TRUE(p.y > 100.0f && p.y < 120.0f);
    TEST_ASSERT_FLOAT_WITHIN(0.1f, 0.0f, p.x);
}

void test_to_local_east_is_positive_x(void) {
    geofence_set_reference(40.0, -105.0);
    LocalPoint p = geofence_to_local(40.0, -104.999);
    TEST_ASSERT_TRUE(p.x > 50.0f && p.x < 100.0f);
    TEST_ASSERT_FLOAT_WITHIN(0.1f, 0.0f, p.y);
}

int main(int argc, char** argv) {
    UNITY_BEGIN();

    // Point in polygon
    RUN_TEST(test_point_inside_square);
    RUN_TEST(test_point_outside_square);
    RUN_TEST(test_point_outside_square_far);
    RUN_TEST(test_point_just_inside_square);
    RUN_TEST(test_point_at_origin_corner);
    RUN_TEST(test_point_inside_l_shape_lower);
    RUN_TEST(test_point_inside_l_shape_upper);
    RUN_TEST(test_point_outside_l_shape_notch);
    RUN_TEST(test_point_inside_triangle);
    RUN_TEST(test_point_outside_triangle);

    // Point to segment distance
    RUN_TEST(test_distance_to_horizontal_segment);
    RUN_TEST(test_distance_to_segment_endpoint);
    RUN_TEST(test_distance_to_segment_perpendicular);
    RUN_TEST(test_distance_to_segment_on_segment);
    RUN_TEST(test_distance_to_degenerate_segment);

    // Point to polygon distance
    RUN_TEST(test_polygon_distance_from_inside);
    RUN_TEST(test_polygon_distance_from_near_edge);
    RUN_TEST(test_polygon_distance_from_outside);

    // Circle distance
    RUN_TEST(test_circle_distance_from_center);
    RUN_TEST(test_circle_distance_from_inside);
    RUN_TEST(test_circle_distance_from_outside);
    RUN_TEST(test_circle_distance_on_boundary);

    // Coordinate conversion
    RUN_TEST(test_to_local_same_point_is_origin);
    RUN_TEST(test_to_local_north_is_positive_y);
    RUN_TEST(test_to_local_east_is_positive_x);

    return UNITY_END();
}
