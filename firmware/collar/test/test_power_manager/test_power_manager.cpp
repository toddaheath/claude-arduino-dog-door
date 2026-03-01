#include <unity.h>

// Include the testable function directly
// voltage_to_percentage is defined in power_manager.cpp
extern float voltage_to_percentage(float voltage_v);

void test_full_charge_returns_100(void) {
    TEST_ASSERT_FLOAT_WITHIN(0.1f, 100.0f, voltage_to_percentage(4.2f));
}

void test_above_full_charge_clamps_100(void) {
    TEST_ASSERT_FLOAT_WITHIN(0.1f, 100.0f, voltage_to_percentage(4.3f));
}

void test_empty_returns_0(void) {
    TEST_ASSERT_FLOAT_WITHIN(0.1f, 0.0f, voltage_to_percentage(3.2f));
}

void test_below_empty_clamps_0(void) {
    TEST_ASSERT_FLOAT_WITHIN(0.1f, 0.0f, voltage_to_percentage(3.0f));
}

void test_nominal_4_1v_is_90_pct(void) {
    TEST_ASSERT_FLOAT_WITHIN(1.0f, 90.0f, voltage_to_percentage(4.1f));
}

void test_nominal_3_95v_is_70_pct(void) {
    TEST_ASSERT_FLOAT_WITHIN(1.0f, 70.0f, voltage_to_percentage(3.95f));
}

void test_nominal_3_8v_is_40_pct(void) {
    TEST_ASSERT_FLOAT_WITHIN(1.0f, 40.0f, voltage_to_percentage(3.8f));
}

void test_nominal_3_7v_is_20_pct(void) {
    TEST_ASSERT_FLOAT_WITHIN(1.0f, 20.0f, voltage_to_percentage(3.7f));
}

void test_nominal_3_5v_is_5_pct(void) {
    TEST_ASSERT_FLOAT_WITHIN(1.0f, 5.0f, voltage_to_percentage(3.5f));
}

void test_mid_range_is_monotonic(void) {
    // Verify voltage-to-percentage is monotonically increasing
    float prev = voltage_to_percentage(3.2f);
    for (float v = 3.3f; v <= 4.2f; v += 0.1f) {
        float current = voltage_to_percentage(v);
        TEST_ASSERT_TRUE_MESSAGE(current >= prev,
            "Percentage should increase with voltage");
        prev = current;
    }
}

void test_low_battery_threshold(void) {
    // 15% battery should be around 3.64V based on the curve
    float pct_at_3_65 = voltage_to_percentage(3.65f);
    TEST_ASSERT_TRUE(pct_at_3_65 > 10.0f && pct_at_3_65 < 20.0f);
}

void test_critical_battery_threshold(void) {
    // 5% battery should be around 3.50V
    float pct_at_3_50 = voltage_to_percentage(3.50f);
    TEST_ASSERT_FLOAT_WITHIN(1.0f, 5.0f, pct_at_3_50);
}

int main(int argc, char** argv) {
    UNITY_BEGIN();

    RUN_TEST(test_full_charge_returns_100);
    RUN_TEST(test_above_full_charge_clamps_100);
    RUN_TEST(test_empty_returns_0);
    RUN_TEST(test_below_empty_clamps_0);
    RUN_TEST(test_nominal_4_1v_is_90_pct);
    RUN_TEST(test_nominal_3_95v_is_70_pct);
    RUN_TEST(test_nominal_3_8v_is_40_pct);
    RUN_TEST(test_nominal_3_7v_is_20_pct);
    RUN_TEST(test_nominal_3_5v_is_5_pct);
    RUN_TEST(test_mid_range_is_monotonic);
    RUN_TEST(test_low_battery_threshold);
    RUN_TEST(test_critical_battery_threshold);

    return UNITY_END();
}
