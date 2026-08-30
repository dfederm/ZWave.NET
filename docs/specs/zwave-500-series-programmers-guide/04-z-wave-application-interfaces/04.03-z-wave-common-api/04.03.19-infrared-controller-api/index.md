<!--
  generated-by: tools/pdf2md/convert.py
  pymupdf: 1.27.1
  source: INS13954-Instruction-Z-Wave-500-Series-Appl-Programmers-Guide-v6_8x_0x.pdf
  section: "4.3.19 Infrared Controller API"
  pages: 308-327
-->
# 4.3.19 Infrared Controller API

The built-in Infrared (IR) Controller is targeted at IR remote control applications. The IR controller can operate either as an IR transmitter or as an IR receiver. When operating as a transmitter one or more of the three outputs (P3.4, P3.5, and P3.6) can be enabled as IR outputs that drive an IR LED, as depicted in figure below. Each output can drive 12mA. Hence, using three outputs give a drive strength of 36mA. If 36mA is insufficient you will have to implement an external driver.

| Optional driver R LED IR receiver module | Z-Wave Chip P3.4/IRTX0 P3.5/IRTX1 P3.6/IRTX2 P3.1/IRRX |
| --- | --- |

Figure 29. External IR Hardware

An external IR receiver module or an IR transistor must be connected to Pin P3.1 when operating in Receive mode. An IR receiver module has a built-in photo transistor and preamplifier with automatic gain control and gives a digital TTL/CMOS output signal. The IR receivers can be found in two versions, with and without demodulator. The versions without demodulator (like Vishay TSOP 98200) generates an output signal with carrier (as depicted in the upper part of figure below), whereas the versions with demodulator (like Vishay TSOP322xx) generates an output signal without the carrier (as depicted in the lower part of figure below). Therefore, the one without demodulator is best for code learning applications, where you want to be able to detect the carrier frequency. The one with modulator has improved immunity against ambient light such as fluorescent lamps.

Using an photo transistor, where the transistor is connected directly to the 500 Series Z-Wave Chip requires that the transmitting IR LED is placed within a short range (2”-4”) of the IR transistor, since the IR transistor signal is analog and isn’t amplified. This circuit is also sensitive also to ambient light.

Figure 30. IR Signal with and without Carrier

In both cases, the IR Receiver detects widths of the marks (high/carrier on) and spaces (low) of a coded IR message, as seen in figure below. The mark/space width data is stored in SRAM using DMA. While running, the IR Controller requires very little MCU processing. The IR receiver is able to detect the waveform of the carrier 1.

The IR Transmitter generates a carrier and the marks and spaces for an IR message. The widths of the marks and the spaces are read from SRAM using DMA. Figure 31. IR Coded Message with Carrier

Both the IR Receiver and the IR Transmitter can be configured to detect/generate a wide range of IR coding formats.

## Contents

- [4.3.19.1 Carrier Detector/Generator](04.03.19.01-carrier-detectorgenerator.md)
- [4.3.19.2 Organization of Mark/Space Data in Memory](04.03.19.02-organization-of-markspace-data-in-memory.md)
- [4.3.19.3 IR Transmitter](04.03.19.03-ir-transmitter.md)
- [4.3.19.4 IR Receiver](04.03.19.04-ir-receiver.md)
- [4.3.19.5 ZW_IR_tx_init](04.03.19.05-zw_ir_tx_init.md)
- [4.3.19.6 ZW_IR_tx_data](04.03.19.06-zw_ir_tx_data.md)
- [4.3.19.7 ZW_IR_tx_status_get](04.03.19.07-zw_ir_tx_status_get.md)
- [4.3.19.8 ZW_IR_learn_init](04.03.19.08-zw_ir_learn_init.md)
- [4.3.19.9 ZW_IR_learn_data](04.03.19.09-zw_ir_learn_data.md)
- [4.3.19.10 ZW_IR_learn_status_get](04.03.19.10-zw_ir_learn_status_get.md)
- [4.3.19.11 ZW_IR_status_clear](04.03.19.11-zw_ir_status_clear.md)
- [4.3.19.12 ZW_IR_disable](04.03.19.12-zw_ir_disable.md)
