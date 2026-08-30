<!--
  generated-by: tools/pdf2md/convert.py
  pymupdf: 1.27.1
  source: INS13954-Instruction-Z-Wave-500-Series-Appl-Programmers-Guide-v6_8x_0x.pdf
  section: "4.3 Z-Wave Common API"
  pages: 38-348
-->
# 4.3 Z-Wave Common API

This section describes interface functions that are implemented within all Z-Wave nodes. The first subsection defines functions that must be implemented within the application modules, while the second subsection defines the functions that are implemented within the Z-Wave basis library.

Functions that does not complete the requested action before returning to the application (e.g. ZW_SEND_DATA) have a callback function pointer as one of the entry parameters. Unless explicitly specified this function pointer can be set to NULL (no action to take on completion).

[A serial API implementation provide an interface to the major part of interface functions via a serial port. The SDK contains a serial API application [18], which enables a host processor to control the interface](../../08-references.md#8-references) functions via a serial port.

## Contents

- [4.3.1 Required Application Functions](04.03.01-required-application-functions/index.md)
- [4.3.2 Z-Wave Basis API](04.03.02-z-wave-basis-api/index.md)
- [4.3.3 Z-Wave Transport API](04.03.03-z-wave-transport-api/index.md)
- [4.3.4 ZWave Firmware Update API](04.03.04-zwave-firmware-update-api.md)
- [4.3.5 Z-Wave Node Mask API](04.03.05-z-wave-node-mask-api.md)
- [4.3.6 IO API](04.03.06-io-api.md)
- [4.3.7 GPIO Macros](04.03.07-gpio-macros.md)
- [4.3.8 Z-Wave NVM Memory API](04.03.08-z-wave-nvm-memory-api.md)
- [4.3.9 Z-Wave Timer API](04.03.09-z-wave-timer-api.md)
- [4.3.10 Power Control API](04.03.10-power-control-api.md)
- [4.3.11 SPI Interface API](04.03.11-spi-interface-api/index.md)
- [4.3.12 ADC Interface API](04.03.12-adc-interface-api/index.md)
- [4.3.13 UART Interface API](04.03.13-uart-interface-api/index.md)
- [4.3.14 Application HW Timers/PWM Interface API](04.03.14-application-hw-timerspwm-interface-api/index.md)
- [4.3.15 Security API](04.03.15-security-api.md)
- [4.3.16 AES API](04.03.16-aes-api.md)
- [4.3.17 TRIAC Controller API](04.03.17-triac-controller-api.md)
- [4.3.18 LED Controller API](04.03.18-led-controller-api.md)
- [4.3.19 Infrared Controller API](04.03.19-infrared-controller-api/index.md)
- [4.3.20 Keypad Scanner Controller API](04.03.20-keypad-scanner-controller-api.md)
- [4.3.21 USB/UART Common API](04.03.21-usbuart-common-api.md)
- [4.3.22 Flash API](04.03.22-flash-api.md)
- [4.3.23 CRC API](04.03.23-crc-api.md)
