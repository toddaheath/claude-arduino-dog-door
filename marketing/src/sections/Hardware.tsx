import SectionWrapper from '../components/SectionWrapper.tsx'
import BomTable from '../components/BomTable.tsx'

const wiringDiagram = `12V PSU \u2500\u2500\u2192 L298N Motor Driver \u2500\u2500\u2192 Linear Actuator
    \u2502
    \u2514\u2500\u2500\u2192 LM2596 Buck \u2500\u2500\u2192 3.3V \u2500\u2500\u2192 ESP32-CAM
                                      \u2502
                                      \u251C\u2500\u2500 GPIO12 \u2190 RCWL-0516 (OUT)
                                      \u251C\u2500\u2500 GPIO13 \u2192 HC-SR04 (TRIG)
                                      \u251C\u2500\u2500 GPIO14 \u2190 HC-SR04 (ECHO)
                                      \u251C\u2500\u2500 GPIO15 \u2190 IR Break Beam
                                      \u251C\u2500\u2500 GPIO2  \u2192 L298N (IN1)
                                      \u251C\u2500\u2500 GPIO4  \u2192 L298N (IN2)
                                      \u251C\u2500\u2500 GPIO16 \u2190 Reed Switch
                                      \u251C\u2500\u2500 GPIO33 \u2192 Status LED (Green)
                                      \u2514\u2500\u2500 GPIO32 \u2192 Status LED (Red)`

export default function Hardware() {
  return (
    <SectionWrapper
      id="hardware"
      title="Hardware"
      subtitle="Off-the-shelf components, ~$84 total build cost"
    >
      <div className="hardware__content">
        <BomTable />
        <div className="hardware__wiring">
          <h3>Wiring Overview</h3>
          <pre>{wiringDiagram}</pre>
        </div>
      </div>
    </SectionWrapper>
  )
}
