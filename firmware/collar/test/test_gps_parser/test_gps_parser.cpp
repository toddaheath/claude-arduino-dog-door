#include <unity.h>

// Include the testable GPS quality function
extern bool gps_quality_ok(const struct GpsFixS* fix);

// Redefine GpsFix for native test (matches gps_tracker.h)
typedef struct GpsFixS {
    double   lat;
    double   lng;
    float    altitude;
    float    accuracy;
    float    speed;
    float    heading;
    unsigned char  satellites;
    float    hdop;
    unsigned char  fix_type;
    unsigned int   age_ms;
    unsigned int   timestamp;
} GpsFix;

static GpsFix make_good_fix() {
    GpsFix fix;
    fix.lat = 40.01;
    fix.lng = -105.27;
    fix.altitude = 1600.0f;
    fix.accuracy = 2.5f;
    fix.speed = 1.2f;
    fix.heading = 90.0f;
    fix.satellites = 8;
    fix.hdop = 1.2f;
    fix.fix_type = 3;
    fix.age_ms = 0;
    fix.timestamp = 1700000000;
    return fix;
}

void test_good_fix_is_quality_ok(void) {
    GpsFix fix = make_good_fix();
    TEST_ASSERT_TRUE(gps_quality_ok(&fix));
}

void test_no_fix_is_not_ok(void) {
    GpsFix fix = make_good_fix();
    fix.fix_type = 0;
    TEST_ASSERT_FALSE(gps_quality_ok(&fix));
}

void test_2d_fix_is_ok(void) {
    GpsFix fix = make_good_fix();
    fix.fix_type = 2;
    TEST_ASSERT_TRUE(gps_quality_ok(&fix));
}

void test_high_hdop_is_not_ok(void) {
    GpsFix fix = make_good_fix();
    fix.hdop = 6.0f;
    TEST_ASSERT_FALSE(gps_quality_ok(&fix));
}

void test_hdop_at_threshold_is_ok(void) {
    GpsFix fix = make_good_fix();
    fix.hdop = 5.0f;
    TEST_ASSERT_TRUE(gps_quality_ok(&fix));
}

void test_too_few_satellites_is_not_ok(void) {
    GpsFix fix = make_good_fix();
    fix.satellites = 3;
    TEST_ASSERT_FALSE(gps_quality_ok(&fix));
}

void test_exactly_4_satellites_is_ok(void) {
    GpsFix fix = make_good_fix();
    fix.satellites = 4;
    TEST_ASSERT_TRUE(gps_quality_ok(&fix));
}

void test_stale_fix_is_not_ok(void) {
    GpsFix fix = make_good_fix();
    fix.age_ms = 31000;
    TEST_ASSERT_FALSE(gps_quality_ok(&fix));
}

void test_fresh_fix_is_ok(void) {
    GpsFix fix = make_good_fix();
    fix.age_ms = 29000;
    TEST_ASSERT_TRUE(gps_quality_ok(&fix));
}

void test_poor_accuracy_is_not_ok(void) {
    GpsFix fix = make_good_fix();
    fix.accuracy = 16.0f;
    TEST_ASSERT_FALSE(gps_quality_ok(&fix));
}

void test_accuracy_at_threshold_is_ok(void) {
    GpsFix fix = make_good_fix();
    fix.accuracy = 15.0f;
    TEST_ASSERT_TRUE(gps_quality_ok(&fix));
}

void test_multiple_issues_first_fails(void) {
    // fix_type=0 should fail before checking hdop
    GpsFix fix = make_good_fix();
    fix.fix_type = 0;
    fix.hdop = 10.0f;
    TEST_ASSERT_FALSE(gps_quality_ok(&fix));
}

int main(int argc, char** argv) {
    UNITY_BEGIN();

    RUN_TEST(test_good_fix_is_quality_ok);
    RUN_TEST(test_no_fix_is_not_ok);
    RUN_TEST(test_2d_fix_is_ok);
    RUN_TEST(test_high_hdop_is_not_ok);
    RUN_TEST(test_hdop_at_threshold_is_ok);
    RUN_TEST(test_too_few_satellites_is_not_ok);
    RUN_TEST(test_exactly_4_satellites_is_ok);
    RUN_TEST(test_stale_fix_is_not_ok);
    RUN_TEST(test_fresh_fix_is_ok);
    RUN_TEST(test_poor_accuracy_is_not_ok);
    RUN_TEST(test_accuracy_at_threshold_is_ok);
    RUN_TEST(test_multiple_issues_first_fails);

    return UNITY_END();
}
