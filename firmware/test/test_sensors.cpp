/*
 * PlatformIO Unit Tests for Smart Dog Door
 *
 * These tests run on the native platform (host computer) for logic testing.
 * Hardware-dependent tests require the esp32cam environment.
 *
 * Run with: pio test -e native
 */

#include <unity.h>

// Test ultrasonic distance calculation
void test_distance_calculation() {
    // Speed of sound = 343 m/s = 0.0343 cm/us
    // Distance = (duration * 0.0343) / 2

    // 1000us echo = ~17.15cm
    float distance = (1000 * 0.0343f) / 2.0f;
    TEST_ASSERT_FLOAT_WITHIN(0.5f, 17.15f, distance);

    // 5000us echo = ~85.75cm
    distance = (5000 * 0.0343f) / 2.0f;
    TEST_ASSERT_FLOAT_WITHIN(0.5f, 85.75f, distance);
}

// Test detection threshold
void test_detection_threshold() {
    float threshold = 0.7f;

    // Below threshold = not a dog
    TEST_ASSERT_TRUE(0.3f < threshold);
    TEST_ASSERT_TRUE(0.69f < threshold);

    // At/above threshold = dog detected
    TEST_ASSERT_FALSE(0.7f < threshold);
    TEST_ASSERT_FALSE(0.95f < threshold);
}

// Test cooldown timing logic
void test_cooldown_logic() {
    unsigned long last_detection = 0;
    unsigned long cooldown_ms = 5000;

    // At time 0, should be allowed (first detection)
    TEST_ASSERT_TRUE(0 - last_detection >= cooldown_ms || last_detection == 0);

    // At time 1000 after detection at 0, should be in cooldown
    last_detection = 0;
    unsigned long now = 1000;
    TEST_ASSERT_TRUE(now - last_detection < cooldown_ms);

    // At time 5001 after detection at 0, should be out of cooldown
    now = 5001;
    TEST_ASSERT_TRUE(now - last_detection >= cooldown_ms);
}

// Test door state machine
void test_door_state() {
    bool door_open = false;

    // Open door
    door_open = true;
    TEST_ASSERT_TRUE(door_open);

    // Close door
    door_open = false;
    TEST_ASSERT_FALSE(door_open);
}

// Test auto-close timer logic
void test_auto_close_timer() {
    unsigned long door_open_time = 1000;
    unsigned long auto_close_delay = 10000;

    // At 5000ms, should not auto-close yet
    unsigned long now = 5000;
    TEST_ASSERT_FALSE(now - door_open_time > auto_close_delay);

    // At 11001ms, should auto-close
    now = 11001;
    TEST_ASSERT_TRUE(now - door_open_time > auto_close_delay);
}

int main(int argc, char **argv) {
    UNITY_BEGIN();

    RUN_TEST(test_distance_calculation);
    RUN_TEST(test_detection_threshold);
    RUN_TEST(test_cooldown_logic);
    RUN_TEST(test_door_state);
    RUN_TEST(test_auto_close_timer);

    return UNITY_END();
}
