const items = [
  { item: 'ESP32-CAM (AI-Thinker)', purpose: 'Main controller with camera', cost: '$10' },
  { item: 'RCWL-0516 Microwave Radar', purpose: 'Long-range motion detection (7m)', cost: '$3' },
  { item: 'HC-SR04 Ultrasonic Sensor', purpose: 'Proximity measurement', cost: '$3' },
  { item: '12V Linear Actuator (50mm)', purpose: 'Opens/closes door flap', cost: '$25' },
  { item: 'L298N Motor Driver', purpose: 'Controls actuator direction', cost: '$7' },
  { item: 'IR Break Beam Sensors', purpose: 'Safety interlock', cost: '$5' },
  { item: '12V 5A Power Supply', purpose: 'Powers system', cost: '$12' },
  { item: 'LM2596 Buck Converter', purpose: '12V to 3.3V for ESP32', cost: '$3' },
  { item: 'Magnetic Reed Switch', purpose: 'Door position detection', cost: '$2' },
  { item: 'LEDs (5-pack)', purpose: 'Status indicators', cost: '$3' },
  { item: 'Breadboard + Jumper Wires', purpose: 'Prototyping connections', cost: '$6' },
  { item: 'Waterproof Enclosure', purpose: 'Weather protection', cost: '$5' },
]

export default function BomTable() {
  return (
    <div style={{ overflowX: 'auto' }}>
      <table className="bom-table">
        <thead>
          <tr>
            <th>#</th>
            <th>Item</th>
            <th>Purpose</th>
            <th>Cost</th>
          </tr>
        </thead>
        <tbody>
          {items.map((row, i) => (
            <tr key={i}>
              <td>{i + 1}</td>
              <td>{row.item}</td>
              <td>{row.purpose}</td>
              <td>{row.cost}</td>
            </tr>
          ))}
          <tr className="bom-table__total">
            <td></td>
            <td>Total</td>
            <td></td>
            <td>~$84</td>
          </tr>
        </tbody>
      </table>
    </div>
  )
}
