<!--
  generated-by: tools/pdf2md/convert.py
  pymupdf: 1.27.1
  source: INS13954-Instruction-Z-Wave-500-Series-Appl-Programmers-Guide-v6_8x_0x.pdf
  section: "3 Z-Wave Software Architecture"
  pages: 15-34
-->
# 3 Z-Wave Software Architecture

Z-Wave software relies on polling of functions, command complete callback function calls, and delayed function calls.

The software is split into two groups of program modules: Z-Wave basis software and Application software. The Z-Wave basis software includes system startup code, low-level poll function, main poll loop, Z-Wave protocol layers, and memory and timer service functions. From the Z-Wave basis point of view the Application software include application hardware and software initialization functions, application state machine (called from the Z-Wave main poll loop), command complete callback functions, and a received command handler function. In addition to that, the application software can include hardware drivers.

|  |  | Completed aeam ctlle kp bcd fl auea cntlle kcbd ta ioc nk |  |
| --- | --- | --- | --- |
|  | Co | aeam ctlle kp bcd fl auea cntlle kcbd ta ioc |  |
| C co am llp bc fl auea cntlle kcbcd f tau ia ocnl function | am llp bc fl auea cntlle kcbcd f tau ia ocnl | aea ctlle kbcd faua cnl |  |
|  |  |  | n |

Application modules

Z-Wave modules

Figure 1. Software Architecture

## Contents

- [3.1 Z-Wave System Startup Code](03.01-z-wave-system-startup-code.md)
- [3.2 Z-Wave Main Loop](03.02-z-wave-main-loop.md)
- [3.3 Z-Wave Protocol Layers](03.03-z-wave-protocol-layers.md)
- [3.4 Z-Wave Routing Principles](03.04-z-wave-routing-principles.md)
- [3.5 Z-Wave Application Layer](03.05-z-wave-application-layer.md)
- [3.6 Z-Wave Software Timers](03.06-z-wave-software-timers.md)
- [3.7 Z-Wave Hardware Timers](03.07-z-wave-hardware-timers.md)
- [3.8 Z-Wave Hardware Interrupts](03.08-z-wave-hardware-interrupts.md)
- [3.9 Interrupt Service Routines](03.09-interrupt-service-routines.md)
- [3.10 Z-Wave Nodes](03.10-z-wave-nodes.md)
